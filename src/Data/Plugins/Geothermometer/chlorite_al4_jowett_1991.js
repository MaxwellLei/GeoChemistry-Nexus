/**
 * Chlorite Al4 Thermometer
 * Jowett, E.C. (1991)
 *
 * Input: args = [SiO2, TiO2, Al2O3, FeO, MnO, MgO, CaO, Na2O, K2O, BaO, Rb2O, CsO, ZnO, F, Cl, Cr2O3, NiO]
 * All in wt.%
 * Output: T(K)
 */
function calculate(args) {
    if (args.length < 17) return null;

    var sio2  = args[0];
    var tio2  = args[1];
    var al2o3 = args[2];
    var feo   = args[3];
    var mno   = args[4];
    var mgo   = args[5];
    var cao   = args[6];
    var na2o  = args[7];
    var k2o   = args[8];
    var bao   = args[9];
    var rb2o  = args[10];
    var cso   = args[11];
    var zno   = args[12];
    var f     = args[13];
    var cl    = args[14];
    var cr2o3 = args[15];
    var nio   = args[16];

    // Oxygen basis
    var oxygenBasisSum = (sio2*2/60.09) + (tio2*2/79.9) + (al2o3*3/101.96) +
                          (cr2o3*3/152) + (feo/71.85) + (mno/70.94) + (mgo/40.31) +
                          (nio/74.708) + (zno/81.38) + (cao/56.08) + (na2o/61.98) +
                          (k2o/94.22) + (bao/153.36) + (rb2o/186.936) + (f*0.5/19) +
                          (cl*0.5/35.45);

    if (oxygenBasisSum === 0) return null;

    var feCation = 28 * (feo / 71.85) / oxygenBasisSum;

    var totalCations = (28*(sio2/60.09)/oxygenBasisSum) + (28*(tio2/79.9)/oxygenBasisSum) +
                       (28*(al2o3*2/101.96)/oxygenBasisSum) + (28*(cr2o3*2/152)/oxygenBasisSum) +
                       feCation + (28*(mno/70.94)/oxygenBasisSum) + (28*(mgo/40.31)/oxygenBasisSum) +
                       (28*(nio/74.708)/oxygenBasisSum) + (28*(zno/81.38)/oxygenBasisSum) +
                       (28*(cao/56.08)/oxygenBasisSum) + (28*(na2o*2/61.98)/oxygenBasisSum) +
                       (28*(k2o*2/94.22)/oxygenBasisSum) + (28*(bao/153.36)/oxygenBasisSum) +
                       (28*(rb2o*2/186.936)/oxygenBasisSum);

    var cationVacancy = 20 - totalCations;
    var nonNegativeVacancy = max(0, cationVacancy);
    var feForChargeBalance = feCation - cationVacancy;
    var feInOctahedral = (nonNegativeVacancy > feCation) ? 0 : feForChargeBalance;
    var feInTetrahedral = min(feCation, nonNegativeVacancy);

    var oxygenNormFactor = (28*(sio2/60.09)/oxygenBasisSum)*2 + (28*(tio2/79.9)/oxygenBasisSum)*2 +
                            (28*(al2o3*2/101.96)/oxygenBasisSum)*1.5 + (28*(cr2o3*2/152)/oxygenBasisSum)*1.5 +
                            feInTetrahedral*1.5 + feInOctahedral + (28*(mno/70.94)/oxygenBasisSum) +
                            (28*(mgo/40.31)/oxygenBasisSum) + (28*(nio/74.708)/oxygenBasisSum) +
                            (28*(zno/81.38)/oxygenBasisSum) + (28*(cao/56.08)/oxygenBasisSum) +
                            (28*(na2o*2/61.98)/oxygenBasisSum) + (28*(k2o*2/94.22)/oxygenBasisSum) +
                            (28*(bao/153.36)/oxygenBasisSum) + (28*(rb2o*2/186.936)/oxygenBasisSum) +
                            (28*(f/19)/oxygenBasisSum) + (28*(cl/35.45)/oxygenBasisSum);

    if (oxygenNormFactor === 0) return null;

    var siInTet = ((28*(sio2/60.09)/oxygenBasisSum)*2 * (28/oxygenNormFactor)) / 2;
    var alCation = 28*(al2o3*2/101.96) / oxygenBasisSum;
    var alInTet_intermediate = (siInTet + alCation > 8) ? (8 - siInTet) : alCation;
    var alInTet = max(0, alInTet_intermediate);

    var feOct_norm = (feInOctahedral * (28/oxygenNormFactor)) / 2;
    var mgCat_norm = (28*(mgo/40.31)/oxygenBasisSum) * (28/oxygenNormFactor);
    var denominator = feOct_norm + mgCat_norm / 2;

    if (denominator === 0) return null;

    var tValueInput = (alInTet / 2) + 0.1 * (feOct_norm / denominator);

    return 319 * tValueInput - 68.7 + 273.15;
}

