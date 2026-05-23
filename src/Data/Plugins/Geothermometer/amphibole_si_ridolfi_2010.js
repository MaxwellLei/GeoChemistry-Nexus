/**
 * Amphibole Si* Thermometer
 * Ridolfi, F., Renzulli, A., Puerini, M. (2010)
 *
 * Input: args = [SiO2, TiO2, Al2O3, Cr2O3, FeO, MnO, MgO, CaO, Na2O, K2O, F, Cl]
 * All in wt.%
 * Output: T(K)
 */

// Molar masses
var MOL_SIO2 = 60.084;
var MOL_TIO2 = 79.898;
var MOL_AL2O3 = 101.961;
var MOL_CR2O3 = 151.9902;
var MOL_FEO = 71.846;
var MOL_MNO = 70.937;
var MOL_MGO = 40.304;
var MOL_CAO = 56.079;
var MOL_NA2O = 61.979;
var MOL_K2O = 94.195;
var MOL_F = 18.998;
var MOL_CL = 35.453;
var MOL_FE2O3 = 159.691;
var OXYGEN_ATOMS = 23 * 15.999;

function calculate(args) {
    if (args.length < 12) return null;

    var sio2 = args[0], tio2 = args[1], al2o3 = args[2], cr2o3 = args[3];
    var feo = args[4], mno = args[5], mgo = args[6], cao = args[7];
    var na2o = args[8], k2o = args[9], f = args[10], cl = args[11];

    if (sio2 === 0) return null;

    // Oxygen normalization
    var tempSumForB44 = (sio2 * 0.532554423806671) + (tio2 * 0.400485619164435) +
                         (al2o3 * 0.470738811898667) + (cr2o3 * 0.315790096993096) +
                         (feo * 0.222684631016341) + (mno * 0.225538153572889) +
                         (mgo * 0.396958118300913) + (cao * 0.285293960305997) +
                         (na2o * 0.258135820197164) + (k2o * 0.169849779712299);
    var b44 = OXYGEN_ATOMS / tempSumForB44;

    // Cation numbers
    var b45 = (sio2 * b44) / MOL_SIO2;
    var b46 = (tio2 * b44) / MOL_TIO2;
    var b47 = (al2o3 * b44) / MOL_AL2O3 * 2;
    var b48 = (cr2o3 * b44) / MOL_CR2O3 * 2;
    var b49 = (feo * b44) / MOL_FEO;
    var b50 = (mno * b44) / MOL_MNO;
    var b51 = (mgo * b44) / MOL_MGO;
    var b52 = (cao * b44) / MOL_CAO;
    var b53 = (na2o * b44) / MOL_NA2O * 2;
    var b54 = (k2o * b44) / MOL_K2O * 2;
    var b57 = (f * b44) / MOL_F;
    var b58 = (cl * b44) / MOL_CL;

    // Sum
    var b62 = b45 + b46 + b47 + b48 + b49 + b50 + b51;
    if (b62 === 0) return null;
    var b78 = b62 + b52;
    if (b78 === 0) return null;

    // Normalize to 13 or 15 cations
    var b63 = (b45 * 13) / b62; var b79 = (b45 * 15) / b78;
    var b64 = (b46 * 13) / b62; var b80 = (b46 * 15) / b78;
    var b65 = (b47 * 13) / b62; var b81 = (b47 * 15) / b78;
    var b66 = (b48 * 13) / b62; var b82 = (b48 * 15) / b78;
    var b67 = (b49 * 13) / b62; var b83 = (b49 * 15) / b78;
    var b68 = (b50 * 13) / b62; var b84 = (b50 * 15) / b78;
    var b69 = (b51 * 13) / b62; var b85 = (b51 * 15) / b78;
    var b70 = (b52 * 13) / b62; var b86 = (b52 * 15) / b78;
    var b71 = (b53 * 13) / b62; var b87 = (b53 * 15) / b78;
    var b72 = (b54 * 13) / b62; var b88 = (b54 * 15) / b78;

    var isSumLt8 = (b63 + b64 + b65) < 8;

    var b96  = isSumLt8 ? b79 : b63;
    var b97  = isSumLt8 ? b80 : b64;
    var b98  = isSumLt8 ? b81 : b65;
    var b99  = isSumLt8 ? b82 : b66;
    var b102 = isSumLt8 ? b84 : b68;
    var b103 = isSumLt8 ? b85 : b69;
    var b104 = isSumLt8 ? b86 : b70;
    var b105 = isSumLt8 ? b87 : b71;
    var b106 = isSumLt8 ? b88 : b72;

    // Fe3+ / Fe2+
    var b94 = (b70 < 1.5)
        ? (b79*4 + b80*4 + b81*3 + b82*3 + b83*2 + b84*2 + b85*2 + b86*2 + b87 + b88)
        : (b63*4 + b64*4 + b65*3 + b66*3 + b67*2 + b68*2 + b69*2 + b70*2 + b71 + b72);

    var b100 = b94 > 46 ? 0 : 46 - b94;
    var b101 = (isSumLt8 ? b83 : b67) - b100;

    // Site assignment
    var b131 = b96;
    var b132 = min(8 - b131, b98);
    var b133 = min(8 - b131 - b132, b97);

    var b136 = b98 - b132;
    var b137 = b97 - b133;
    var b138 = b99;
    var b139 = b100;
    var b140 = b103;
    var b142 = b102;
    var b141 = min(5 - b136 - b137 - b138 - b139 - b140 - b142, b101);

    var b145 = b100 + b101 - b139 - b141;
    var b146 = b104;
    var b147 = min(2 - b145 - b146, b105);

    var b150 = b105 - b147;
    var b151 = b106;
    var b152 = b150 + b151;

    // Si* parameter
    var b164;
    if (b104 < 1.5) {
        b164 = 0;
    } else {
        b164 = b131 + b132/15 - b133*2 - b136/2 - b137/1.8 + b139/9 + b141/3.3 +
               b140/26 + b146/5 + b147/1.3 - b150/15 + (1 - b152)/2.3;
    }

    // Validity check
    var b59 = 2 - b57 - b58;
    var b21 = (b59 * 17) / b44 / 2;
    var b27 = (b100 * b62) / 13 / b44 * MOL_FE2O3 / 2;
    var b28 = (b101 * b62) / 13 / b44 * MOL_FEO;
    var b37Arr = [sio2, tio2, al2o3, cr2o3, b27, b28, mno, mgo, cao, na2o, k2o, f, cl, b21];
    var b37 = 0;
    for (var i = 0; i < b37Arr.length; i++) b37 += b37Arr[i];

    var b41 = -(f * 0.421070639014633 + cl * 0.225636758525372);
    var b39 = b37 + b41;

    var b155 = b103 / (b103 + b141 + b145);

    var b110 = "ok";
    if (b39 < 98 || b139 < 0 || b141 < 0 || b150 < 0 || b152 > 1 || b155 < 0.5) {
        b110 = "wrong";
    }

    var b112;
    if (b164 === 0) b112 = "low-Ca";
    else if (b110 === "wrong") b112 = "invalid";
    else {
        var denominator = b136 + b132;
        if (denominator === 0) return null;
        var b111 = b136 / denominator;

        if (b111 > 0.21) b112 = "Xenocryst";
        else if (b131 >= 6.5) b112 = "Mg-Hbl";
        else if (b152 > 0.5) b112 = "Mg-Hst";
        else b112 = "Tsch-Prg";
    }

    if (b164 === 0 || b112 === "invalid") return null;

    var tK = -151.487 * b164 + 2041 + 273.15;
    return tK;
}

