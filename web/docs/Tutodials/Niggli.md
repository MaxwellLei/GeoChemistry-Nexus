---
sidebar_position: 6
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Niggli Normative Mineral Calculation

**Niggli normative mineral calculation** produces two sets of results for the same row of whole-rock oxides:

1. **Niggli parameters**: `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, and `qz`, plus `ti` and `p`, which appear only in the calculation details.
2. **Barth–Niggli molecular normative minerals (Catanorm)**: normative minerals in cation percent (cat%), end-member compositions, and the desilication history when silica is undersaturated.

The Catanorm was established by Paul Niggli (1948) and Tom F. W. Barth (1948). It allocates by cation equivalents and reduces the weight that heavy elements such as iron take in a weight percent, so it lies closer to mineral volume relations in thin section than CIPW wt%. For normative minerals in weight percent, see [CIPW normative mineral calculation](./CIPW).

The algorithm follows the molecular normative-mineral procedure of GCDkit and is reimplemented. The product of further desilication of leucite is recorded as kaliophilite `Kp`.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/niggli.webp')}
    alt="Niggli interface"
    style={{width: '100%'}}
  />
</div>

## Interface

The top bar is the same as in CIPW: **Fe³⁺/ΣFe**, **Notes**, **Example**, **Export**, **Clear**, and **Run Calculation**. The range of Fe³⁺/ΣFe is `0`–`1`, and the default is `0.15` (Le Maitre, 2002).

**Example** fills three rows: granite, basalt, and phonolite. The first two rows are common volcanic rocks with higher silica. Phonolite is used to inspect undersaturated results such as nepheline and leucite.

The worksheet is divided from left to right into five regions:

| Region | Group | Unit | Content |
| --- | --- | --- | --- |
| Sample | — | — | Sample name. It is not used in the calculation and stays visible when you scroll horizontally |
| Oxides | Major elements; other oxides / volatiles | wt% | Input area |
| Niggli parameters | Niggli parameters | Dimensionless | `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz` |
| Diagnostics | Diagnostics | — | Silica saturation, alumina saturation, mineral total |
| Normative minerals | Normative minerals | cat% | Cation percent. The last column is `Sum (total)` |

The sample column is frozen. The sheet starts with 500 rows and supports filling downward by double-clicking the fill handle. After the input changes, a row that has already been calculated becomes stale and **Run Calculation** must be clicked again.

### Row-header status

| Dot | Meaning |
| --- | --- |
| Hollow gray | Input is present, but the row has not been calculated |
| Solid teal | Calculation succeeded, with no warning |
| Solid orange | Calculation succeeded, but FeOT was used to estimate the iron valence |
| Solid red | Calculation failed. The diagnostic column shows “Calculation error” and the reason |
| Hollow orange | Oxides were changed after calculation, and the result is stale |

A sample with both FeO and Fe₂O₃ shows a teal dot after a successful calculation. FeOT alone produces an iron-valence warning, and the row header is orange.

## Input

Each row is one sample, in oxide weight percent (wt%). Only cells greater than 0 are read. Blanks and zeros are treated as not provided.

Major elements, through `K2O`:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`

Other oxides / volatiles:

`CO2`, `P2O5`, `F`, `S`

This module does not read `ZrO2`, `Cr2O3`, `Cl`, or `SO3`. If those four need to enter the norm, use the CIPW module.

### Iron handling

Iron is handled before conversion to cations. When splitting, iron is first converted to Fe atoms: total iron moles = mass of iron / molar mass of FeO. It is then divided into ferric and ferrous iron by Fe³⁺/ΣFe, and half of the ferric iron is written into Fe₂O₃.

| What is entered | Handling |
| --- | --- |
| Both `Fe2O3` and `FeO` are present | Use the measured values. Even if `FeOT` is also present, do not split again |
| `FeOT` is present, and Fe₂O₃ and FeO are not both present | Split `FeOT` by the current ratio, and warn in the details |
| Only `Fe2O3`, with neither `FeO` nor `FeOT` | Treat this Fe₂O₃ as total iron, then split it into Fe₂O₃ and FeO by the ratio |
| Only `FeO`, with neither `Fe2O3` nor `FeOT` | Treat this FeO as total iron, then split it into Fe₂O₃ and FeO by the ratio |

Therefore, if only FeO or only Fe₂O₃ was analyzed, the result changes with the ratio in the top bar. If both are measured values, fill in both columns.

`MnO` is kept through the cation conversion. It enters `fm` and `mg` of the Niggli parameters, and after it enters the Catanorm it is combined into Fe²⁺.

## Niggli parameters

The parameters are calculated from the **molecular numbers** of the oxides. Molecular number = wt% / molar mass. Here `al` is the molecular number of Al₂O₃. One Al₂O₃ counts as 1, which is not the same basis as in the later Catanorm, where one Al₂O₃ counts as 2 Al.

