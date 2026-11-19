using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoChemistryNexus.Services.Epma;

public sealed record EpmaJsonOption(string Value, string Label);

/// <summary>A lossless structured draft; invalid numeric edits never fall back to old values.</summary>
public partial class EpmaJsonDocument : ObservableObject
{
    private readonly string _original;
    private bool _dirty;
    public EpmaJsonNode Root { get; }
    public ObservableCollection<EpmaJsonNode> Roots { get; } = [];
    [ObservableProperty] private EpmaJsonNode _selectedNode;
    [ObservableProperty] private string _newField = "";
    [ObservableProperty] private string _error = "";
    public Func<string, IEnumerable<string>>? FieldSuggestions { get; set; }
    public Func<string, IEnumerable<string>>? ConditionOptions { get; set; }
    public Func<JsonObject>? NewInputRow { get; set; }
    public event Action? Edited;
    public IEnumerable<EpmaJsonOption> AvailableFields => SelectedNode.AvailableFields;
    public bool CanAdd => SelectedNode.IsContainer && (!SelectedNode.HasFixedFields || AvailableFields.Any());
    public bool CanRemove => SelectedNode.CanRemove;
    public bool IsArrayItem => SelectedNode.Parent?.Kind == "array";
    public string AddLabel => EpmaLocalizer.Format(SelectedNode.Kind == "array" ? "epma_json_add_item" : "epma_json_add_field");
    public EpmaJsonDocument(string text, string context = "json")
    {
        _original = text;
        Root = new(this, null, context, JsonNode.Parse(text), context);
        Roots.Add(Root);
        Root.IsExpanded = true;
        _selectedNode = context switch
        {
            "definitions" => Root.Children.First().Children.FirstOrDefault() ?? Root.Children.First(),
            "examples" => Root.Children.FirstOrDefault(n => n.Name == "inputs")?.Children.FirstOrDefault() ?? Root,
            _ => Root
        };
        OnSelectedNodeChanged(_selectedNode);
    }
    public string ExportText() => _dirty ? Root.Build()?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null" : _original;
    internal void Changed()
    {
        _dirty = true;
        OnPropertyChanged(nameof(AvailableFields));
        OnPropertyChanged(nameof(CanAdd)); OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(AddLabel));
        try { Root.Build(); Error = ""; }
        catch (InvalidDataException ex) { Error = ex.Message; }
        Edited?.Invoke();
    }
    public void Select(EpmaJsonNode node)
    {
        if (SelectedNode != node) SelectedNode.IsSelected = false;
        SelectedNode = node;
    }
    partial void OnSelectedNodeChanged(EpmaJsonNode value)
    {
        for (var parent = value.Parent; parent is not null; parent = parent.Parent) parent.IsExpanded = true;
        value.IsSelected = true;
        NewField = "";
        OnPropertyChanged(nameof(AvailableFields)); OnPropertyChanged(nameof(CanAdd)); OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(IsArrayItem)); OnPropertyChanged(nameof(AddLabel));
    }
    public EpmaJsonNode Add()
    {
        var parent = SelectedNode;
        if (!parent.IsContainer) throw new InvalidDataException(EpmaLocalizer.Format("epma_json_select_group"));
        string key = parent.Kind == "array" ? parent.Children.Count.ToString(CultureInfo.InvariantCulture) : NewField?.Trim() ?? "";
        if (parent.Kind == "object" && string.IsNullOrWhiteSpace(key)) key = parent.AvailableFields.FirstOrDefault()?.Value ?? "field";
        if (parent.Kind == "object" && parent.Children.Any(n => n.Name == key))
            throw new InvalidDataException(EpmaLocalizer.Format("epma_json_duplicate", key));
        if (parent.HasFixedFields && !parent.FieldNames.Contains(key))
            throw new InvalidDataException(EpmaLocalizer.Format("epma_json_unknown_field", key));
        string shape = EpmaJsonSchema.ChildShape(parent.Shape, key);
        var child = new EpmaJsonNode(this, parent, key, DefaultValue(shape, parent, key), shape);
        parent.Children.Add(child);
        parent.IsExpanded = true;
        Changed(); OnPropertyChanged(nameof(AvailableFields));
        Select(child.IsContainer ? child : parent);
        return child;
    }
    private JsonNode? DefaultValue(string shape, EpmaJsonNode parent, string key)
    {
        string UniqueId(string prefix)
        {
            int n = 1;
            while (parent.Children.Any(c => c.Children.Any(f => f.Name is "id" or "value" && f.ScalarText == prefix + n))) n++;
            return prefix + n;
        }
        return shape switch
        {
            "input" => new JsonObject { ["id"] = UniqueId("Input"), ["type"] = "number" },
            "output" => new JsonObject { ["id"] = UniqueId("Output"), ["type"] = "number" },
            "values" => NewInputRow?.Invoke() ?? new JsonObject(),
            "inputs" or "outputs" or "rows" or "strings" => new JsonArray(),
            "examples" or "definitions" or "settings" or "when" => new JsonObject(),
            "number" => JsonValue.Create(0),
            "scalar" when parent.Shape == "settings" && ConditionOptions?.Invoke(key).FirstOrDefault() is { } option => JsonValue.Create(option),
            "nullableNumber" or "scalar" => null,
            "inputType" or "outputType" => JsonValue.Create("number"),
            "string" when parent.Shape == "strings" => JsonValue.Create(ConditionOptions?.Invoke(parent.Name).FirstOrDefault() ?? ""),
            _ => JsonValue.Create("")
        };
    }
    public void Remove(EpmaJsonNode node)
    {
        if (!node.CanRemove || node.Parent is null) return;
        var parent = node.Parent;
        parent.Children.Remove(node);
        Select(parent); Changed();
    }
    public void Move(int offset)
    {
        var node = SelectedNode;
        if (node.Parent is not { Kind: "array" } parent) return;
        int index = parent.Children.IndexOf(node), target = index + offset;
        if (target < 0 || target >= parent.Children.Count) return;
        parent.Children.Move(index, target); Changed();
    }
}

