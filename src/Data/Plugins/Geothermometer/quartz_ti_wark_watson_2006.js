/**
 * Quartz TitaniQ Thermometer
 * Wark, D.A. and Watson, E.B. (2006)
 *
 * Input: args = [Ti(ppm), α(TiO2)]
 * Output: T(K)
 */
function calculate(args) {
    if (args.length < 2) return null;

    var ti = args[0];
    var aTiO2 = args[1];

    if (ti <= 0) return null;
    if (aTiO2 <= 0 || aTiO2 > 1) return null;

    var logValue = log10(ti / aTiO2);
    var tK = -3765 / (logValue - 5.69);

    return tK;
}

/**
 * 详细计算过程
 */
function calculateDetailed(args) {
    if (args.length < 2) return [{ name: "Error", value: "Missing parameters" }];

    var ti = args[0];
    var aTiO2 = args[1];

    var steps = [];

    steps.push({ name: "Ti (ppm)", value: ti.toFixed(4), desc: "Input: Ti concentration" });
    steps.push({ name: "α(TiO2)", value: aTiO2.toFixed(4), desc: "Input: TiO2 activity" });

    var ratio = ti / aTiO2;
    steps.push({ name: "Ti/α(TiO2)", value: ratio.toFixed(6), desc: "Ti / α(TiO2)" });

    var logValue = log10(ratio);
    steps.push({ name: "log10(Ti/α(TiO2))", value: logValue.toFixed(6), desc: "log10(Ti / α(TiO2))" });

    var denominator = logValue - 5.69;
    steps.push({ name: "Denominator", value: denominator.toFixed(6), desc: "log10(Ti/α(TiO2)) - 5.69" });

    var tK = -3765 / denominator;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "-3765 / Denominator", isResult: true });

    var tC = tK - 273.15;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "T(K) - 273.15", isResult: true });

    return steps;
}
