---
sidebar_position: 5
---

# CIPW Norm Mineral Calculation

The **CIPW norm** converts whole-rock major-element oxides (wt%) into an idealized set of normative minerals. The result is used to compare igneous rocks and to read silica and alumina saturation. Cross, Iddings, Pirsson, and Washington introduced the method in 1902. This module calculates many samples in a spreadsheet and keeps a diagnostic record for every row.

For the cation norm and the Niggli numbers of the same analysis, see [Niggli Norm Calculation](./Niggli).

![CIPW norm page](/img/v0.9.0/cipw-result.png)

## Interface

The toolbar carries **Fe³⁺/ΣFe** on the left and **Help**, **Example**, **Export**, **Clear**, and **Calculate** on the right.

The sheet has four regions. Group labels sit above the column headers:

| Region | Group | Unit | Role |
| --- | --- | --- | --- |
| Sample | — | — | Sample name only. This column stays visible while scrolling right |
| Oxides | Major elements; Other oxides / volatiles | wt% | Input |
| Diagnostics | Diagnostics | — | Silica saturation, alumina state, mass-balance error |
| Norm | CIPW normative minerals | wt% | Result area. Headers use the symbol and, in a localized interface, the mineral name |

A narrow separator stands between input and results. The sheet starts with 500 rows. Double-click the fill handle to fill downward.

### Toolbar

| Control | Action |
| --- | --- |
| **Fe³⁺/ΣFe** | Splits FeO and Fe₂O₃ when the analysis provides **FeOT** only. Range `0`–`1`, default `0.15` (Le Maitre, 2002) |
| **Help** | Opens the in-app algorithm summary |
| **Example** | Fills granite, basalt, and andesite, including measured Fe₂O₃, FeO, and MnO |
| **Export** | Saves the sheet as UTF-8 CSV. The default name is `CIPW_Results.csv` |
| **Clear** | Asks for confirmation, then clears input, results, and the detail panel |
| **Calculate** | Calculates every row that contains a valid oxide |

### Row status

| Dot | Meaning |
| --- | --- |
| Gray ring | Input is present and has not been calculated |
| Teal | Succeeded, with no warning and an acceptable mass balance |
| Orange | Succeeded, but a warning is present, or the mass-balance error is greater than `1×10⁻⁶` |
| Red | The row failed. The diagnostic columns contain the error |
| Orange ring | An oxide was edited after the last calculation, so the result is stale |

The example rows contain MnO. MnO is moved into FeO and that conversion is a warning, so those rows are orange after a successful calculation. Editing the sample name does not stale the row. Editing any oxide does.

## Input

Each row is one whole-rock sample in weight percent. The calculator reads only values **greater than 0**. Blanks and zeros are treated as not provided. Empty rows are skipped.

Paste directly from a spreadsheet. The column order is fixed.

### Oxide columns

