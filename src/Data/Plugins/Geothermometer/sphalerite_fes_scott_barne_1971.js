/**
 * Sphalerite ΔFeS Thermometer
 * Scott, S.D. and Barnes, H.L. (1971)
 *
 * Input: args = [ΔMole%FeS_matrix, ΔMole%FeS_patch]
 * Output: T(K)
 */
function calculate(args) {
    if (args.length < 2) return null;

    var deltaMolePercentFeS = args[0];
    var deltaMolePercentFeS_patch = args[1];

    var slope = 4.975;
    var intercept = 59.54;

    var delta = abs(deltaMolePercentFeS - deltaMolePercentFeS_patch);

    var tK = (slope * delta * delta) - intercept * delta + 526.6 + 273.15;

    return tK;
}

/**
 * 详细计算过程
 */
function calculateDetailed(args) {
    if (args.length < 2) return [{ name: "Error", value: "Missing parameters" }];

    var deltaMolePercentFeS = args[0];
    var deltaMolePercentFeS_patch = args[1];

    var steps = [];

    steps.push({ name: "Mole%FeS (matrix)", value: deltaMolePercentFeS.toFixed(4), desc: "Input: Matrix FeS mole%" });
    steps.push({ name: "Mole%FeS (patch)", value: deltaMolePercentFeS_patch.toFixed(4), desc: "Input: Patch FeS mole%" });

    var delta = abs(deltaMolePercentFeS - deltaMolePercentFeS_patch);
    steps.push({ name: "|Δ|", value: delta.toFixed(4), desc: "|Matrix - Patch|" });

    var slope = 4.975;
    var intercept = 59.54;

    var term1 = slope * delta * delta;
    steps.push({ name: "4.975 × Δ²", value: term1.toFixed(4), desc: "slope × Δ²" });

    var term2 = intercept * delta;
    steps.push({ name: "59.54 × Δ", value: term2.toFixed(4), desc: "intercept × Δ" });

    var tC = term1 - term2 + 526.6;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "4.975×Δ² - 59.54×Δ + 526.6" });

    var tK = tC + 273.15;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "T(℃) + 273.15", isResult: true });

    return steps;
}
