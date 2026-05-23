/**
 * Sphalerite GGIMFis Thermometer
 * Frenzel, M. et al. (2016)
 *
 * Input: args = [Ga(ppm), Ge(ppm), Fe(ppm), Mn(ppm), In(ppm)]
 * Output: T(K)
 */
function calculate(args) {
    if (args.length < 5) return null;

    var ga = args[0];
    var ge = args[1];
    var fe = args[2] / 10000;  // ppm -> wt.%
    var mn = args[3];
    var inConc = args[4];

    if (ga <= 0 || ge <= 0 || fe <= 0 || mn <= 0 || inConc <= 0) return null;

    var pc1Star = (Math.log(ga) * 0.22 + Math.log(ge) * 0.22) -
                   Math.log(fe) * 0.37 - Math.log(mn) * 0.20 -
                   Math.log(inConc) * 0.11;

    var tK = (-54.4 * pc1Star + 208) + 273.15;

    return tK;
}

/**
 * 详细计算过程
 */
function calculateDetailed(args) {
    if (args.length < 5) return [{ name: "Error", value: "Missing parameters" }];

    var ga = args[0];
    var ge = args[1];
    var fe = args[2] / 10000;
    var mn = args[3];
    var inConc = args[4];

    var steps = [];

    steps.push({ name: "Ga (ppm)", value: ga.toFixed(4), desc: "Input: Ga concentration" });
    steps.push({ name: "Ge (ppm)", value: ge.toFixed(4), desc: "Input: Ge concentration" });
    steps.push({ name: "Fe (wt.%)", value: fe.toFixed(6), desc: "Input: Fe(ppm) / 10000 → wt.%" });
    steps.push({ name: "Mn (ppm)", value: mn.toFixed(4), desc: "Input: Mn concentration" });
    steps.push({ name: "In (ppm)", value: inConc.toFixed(4), desc: "Input: In concentration" });

    var lnGa = Math.log(ga);
    var lnGe = Math.log(ge);
    var lnFe = Math.log(fe);
    var lnMn = Math.log(mn);
    var lnIn = Math.log(inConc);

    steps.push({ name: "ln(Ga)", value: lnGa.toFixed(6), desc: "ln(Ga)" });
    steps.push({ name: "ln(Ge)", value: lnGe.toFixed(6), desc: "ln(Ge)" });
    steps.push({ name: "ln(Fe)", value: lnFe.toFixed(6), desc: "ln(Fe wt.%)" });
    steps.push({ name: "ln(Mn)", value: lnMn.toFixed(6), desc: "ln(Mn)" });
    steps.push({ name: "ln(In)", value: lnIn.toFixed(6), desc: "ln(In)" });

    var pc1Star = (lnGa * 0.22 + lnGe * 0.22) - lnFe * 0.37 - lnMn * 0.20 - lnIn * 0.11;
    steps.push({ name: "PC1*", value: pc1Star.toFixed(6), desc: "0.22×ln(Ga) + 0.22×ln(Ge) - 0.37×ln(Fe) - 0.20×ln(Mn) - 0.11×ln(In)" });

    var tC = -54.4 * pc1Star + 208;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "-54.4 × PC1* + 208" });

    var tK = tC + 273.15;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "T(℃) + 273.15", isResult: true });

    return steps;
}