Major elements:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`, `P2O5`

Other oxides / volatiles:

`ZrO2`, `Cr2O3`, `CO2`, `S`, `F`, `Cl`, `SO3`

`FeOT` is total iron as FeO equivalent. Enter `S`, `F`, and `Cl` as elemental weight percent.

### Iron

| Input | Treatment |
| --- | --- |
| Both `FeO` and `Fe2O3`, and no `FeOT` | Use the measured pair. The iron mode is “measured” |
| Only `FeO` or only `Fe2O3` | The missing valence is 0. The mode is “partially assumed”, with a warning |
| `FeOT` only | Split by Fe³⁺/ΣFe. The mode is “estimated from FeOT” |
| `FeOT` together with `FeO` or `Fe2O3` | Ignore `FeOT`. The mode is “inconsistent input”, with a warning |
| No iron values | FeO and Fe₂O₃ are both 0. The mode is “missing”, with a warning |

The FeOT split is:

```text
Fe2O3 = FeOT × (Fe³⁺/ΣFe) × 1.11134
FeO   = FeOT × (1 − Fe³⁺/ΣFe)
```

The factor `1.11134` is the molar-mass ratio of Fe₂O₃ to two FeO, so ferric iron expressed as FeO equivalent is restored to an Fe₂O₃ weight percent.

Before the norm, `MnO` is converted to an FeO equivalent and added to FeO:

```text
FeO equivalent = MnO × 71.844 / 70.937
```

Any entered MnO produces the warning that MnO was converted to an FeO equivalent.

## Recommended workflow

1. Enter the sample name and the oxide wt%.
2. Confirm **Fe³⁺/ΣFe** when the analysis has total iron only. The ratio is unused when both FeO and Fe₂O₃ are present.
3. Click **Calculate**. A full success reports the sample count. A partial success reports successful rows over rows attempted.
4. Read saturation, the mass-balance error, and the minerals to the right. Minerals at or below `0.001 wt%` are left blank. Written values are rounded to 3 decimal places.
5. Select a row and open **Calculation details**. That panel can be copied or exported as its own CSV.
6. Use **Export** to archive the sheet. The file includes the sample, input, diagnostics, and minerals. The separator column is `---`.

## Output

### Diagnostic columns

| Column | Values |
| --- | --- |
| **Silica saturation** | Oversaturated, saturated, undersaturated |
| **Alumina state** | Peralkaline, metaluminous, peraluminous |
| **Mass-balance error (%)** | Closure between mineral mass and the entered oxides. Errors below `0.01` use scientific notation; larger errors keep 4 decimal places |

The three silica states:

- **Oversaturated**: SiO₂ remains and is reported as quartz `Q`.
- **Saturated**: SiO₂ is exactly consumed. There is no quartz and no desilication.
- **Undersaturated**: SiO₂ is short and the desilication sequence below runs. If a deficit remains after the whole sequence, a warning gives that remainder in mol/100 g. The norm is then unreliable.

Alumina is judged from the oxide moles still available before feldspar allocation:

- **Peralkaline**: Na₂O + K₂O > Al₂O₃
- **Peraluminous**: Al₂O₃ / (CaO + Na₂O + K₂O) > 1
- **Metaluminous**: every remaining case

Apatite, calcite, fluorite, halite, and thenardite have already consumed the corresponding CaO or Na₂O, so the test uses what is left.

### Normative minerals

Results follow this order. Names match the localized headers.

| Symbol | Mineral | Symbol | Mineral |
| --- | --- | --- | --- |
| `Q` | Quartz | `Fo` | Forsterite |
| `Or` | Orthoclase | `Fa` | Fayalite |
| `Ab` | Albite | `Mt` | Magnetite |
| `An` | Anorthite | `Hm` | Hematite |
| `Le` | Leucite | `Ilm` | Ilmenite |
| `Ne` | Nepheline | `Cm` | Chromite |
| `Kp` | Kaliophilite | `Ru` | Rutile |
| `Cor` | Corundum | `Tn` | Titanite |
| `Ac` | Acmite | `Pf` | Perovskite |
| `Di` | Diopside | `Z` | Zircon |
| `Hd` | Hedenbergite | `Ap` | Apatite |
| `Wo` | Wollastonite | `Cc` | Calcite |
| `Cs` | Calcium orthosilicate | `Py` | Pyrite |
| `En` | Enstatite | `Fl` | Fluorite |
| `Fs` | Ferrosilite | `Hl` | Halite |
| `ns` | Sodium metasilicate | `Th` | Thenardite |
| `ks` | Potassium metasilicate | | |

Orthoclase, albite, and acmite are counted in formula units: 1 mol K₂O produces 2 mol `Or`, and 1 mol Na₂O produces 2 mol `Ab` or `Ac`. The sheet always shows weight percent.

## Calculation details

Selecting a successful row shows:

- The sample name, or “Row N” when the sample cell is empty.
- Silica and alumina saturation. Undersaturated silica is highlighted.
- **Iron handling mode**. Any mode other than measured is highlighted.
- **Mass-balance error**, to 6 decimal places. An error above `1×10⁻⁶` is highlighted.
- **Total minerals**, the sum of normative wt%, to 4 decimal places. Oxygen released while forming pyrite, fluorite, and halite is excluded from this sum and included in the mass balance.
- **Main minerals**: minerals above `0.01 wt%`, largest first. Entries above `10 wt%` are highlighted.
- **Warnings**: missing or inconsistent iron, estimation from FeOT, the MnO conversion, and a residual silica deficit.

## Calculation

The steps below follow the current calculator. Apatite takes calcium before any volatile mineral is formed, and titanite and rutile are formed after anorthite.

Mineral molar masses are derived from the oxide molar masses, so rounding of a published mineral weight does not open a mass-balance gap. The numerical tolerance is `1×10⁻¹²`. If any oxide other than SiO₂ becomes meaningfully negative, that row fails. SiO₂ is allowed to go negative until desilication.

### 1. Anhydrous normalization, iron, and manganese

Non-negative oxides are normalized to 100%. Iron is handled as above, MnO is moved into FeO, and the oxides are **normalized again**. A total that is not positive fails the row.

### 2. Moles

Moles of each oxide = normalized wt% / molar mass. Allocation then proceeds in mol/100 g.

### 3. Apatite

Apatite takes calcium first. One mole of `P2O5` combines with `10/3` mole of `CaO`, with no OH or F in the formula:

```text
Ap = min(P2O5, CaO / (10/3))
```

### 4. Volatile minerals

Each mineral forms from whatever can still be paired:

| Mineral | Amount |
| --- | --- |
| Calcite `Cc` | `min(CO2, CaO)` |
| Fluorite `Fl` | `min(F/2, CaO)`, so 1 mol CaO takes 2 mol F |
| Pyrite `Py` | `min(S/2, FeO)`, so 1 mol FeO takes 2 mol S |
| Halite `Hl` | `min(Cl, 2×Na2O)` |
| Thenardite `Th` | `min(SO3, Na2O)` |

### 5. Zircon, chromite, and ilmenite

- Zircon `Z`: all `ZrO2` plus an equal amount of `SiO2`.
- Chromite `Cm`: `min(Cr2O3, FeO)`.
- Ilmenite `Ilm`: `min(TiO2, FeO)`.

Only the titanium that can pair with FeO is used here. The rest waits until after anorthite.

### 6. Feldspars, corundum, and residual alkalis

The alumina state is recorded, then:

| Mineral | Rule |
| --- | --- |
| Orthoclase `Or` | `min(K2O, Al2O3)`, consuming 6 times as much SiO₂. The formula-unit amount is twice that value |
| Albite `Ab` | `min(Na2O, Al2O3)`, with the same silica factor and the same doubling |
| Anorthite `An` | `min(CaO, Al2O3)`, consuming twice as much SiO₂ |
| Corundum `Cor` | Remaining Al₂O₃ |
| Acmite `Ac` | `min(remaining Na2O, Fe2O3)`, consuming 4 times as much SiO₂. The formula-unit amount is twice that value |
| Sodium metasilicate `ns` | Remaining Na₂O plus an equal amount of SiO₂ |
| Potassium metasilicate `ks` | Remaining K₂O plus an equal amount of SiO₂ |

### 7. Titanite and rutile

Titanite uses only the CaO left after anorthite. Available SiO₂ does not limit it:

```text
Tn = min(remaining TiO2, remaining CaO)
```

`Tn` also consumes an equal amount of SiO₂. TiO₂ still left is rutile `Ru`.

### 8. Iron oxides

```text
Mt = min(Fe2O3, FeO)
Hm = remaining Fe2O3
```

### 9. Pyroxene and wollastonite

Let `MgFe = MgO + FeO`. Clinopyroxene is `min(CaO, MgFe)`, split into diopside `Di` and hedenbergite `Hd` by the Mg/Fe ratio. Each portion consumes twice as much SiO₂.

MgO and FeO still left become enstatite `En` and ferrosilite `Fs`, each consuming one SiO₂. After magnesium and iron are gone, leftover CaO becomes wollastonite `Wo`.

### 10. Silica saturation and desilication

The order is hypersthene, titanite, albite, orthoclase, wollastonite, diopside/hedenbergite, then leucite. Each step removes only the silica still missing.

| Order | Reaction | SiO₂ released |
| --- | --- | --- |
| 1 | 2 enstatite/ferrosilite → 1 forsterite/fayalite | 1 mol per 2 mol pyroxene |
| 2 | Titanite `Tn` → perovskite `Pf` | 1 mol per 1 mol `Tn` |
| 3 | Albite `Ab` → nepheline `Ne` | 2 mol per 1 mol `Ab` |
| 4 | Orthoclase `Or` → leucite `Le` | 1 mol per 1 mol `Or` |
| 5 | 2 wollastonite `Wo` → 1 calcium orthosilicate `Cs` | 1 mol per 2 mol `Wo` |
| 6 | Diopside/hedenbergite → calcium orthosilicate + olivine | 1 mol per 1 mol pyroxene. Mg goes to `Fo` and Fe goes to `Fa` |
| 7 | Leucite `Le` → kaliophilite `Kp` | 1 mol per 1 mol `Le` |

Leftover positive SiO₂ becomes quartz and the state is oversaturated. Exact closure is saturated. Entering the table makes the state undersaturated.

### 11. Weight percent and mass balance

Mineral mass = moles × the formula molar mass. That mass is reported as wt% of the normalized oxide total.

S, F, and Cl in pyrite, fluorite, and halite replace oxygen from the original oxides. The released oxygen is not stored in a mineral, so the balance adds it back:

```text
moles of released oxygen = Fl + Py + Hl / 2
mass-balance error = |100 − (sum of mineral wt% + wt% of released oxygen)|
```

An error greater than `1×10⁻⁶` marks the row with a warning.

## References

- Cross, W., Iddings, J. P., Pirsson, L. V., Washington, H. S. (1902). A quantitative chemico-mineralogical classification and nomenclature of igneous rocks. *The Journal of Geology*, 10, 555–690.
- Kelsey, C. H. (1965). Calculation of the C.I.P.W. norm. *Mineralogical Magazine*, 34, 276–282.
- Verma, S. P., Torres-Alvarado, I. S., Velasco-Tapia, F. (2003). A revised CIPW norm. *Schweizerische Mineralogische und Petrographische Mitteilungen*, 83, 197–216.
- Philpotts, A. R., Ague, J. J. (2009). *Principles of Igneous and Metamorphic Petrology* (2nd ed.). Cambridge University Press.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