/**
 * 详细计算过程
 */
function calculateDetailed(args) {
    if (args.length < 17) return [{ name: "Error", value: "Missing parameters" }];

    var sio2  = args[0],  tio2  = args[1],  al2o3 = args[2];
    var feo   = args[3],  mno   = args[4],  mgo   = args[5];
    var cao   = args[6],  na2o  = args[7],  k2o   = args[8];
    var bao   = args[9],  rb2o  = args[10], cso   = args[11];
    var zno   = args[12], f     = args[13], cl    = args[14];
    var cr2o3 = args[15], nio   = args[16];

    var steps = [];

    var inputNames = ["SiO2","TiO2","Al2O3","FeO","MnO","MgO","CaO","Na2O","K2O","BaO","Rb2O","CsO","ZnO","F","Cl","Cr2O3","NiO"];
    for (var i = 0; i < 17; i++) {
        steps.push({ name: inputNames[i] + " (wt.%)", value: args[i].toFixed(4), desc: "Input" });
    }

    var oxygenBasisSum = (sio2*2/60.09) + (tio2*2/79.9) + (al2o3*3/101.96) +
                          (cr2o3*3/152) + (feo/71.85) + (mno/70.94) + (mgo/40.31) +
                          (nio/74.708) + (zno/81.38) + (cao/56.08) + (na2o/61.98) +
                          (k2o/94.22) + (bao/153.36) + (rb2o/186.936) + (f*0.5/19) +
                          (cl*0.5/35.45);

    steps.push({ name: "O-basis sum", value: oxygenBasisSum.toFixed(6), desc: "Oxygen basis normalization sum" });

    if (oxygenBasisSum === 0) {
        steps.push({ name: "Error", value: "O-basis = 0", desc: "Cannot proceed" });
        return steps;
    }

    var feCation = 28 * (feo / 71.85) / oxygenBasisSum;
    steps.push({ name: "Fe (cation)", value: feCation.toFixed(6), desc: "28 × (FeO/71.85) / O-basis" });

    var totalCations = (28*(sio2/60.09)/oxygenBasisSum) + (28*(tio2/79.9)/oxygenBasisSum) +
                       (28*(al2o3*2/101.96)/oxygenBasisSum) + (28*(cr2o3*2/152)/oxygenBasisSum) +
                       feCation + (28*(mno/70.94)/oxygenBasisSum) + (28*(mgo/40.31)/oxygenBasisSum) +
                       (28*(nio/74.708)/oxygenBasisSum) + (28*(zno/81.38)/oxygenBasisSum) +
                       (28*(cao/56.08)/oxygenBasisSum) + (28*(na2o*2/61.98)/oxygenBasisSum) +
                       (28*(k2o*2/94.22)/oxygenBasisSum) + (28*(bao/153.36)/oxygenBasisSum) +
                       (28*(rb2o*2/186.936)/oxygenBasisSum);
    steps.push({ name: "Total cations", value: totalCations.toFixed(6), desc: "Sum of all cations (28-O basis)" });

    var cationVacancy = 20 - totalCations;
    var nonNegativeVacancy = max(0, cationVacancy);
    steps.push({ name: "Cation vacancy", value: cationVacancy.toFixed(6), desc: "20 - Total cations" });

    var feForChargeBalance = feCation - cationVacancy;
    var feInOctahedral = (nonNegativeVacancy > feCation) ? 0 : feForChargeBalance;
    var feInTetrahedral = min(feCation, nonNegativeVacancy);

    steps.push({ name: "Fe³⁺ (tet)", value: feInTetrahedral.toFixed(6), desc: "Fe in tetrahedral site" });
    steps.push({ name: "Fe²⁺ (oct)", value: feInOctahedral.toFixed(6), desc: "Fe in octahedral site" });

    var oxygenNormFactor = (28*(sio2/60.09)/oxygenBasisSum)*2 + (28*(tio2/79.9)/oxygenBasisSum)*2 +
                            (28*(al2o3*2/101.96)/oxygenBasisSum)*1.5 + (28*(cr2o3*2/152)/oxygenBasisSum)*1.5 +
                            feInTetrahedral*1.5 + feInOctahedral + (28*(mno/70.94)/oxygenBasisSum) +
                            (28*(mgo/40.31)/oxygenBasisSum) + (28*(nio/74.708)/oxygenBasisSum) +
                            (28*(zno/81.38)/oxygenBasisSum) + (28*(cao/56.08)/oxygenBasisSum) +
                            (28*(na2o*2/61.98)/oxygenBasisSum) + (28*(k2o*2/94.22)/oxygenBasisSum) +
                            (28*(bao/153.36)/oxygenBasisSum) + (28*(rb2o*2/186.936)/oxygenBasisSum) +
                            (28*(f/19)/oxygenBasisSum) + (28*(cl/35.45)/oxygenBasisSum);
    steps.push({ name: "O-norm factor", value: oxygenNormFactor.toFixed(6), desc: "Oxygen normalization factor" });

    if (oxygenNormFactor === 0) {
        steps.push({ name: "Error", value: "Norm factor = 0", desc: "Cannot proceed" });
        return steps;
    }

    var siInTet = ((28*(sio2/60.09)/oxygenBasisSum)*2 * (28/oxygenNormFactor)) / 2;
    var alCation = 28*(al2o3*2/101.96) / oxygenBasisSum;
    var alInTet_intermediate = (siInTet + alCation > 8) ? (8 - siInTet) : alCation;
    var alInTet = max(0, alInTet_intermediate);

    steps.push({ name: "Si (tet)", value: siInTet.toFixed(6), desc: "Si in tetrahedral" });
    steps.push({ name: "Al (tet)", value: alInTet.toFixed(6), desc: "Al in tetrahedral (for thermometer)" });

    var feOct_norm = (feInOctahedral * (28/oxygenNormFactor)) / 2;
    var mgCat_norm = (28*(mgo/40.31)/oxygenBasisSum) * (28/oxygenNormFactor);
    steps.push({ name: "Fe²⁺ (oct, norm)", value: feOct_norm.toFixed(6), desc: "Normalized Fe²⁺ octahedral" });
    steps.push({ name: "Mg (norm)", value: mgCat_norm.toFixed(6), desc: "Normalized Mg" });

    var denominator = feOct_norm + mgCat_norm / 2;
    steps.push({ name: "Denominator", value: denominator.toFixed(6), desc: "Fe²⁺(oct) + Mg/2" });

    if (denominator === 0) {
        steps.push({ name: "Error", value: "Denominator = 0", desc: "Cannot calculate temperature" });
        return steps;
    }

    var tValueInput = (alInTet / 2) + 0.1 * (feOct_norm / denominator);
    steps.push({ name: "Al(IV)/2 + correction", value: tValueInput.toFixed(6), desc: "Al(tet)/2 + 0.1×(Fe²⁺oct/Denom)" });

    var tC = 319 * tValueInput - 68.7;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "319 × input - 68.7" });

    var tK = tC + 273.15;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "T(℃) + 273.15", isResult: true });

    return steps;
}
