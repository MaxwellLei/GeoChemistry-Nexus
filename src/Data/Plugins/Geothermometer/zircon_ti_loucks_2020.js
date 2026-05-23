/**
 * Zircon Ti-in-zircon Thermometer
 * Loucks, R.R., Fiorentini, M.L., Henriquez, G.J. (2020)
 *
 * Input: args = [Ti(ppm), P(MPa), α(TiO2), α(SiO2)]
 * Output: T(K)
 */
function calculate(args) {
    if (args.length < 4) return null;

    var ti    = args[0];
    var p     = args[1];
    var aTiO2 = args[2];
    var aSiO2 = args[3];

    if (ti <= 0 || aTiO2 <= 0) return null;

    var tK = (-4800 + (0.4748 * (p - 1000))) /
             (log10(ti) - 5.711 - log10(aTiO2) + log10(aSiO2));

    return tK;
}

/**
 * 详细计算过程
 * Input: args = [Ti(ppm), P(MPa), α(TiO2), α(SiO2)]
 * Output: 中间步骤数组
 */
function calculateDetailed(args) {
    if (args.length < 4) return [{ name: "Error", value: "Missing parameters", desc: "" }];

    var ti    = args[0];
    var p     = args[1];
    var aTiO2 = args[2];
    var aSiO2 = args[3];

    var steps = [];

    steps.push({ name: "Ti (ppm)",   value: ti.toFixed(4),    desc: "Input: Ti concentration" });
    steps.push({ name: "P (MPa)",    value: p.toFixed(2),     desc: "Input: Pressure" });
    steps.push({ name: "α(TiO2)",    value: aTiO2.toFixed(4), desc: "Input: TiO2 activity" });
    steps.push({ name: "α(SiO2)",    value: aSiO2.toFixed(4), desc: "Input: SiO2 activity" });

    var logTi = log10(ti);
    steps.push({ name: "log10(Ti)", value: logTi.toFixed(6), desc: "log10(Ti)" });

    var logATiO2 = log10(aTiO2);
    steps.push({ name: "log10(α(TiO2))", value: logATiO2.toFixed(6), desc: "log10(α(TiO2))" });

    var logASiO2 = log10(aSiO2);
    steps.push({ name: "log10(α(SiO2))", value: logASiO2.toFixed(6), desc: "log10(α(SiO2))" });

    var numerator = -4800 + 0.4748 * (p - 1000);
    steps.push({ name: "Numerator", value: numerator.toFixed(4), desc: "-4800 + 0.4748 × (P - 1000)" });

    var denominator = logTi - 5.711 - logATiO2 + logASiO2;
    steps.push({ name: "Denominator", value: denominator.toFixed(6), desc: "log10(Ti) - 5.711 - log10(α(TiO2)) + log10(α(SiO2))" });

    var tK = numerator / denominator;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "= Numerator / Denominator", isResult: true });

    var tC = tK - 273.15;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "= T(K) - 273.15", isResult: true });

    return steps;
}
