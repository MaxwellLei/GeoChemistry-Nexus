/**
 * Biotite Ti Thermometer
 * Henry, D.J. et al. (2005)
 *
 * Input: args = [Ti(apfu), Mg(apfu), Fe(apfu)]
 * Output: T(K)
 */
function calculate(args) {
    if (args.length < 3) return null;

    var ti = args[0];
    var mg = args[1];
    var fe = args[2];

    if (mg + fe === 0) return null;

    var a = -2.3594;
    var b = 4.6482e-9;
    var c = -1.7283;

    var xMg = mg / (mg + fe);
    var numerator = Math.log(ti) - a - c * pow(xMg, 3);
    var tK = pow(numerator / b, 1.0 / 3.0) + 273.15;

    return tK;
}

/**
 * 详细计算过程
 */
function calculateDetailed(args) {
    if (args.length < 3) return [{ name: "Error", value: "Missing parameters" }];

    var ti = args[0];
    var mg = args[1];
    var fe = args[2];

    var steps = [];

    steps.push({ name: "Ti (apfu)", value: ti.toFixed(6), desc: "Input: Ti atoms per formula unit" });
    steps.push({ name: "Mg (apfu)", value: mg.toFixed(6), desc: "Input: Mg atoms per formula unit" });
    steps.push({ name: "Fe (apfu)", value: fe.toFixed(6), desc: "Input: Fe atoms per formula unit" });

    var a = -2.3594;
    var b = 4.6482e-9;
    var c = -1.7283;

    steps.push({ name: "a", value: a.toString(), desc: "Constant a = -2.3594" });
    steps.push({ name: "b", value: b.toExponential(4), desc: "Constant b = 4.6482×10⁻⁹" });
    steps.push({ name: "c", value: c.toString(), desc: "Constant c = -1.7283" });

    var xMg = mg / (mg + fe);
    steps.push({ name: "X(Mg)", value: xMg.toFixed(6), desc: "Mg / (Mg + Fe)" });

    var xMg3 = pow(xMg, 3);
    steps.push({ name: "X(Mg)³", value: xMg3.toFixed(6), desc: "X(Mg)³" });

    var lnTi = Math.log(ti);
    steps.push({ name: "ln(Ti)", value: lnTi.toFixed(6), desc: "ln(Ti)" });

    var numerator = lnTi - a - c * xMg3;
    steps.push({ name: "Numerator", value: numerator.toFixed(6), desc: "ln(Ti) - a - c×X(Mg)³" });

    var ratio = numerator / b;
    steps.push({ name: "Numerator/b", value: ratio.toExponential(4), desc: "Numerator / b" });

    var tK = pow(ratio, 1.0 / 3.0) + 273.15;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "(Numerator/b)^(1/3) + 273.15", isResult: true });

    var tC = tK - 273.15;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "T(K) - 273.15", isResult: true });

    return steps;
}
