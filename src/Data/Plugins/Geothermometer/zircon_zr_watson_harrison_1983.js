/**
 * Zircon Zr-saturation Thermometer
 * Watson, E.B. and Harrison, T.M. (1983)
 *
 * Input: args = [Zr(ppm), SiO2, Al2O3, Fe2O3, FeO, MgO, P2O5, CaO, K2O, Na2O]
 * Output: T(K)
 */
function calAtomicMass(sampleValue, atomicMassValue, num) {
    return (sampleValue * num) / atomicMassValue;
}

function calculate(args) {
    if (args.length < 10) return null;

    var zr    = args[0];
    var sio2  = args[1];
    var al2o3 = args[2];
    var fe2o3 = args[3];
    var feo   = args[4];
    var mgo   = args[5];
    var p2o5  = args[6];
    var cao   = args[7];
    var k2o   = args[8];
    var na2o  = args[9];

    if (zr <= 0) return null;

    var tsiO2  = calAtomicMass(sio2,  60.083, 10000);
    var tal2O3 = calAtomicMass(al2o3, 101.961, 20000);
    var tfe2O3 = calAtomicMass(fe2o3, 159.687, 10000);
    var tfeO   = calAtomicMass(feo,   71.844, 10000);
    var tmgO   = calAtomicMass(mgo,   40.304, 10000);
    var tp2O5  = calAtomicMass(p2o5,  141.943, 20000);
    var tcaO   = calAtomicMass(cao,   56.077, 10000);
    var tk2O   = calAtomicMass(k2o,   94.195, 20000);
    var tna2O  = calAtomicMass(na2o,  61.979, 20000);

    var tempTotal = tsiO2 + tal2O3 + tfe2O3 + tfeO + tmgO + tp2O5 + tcaO + tk2O + tna2O;
    if (tempTotal === 0) return null;

    var nSiO2  = tsiO2 / tempTotal;
    var nAl2O3 = tal2O3 / tempTotal;
    var nCaO   = tcaO / tempTotal;
    var nK2O   = tk2O / tempTotal;
    var nNa2O  = tna2O / tempTotal;

    var m = (2 * nCaO + nK2O + nNa2O) / (nSiO2 * nAl2O3);

    var tK = 12900 / (Math.log(496000 / zr) + 0.85 * m + 2.95);

    return tK;
}

/**
 * 详细计算过程
 */
function calculateDetailed(args) {
    if (args.length < 10) return [{ name: "Error", value: "Missing parameters" }];

    var zr    = args[0];
    var sio2  = args[1];
    var al2o3 = args[2];
    var fe2o3 = args[3];
    var feo   = args[4];
    var mgo   = args[5];
    var p2o5  = args[6];
    var cao   = args[7];
    var k2o   = args[8];
    var na2o  = args[9];

    var steps = [];
    steps.push({ name: "Zr (ppm)", value: zr.toFixed(2), desc: "Input: Zr concentration" });

    var tsiO2  = calAtomicMass(sio2,  60.083, 10000);
    var tal2O3 = calAtomicMass(al2o3, 101.961, 20000);
    var tfe2O3 = calAtomicMass(fe2o3, 159.687, 10000);
    var tfeO   = calAtomicMass(feo,   71.844, 10000);
    var tmgO   = calAtomicMass(mgo,   40.304, 10000);
    var tp2O5  = calAtomicMass(p2o5,  141.943, 20000);
    var tcaO   = calAtomicMass(cao,   56.077, 10000);
    var tk2O   = calAtomicMass(k2o,   94.195, 20000);
    var tna2O  = calAtomicMass(na2o,  61.979, 20000);

    steps.push({ name: "tSiO2",  value: tsiO2.toFixed(4),  desc: "SiO2 × 10000 / 60.083" });
    steps.push({ name: "tAl2O3", value: tal2O3.toFixed(4), desc: "Al2O3 × 20000 / 101.961" });
    steps.push({ name: "tFe2O3", value: tfe2O3.toFixed(4), desc: "Fe2O3 × 10000 / 159.687" });
    steps.push({ name: "tFeO",   value: tfeO.toFixed(4),   desc: "FeO × 10000 / 71.844" });
    steps.push({ name: "tMgO",   value: tmgO.toFixed(4),   desc: "MgO × 10000 / 40.304" });
    steps.push({ name: "tP2O5",  value: tp2O5.toFixed(4),  desc: "P2O5 × 20000 / 141.943" });
    steps.push({ name: "tCaO",   value: tcaO.toFixed(4),   desc: "CaO × 10000 / 56.077" });
    steps.push({ name: "tK2O",   value: tk2O.toFixed(4),   desc: "K2O × 20000 / 94.195" });
    steps.push({ name: "tNa2O",  value: tna2O.toFixed(4),  desc: "Na2O × 20000 / 61.979" });

    var tempTotal = tsiO2 + tal2O3 + tfe2O3 + tfeO + tmgO + tp2O5 + tcaO + tk2O + tna2O;
    steps.push({ name: "Total", value: tempTotal.toFixed(4), desc: "Sum of all atomic mass values" });

    var nSiO2  = tsiO2 / tempTotal;
    var nAl2O3 = tal2O3 / tempTotal;
    var nCaO   = tcaO / tempTotal;
    var nK2O   = tk2O / tempTotal;
    var nNa2O  = tna2O / tempTotal;

    steps.push({ name: "Norm.SiO2",  value: nSiO2.toFixed(6),  desc: "tSiO2 / Total" });
    steps.push({ name: "Norm.Al2O3", value: nAl2O3.toFixed(6), desc: "tAl2O3 / Total" });
    steps.push({ name: "Norm.CaO",   value: nCaO.toFixed(6),   desc: "tCaO / Total" });
    steps.push({ name: "Norm.K2O",   value: nK2O.toFixed(6),   desc: "tK2O / Total" });
    steps.push({ name: "Norm.Na2O",  value: nNa2O.toFixed(6),  desc: "tNa2O / Total" });

    var m = (2 * nCaO + nK2O + nNa2O) / (nSiO2 * nAl2O3);
    steps.push({ name: "M", value: m.toFixed(6), desc: "(2×Norm.CaO + Norm.K2O + Norm.Na2O) / (Norm.SiO2 × Norm.Al2O3)" });

    var lnRatio = Math.log(496000 / zr);
    steps.push({ name: "ln(496000/Zr)", value: lnRatio.toFixed(6), desc: "ln(496000 / Zr)" });

    var denom = lnRatio + 0.85 * m + 2.95;
    steps.push({ name: "Denominator", value: denom.toFixed(6), desc: "ln(496000/Zr) + 0.85×M + 2.95" });

    var tK = 12900 / denom;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "12900 / Denominator", isResult: true });

    var tC = tK - 273.15;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "T(K) - 273.15", isResult: true });

    return steps;
}
