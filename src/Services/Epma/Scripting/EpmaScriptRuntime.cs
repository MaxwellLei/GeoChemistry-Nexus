using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GeoChemistryNexus.Services.Epma.Input;
using GeoChemistryNexus.Helpers;
using Jint;
using Jint.Native;

namespace GeoChemistryNexus.Services.Epma;

public sealed class EpmaScriptRuntime
{
    private static readonly ConcurrentDictionary<string, Action<Engine>> Prepared = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, bool> Renderers = new(StringComparer.Ordinal);
    // Capture built-ins before package code executes. Reject values JSON.stringify would silently erase/change.
    private const string Bridge = """
        (function() {
          const parse=JSON.parse, stringify=JSON.stringify, finite=Number.isFinite,
                keys=Object.getOwnPropertyNames, own=Object.hasOwn, array=Array.isArray, create=Object.create,
                getPrototype=Object.getPrototypeOf, objectPrototype=Object.prototype, arrayPrototype=Array.prototype,
                prototype=Object.setPrototypeOf, descriptor=Object.getOwnPropertyDescriptor;
          return function(fn,json) {
            if(typeof fn!=='function') throw new Error('Missing callable script entry');
            const value=fn(parse(json)); let nodes=0;
            function copy(v,depth,ancestors,count) {
              if(++nodes>100000 || depth>40) throw new Error('Result exceeds structural limits');
              if(v===null || typeof v==='string' || typeof v==='boolean') return v;
              if(typeof v==='number') {if(!finite(v)) throw new Error('Nonfinite script number'); return v;}
              if(typeof v!=='object') throw new Error('Unsupported script result type');
              for(let i=0;i<count;i++) if(ancestors[i]===v) throw new Error('Circular script result');
              if(typeof v.toJSON==='function') throw new Error('Result toJSON is unsupported');
              if(typeof v.then==='function') throw new Error('Async script results are unsupported');
              ancestors[count]=v;
              const isArray=array(v), base=getPrototype(v);
              if(base!==null && base!==(isArray?arrayPrototype:objectPrototype))throw new Error('Only plain JSON objects/arrays are supported');
              const result=isArray?prototype([],null):create(null), names=keys(v);
              if(isArray && names.length!==v.length+1)throw new Error('Array properties/sparse results are unsupported');
              const size=isArray?v.length:names.length;
              if(size>100000)throw new Error('Result exceeds structural limits');
              for(let i=0;i<size;i++) {
                const key=isArray?i:names[i], desc=descriptor(v,key);
                if(!desc || !own(desc,'value'))throw new Error('Sparse/accessor script result is unsupported');
                if(!desc.enumerable)throw new Error('Non-enumerable script result is unsupported');
                result[key]=copy(desc.value,depth+1,ancestors,count+1);
              }
              return result;
            }
            return stringify(copy(value,0,create(null),0));
          };
        })()
        """;
    private Engine Create(EpmaScriptPackage package, CancellationToken token, out JsValue bridge)
    {
        var engine = Create(package.Scripts, package.Sha256, token, out bridge);
        Renderers[package.Sha256] = !engine.GetValue("renderDisplay").IsUndefined();
        return engine;
    }
    private Engine Create(IReadOnlyDictionary<string, string> scripts, string? cacheKey, CancellationToken token, out JsValue bridge)
    {
        token.ThrowIfCancellationRequested();
        var engine = JintHelper.CreateEngine(TimeSpan.FromSeconds(5), options =>
        {
            options.LimitMemory(64L * 1024 * 1024).CancellationToken(token).DisableStringCompilation();
            options.Constraints.Constraints.Add(new EpmaOperationBudget());
        });
        try
        {
            bridge = engine.Evaluate(Bridge);
            foreach (var (path, source) in scripts)
            {
                token.ThrowIfCancellationRequested();
                if (cacheKey is null)
                {
                    engine.Execute(Engine.PrepareScript(source, path));
                    continue;
                }
                var load = Prepared.GetOrAdd(cacheKey + "/" + path, _ =>
                {
                    var script = Engine.PrepareScript(source, path);
                    return e => e.Execute(script);
                });
                load(engine);
            }
            if (Prepared.Count > 256) { Prepared.Clear(); Renderers.Clear(); }
            return engine;
        }
        catch { engine.Dispose(); throw; }
    }
    private JsonElement Invoke(Engine engine, JsValue bridge, string function, JsonElement context, bool optional = false)
    {
        var callable = engine.GetValue(function);
        if (optional && callable.IsUndefined()) return JsonSerializer.SerializeToElement(Array.Empty<object>());
        string json = engine.Invoke(bridge, callable, context.GetRawText()).AsString();
        if (json.Length > EpmaScriptPackage.MaximumBytes) throw new InvalidOperationException("Script result exceeds 10 MiB.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 48 });
        return document.RootElement.Clone();
    }
    internal EpmaScriptSetting[] ReadSettings(EpmaScriptPackage package, out EpmaIronOptions? ironOptions)
    {
        using var operation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var engine = Create(package, operation.Token, out var bridge);
        ironOptions = engine.GetValue("getIronOptions").IsUndefined() ? null
            : Invoke(engine, bridge, "getIronOptions", Context(new { })).Deserialize<EpmaIronOptions>(EpmaScriptPackage.JsonOptions);
        return Invoke(engine, bridge, "getSettings", Context(new { }), true).Deserialize<EpmaScriptSetting[]>(EpmaScriptPackage.JsonOptions)
            ?? throw new InvalidOperationException("getSettings must return an array.");
    }
    public IReadOnlyDictionary<string, JsonElement> Defaults(EpmaScriptPackage package) =>
        EpmaInputPolicy.Parameters(package).ToDictionary(p => p.Id, p => p.Value.Clone(), StringComparer.Ordinal);
    private static Dictionary<string, JsonElement> WithReportingDefault(EpmaScriptPackage package, IReadOnlyDictionary<string, JsonElement> parameters)
    {
        var settings = parameters.ToDictionary(p => p.Key, p => p.Value.Clone(), StringComparer.Ordinal);
        if (!settings.ContainsKey("ironForm") && !package.Settings.Any(p => p.Id == "ironForm")
            && EpmaInputPolicy.Parameters(package).FirstOrDefault(p => p.Id == "ironForm") is { } reporting)
            settings["ironForm"] = reporting.Value.Clone();
        foreach (var fixedSetting in package.Settings.Where(p => !p.Editable))
            settings.TryAdd(fixedSetting.Id, fixedSetting.Value.Clone());
        return settings;
    }
    internal static Dictionary<string, JsonElement> CalculationSettings(EpmaScriptPackage package, IReadOnlyDictionary<string, JsonElement> parameters)
    {
        var active = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var parameter in EpmaInputPolicy.Parameters(package))
            if (EpmaInputPolicy.ParameterActive(package, parameter, parameters))
                active[parameter.Id] = !parameter.Editable ? parameter.Value : parameters.GetValueOrDefault(parameter.Id,
                    parameter.Id == "ironForm" && !package.Settings.Any(p => p.Id == "ironForm")
                        ? parameter.Value : JsonSerializer.SerializeToElement<object?>(null));
        return active;
    }
    private static bool ValidNumber(JsonElement value, double? min, double? max) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number) && double.IsFinite(number)
        && !(number < min || number > max);
    private static JsonElement Context(object value) => JsonSerializer.SerializeToElement(value, EpmaScriptPackage.JsonOptions);
    public IReadOnlyList<EpmaScriptFlag> ValidateParameterValues(EpmaScriptPackage package, IReadOnlyDictionary<string, JsonElement> parameters)
    {
        var errors = new List<EpmaScriptFlag>();
        var declared = EpmaInputPolicy.Parameters(package);
        if (parameters.Keys.Any(k => !declared.Any(p => p.Id == k))) errors.Add(EpmaScriptFlag.Error("EPMA_SCRIPT_PARAMETER_UNKNOWN", "Undeclared parameter."));
        var active = CalculationSettings(package, parameters);
        foreach (var p in declared.Where(p => active.ContainsKey(p.Id)))
        {
            var v = active[p.Id];
            bool valid = p.Type switch
            {
                "number" => ValidNumber(v, p.Min, p.Max),
                "choice" => v.ValueKind == JsonValueKind.String && p.Choices.Any(c => c.Value == v.GetString()),
                _ => false,
            };
            if (!valid) errors.Add(EpmaScriptFlag.Error("EPMA_SCRIPT_PARAMETER_INVALID", p.DisplayLabel));
        }
        foreach (var p in declared.Where(p => !p.Editable))
            if (parameters.TryGetValue(p.Id, out var supplied) && !JsonElement.DeepEquals(supplied, p.Value))
                errors.Add(EpmaScriptFlag.Error("EPMA_SCRIPT_SETTING_READONLY", p.DisplayLabel));
        return errors;
    }
    public IReadOnlyList<EpmaScriptFlag> ValidateSettings(EpmaScriptPackage package, IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken token = default)
    {
        var errors = ValidateParameterValues(package, parameters);
        if (errors.Count > 0) return errors;
        using var settingsDeadline = CancellationTokenSource.CreateLinkedTokenSource(token); settingsDeadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var engine = Create(package, settingsDeadline.Token, out var bridge);
        var context = Context(new { parameters = CalculationSettings(package, parameters) });
        return Flags(Invoke(engine, bridge, "validateSettings", context, true));
    }
    public static EpmaScriptCell Parse(RawCell raw, NumericFormatChoice format)
    {
        string text = ValueParser.RawText(raw);
        if (raw is RawCell.Empty || string.IsNullOrWhiteSpace(text)) return new("Missing", text);
        if (raw is RawCell.NativeNumber n) return double.IsFinite(n.Value) ? new("Number", text, n.Value) : new("Text", text);
        if (ValueParser.TryParseDetectionLimit(text, format, out double? limit))
            return new("DetectionLimit", text, null, limit);
        return ValueParser.TryParseStrict(ValueParser.NumericCandidate(text), format, out double number) ? new("Number", text, number) : new("Text", text);
    }
    public Task<EpmaScriptRun> CalculateAsync(EpmaScriptPackage package, InputTableSnapshot input,
        IReadOnlyDictionary<string, JsonElement> parameters, NumericFormatChoice format = NumericFormatChoice.DecimalPoint,
        CancellationToken token = default) => Task.Run(() => Calculate(package, input, parameters, format, token), token);

    /// <summary>Executes an editor snapshot without building a ZIP or validating package metadata/declarations.</summary>
    public Task<IReadOnlyList<EpmaScriptRow>> TestScriptAsync(string path, string source, EpmaScriptManifest columns,
        EpmaExampleDataset examples, CancellationToken token = default) => Task.Run<IReadOnlyList<EpmaScriptRow>>(() =>
    {
        try { return TestScript(path, source, columns, examples, token); }
        catch { token.ThrowIfCancellationRequested(); throw; }
    }, token);

    private IReadOnlyList<EpmaScriptRow> TestScript(string path, string source, EpmaScriptManifest columns,
        EpmaExampleDataset examples, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        var scripts = new Dictionary<string, string> { [path] = source };
        using var settingsDeadline = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        settingsDeadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var settingsEngine = Create(scripts, null, settingsDeadline.Token, out var settingsBridge);
        var settings = Invoke(settingsEngine, settingsBridge, "getSettings", Context(new { }), true)
            .Deserialize<EpmaScriptSetting[]>(EpmaScriptPackage.JsonOptions)
            ?? throw new InvalidOperationException("getSettings must return an array.");
        // Apply the same reporting defaults and active-setting rules as a normal calculation.
        var declared = EpmaInputPolicy.Parameters(columns, settings,
            settingsEngine.GetValue("getIronOptions").IsUndefined());
        var parameters = declared.ToDictionary(p => p.Id, p => p.Value, StringComparer.Ordinal);
        foreach (var parameter in examples.Parameters) parameters[parameter.Key] = parameter.Value;
        var active = declared.Where(p => EpmaInputPolicy.ParameterActive(p, parameters))
            .ToDictionary(p => p.Id, p => p.Editable ? parameters[p.Id] : p.Value, StringComparer.Ordinal);
        var flags = Flags(Invoke(settingsEngine, settingsBridge, "validateSettings", Context(new { parameters = active }), true));
        if (flags.Any(flag => flag.Severity == "Error"))
            throw new InvalidOperationException(string.Join("\n", flags.Select(flag => flag.Text)));
        var declarations = EpmaInputPolicy.ActiveInputs(columns, active);
        var rows = new List<EpmaScriptRow>();
        foreach (var example in examples.Inputs)
        {
            deadline.Token.ThrowIfCancellationRequested();
            var inputs = declarations.ToDictionary(input => input.Id, input =>
            {
                var value = example.GetValueOrDefault(input.Id);
                RawCell raw = value.ValueKind switch
                {
                    JsonValueKind.Number => new RawCell.NativeNumber(value.GetDouble()),
                    JsonValueKind.String => new RawCell.Text(value.GetString()!),
                    _ => new RawCell.Empty()
                };
                return Parse(raw, NumericFormatChoice.DecimalPoint);
            }, StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal);
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
            operation.CancelAfter(TimeSpan.FromSeconds(5));
            using var engine = Create(scripts, null, operation.Token, out var bridge);
            var context = Context(new { inputs, parameters = active, outputs = columns.Outputs.Select(f => f.Id).ToArray() });
            var result = CalculateResponse(engine, bridge, context, columns.Outputs);
            rows.Add(new(rows.Count + 1, inputs, result));
        }
        deadline.Token.ThrowIfCancellationRequested();
        return rows.AsReadOnly();
    }

    private EpmaScriptResponse CalculateResponse(Engine engine, JsValue bridge, JsonElement context, IReadOnlyList<EpmaScriptOutput> fields)
    {
        var result = Invoke(engine, bridge, "calculate", context).Deserialize<EpmaScriptResponse>(EpmaScriptPackage.JsonOptions)
            ?? throw new InvalidOperationException("calculate returned null.");
        ValidateResponse(result, fields);
        return result;
    }
    public EpmaScriptRun Calculate(EpmaScriptPackage package, InputTableSnapshot input,
        IReadOnlyDictionary<string, JsonElement> parameters, NumericFormatChoice format = NumericFormatChoice.DecimalPoint, CancellationToken token = default)
    {
        parameters = WithReportingDefault(package, parameters);
        var calculationSettings = CalculationSettings(package, parameters);
        var declarations = EpmaInputPolicy.ActiveInputs(package.Manifest, calculationSettings);
        if (input.Headers.Distinct(StringComparer.Ordinal).Count() != input.Headers.Count
            || input.Headers.Any(id => !package.Manifest.Inputs.Any(i => i.Id == id)))
            throw new InvalidOperationException("Input columns must be unique declared input IDs.");
        var columns = input.Headers.Select((id, index) => (id, index)).ToDictionary(p => p.id, p => p.index, StringComparer.Ordinal);
        var mapping = declarations.ToDictionary(i => i.Id, i => columns.TryGetValue(i.Id, out int index) ? index
            : throw new InvalidOperationException("Missing active input column: " + i.Id), StringComparer.Ordinal);
        var errors = ValidateSettings(package, parameters, token);
        if (errors.Any(f => f.Severity == "Error")) throw new InvalidOperationException(string.Join("\n", errors.Select(f => f.Text)));
        var rows = new List<EpmaScriptRow>(); var fields = package.Manifest.Outputs;
        var outputs = fields.Select(f => f.Id).ToArray();
        for (int r = 0; r < input.Rows.Count; r++)
        {
            token.ThrowIfCancellationRequested();
            var inputs = declarations.ToDictionary(i => i.Id, i => Parse(input.Rows[r][mapping[i.Id]], format), StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal);
            if (inputs.Values.All(c => c.Kind == "Missing")) { rows.Add(new(r + 1, inputs, new() { Status = "Empty" })); continue; }
            var context = Context(new
            {
                inputs,
                parameters = calculationSettings,
                outputs
            });
            try
            {
                using var operation = CancellationTokenSource.CreateLinkedTokenSource(token); operation.CancelAfter(TimeSpan.FromSeconds(5));
                using var engine = Create(package, operation.Token, out var bridge);
                var result = CalculateResponse(engine, bridge, context, fields);
                rows.Add(new(r + 1, inputs, result));
            }
            catch (Exception ex)
            {
                token.ThrowIfCancellationRequested();
                rows.Add(new(r + 1, inputs, new()
                {
                    Status = "Skipped",
                    Values = fields.ToDictionary(f => f.Id, _ => JsonSerializer.SerializeToElement<object?>(null)),
                    UndefinedReasons = fields.ToDictionary(f => f.Id, _ => "RowSkipped"),
                    Flags = [EpmaScriptFlag.Error("EPMA_SCRIPT_EXECUTION_ERROR", ex.Message)]
                }));
            }
        }
        token.ThrowIfCancellationRequested();
        return new(package, input, parameters, format, DateTimeOffset.UtcNow, rows.AsReadOnly());
    }
    private static void ValidateResponse(EpmaScriptResponse response, IReadOnlyList<EpmaScriptOutput> fields)
    {
        if (response.Status is not ("Computed" or "Skipped")) throw new InvalidOperationException("Invalid result status.");
        if (response.Status == "Computed" && fields.Any(f => !response.Values.ContainsKey(f.Id)))
            throw new InvalidOperationException("Computed result must return every declared output.");
        foreach (var (id, value) in response.Values)
        {
            var field = fields.SingleOrDefault(f => f.Id == id) ?? throw new InvalidOperationException("Undeclared output: " + id);
            if (value.ValueKind == JsonValueKind.Null) { if (!response.UndefinedReasons.TryGetValue(id, out var reason) || string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Undefined result needs a reason: " + id); continue; }
            bool number = value.ValueKind == JsonValueKind.Number && double.IsFinite(value.GetDouble());
            if (!number && value.ValueKind != JsonValueKind.String) throw new InvalidOperationException("Wrong result type: " + id);
        }
        if (response.FieldSources.Values.Any(string.IsNullOrWhiteSpace)) throw new InvalidOperationException("Invalid field source.");
        if (response.FieldSources.Keys.Concat(response.UndefinedReasons.Keys).Any(id => !fields.Any(f => f.Id == id))) throw new InvalidOperationException("Undeclared result metadata.");
        if (response.UndefinedReasons.Any(p => string.IsNullOrWhiteSpace(p.Value) || !response.Values.TryGetValue(p.Key, out var v) || v.ValueKind != JsonValueKind.Null))
            throw new InvalidOperationException("Undefined reasons must describe null outputs.");
        if (response.Status == "Skipped")
            foreach (var field in fields.Where(f => !response.Values.ContainsKey(f.Id)))
            {
                response.Values[field.Id] = JsonSerializer.SerializeToElement<object?>(null);
                response.UndefinedReasons[field.Id] = "RowSkipped";
            }
        ValidateFlags(response.Flags);
    }
    private static void ValidateFlags(IEnumerable<EpmaScriptFlag> flags)
    { if (flags.Count() > 512 || flags.Any(f => string.IsNullOrWhiteSpace(f.Code) || f.Severity is not ("Info" or "Warning" or "Error"))) throw new InvalidOperationException("Invalid QC flags."); }
    private static EpmaScriptFlag[] Flags(JsonElement value)
    {
        var flags = value.Deserialize<EpmaScriptFlag[]>(EpmaScriptPackage.JsonOptions) ?? throw new InvalidOperationException("QC entry must return an array.");
        ValidateFlags(flags); return flags;
    }
    public IReadOnlyDictionary<string, string> Render(EpmaScriptRun run, EpmaScriptRow row, int decimals, CancellationToken token = default)
    {
        if (Renderers.TryGetValue(run.Package.Sha256, out bool available) && !available) return new Dictionary<string, string>();
        using var displayDeadline = CancellationTokenSource.CreateLinkedTokenSource(token); displayDeadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var engine = Create(run.Package, displayDeadline.Token, out var bridge);
        if (engine.GetValue("renderDisplay").IsUndefined()) return new Dictionary<string, string>();
        var context = Context(new { values = row.Result.Values, trace = row.Result.Trace, displayDecimals = decimals });
        var result = Invoke(engine, bridge, "renderDisplay", context).Deserialize<Dictionary<string, string>>() ?? throw new InvalidOperationException("Invalid display result.");
        if (result.Keys.Any(k => !row.Result.Values.TryGetValue(k, out var value) || value.ValueKind != JsonValueKind.String))
            throw new InvalidOperationException("Display renderer changed a numeric or undeclared field.");
        return result;
    }
    public void ValidateEntries(EpmaScriptPackage package, CancellationToken token = default)
    {
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(token); operation.CancelAfter(TimeSpan.FromSeconds(5));
        using var engine = Create(package, operation.Token, out _);
        if (!engine.Evaluate("typeof calculate === 'function'").AsBoolean())
            throw new InvalidOperationException("Missing required callable script entry.");
        foreach (string entry in new[] { "getSettings", "getIronOptions", "validateSettings", "renderDisplay" })
            if (!engine.Evaluate($"typeof {entry} === 'undefined' || typeof {entry} === 'function'").AsBoolean())
                throw new InvalidOperationException("Invalid optional script entry: " + entry);
    }

    public async Task ValidateAsync(EpmaScriptPackage package, CancellationToken token = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(60));
        ValidateEntries(package, deadline.Token);
        var exampleParameters = Defaults(package).ToDictionary(p => p.Key, p => p.Value);
        foreach (var p in package.Examples.Parameters) exampleParameters[p.Key] = p.Value;
        var examples = await CalculateAsync(package, ExampleInput(package), exampleParameters, NumericFormatChoice.DecimalPoint, deadline.Token);
        foreach (var row in examples.Rows) CheckCase(row, "example row " + row.InputRow);
    }

    private static void CheckCase(EpmaScriptRow row, string id)
    {
        if (row.Result.Status != "Computed" || row.Flags.Any(f => f.Code == "EPMA_SCRIPT_EXECUTION_ERROR"))
            throw new InvalidOperationException("Case failed: " + id + " " + string.Join(";", row.Flags.Select(f => f.Text)));
    }
    public static InputTableSnapshot ExampleInput(EpmaScriptPackage package) => Input(package, package.Examples.Inputs);
    private static InputTableSnapshot Input(EpmaScriptPackage package, IEnumerable<Dictionary<string, JsonElement>> inputs)
    {
        var rows = inputs.Select(input => (IReadOnlyList<RawCell>)package.Manifest.Inputs.Select(i =>
            !input.TryGetValue(i.Id, out var v) || v.ValueKind == JsonValueKind.Null ? (RawCell)new RawCell.Empty()
                : v.ValueKind == JsonValueKind.Number ? new RawCell.NativeNumber(v.GetDouble()) : new RawCell.Text(v.GetString()!)).ToArray()).ToArray();
        return new(package.Manifest.Inputs.Select(i => i.Id).ToArray(), rows);
    }
}