```text
al  = n(Al2O3)
fm  = n(FeO) + 2 n(Fe2O3) + n(MnO) + n(MgO)
c   = n(CaO)
alk = n(Na2O) + n(K2O)
base = al + fm + c + alk

si  = 100 × n(SiO2) / base
al, fm, c, and alk are likewise expressed as parts of 100 relative to base
ti  = 100 × n(TiO2) / base
p   = 100 × n(P2O5) / base
k   = n(K2O) / (n(Na2O) + n(K2O))
mg  = n(MgO) / (n(MgO) + n(FeO) + 2 n(Fe2O3) + n(MnO))
```

Quartz index `qz`:

```text
when al > alk      qz = si − (100 + 4 al)
when al ≤ alk      qz = si − (100 + 3 al + alk)
```

The second expression is for samples in which aluminum does not exceed the alkalis. The in-app Notes window lists only the first expression.

`si`, `al`, `fm`, `c`, `alk`, `ti`, `p`, and `qz` are kept to 2 decimal places. `k` and `mg` are kept to 3 decimal places. The sheet shows only the eight columns of the first group. `ti` and `p` are in the calculation details. When `qz` is positive, the details prefix it with a plus sign.

In the sheet, `al + fm + c + alk` should be 100. `si` can be far above 100, because silicon is not inside that 100-part share. `k` and `mg` are ratios from 0 to 1.

## Diagnostic columns

| Column | Meaning |
| --- | --- |
| **Silica saturation** | Silica-oversaturated, silica-saturated, silica-undersaturated |
| **Alumina saturation** | Peraluminous, metaluminous, peralkaline |
| **Mineral total** | The sum of the Catanorm cation percentages, kept to 2 decimal places |

Silica saturation is judged from the final minerals. The threshold is `0.001 cat%`:

- Quartz `Q` is present: silica-oversaturated.
- There is no quartz, but nepheline `Ne`, leucite `Lc`, kaliophilite `Kp`, or olivine `Ol` is present: silica-undersaturated.
- Otherwise: silica-saturated.

The sign of `qz` shows whether silica is in excess or in deficit relative to the Niggli basis. Silica saturation in the sheet is based on the normative minerals. The two columns can be compared. Their bases of judgment differ.

Alumina saturation is judged from the normalized cations, before mineral allocation:

- **Peraluminous**: Al > Na + K + Ca
- **Metaluminous**: Na + K < Al ≤ Na + K + Ca
- **Peralkaline**: Al ≤ Na + K

## Normative minerals

The unit of the mineral columns is cation percent. `Plag` equals `Ab + An`. The total `Sum` includes `Plag` and does not add `Ab` and `An` a second time. When checking closure, use `Sum` or the mineral total in the diagnostic columns.

| Abbreviation | Mineral | Abbreviation | Mineral |
| --- | --- | --- | --- |
| `Q` | quartz | `Hy` | orthopyroxene |
| `C` | corundum | `Ol` | olivine |
| `Or` | orthoclase | `Cs` | rankinite |
| `Plag` | plagioclase | `Wo` | wollastonite |
| `Ab` | albite | `Mt` | magnetite |
| `An` | anorthite | `Hm` | hematite |
| `Lc` | leucite | `Il` | ilmenite |
| `Ne` | nepheline | `Tn` | titanite |
| `Kp` | kaliophilite | `Pf` | perovskite |
| `Ac` | acmite | `Ru` | rutile |
| `Ns` | sodium metasilicate | `Ap` | apatite |
| `Ks` | potassium metasilicate | `Fr` | fluorite |
| `Di` | diopside | `Py` | pyrite |
| | | `Cc` | calcite |

End-member proportions do not have their own result columns. They are written entirely in the calculation details.

## Calculation details

After you select a successful row, the inspector on the right shows six sections:

1. **Rock classification and saturation**: silica saturation, alumina saturation, and the normative mineral total. Silica saturation is highlighted when undersaturated, and alumina saturation is highlighted when peralkaline.
2. **Niggli parameters**: `si`, `al`, `fm`, `c`, `alk`, `ti`, `p`, `k`, `mg`, `qz`.
3. **Mineral end-members**, shown only when the corresponding mineral is present:
   - Plagioclase `An% = 100 × An / (Ab + An)`
   - Diopside is fixed at Wo 50%, and the other 50% is split into En and Fs by the magnesium-to-iron ratio
   - Olivine Fo and Fa; Fo equals the magnesium number below
   - Magnesium number `Mgr = 100 × En / (En + Fs)`, calculated before orthopyroxene is combined into diopside
4. **Desilication history**: when silica remains, the quartz cat% is stated; when silica is in deficit, the history lists step by step which mineral was consumed and whether the deficit has already been removed.
5. **Major normative minerals**: minerals greater than `1 cat%`, from highest to lowest, without a duplicate `Plag`. Entries greater than `10 cat%` are highlighted.
6. **Warnings**: the current implementation writes a warning when the iron valence is estimated from FeOT.

## Calculation procedure

### 1. Cation basis

Each oxide is first converted to a cation amount: `(wt% / molar mass) × number of cations in the formula`. The cation count is 1 for SiO₂, TiO₂, FeO, MnO, MgO, CaO, CO₂, F, and S, and 2 for Al₂O₃, Fe₂O₃, Na₂O, K₂O, and P₂O₅.