/**
 * 详细计算过程
 */
function calculateDetailed(args) {
    if (args.length < 12) return [{ name: "Error", value: "Missing parameters" }];

    var sio2 = args[0], tio2 = args[1], al2o3 = args[2], cr2o3 = args[3];
    var feo = args[4], mno = args[5], mgo = args[6], cao = args[7];
    var na2o = args[8], k2o = args[9], f = args[10], cl = args[11];

    var steps = [];

    // Inputs
    var inputNames = ["SiO2","TiO2","Al2O3","Cr2O3","FeO","MnO","MgO","CaO","Na2O","K2O","F","Cl"];
    for (var i = 0; i < 12; i++) {
        steps.push({ name: inputNames[i] + " (wt.%)", value: args[i].toFixed(4), desc: "Input" });
    }

    if (sio2 === 0) {
        steps.push({ name: "Error", value: "SiO2 = 0", desc: "Cannot proceed" });
        return steps;
    }

    // Oxygen normalization
    var tempSumForB44 = (sio2 * 0.532554423806671) + (tio2 * 0.400485619164435) +
                         (al2o3 * 0.470738811898667) + (cr2o3 * 0.315790096993096) +
                         (feo * 0.222684631016341) + (mno * 0.225538153572889) +
                         (mgo * 0.396958118300913) + (cao * 0.285293960305997) +
                         (na2o * 0.258135820197164) + (k2o * 0.169849779712299);
    var b44 = OXYGEN_ATOMS / tempSumForB44;
    steps.push({ name: "O-norm factor (b44)", value: b44.toFixed(6), desc: "23×15.999 / Σ(oxide × coeff)" });

    var b45 = (sio2 * b44) / MOL_SIO2;
    var b46 = (tio2 * b44) / MOL_TIO2;
    var b47 = (al2o3 * b44) / MOL_AL2O3 * 2;
    var b48 = (cr2o3 * b44) / MOL_CR2O3 * 2;
    var b49 = (feo * b44) / MOL_FEO;
    var b50 = (mno * b44) / MOL_MNO;
    var b51 = (mgo * b44) / MOL_MGO;
    var b52 = (cao * b44) / MOL_CAO;
    var b53 = (na2o * b44) / MOL_NA2O * 2;
    var b54 = (k2o * b44) / MOL_K2O * 2;
    var b57 = (f * b44) / MOL_F;
    var b58 = (cl * b44) / MOL_CL;

    steps.push({ name: "Si (cation)", value: b45.toFixed(4), desc: "Si cation from SiO2" });
    steps.push({ name: "Ti (cation)", value: b46.toFixed(4), desc: "Ti cation from TiO2" });
    steps.push({ name: "Al (cation)", value: b47.toFixed(4), desc: "Al cation from Al2O3" });
    steps.push({ name: "Fe²⁺tot (cation)", value: b49.toFixed(4), desc: "Fe cation from FeO" });
    steps.push({ name: "Mg (cation)", value: b51.toFixed(4), desc: "Mg cation from MgO" });
    steps.push({ name: "Ca (cation)", value: b52.toFixed(4), desc: "Ca cation from CaO" });

    var b62 = b45 + b46 + b47 + b48 + b49 + b50 + b51;
    var b78 = b62 + b52;
    steps.push({ name: "Σ cations (excl.Ca)", value: b62.toFixed(4), desc: "Si+Ti+Al+Cr+Fe+Mn+Mg" });
    steps.push({ name: "Σ cations (incl.Ca)", value: b78.toFixed(4), desc: "above + Ca" });

    var b63 = (b45 * 13) / b62; var b79 = (b45 * 15) / b78;
    var b64 = (b46 * 13) / b62; var b80 = (b46 * 15) / b78;
    var b65 = (b47 * 13) / b62; var b81 = (b47 * 15) / b78;
    var b66 = (b48 * 13) / b62; var b82 = (b48 * 15) / b78;
    var b67 = (b49 * 13) / b62; var b83 = (b49 * 15) / b78;
    var b68 = (b50 * 13) / b62; var b84 = (b50 * 15) / b78;
    var b69 = (b51 * 13) / b62; var b85 = (b51 * 15) / b78;
    var b70 = (b52 * 13) / b62; var b86 = (b52 * 15) / b78;
    var b71 = (b53 * 13) / b62; var b87 = (b53 * 15) / b78;
    var b72 = (b54 * 13) / b62; var b88 = (b54 * 15) / b78;

    var sumSiTiAl_13 = b63 + b64 + b65;
    var isSumLt8 = sumSiTiAl_13 < 8;
    steps.push({ name: "Si+Ti+Al (13-norm)", value: sumSiTiAl_13.toFixed(4), desc: "Determines 13 vs 15 cation basis" });
    steps.push({ name: "Basis", value: isSumLt8 ? "15-cation" : "13-cation", desc: isSumLt8 ? "Sum < 8 → use 15" : "Sum ≥ 8 → use 13" });

    var b96  = isSumLt8 ? b79 : b63;
    var b97  = isSumLt8 ? b80 : b64;
    var b98  = isSumLt8 ? b81 : b65;
    var b99  = isSumLt8 ? b82 : b66;
    var b102 = isSumLt8 ? b84 : b68;
    var b103 = isSumLt8 ? b85 : b69;
    var b104 = isSumLt8 ? b86 : b70;
    var b105 = isSumLt8 ? b87 : b71;
    var b106 = isSumLt8 ? b88 : b72;

    var b94 = (b70 < 1.5)
        ? (b79*4 + b80*4 + b81*3 + b82*3 + b83*2 + b84*2 + b85*2 + b86*2 + b87 + b88)
        : (b63*4 + b64*4 + b65*3 + b66*3 + b67*2 + b68*2 + b69*2 + b70*2 + b71 + b72);

    var b100 = b94 > 46 ? 0 : 46 - b94;
    var b101 = (isSumLt8 ? b83 : b67) - b100;

    steps.push({ name: "Fe³⁺", value: b100.toFixed(4), desc: "Fe³⁺ from charge balance" });
    steps.push({ name: "Fe²⁺", value: b101.toFixed(4), desc: "Fe²⁺ = FeTotal - Fe³⁺" });

    // Site assignment
    var b131 = b96;
    var b132 = min(8 - b131, b98);
    var b133 = min(8 - b131 - b132, b97);
    var b136 = b98 - b132;
    var b137 = b97 - b133;
    var b138 = b99;
    var b139 = b100;
    var b140 = b103;
    var b142 = b102;
    var b141 = min(5 - b136 - b137 - b138 - b139 - b140 - b142, b101);
    var b145 = b100 + b101 - b139 - b141;
    var b146 = b104;
    var b147 = min(2 - b145 - b146, b105);
    var b150 = b105 - b147;
    var b151 = b106;
    var b152 = b150 + b151;

    steps.push({ name: "Si(T)", value: b131.toFixed(4), desc: "T-site Si" });
    steps.push({ name: "Al(T)", value: b132.toFixed(4), desc: "T-site Al" });
    steps.push({ name: "Al(M)", value: b136.toFixed(4), desc: "M-site Al" });
    steps.push({ name: "Mg(M)", value: b140.toFixed(4), desc: "M-site Mg" });
    steps.push({ name: "Ca(M4)", value: b146.toFixed(4), desc: "M4-site Ca" });
    steps.push({ name: "Na(M4)", value: b147.toFixed(4), desc: "M4-site Na" });
    steps.push({ name: "Na(A)", value: b150.toFixed(4), desc: "A-site Na" });
    steps.push({ name: "K(A)", value: b151.toFixed(4), desc: "A-site K" });
    steps.push({ name: "A-occ", value: b152.toFixed(4), desc: "A-site occupancy = Na(A) + K(A)" });

    // Si* parameter
    var b164;
    if (b104 < 1.5) {
        b164 = 0;
        steps.push({ name: "Si*", value: "0 (low-Ca)", desc: "Ca < 1.5 → low-Ca amphibole" });
    } else {
        b164 = b131 + b132/15 - b133*2 - b136/2 - b137/1.8 + b139/9 + b141/3.3 +
               b140/26 + b146/5 + b147/1.3 - b150/15 + (1 - b152)/2.3;
        steps.push({ name: "Si*", value: b164.toFixed(6), desc: "Composite Si* parameter" });
    }

    // Validity
    var b59 = 2 - b57 - b58;
    var b21 = (b59 * 17) / b44 / 2;
    var b27 = (b100 * b62) / 13 / b44 * MOL_FE2O3 / 2;
    var b28 = (b101 * b62) / 13 / b44 * MOL_FEO;
    var b37Arr = [sio2, tio2, al2o3, cr2o3, b27, b28, mno, mgo, cao, na2o, k2o, f, cl, b21];
    var b37 = 0;
    for (var ii = 0; ii < b37Arr.length; ii++) b37 += b37Arr[ii];
    var b41 = -(f * 0.421070639014633 + cl * 0.225636758525372);
    var b39 = b37 + b41;
    var b155 = b103 / (b103 + b141 + b145);

    var validity = "ok";
    if (b39 < 98 || b139 < 0 || b141 < 0 || b150 < 0 || b152 > 1 || b155 < 0.5) {
        validity = "invalid";
    }

    var classification;
    if (b164 === 0) classification = "low-Ca";
    else if (validity === "invalid") classification = "invalid";
    else {
        var denom = b136 + b132;
        if (denom === 0) classification = "Error";
        else {
            var b111 = b136 / denom;
            if (b111 > 0.21) classification = "Xenocryst";
            else if (b131 >= 6.5) classification = "Mg-Hbl";
            else if (b152 > 0.5) classification = "Mg-Hst";
            else classification = "Tsch-Prg";
        }
    }

    steps.push({ name: "Total (recalc)", value: b39.toFixed(2), desc: "Recalculated total" });
    steps.push({ name: "Mg#", value: b155.toFixed(4), desc: "Mg / (Mg + Fe²⁺ + excess)" });
    steps.push({ name: "Validity", value: validity, desc: validity === "ok" ? "Analysis passes checks" : "Analysis fails checks" });
    steps.push({ name: "Classification", value: classification, desc: "Amphibole classification" });

    if (b164 === 0 || classification === "invalid") {
        steps.push({ name: "T(K)", value: "N/A", desc: "Cannot calculate (invalid or low-Ca)" });
        return steps;
    }

    var tK = -151.487 * b164 + 2041 + 273.15;
    steps.push({ name: "T(K)", value: tK.toFixed(2), desc: "-151.487 × Si* + 2041 + 273.15", isResult: true });

    var tC = tK - 273.15;
    steps.push({ name: "T(℃)", value: tC.toFixed(2), desc: "T(K) - 273.15", isResult: true });

    return steps;
}
