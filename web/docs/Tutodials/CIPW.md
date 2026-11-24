---
sidebar_position: 5
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# CIPW Normative Mineral Calculation

**CIPW normative mineral calculation** converts whole-rock major-element oxides (wt%) into a set of idealized normative minerals. The result is used to compare igneous rocks and to judge silica saturation and alumina saturation. The CIPW norm was proposed by Cross, Iddings, Pirsson, and Washington in 1902. This module calculates in batches in an Excel-like spreadsheet and reports diagnostics and the mass balance for every row.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/cipw.webp')}
    alt="CIPW interface"
    style={{width: '100%'}}
  />
</div>

## Interface

The page consists of a top bar and a data worksheet. On the left of the top bar is **Fe³⁺/ΣFe**. On the right are **Notes**, **Example**, **Export**, **Clear**, and **Run Calculation**.

The worksheet is divided from left to right into four regions. Group names sit above the column headers:

| Region | Group | Unit | Role |
| --- | --- | --- | --- |
| Sample | — | — | Sample name. It is an identifier only and is not used in the calculation. This column is frozen and stays visible when you scroll horizontally |
| Oxides | Major elements; other oxides / volatiles | wt% | White input area |
| Diagnostics | Diagnostics | — | `SiO2 saturation`, `Al2O3 status`, `Mass-balance error (%)` |
| Normative minerals | CIPW normative minerals | wt% | Lightly shaded result area. Headers show the abbreviation plus the mineral name, for example `Q (quartz)` |

A narrow separator column lies between the input area and the result area. Result columns are empty before calculation. The sheet starts with 500 rows. Double-click the fill handle to fill downward, as in Excel.

### Top bar

| Control | Role |
| --- | --- |
| **Fe³⁺/ΣFe** | Used only when total iron is entered as `FeOT`. Splits FeO and Fe₂O₃ by this ratio. Range `0`–`1`, default `0.15` (Le Maitre, 2002), shown to two decimal places |
| **Notes** | Opens the in-app algorithm description |
| **Example** | Fills three example rows: granite, basalt, and andesite, with measured Fe₂O₃, FeO, and MnO |
| **Export** | Saves the current sheet as a UTF-8 CSV. The default file name is `CIPW_Results.csv` |
| **Clear** | After confirmation, clears all input, results, and calculation details |
| **Run Calculation** | Calculates every row that contains valid oxides |

### Row-header status

The dot beside the row number shows the status of that row:

| Dot | Meaning |
| --- | --- |
| Hollow gray | Input is present, but the row has not been calculated |
| Solid teal | Calculation succeeded, with no warning, and the mass balance also passed |
| Solid orange | Calculation succeeded, but there is a warning, or the mass-balance error is greater than `1×10⁻⁶` |
| Solid red | Calculation failed for this row. The diagnostic column shows “Error” and the reason |
| Hollow orange | Oxides were changed after calculation, and the current result is stale |

Every example row contains MnO. MnO is combined into FeO and produces a warning, so example rows are orange after calculation. That is the expected result. Changing the sample name does not make the result stale. Changing any oxide input does.

## Input

Each row is one whole-rock sample. Enter oxide weight percentages (wt%). The software reads only cells **greater than 0**. Blanks and zeros are both treated as not provided. Empty rows are skipped automatically.

You can paste directly from spreadsheet software. The column order is fixed, so align the paste with the headers below.

### Oxide columns