All cations are summed and scaled to 100, which is the allocation pool for the Catanorm. If the total is near 0, the row fails and reports that the cation total is insufficient. This step does **not** normalize oxide wt% to 100 again. Unfilled H₂O and loss on ignition do not enter this 100.

### 2. Accessory minerals and salts

Mn is combined into Fe²⁺ first. Amounts are then in cations:

| Order | Mineral | Stoichiometry |
| --- | --- | --- |
| 1 | Calcite `Cc` | Mineral amount = `2 × CO2`, and an equal amount of Ca is consumed |
| 2 | Apatite `Ap` | When P is low relative to F, `Ap = 3P` and part of the F is consumed; otherwise all remaining F is incorporated into apatite |
| 3 | Fluorite `Fr` | Remaining F: `Fr = 1.5 × F`, and `0.5 × F` of Ca is consumed |
| 4 | Pyrite `Py` | `Py = 1.5 × S`, and `0.5 × S` of Fe²⁺ is consumed |
| 5 | Ilmenite `Il` | The smaller of Ti and Fe²⁺ sets the yield, and the mineral amount is twice that quantity |

CO₂, P, F, and S claim Ca or Fe²⁺ first. When these volatiles are much higher than the cations that can combine with them, later Ca–Mg minerals will be distorted. Check them against the mineral total and the desilication record.

### 3. Feldspars, feldspathoid precursors, and titanium minerals

The allocation unit is cations. When the end-member is complete, orthoclase, albite, and anorthite are each a group of 5 cations.

1. K with Al and Si forms orthoclase `Or` (each K is paired with 1 Al and 3 Si). Remaining K forms potassium metasilicate `Ks`.
2. Na with Al and Si forms albite `Ab`. Remaining Na then forms acmite `Ac` with Fe³⁺ (each Na is paired with 1 Fe³⁺ and 2 Si). Na that still remains forms sodium metasilicate `Ns`.
3. Remaining Al and Ca form anorthite `An`. When Ca is insufficient, Al is limiting: each limiting Al corresponds to 0.5 Ca and 1 Si.
4. Remaining Ti with Ca and Si forms titanite `Tn` (3 cations). Ti that still remains is rutile `Ru`.
5. Al that remains at the end is corundum `C`.

### 4. Iron oxides and pyroxene

Fe³⁺ and Fe²⁺ form magnetite `Mt`: when Fe³⁺ does not exceed twice Fe²⁺, `Mt = 1.5 × Fe³⁺`. Remaining Fe³⁺ is hematite `Hm`.

Remaining Ca, Mg, and Fe²⁺ become wollastonite `Wo`, the enstatite molecule, and the ferrosilite molecule. Each is twice the cation count, and each consumes an equal share of Si. The enstatite and ferrosilite molecules sum to orthopyroxene `Hy`. The magnesium number is calculated at this point.

`Wo` and `Hy` then combine in equal cations to form diopside `Di`: the smaller amount enters `Di` entirely, and the cation count of `Di` is twice that amount. The larger amount remains as `Wo` or `Hy`.

### 5. Quartz, or seven desilication steps

When remaining Si ≥ 0, all of it becomes quartz `Q`, and the calculation ends.

When Si < 0, the deficit is `D = −Si`. Each step below either reduces the deficit to 0 and stops, or uses up that mineral and continues to the next step. The coefficients are cation percentages:

| Step | Reaction | For each 1 part of Si deficit removed |
| --- | --- | --- |
| 1 | Orthopyroxene → olivine | Consume 4 `Hy` and produce 3 `Ol` |
| 2 | Titanite → perovskite | Consume 3 `Tn` and produce 2 `Pf` |
| 3 | Albite → nepheline | Consume 2.5 `Ab` and produce 1.5 `Ne` |
| 4 | Orthoclase → leucite | Consume 5 `Or` and produce 4 `Lc` |
| 5 | Leucite → kaliophilite | Consume 4 `Lc` and produce 3 `Kp` |
| 6 | Wollastonite → rankinite | Consume 4 `Wo` and produce 3 `Cs` |
| 7 | Diopside → rankinite + olivine | Consume 4 `Di` and produce 1.5 `Cs` and 1.5 `Ol` |

The product of step 5 is `Kp`. After desilication, plagioclase is `Plag = Ab + An`, and silica saturation is then judged by the rules above.

## References

- Niggli, P. (1948). *Gesteine und Minerallagerstätten*. Birkhäuser, Basel.
- Barth, T. F. W. (1948). The distribution of oxygen in the lithosphere. *The Journal of Geology*, 56(1), 41–49.
- Janoušek, V., Farrow, C. M., Erban, V. (2006). Interpretation of whole-rock geochemical data in igneous geochemistry: introducing Geochemical Data Toolkit (GCDkit). *Journal of Petrology*, 47(6), 1255–1259.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
- GCDkit: [https://www.gcdkit.org/](https://www.gcdkit.org/)
