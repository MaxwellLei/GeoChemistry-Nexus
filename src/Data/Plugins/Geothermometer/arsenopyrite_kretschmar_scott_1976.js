/**
 * Arsenopyrite Thermometer
 * Kretschmar, U. and Scott, S.D. (1976)
 *
 * Input: args = [AtomicPercent_As, Assemblage_Code]
 * Assemblage codes:
 *   0 = Asp + Po + Lo
 *   1 = Asp + Py + Po
 *   2 = Asp + Py + As
 *   3 = Asp + Po + L
 *   4 = Asp + As + L (or Asp + As + Lo)
 *   5 = Asp + Py + L
 * Output: T(K)
 */

/**
 * 辅助函数：将矿物组合名称转换为编码
 * 注册为 ReoGrid 自定义函数 DefineArsenopyriteAssemblage
 * Input: assemblageName (string)
 * Output: assemblage code (number)
 */
function defineAssemblage(assemblageName) {
    if (assemblageName == null) return null;
    var name = String(assemblageName).toUpperCase().replace(/\s/g, "").replace(/\+/g, "_");
    switch (name) {
        case "ASP_PO_LO": return 0;
        case "ASP_PY_PO": return 1;
        case "ASP_PY_AS": return 2;
        case "ASP_PO_L":  return 3;
        case "ASP_AS_L":  return 4;
        case "ASP_AS_LO": return 4;
        case "ASP_PY_L":  return 5;
        default: return null;
    }
}

// Assemblage boundaries and coefficients
var assemblageData = [
    { minAs: 33.58875219683655, maxAs: 38.68937000657703, slope: 78.7989470836,        intercept: 2346.68161985  },  // 0: Asp+Po+Lo
    { minAs: 29.982425307557115, maxAs: 33.04999622629303, slope: 62.2982482,          intercept: 1567.7758864   },  // 1: Asp+Py+Po
    { minAs: 29.180792909743708, maxAs: 30.210594412757285, slope: 57.861529038539935, intercept: 1384.610940486535 }, // 2: Asp+Py+As
    { minAs: 33.04999622629303, maxAs: 38.68937000657703, slope: 37.219139254543215,   intercept: 738.9114303134363 }, // 3: Asp+Po+L
    { minAs: 31.311072056239013, maxAs: 38.66828037564557, slope: 54.50395246,         intercept: 1406.500496    },  // 4: Asp+As+L
    { minAs: 30.210626758816996, maxAs: 33.028906595361576, slope: 45.5233523473,      intercept: 1012.40557099  }   // 5: Asp+Py+L
];

var assemblageNames = [
    "Asp + Po + Lo",
    "Asp + Py + Po",
    "Asp + Py + As",
    "Asp + Po + L",
    "Asp + As + L",
    "Asp + Py + L"
];

function calculate(args) {
    if (args.length < 2) return null;

    var atomicPercentAs = args[0];
    var assemblage = Math.round(args[1]);

    if (assemblage < 0 || assemblage > 5) return null;

    var data = assemblageData[assemblage];

    if (atomicPercentAs < data.minAs || atomicPercentAs > data.maxAs) return null;

    var tC = data.slope * atomicPercentAs - data.intercept;
    return tC + 273.15;
}

/**
 * 详细计算过程
 */
function calculateDetailed(args) {
    if (args.length < 2) return [{ name: "Error", value: "Missing parameters" }];

    var atomicPercentAs = args[0];
    var assemblage = Math.round(args[1]);

    var steps = [];

    steps.push({ name: "At.% As", value: atomicPercentAs.toFixed(4), desc: "Input: Atomic percent As" });
    steps.push({ name: "Assemblage code", value: assemblage.toString(), desc: "Input: Mineral assemblage index" });

    if (assemblage < 0 || assemblage > 5) {
        steps.push({ name: "Error", value: "Invalid assemblage: " + assemblage, desc: "Valid range: 0-5" });
        return steps;
    }

    steps.push({ name: "Assemblage", value: assemblageNames[assemblage], desc: "Mineral assemblage" });

    var data = assemblageData[assemblage];

    steps.push({ name: "As range (min)", value: data.minAs.toFixed(4), desc: "Minimum valid At.% As" });
    steps.push({ name: "As range (max)", value: data.maxAs.toFixed(4), desc: "Maximum valid At.% As" });

    var inRange = (atomicPercentAs >= data.minAs && atomicPercentAs <= data.maxAs);
    steps.push({ name: "In range?", value: inRange ? "Yes" : "No", desc: inRange ? "Value is within valid range" : "Out of range!" });

    if (!inRange) {
        steps.push({ name: "T(K)", value: "N/A", desc: "Cannot calculate (out of range)" });
        return steps;
    }

    steps.push({ name: "Slope", value: data.slope.toFixed(6), desc: "Linear regression slope" });
    steps.push({ name: "Intercept", value: data.intercept.toFixed(6), desc: "Linear regression intercept" });

    var tC = data.slope * atomicPercentAs - data.intercept;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "Slope × At.%As - Intercept" });

    var tK = tC + 273.15;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "T(℃) + 273.15", isResult: true });

    return steps;
}