public partial class EpmaJsonNode : ObservableObject
{
    private readonly EpmaJsonDocument _document;
    private bool _loading = true;
    public EpmaJsonNode? Parent { get; }
    public string Shape { get; }
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _kind = "null";
    [ObservableProperty] private string _scalarText = "";
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isSelected;
    public ObservableCollection<EpmaJsonNode> Children { get; } = [];
    public bool IsContainer => Kind is "array" or "object";
    public bool IsScalar => !IsContainer && Kind != "null";
    public bool IsNameReadOnly => Parent is null || Parent.Kind == "array" || Parent.HasFixedFields;
    public IEnumerable<string> FieldNames => EpmaJsonSchema.Fields(Shape).Length > 0 ? EpmaJsonSchema.Fields(Shape)
        : _document.FieldSuggestions?.Invoke(Shape) ?? [];
    public bool HasFixedFields => EpmaJsonSchema.Fields(Shape).Length > 0
        || _document.FieldSuggestions is not null && Shape is "values" or "settings" or "when";
    public bool CanRemove => Parent is not null && !(Parent.Shape == "definitions")
        && !(Name == "id" && Parent.Shape is "input" or "output")
        && !(Name == "inputs" && Parent.Shape == "examples");
    public string DisplayName
    {
        get
        {
            if (Parent?.Kind == "array")
                return Children.FirstOrDefault(c => c.Name is "id" or "value")?.ScalarText is { Length: > 0 } id ? id : EpmaLocalizer.Format("epma_json_item", Parent.Children.IndexOf(this) + 1);
            if (Parent?.Shape == "examples" && Name == "inputs") return EpmaJsonSchema.Label("rows");
            if (Shape == "settings") return EpmaLocalizer.Format("epma_json_field_settings");
            return EpmaJsonSchema.Label(Name);
        }
    }
    public string Summary => IsContainer ? EpmaLocalizer.Format("epma_json_count", Children.Count) : ScalarText;
    public string Location => Parent is null ? DisplayName : Parent.Location + " / " + DisplayName;
    public IEnumerable<EpmaJsonOption> TypeOptions => EpmaJsonSchema.Types(Shape).Select(k => new EpmaJsonOption(k, EpmaLocalizer.Format("epma_json_kind_" + k)));
    public bool HasMultipleTypes => EpmaJsonSchema.Types(Shape).Length > 1;
    public IEnumerable<EpmaJsonOption> ValueOptions
    {
        get
        {
            string[] values = Kind == "boolean" ? ["true", "false"] : Shape switch
            {
                "inputType" or "outputType" => ["number", "text"],
                _ => []
            };
            if (values.Length == 0 && (Shape == "string" && Parent?.Shape == "strings" || Shape == "scalar" && Parent?.Shape == "settings"))
                return (_document.ConditionOptions?.Invoke(Parent.Shape == "settings" ? Name : Parent.Name) ?? []).Select(v => new EpmaJsonOption(v, v));
            return values.Select(v => new EpmaJsonOption(v, EpmaLocalizer.Format("epma_json_value_" + v)));
        }
    }
    public bool HasValueOptions => ValueOptions.Any();
    public IEnumerable<EpmaJsonOption> AvailableFields
    {
        get
        {
            return FieldNames.Where(n => !Children.Any(c => c.Name == n)).Select(n => new EpmaJsonOption(n, EpmaJsonSchema.Label(n)));
        }
    }
    internal EpmaJsonNode(EpmaJsonDocument document, EpmaJsonNode? parent, string name, JsonNode? value, string shape)
    {
        _document = document; Parent = parent; _name = name; Shape = shape;
        _kind = value switch { JsonObject => "object", JsonArray => "array", null => "null", _ => value.GetValueKind().ToString().ToLowerInvariant() };
        if (_kind is "true" or "false") _kind = "boolean";
        if (value is JsonObject obj)
            foreach (var (key, child) in obj) Children.Add(new(document, this, key, child, EpmaJsonSchema.ChildShape(shape, key)));
        else if (value is JsonArray array)
            foreach (var child in array) Children.Add(new(document, this, Children.Count.ToString(CultureInfo.InvariantCulture), child, EpmaJsonSchema.ChildShape(shape, "")));
        else if (value is not null) _scalarText = _kind == "string" ? value.GetValue<string>() : value.ToJsonString();
        Children.CollectionChanged += (_, _) => { Refresh(); foreach (var child in Children) child.Refresh(); if (!_loading) _document.Changed(); };
        _loading = false;
    }
    partial void OnNameChanged(string value) => Change();
    partial void OnKindChanged(string value)
    {
        if (!IsContainer && Children.Count > 0) Children.Clear();
        if (value == "number" && !double.TryParse(ScalarText, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) ScalarText = "0";
        if (value == "boolean" && ScalarText is not ("true" or "false")) ScalarText = "false";
        OnPropertyChanged(nameof(IsContainer)); OnPropertyChanged(nameof(IsScalar));
        OnPropertyChanged(nameof(HasValueOptions)); OnPropertyChanged(nameof(ValueOptions));
        Change();
    }
    partial void OnScalarTextChanged(string value) => Change();
    private void Change()
    {
        if (_loading) return;
        Refresh(); Parent?.Refresh(); _document.Changed();
    }
    private void Refresh()
    {
        OnPropertyChanged(nameof(DisplayName)); OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(Location));
        OnPropertyChanged(nameof(IsNameReadOnly));
    }
    public JsonNode? Build()
    {
        InvalidDataException Invalid(string reason) => new(Location + ": " + reason);
        switch (Kind)
        {
            case "object":
                var obj = new JsonObject();
                foreach (var child in Children)
                {
                    if (obj.ContainsKey(child.Name)) throw Invalid(EpmaLocalizer.Format("epma_json_duplicate", child.Name));
                    obj.Add(child.Name, child.Build());
                }
                return obj;
            case "array": return new JsonArray(Children.Select(c => c.Build()).ToArray());
            case "string": return JsonValue.Create(ScalarText);
            case "null": return null;
            case "boolean":
                if (ScalarText is not ("true" or "false")) throw Invalid(EpmaLocalizer.Format("epma_json_invalid_boolean"));
                return JsonValue.Create(ScalarText == "true");
            case "number":
                if (!double.TryParse(ScalarText, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || !double.IsFinite(number))
                    throw Invalid(EpmaLocalizer.Format("epma_json_invalid_number"));
                try
                {
                    var numeric = JsonNode.Parse(ScalarText.Trim());
                    if (numeric?.GetValueKind() != JsonValueKind.Number) throw Invalid(EpmaLocalizer.Format("epma_json_invalid_number"));
                    return numeric;
                }
                catch (JsonException)
                {
                    // Accept ordinary form input such as +01.50 or .5 without rounding its digits.
                    var match = Regex.Match(ScalarText.Trim(), @"\A(?<sign>[+-]?)(?<integer>[0-9]*)(?<fraction>\.[0-9]*)?(?<exponent>[eE][+-]?[0-9]+)?\z");
                    if (!match.Success) throw Invalid(EpmaLocalizer.Format("epma_json_invalid_number"));
                    string integer = match.Groups["integer"].Value.TrimStart('0');
                    if (integer.Length == 0) integer = "0";
                    string fraction = match.Groups["fraction"].Value;
                    if (fraction == ".") fraction = ".0";
                    return JsonNode.Parse((match.Groups["sign"].Value == "-" ? "-" : "") + integer + fraction + match.Groups["exponent"].Value);
                }
            default: throw Invalid("Unsupported value type.");
        }
    }
}

internal static class EpmaJsonSchema
{
    public static string[] Fields(string shape) => shape switch
    {
        "definitions" => ["inputs", "outputs"],
        "input" => ["id", "type", "unit", "when"],
        "output" => ["id", "label", "type", "unit", "description"],
        "examples" => ["parameters", "inputs"], _ => []
    };
    public static string ChildShape(string shape, string key) => shape switch
    {
        "definitions" => key,
        "inputs" => "input", "outputs" => "output", "rows" => "values", "strings" => "string", "when" => "strings",
        "localized" => "string", "values" or "settings" => "scalar",
        "examples" => key == "inputs" ? "rows" : "settings",
        "input" or "output" => key switch
        {
            "id" => "string", "type" => shape + "Type", "label" or "unit" or "description" => "localized",
            "when" => "when", _ => "json"
        }, _ => "json"
    };
    public static string[] Types(string shape) => shape switch
    {
        "definitions" or "input" or "output" or "examples" or "values" or "settings" => ["object"],
        "inputs" or "outputs" or "rows" or "strings" => ["array"],
        "string" or "inputType" or "outputType" => ["string"],
        "localized" => ["string", "object", "null"], "when" => ["object", "null"],
        "number" => ["number"], "nullableNumber" => ["number", "null"], "scalar" => ["number", "string", "null"],
        _ => ["string", "number", "boolean", "object", "array", "null"]
    };
    public static string Label(string key) => Fields("definitions").Concat(Fields("input")).Concat(Fields("output")).Concat(new[] { "parameters", "examples", "rows", "definitions" }).Contains(key)
        ? EpmaLocalizer.Format("epma_json_field_" + key) : key;
}
