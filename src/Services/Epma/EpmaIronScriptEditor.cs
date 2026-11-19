using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Acornima;
using Acornima.Ast;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Edits only a named function using syntax ranges, never regex or text matching inside arbitrary code.</summary>
public static class EpmaIronScriptEditor
{
    private static readonly JsonSerializerOptions Json = new(EpmaScriptPackage.JsonOptions) { WriteIndented = true };
    public static EpmaIronOptions? Read(string source)
    {
        var declaration = new Parser().ParseScript(source).Body.OfType<FunctionDeclaration>().SingleOrDefault(f => f.Id?.Name == "getIronOptions");
        if (declaration is null) return null;
        if (declaration.Body.Body.Count != 1 || declaration.Body.Body[0] is not ReturnStatement { Argument: { } argument })
            throw new InvalidDataException(EpmaLocalizer.Format("epma_iron_literal_required"));
        try { return JsonSerializer.Deserialize<EpmaIronOptions>(source[argument.Start..argument.End], Json) ?? throw new InvalidDataException("Empty iron options."); }
        catch (JsonException ex) { throw new InvalidDataException(EpmaLocalizer.Format("epma_iron_literal_required"), ex); }
    }
    public static string Write(string source, EpmaIronOptions options)
    {
        options.Validate();
        var ast = new Parser().ParseScript(source);
        var function = ast.Body.OfType<FunctionDeclaration>().SingleOrDefault(f => f.Id?.Name == "getIronOptions");
        string replacement = "function getIronOptions() { return " + JsonSerializer.Serialize(options, Json) + "; }";
        if (function is not null) { Read(source); return source[..function.Start] + replacement + source[function.End..]; }
        var settings = ast.Body.OfType<FunctionDeclaration>().SingleOrDefault(f => f.Id?.Name == "getSettings");
        if (settings is not null)
        {
            if (settings.Body.Body.Count != 1 || settings.Body.Body[0] is not ReturnStatement { Argument: ArrayExpression array })
                throw new InvalidDataException(EpmaLocalizer.Format("epma_iron_settings_array_required"));
            source = source.Insert(array.Start + 1, "...epmaIronSettings(),");
        }
        else
        {
            if (ast.Body.OfType<VariableDeclaration>().Any(declaration => declaration.Declarations.Any(variable => variable.Id is Identifier { Name: "getSettings" })))
                throw new InvalidDataException(EpmaLocalizer.Format("epma_iron_settings_array_required"));
            source += "\nfunction getSettings() { return epmaIronSettings(); }\n";
        }
        if (!ast.Body.OfType<FunctionDeclaration>().Any(f => f.Id?.Name == "epmaIronSettings")) source += "\n" + Helpers;
        return source + "\n" + replacement + "\n";
    }

    // Copied into each self-contained method. No host chemistry implementation or runtime resource file is needed.
    public const string Helpers = """
function epmaIronSettings() {
    const o=getIronOptions();
    if(o.enabled===false)return [];
    const labels={FeOt:'FeOt',Fe2O3t:'Fe₂O₃t',Separated:'FeO + Fe₂O₃'};
    const result=[{id:'ironForm',type:'choice',editable:o.forms.length>1,value:o.defaultForm,
        label:{'zh-CN':'铁报告形式','en-US':'Iron reporting'},choices:o.forms.map(form=>({value:form,label:labels[form]}))}];
    if(o.forms.some(form=>form!=='Separated')) {
        const methods=o.methods.filter(method=>method.enabled!==false);
        result.push({id:'ironMethod',type:'choice',editable:methods.length>1,value:o.defaultMethod,
            label:{'zh-CN':'价态处理方法','en-US':'Iron valence treatment'},choices:methods.map(method=>({value:method.id,label:method.label||method.id}))});
        if(methods.some(method=>method.id==='UserRatio'))result.push({id:'fe3Ratio',value:o.ratio,editable:true,min:0,max:1,
            label:{'zh-CN':'Fe³⁺/ΣFe（0–1）','en-US':'Fe³⁺/ΣFe (0–1)'},when:{ironMethod:['UserRatio']}});
    }
    return result;
}
// Standard iron amounts for custom formulas. Mineral-specific estimators stay in the method's own algorithm.
function epmaIron(context) {
    const o=getIronOptions(); if(o.enabled===false)return null;
    const p=context.parameters,form=p.ironForm,method=form==='Separated'?'DeclaredSeparated':p.ironMethod;
    const keys=form==='Separated'?['FeO','Fe2O3']:[form];
    const weights=typeof chemicalData==='object'?chemicalData.weights:{Fe:55.845,O:15.999};
    const feOMass=weights.Fe+weights.O,fe2O3Mass=2*weights.Fe+3*weights.O;
    const missing=[];let totalFe=0,fe2=0,fe3=0,reportedTotal=0;
    for(const key of keys) {
        const cell=context.inputs[key];if(!cell)throw new Error('Missing active iron column: '+key);
        let value;
        if(cell.kind==='Missing'){missing.push(key);value=0;}
        else if(cell.kind==='Number')value=cell.number;
        else if(cell.kind==='DetectionLimit') {
            const strategy=p.detectionLimitStrategy||'Zero';
            if(strategy!=='Zero'&&strategy!=='HalfLimit')throw new Error('Unsupported detection limit strategy');
            value=strategy==='HalfLimit'&&cell.limit!==null?cell.limit/2:0;
        }else throw new Error('Invalid numeric iron input: '+key);
        if(!Number.isFinite(value)||value<0)throw new Error('Iron input must be finite and nonnegative: '+key);
        reportedTotal+=value;
        const moles=(key==='Fe2O3'||key==='Fe2O3t')?2*value/fe2O3Mass:value/feOMass;
        totalFe+=moles;if(key==='FeO')fe2=moles;if(key==='Fe2O3')fe3=moles;
    }
    let source='Unknown';
    if(method==='DeclaredSeparated')source='UserReported';
    else if(method==='AllFe2'){fe2=totalFe;fe3=0;source='Assumed';}
    else if(method==='UserRatio') {
        if(!Number.isFinite(p.fe3Ratio)||p.fe3Ratio<0||p.fe3Ratio>1)throw new Error('Fe³⁺/ΣFe must be in [0,1]');
        fe3=totalFe*p.fe3Ratio;fe2=totalFe-fe3;source='UserSpecified';
    }else if(method==='ReportedEquivalentNoValence'){fe2=null;fe3=null;}
    else throw new Error('This method needs its mineral-specific iron estimator: '+method);
    return {form:form,method:method,source:source,missing:missing,reportedTotal:reportedTotal,totalFe:totalFe,fe2:fe2,fe3:fe3,
        oxygen:source==='Unknown'?totalFe*(form==='Fe2O3t'?1.5:1):fe2+1.5*fe3,
        feOEquivalent:totalFe*feOMass,fe2O3Equivalent:totalFe*fe2O3Mass/2};
}
""";
}