Major elements:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`, `P2O5`

Other oxides / volatiles:

`ZrO2`, `Cr2O3`, `CO2`, `S`, `F`, `Cl`, `SO3`

`FeOT` is total iron expressed as an FeO equivalent. Enter `S`, `F`, and `Cl` as elemental weight percentages.

### Iron handling

| What is entered | Handling |
| --- | --- |
| Both `FeO` and `Fe2O3` are present, and `FeOT` is not | Use the measured distribution. The iron-handling mode is “measured values” |
| Only `FeO`, or only `Fe2O3` | The missing item is taken as 0. The mode is “partial assumption”, and a warning is given |
| Only `FeOT` | Split by Fe³⁺/ΣFe. The mode is “estimated from FeOT” |
| `FeOT` appears together with `FeO` or `Fe2O3` | Ignore `FeOT`. The mode is “inconsistent input”, and a warning is given |
| No iron data | Both FeO and Fe₂O₃ are taken as 0. The mode is “missing”, and a warning is given |

When only `FeOT` is present, the split is:

```text
Fe2O3 = FeOT × (Fe³⁺/ΣFe) × 1.11134
FeO   = FeOT × (1 − Fe³⁺/ΣFe)
```

The coefficient `1.11134` is the molar-mass ratio of Fe₂O₃ to twice FeO. It converts ferric iron from an FeO equivalent back to a weight percent of Fe₂O₃.

Before calculation, `MnO` is converted to an FeO equivalent by the molar-mass ratio and added to FeO:

```text
FeO equivalent = MnO × 71.844 / 70.937
```

Whenever MnO is entered, the calculation details include “MnO has been converted to an FeO equivalent by the molar ratio.”

## Output

### Diagnostic columns

| Column | Values |
| --- | --- |
| **SiO2 saturation** | Oversaturated, saturated, undersaturated |
| **Al2O3 status** | Peralkaline, metaluminous, peraluminous |
| **Mass-balance error (%)** | The closure deviation between mineral masses and the input oxides. Scientific notation when less than `0.01`; otherwise 4 decimal places |

The three saturation states mean:

- **Oversaturated**: SiO₂ still remains after allocation and is recorded as quartz `Q`.
- **Saturated**: SiO₂ is exactly used up. There is neither quartz nor a need to desilicate.
- **Undersaturated**: SiO₂ is insufficient, and the desilication reactions below are entered. If the whole reaction chain still cannot remove the silica deficit, a further warning is added and the residual deficit (mol/100 g) is stated. The normative minerals are then unreliable.

Alumina saturation is judged before feldspar allocation, from the oxide moles remaining at that time:

- **Peralkaline**: Na₂O + K₂O > Al₂O₃
- **Peraluminous**: Al₂O₃ / (CaO + Na₂O + K₂O) > 1
- **Metaluminous**: all other cases

Apatite, calcite, fluorite, halite, and thenardite have already consumed the corresponding CaO or Na₂O, so the amounts used here are what remains.

### Normative minerals

Results are listed in the order below. The names match the column headers in the software.

| Abbreviation | Mineral | Abbreviation | Mineral |
| --- | --- | --- | --- |
| `Q` | quartz | `Fo` | forsterite |
| `Or` | orthoclase | `Fa` | fayalite |
| `Ab` | albite | `Mt` | magnetite |
| `An` | anorthite | `Hm` | hematite |
| `Le` | leucite | `Ilm` | ilmenite |
| `Ne` | nepheline | `Cm` | chromite |
| `Kp` | pseudoleucite | `Ru` | rutile |
| `Cor` | corundum | `Tn` | titanite |
| `Ac` | acmite | `Pf` | perovskite |
| `Di` | diopside | `Z` | zircon |
| `Hd` | hedenbergite | `Ap` | apatite |
| `Wo` | wollastonite | `Cc` | calcite |
| `Cs` | larnite | `Py` | pyrite |
| `En` | enstatite | `Fl` | fluorite |
| `Fs` | ferrosilite | `Hl` | halite |
| `ns` | sodium metasilicate | `Th` | thenardite |
| `ks` | potassium metasilicate | | |

Orthoclase, albite, and acmite are counted internally in formula units: 1 mol K₂O produces 2 mol `Or`, and 1 mol Na₂O produces 2 mol `Ab` or `Ac`. What is written out is always weight percent.

## Calculation details

After you select a row that calculated successfully, the inspector on the right shows:

- The sample name. If the sample column is empty, it shows “Row N”.
- Silica saturation and alumina saturation. Silica saturation is highlighted when the rock is undersaturated.
- **Iron-handling mode**. It is highlighted whenever the mode is not “measured values”.
- **Mass-balance error**, kept to 6 decimal places. It is highlighted when it exceeds `1×10⁻⁶`.
- **Mineral total**, the sum of the normative mineral wt%, kept to 4 decimal places. Oxygen released when pyrite, fluorite, and halite form is not included in this total, but it is included in the mass balance.
- **Major minerals**: minerals greater than `0.01 wt%`, from highest to lowest. Entries greater than `10 wt%` are highlighted.
- **Warnings**: missing or inconsistent iron data, estimation from FeOT, MnO conversion, residual silica deficit, and similar cases.

The upper right of the inspector can export this set of details as CSV, or copy it to the clipboard.

## Calculation procedure

The steps below follow the actual order in the current calculation code. Apatite claims calcium first, and the volatile-bearing minerals form after that. Titanite and rutile are placed after anorthite.

Every mineral molar mass is derived from oxide molar masses by chemical formula, so manual rounding does not leave the mass unbalanced. The numerical tolerance used in the calculation is `1×10⁻¹²`. If any step drives an oxide other than SiO₂ to a clearly negative value, the row fails.

### 1. Anhydrous normalization, iron, and manganese

Normalize every provided, non-negative oxide to 100%. Handle iron as in the previous section, convert MnO to an FeO equivalent, then **normalize once more**. If the total is not greater than 0, the row fails.

### 2. Conversion to moles

Moles of each oxide = normalized wt% / molar mass. Later allocation is carried out on the mol/100 g scale.

### 3. Apatite

Apatite claims calcium first. 1 mol `P2O5` combines with `10/3` mol `CaO` (no OH or F):

```text
Ap = min(P2O5, CaO / (10/3))
```

### 4. Volatile-bearing minerals

In the order below, form as much as can combine:

| Mineral | Stoichiometry |
| --- | --- |
| Calcite `Cc` | `min(CO2, CaO)` |
| Fluorite `Fl` | `min(F/2, CaO)`, that is, 1 mol CaO combines with 2 mol F |
| Pyrite `Py` | `min(S/2, FeO)`, that is, 1 mol FeO combines with 2 mol S |
| Halite `Hl` | `min(Cl, 2×Na2O)` |
| Thenardite `Th` | `min(SO3, Na2O)` |

### 5. Zircon, chromite, and ilmenite

- Zircon `Z`: all of the `ZrO2`, plus an equal amount of `SiO2`.
- Chromite `Cm`: `min(Cr2O3, FeO)`.
- Ilmenite `Ilm`: `min(TiO2, FeO)`.

This step consumes only the titanium that can combine with FeO. Remaining TiO₂ is left until after anorthite.

### 6. Feldspars, corundum, and remaining alkalis

Record the alumina saturation state first, then allocate:

| Mineral | Conditions and products |
| --- | --- |
| Orthoclase `Or` | `min(K2O, Al2O3)`. Consumes 6 times as much SiO₂. The product moles are twice that value |
| Albite `Ab` | `min(Na2O, Al2O3)`. Likewise consumes 6 times as much SiO₂. The product is twice that value |
| Anorthite `An` | `min(CaO, Al2O3)`. Consumes 2 times as much SiO₂ |
| Corundum `Cor` | Remaining Al₂O₃ |
| Acmite `Ac` | `min(remaining Na2O, Fe2O3)`. Consumes 4 times as much SiO₂. The product is twice that value |
| Sodium metasilicate `ns` | Remaining Na₂O plus an equal amount of SiO₂ |
| Potassium metasilicate `ks` | Remaining K₂O plus an equal amount of SiO₂ |

The feldspar stage may record SiO₂ as negative first. Desilication later handles that in one place.

### 7. Titanite and rutile

Titanite uses only the CaO left after anorthite, and **does not use the SiO₂ remaining at that time to limit titanite**:

```text
Tn = min(remaining TiO2, remaining CaO)
```

`Tn` also consumes an equal amount of SiO₂. TiO₂ that still remains at the end is recorded as rutile `Ru`.

### 8. Iron oxides

```text
Mt = min(Fe2O3, FeO)
Hm = remaining Fe2O3
```

### 9. Pyroxene and wollastonite

Let `MgFe = MgO + FeO`. The total clinopyroxene is `min(CaO, MgFe)`, then split by the magnesium-to-iron ratio into diopside `Di` and hedenbergite `Hd`. Each portion of pyroxene consumes 2 portions of SiO₂.

All remaining MgO and FeO become enstatite `En` and ferrosilite `Fs`, and each consumes 1 portion of SiO₂. After both magnesium and iron are used up, any CaO that remains becomes wollastonite `Wo`.

### 10. Silica saturation and desilication

The desilication order is: hypersthene → titanite → albite → orthoclase → wollastonite → diopside/hedenbergite → leucite. Each step consumes only the SiO₂ that is still missing at that time.

| Order | Reaction | SiO₂ released |
| --- | --- | --- |
| 1 | 2 enstatite/ferrosilite → 1 forsterite/fayalite | 1 mol released per 2 mol pyroxene |
| 2 | Titanite `Tn` → perovskite `Pf` | 1 mol `Tn` releases 1 mol |
| 3 | Albite `Ab` → nepheline `Ne` | 1 mol `Ab` releases 2 mol |
| 4 | Orthoclase `Or` → leucite `Le` | 1 mol `Or` releases 1 mol |
| 5 | 2 wollastonite `Wo` → 1 larnite `Cs` | 1 mol released per 2 mol `Wo` |
| 6 | Diopside/hedenbergite → larnite + olivine | 1 mol pyroxene releases 1 mol; magnesium enters `Fo` and iron enters `Fa` |
| 7 | Leucite `Le` → pseudoleucite `Kp` | 1 mol `Le` releases 1 mol |

When SiO₂ remains, quartz forms and the state is oversaturated. When SiO₂ is exactly zero, the state is saturated. Entering the table above means undersaturated.

### 11. Weight percent and mass balance

Normative mineral mass = moles × the molar mass derived from the chemical formula, then converted to wt% relative to the normalized oxide total.

S, F, and Cl in pyrite, fluorite, and halite replace oxygen in the oxides. The released oxygen does not enter any mineral. It is added back in the mass balance:

```text
moles of oxygen released = Fl + Py + Hl / 2
mass-balance error = |100 − (sum of mineral wt% + wt% of released oxygen)|
```

When the error is greater than `1×10⁻⁶`, the row is marked with a warning.

## References

- Cross, W., Iddings, J. P., Pirsson, L. V., Washington, H. S. (1902). A quantitative chemico-mineralogical classification and nomenclature of igneous rocks. *The Journal of Geology*, 10, 555–690.
- Kelsey, C. H. (1965). Calculation of the C.I.P.W. norm. *Mineralogical Magazine*, 34, 276–282.
- Verma, S. P., Torres-Alvarado, I. S., Velasco-Tapia, F. (2003). A revised CIPW norm. *Schweizerische Mineralogische und Petrographische Mitteilungen*, 83, 197–216.
- Philpotts, A. R., Ague, J. J. (2009). *Principles of Igneous and Metamorphic Petrology* (2nd ed.). Cambridge University Press.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
