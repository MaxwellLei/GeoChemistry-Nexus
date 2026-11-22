using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GeoChemistryNexus.Services.Epma.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using Jint;
using Jint.Native;

// The Scripting folder intentionally shares the Epma service namespace with the rest of the EPMA services.
#pragma warning disable IDE0130
namespace GeoChemistryNexus.Services.Epma;
#pragma warning restore IDE0130

public sealed class EpmaScriptRuntime
{
    private static readonly ConcurrentDictionary<string, Action<Engine>> Prepared = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, bool> Renderers = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, bool> Detailers = new(StringComparer.Ordinal);
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
    private static Engine Create(EpmaScriptPackage package, CancellationToken token, out JsValue bridge, bool prepare = false)
    {
        var engine = Create(package.Scripts, package.Sha256, token, out bridge, prepare);
        Renderers[package.Sha256] = !engine.GetValue("renderDisplay").IsUndefined();
        Detailers[package.Sha256] = !engine.GetValue("calculateDetailed").IsUndefined();
        return engine;
    }
    private static Engine Create(IReadOnlyDictionary<string, string> scripts, string? cacheKey, CancellationToken token, out JsValue bridge, bool prepare = false)
    {
        token.ThrowIfCancellationRequested();
        int statements = prepare ? EpmaOperationBudget.PrepareStatements : EpmaOperationBudget.RowStatements;
        int seconds = prepare ? EpmaOperationBudget.PrepareSeconds : EpmaOperationBudget.RowSeconds;
        var engine = JintHelper.CreateEngine(TimeSpan.FromSeconds(seconds), options =>
        {
            options.LimitMemory(64L * 1024 * 1024).CancellationToken(token).DisableStringCompilation();
            options.MaxStatements(statements);
            options.Constraints.Constraints.Add(new EpmaOperationBudget(statements, seconds));
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
            if (Prepared.Count > 256) { Prepared.Clear(); Renderers.Clear(); Detailers.Clear(); }
            return engine;
        }
        catch { engine.Dispose(); throw; }
    }
    private static JsonElement Invoke(Engine engine, JsValue bridge, string function, JsonElement context, bool optional = false)
    {
        var callable = engine.GetValue(function);
        if (optional && callable.IsUndefined()) return JsonSerializer.SerializeToElement(Array.Empty<object>());
        string json = engine.Invoke(bridge, callable, context.GetRawText()).AsString();
        if (json.Length > EpmaScriptPackage.MaximumBytes) throw new InvalidOperationException("Script result exceeds 10 MiB.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 48 });
        return document.RootElement.Clone();
    }
    private static JsonElement Context(object value) => JsonSerializer.SerializeToElement(value, EpmaScriptPackage.JsonOptions);
    public static EpmaScriptCell Parse(RawCell raw, NumericFormatChoice format)
    {
        string text = ValueParser.RawText(raw);
        if (raw is RawCell.Empty || string.IsNullOrWhiteSpace(text)) return new("Missing", text);
        if (raw is RawCell.NativeNumber n) return double.IsFinite(n.Value) ? new("Number", text, n.Value) : new("Text", text);
        if (ValueParser.TryParseDetectionLimit(text, format, out double? limit))
            return new("DetectionLimit", text, null, limit);
        return ValueParser.TryParseStrict(ValueParser.NumericCandidate(text), format, out double number) ? new("Number", text, number) : new("Text", text);
    }
    /// <summary>Whether a row holds any value besides its sample name; a row with only a sample name is treated as empty.</summary>
    private static bool HasCalculationInput(IReadOnlyDictionary<string, EpmaScriptCell> inputs)
        => inputs.Any(pair => pair.Key != EpmaScriptPackage.SampleInputId && pair.Value.Kind != "Missing");
    public static Task<EpmaScriptRun> CalculateAsync(EpmaScriptPackage package, InputTableSnapshot input,
        NumericFormatChoice format = NumericFormatChoice.DecimalPoint,
        CancellationToken token = default) => Task.Run(() => Calculate(package, input, format, token), token);

    /// <summary>Executes an editor snapshot without building a ZIP or validating package metadata/declarations.</summary>
    public static Task<IReadOnlyList<EpmaScriptRow>> TestScriptAsync(string path, string source, EpmaScriptManifest columns,
        EpmaExampleDataset examples, CancellationToken token = default) => Task.Run<IReadOnlyList<EpmaScriptRow>>(() =>
    {
        try { return TestScript(path, source, columns, examples, token); }
        catch { token.ThrowIfCancellationRequested(); throw; }
    }, token);

    private static ReadOnlyCollection<EpmaScriptRow> TestScript(string path, string source, EpmaScriptManifest columns,
        EpmaExampleDataset examples, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        var scripts = new Dictionary<string, string> { [path] = source };
        var declarations = columns.Inputs;
        var fields = columns.Outputs;
        var parsed = new List<IReadOnlyDictionary<string, EpmaScriptCell>>();
        foreach (var example in examples.Inputs)
            parsed.Add(declarations.ToDictionary(input => input.Id, input =>
            {
                var value = example.GetValueOrDefault(input.Id);
                RawCell raw = value.ValueKind switch
                {
                    JsonValueKind.Number => new RawCell.NativeNumber(value.GetDouble()),
                    JsonValueKind.String => new RawCell.Text(value.GetString()!),
                    _ => new RawCell.Empty()
                };
                return Parse(raw, NumericFormatChoice.DecimalPoint);
            }, StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal));
        var batch = Prepare(scripts, null, parsed, deadline.Token);
        var rows = new List<EpmaScriptRow>();
        foreach (var inputs in parsed)
        {
            deadline.Token.ThrowIfCancellationRequested();
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
            operation.CancelAfter(TimeSpan.FromSeconds(5));
            using var engine = Create(scripts, null, operation.Token, out var bridge);
            var context = Context(new { inputs, outputs = fields.Select(f => f.Id).ToArray(), batch });
            var result = CalculateResponse(engine, bridge, context, fields);
            rows.Add(new(rows.Count + 1, inputs, result));
        }
        deadline.Token.ThrowIfCancellationRequested();
        return rows.AsReadOnly();
    }

    /// <summary>
    /// Optional once-per-run <c>prepare(context)</c> entry: it sees every non-empty row, and whatever it
    /// returns (for example a batch mean or a calibration) is handed to each <c>calculate</c> call as <c>context.batch</c>.
    /// </summary>
    private static JsonElement? Prepare(IReadOnlyDictionary<string, string> scripts, EpmaScriptPackage? package,
        IReadOnlyList<IReadOnlyDictionary<string, EpmaScriptCell>> rows, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(EpmaOperationBudget.PrepareSeconds));
        using var engine = package is null ? Create(scripts, null, deadline.Token, out var bridge, true) : Create(package, deadline.Token, out bridge, true);
        if (engine.GetValue("prepare").IsUndefined()) return null;
        var content = rows.Select((inputs, index) => new { row = index + 1, inputs })
            .Where(item => HasCalculationInput(item.inputs)).ToArray();
        return Invoke(engine, bridge, "prepare", Context(new { rows = content }));
    }

    private static EpmaScriptResponse CalculateResponse(Engine engine, JsValue bridge, JsonElement context, IReadOnlyList<EpmaScriptOutput> fields)
    {
        var result = Invoke(engine, bridge, "calculate", context).Deserialize<EpmaScriptResponse>(EpmaScriptPackage.JsonOptions)
            ?? throw new InvalidOperationException("calculate returned null.");
        ValidateResponse(result, fields);
        return result;
    }
    public static EpmaScriptRun Calculate(EpmaScriptPackage package, InputTableSnapshot input,
        NumericFormatChoice format = NumericFormatChoice.DecimalPoint, CancellationToken token = default)
    {
        var declarations = package.Manifest.Inputs;
        if (input.Headers.Distinct(StringComparer.Ordinal).Count() != input.Headers.Count
            || input.Headers.Any(id => !package.Manifest.Inputs.Any(i => i.Id == id)))
            throw new InvalidOperationException("Input columns must be unique declared input IDs.");
        var columns = input.Headers.Select((id, index) => (id, index)).ToDictionary(p => p.id, p => p.index, StringComparer.Ordinal);
        var mapping = declarations.ToDictionary(i => i.Id, i => columns.TryGetValue(i.Id, out int index) ? index
            : throw new InvalidOperationException("Missing active input column: " + i.Id), StringComparer.Ordinal);
        var rows = new List<EpmaScriptRow>(); var fields = package.Manifest.Outputs;
        var outputs = fields.Select(f => f.Id).ToArray();
        var parsed = new List<IReadOnlyDictionary<string, EpmaScriptCell>>(input.Rows.Count);
        for (int r = 0; r < input.Rows.Count; r++)
        {
            token.ThrowIfCancellationRequested();
            parsed.Add(declarations.ToDictionary(i => i.Id, i => Parse(input.Rows[r][mapping[i.Id]], format), StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal));
        }
        var batch = Prepare(package.Scripts, package, parsed, token);
        for (int r = 0; r < parsed.Count; r++)
        {
            token.ThrowIfCancellationRequested();
            var inputs = parsed[r];
            if (!HasCalculationInput(inputs)) { rows.Add(new(r + 1, inputs, new() { Status = "Empty" })); continue; }
            var context = Context(new
            {
                inputs,
                outputs,
                batch
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
                    Flags = [EpmaScriptFlag.Error("EPMA_SCRIPT_EXECUTION_ERROR", ex.Message)]
                }));
            }
        }
        token.ThrowIfCancellationRequested();
        return new(package, input, format, DateTimeOffset.UtcNow, rows.AsReadOnly(), batch);
    }
    private static void ValidateResponse(EpmaScriptResponse response, IReadOnlyList<EpmaScriptOutput> fields)
    {
        if (response.Status is not ("Computed" or "Skipped")) throw new InvalidOperationException("Invalid result status.");
        if (response.Status == "Computed" && fields.Any(f => !response.Values.ContainsKey(f.Id)))
            throw new InvalidOperationException("Computed result must return every declared output.");
        foreach (var (id, value) in response.Values)
        {
            var field = fields.SingleOrDefault(f => f.Id == id) ?? throw new InvalidOperationException("Undeclared output: " + id);
            // A null value is an undefined result and needs no explanation.
            if (value.ValueKind == JsonValueKind.Null) continue;
            bool number = value.ValueKind == JsonValueKind.Number && double.IsFinite(value.GetDouble());
            if (!number && value.ValueKind != JsonValueKind.String) throw new InvalidOperationException("Wrong result type: " + id);
        }
        if (response.Status == "Skipped")
            foreach (var field in fields.Where(f => !response.Values.ContainsKey(f.Id)))
                response.Values[field.Id] = JsonSerializer.SerializeToElement<object?>(null);
        ValidateFlags(response.Flags);
    }
    private static void ValidateFlags(IEnumerable<EpmaScriptFlag> flags)
    { if (flags.Count() > 512 || flags.Any(f => string.IsNullOrWhiteSpace(f.Code) || f.Severity is not ("Info" or "Warning" or "Error"))) throw new InvalidOperationException("Invalid QC flags."); }
    private static EpmaScriptFlag[] Flags(JsonElement value)
    {
        var flags = value.Deserialize<EpmaScriptFlag[]>(EpmaScriptPackage.JsonOptions) ?? throw new InvalidOperationException("QC entry must return an array.");
        ValidateFlags(flags); return flags;
    }
    public static IReadOnlyDictionary<string, string> Render(EpmaScriptRun run, EpmaScriptRow row, int decimals, CancellationToken token = default)
    {
        if (Renderers.TryGetValue(run.Package.Sha256, out bool available) && !available) return new Dictionary<string, string>();
        using var displayDeadline = CancellationTokenSource.CreateLinkedTokenSource(token); displayDeadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var engine = Create(run.Package, displayDeadline.Token, out var bridge);
        if (engine.GetValue("renderDisplay").IsUndefined()) return new Dictionary<string, string>();
        var context = Context(new { values = row.Result.Values, displayDecimals = decimals });
        var result = Invoke(engine, bridge, "renderDisplay", context).Deserialize<Dictionary<string, string>>() ?? throw new InvalidOperationException("Invalid display result.");
        if (result.Keys.Any(k => !row.Result.Values.TryGetValue(k, out var value) || value.ValueKind != JsonValueKind.String))
            throw new InvalidOperationException("Display renderer changed a numeric or undeclared field.");
        return result;
    }
    /// <summary>Whether the package defines the optional <c>calculateDetailed</c> entry. Unknown until the package has run once, so it answers true then.</summary>
    public static bool MayDescribe(EpmaScriptPackage package) => !Detailers.TryGetValue(package.Sha256, out bool available) || available;
    /// <summary>
    /// Runs the optional <c>calculateDetailed(context)</c> entry for one row: the name / value / description lines of the inspector,
    /// in the same shape the geothermometers return. Returns <c>null</c> when the package has no such entry.
    /// The context carries <c>inputs</c>, <c>values</c>, <c>flags</c>, <c>batch</c> and <c>displayDecimals</c>, i.e. what <c>calculate</c> saw and returned.
    /// </summary>
    public static List<CalculationStep>? Describe(EpmaScriptRun run, EpmaScriptRow row, int decimals, CancellationToken token = default)
    {
        if (!MayDescribe(run.Package)) return null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var engine = Create(run.Package, deadline.Token, out var bridge);
        if (engine.GetValue("calculateDetailed").IsUndefined()) return null;
        decimals = Math.Clamp(decimals, 0, 10);
        var context = Context(new { inputs = row.Inputs, values = row.Result.Values, flags = row.Result.Flags, batch = run.Batch, displayDecimals = decimals });
        return CalculationStepHelper.FromJson(Invoke(engine, bridge, "calculateDetailed", context), value => EpmaResultPresentation.Number(value, decimals));
    }
    public static void ValidateEntries(EpmaScriptPackage package, CancellationToken token = default)
    {
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(token); operation.CancelAfter(TimeSpan.FromSeconds(5));
        using var engine = Create(package, operation.Token, out _);
        if (!engine.Evaluate("typeof calculate === 'function'").AsBoolean())
            throw new InvalidOperationException("Missing required callable script entry.");
        foreach (string entry in new[] { "prepare", "renderDisplay", "calculateDetailed" })
            if (!engine.Evaluate($"typeof {entry} === 'undefined' || typeof {entry} === 'function'").AsBoolean())
                throw new InvalidOperationException("Invalid optional script entry: " + entry);
    }

    public static async Task ValidateAsync(EpmaScriptPackage package, CancellationToken token = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(60));
        ValidateEntries(package, deadline.Token);
        var examples = await CalculateAsync(package, ExampleInput(package), NumericFormatChoice.DecimalPoint, deadline.Token);
        foreach (var row in examples.Rows) CheckCase(row, "example row " + row.InputRow);
        // The inspector entry is optional, but when it exists it must work and return well-formed steps for every example.
        foreach (var row in examples.Rows.Where(r => r.Result.Status == "Computed"))
        {
            deadline.Token.ThrowIfCancellationRequested();
            try { Describe(examples, row, 4, deadline.Token); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { throw new InvalidOperationException("calculateDetailed failed: example row " + row.InputRow + " " + ex.Message, ex); }
        }
    }

    private static void CheckCase(EpmaScriptRow row, string id)
    {
        if (row.Result.Status != "Computed" || row.Flags.Any(f => f.Code == "EPMA_SCRIPT_EXECUTION_ERROR"))
            throw new InvalidOperationException("Case failed: " + id + " " + string.Join(";", row.Flags.Select(f => f.Text)));
    }
    public static InputTableSnapshot ExampleInput(EpmaScriptPackage package) => Input(package, package.Examples.Inputs);
    private static InputTableSnapshot Input(EpmaScriptPackage package, IEnumerable<Dictionary<string, JsonElement>> inputs)
    {
        IReadOnlyList<IReadOnlyList<RawCell>> rows = [.. inputs.Select(input => (IReadOnlyList<RawCell>)[.. package.Manifest.Inputs.Select(i =>
            !input.TryGetValue(i.Id, out var v) || v.ValueKind == JsonValueKind.Null ? (RawCell)new RawCell.Empty()
                : v.ValueKind == JsonValueKind.Number ? new RawCell.NativeNumber(v.GetDouble()) : new RawCell.Text(v.GetString()!))])];
        return new([.. package.Manifest.Inputs.Select(i => i.Id)], rows);
    }
}
