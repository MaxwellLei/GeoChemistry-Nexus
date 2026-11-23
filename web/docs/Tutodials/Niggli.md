---
sidebar_position: 6
---

# Niggli Norm Calculation

The **Niggli norm** module returns two results for each whole-rock analysis:

1. **Niggli numbers**: `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, and `qz`. `ti` and `p` are calculated as well and shown in the calculation details.
2. **Barth–Niggli molecular norm (Catanorm)**: normative minerals in cation percent (cat%), end-member ratios, and the desilication steps used when silica is short.

Paul Niggli (1948) and Tom F. W. Barth (1948) built the catanorm on cation equivalents. Iron and other heavy elements therefore do not weigh the result the way they do in a CIPW weight-percent norm, and the numbers sit closer to the volume relations seen in thin section. For the weight-percent norm, see [CIPW Norm Mineral Calculation](./CIPW).

The implementation follows the GCDkit molecular-norm sequence and records the leucite desilication product as kaliophilite `Kp`.

![Niggli norm page](/img/v0.9.0/niggli-result.png)

## Interface

The toolbar matches CIPW: **Fe³⁺/ΣFe**, **Help**, **Example**, **Export**, **Clear**, and **Calculate**. Fe³⁺/ΣFe accepts `0`–`1` and defaults to `0.15` (Le Maitre, 2002).

**Example** fills three rows: granite, basalt, and phonolite. The phonolite row is there to show a strongly undersaturated result.

The sheet is arranged left to right:

| Area | Group | Unit | Contents |
| --- | --- | --- | --- |
| Sample | — | — | Sample name. It is not used in the calculation and stays visible while scrolling right |
| Oxides | Major elements; Other oxides / volatiles | wt% | Input |
| Niggli numbers | Niggli values | dimensionless | `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz` |
| Diagnostics | Diagnostics | — | Silica saturation, alumina saturation, mineral sum |
| Norm | Normative minerals | cat% | Cation percent. The last column is `Sum` |

The sheet starts with 500 rows. Double-click the fill handle to fill downward. Editing an oxide after a calculation marks that row stale.

### Row status

| Dot | Meaning |
| --- | --- |
| Gray ring | Input is present and has not been calculated |
| Teal | Succeeded with no warning |
| Orange | Succeeded, with iron estimated from FeOT |
| Red | Failed. The diagnostic columns contain the error |
| Orange ring | Oxides were edited after the last calculation |

A row with both measured FeO and Fe₂O₃ succeeds without an iron warning. A row that uses FeOT is marked with the estimation warning.

## Input

Each row is one sample, in oxide weight percent. Only values greater than 0 are read. Blanks and zeros count as not provided.

Major elements, through `K2O`:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`

Other oxides / volatiles:

`CO2`, `P2O5`, `F`, `S`

`ZrO2`, `Cr2O3`, `Cl`, and `SO3` are not read here. Use the CIPW module when those components must enter the norm.

### Iron

Iron is partitioned before the cation conversion. Total iron is converted to Fe atoms with the FeO molar mass, split by Fe³⁺/ΣFe, and the ferric half is written back as Fe₂O₃.

| What is filled in | Treatment |
| --- | --- |
| Both `Fe2O3` and `FeO` | Use the measured pair. A neighboring `FeOT` is left unused |
| `FeOT`, without both measured valence columns | Split `FeOT` with the current ratio and add a warning |
| `Fe2O3` only | Treat that Fe₂O₃ as total iron and split it |
| `FeO` only | Treat that FeO as total iron and split it |

A single measured valence column therefore changes when the toolbar ratio changes. Enter both FeO and Fe₂O₃ when both were analyzed.

`MnO` stays through the cation conversion. It enters `fm` and `mg`, then joins Fe²⁺ inside the catanorm.

## Recommended workflow

1. Enter the sample name and oxide wt%.
2. Set **Fe³⁺/ΣFe** when the analysis has only one iron column or only `FeOT`.
3. Click **Calculate**.
4. Read the Niggli numbers and the three diagnostic columns, then the cat% minerals. Minerals above `0.001 cat%` are written to 3 decimal places.
5. Select the row and open **Calculation details** for `ti`, `p`, end-members, and the desilication log. The panel can be copied or exported as its own CSV.
6. Use **Export** for the whole sheet. The default name is `Niggli_Results.csv`, encoded as UTF-8. The separator column is written as `---`.

## Niggli numbers

The numbers use oxide **molecules**, `n = wt% / molar mass`. Here one Al₂O₃ molecule counts as 1. In the catanorm below, one Al₂O₃ formula counts as 2 Al cations. The two bases are different.

```text
al  = n(Al2O3)
fm  = n(FeO) + 2 n(Fe2O3) + n(MnO) + n(MgO)
c   = n(CaO)
alk = n(Na2O) + n(K2O)
base = al + fm + c + alk

si  = 100 × n(SiO2) / base
al, fm, c, and alk are likewise scaled so that they sum to 100
ti  = 100 × n(TiO2) / base
p   = 100 × n(P2O5) / base
k   = n(K2O) / (n(Na2O) + n(K2O))
mg  = n(MgO) / (n(MgO) + n(FeO) + 2 n(Fe2O3) + n(MnO))
```

Quartz index:

```text
when al > alk:   qz = si − (100 + 4 al)
when al ≤ alk:   qz = si − (100 + 3 al + alk)
```

The second expression is used when alumina does not exceed alkalis. The in-app Help window shows only the first expression.

`si`, `al`, `fm`, `c`, `alk`, `ti`, `p`, and `qz` are rounded to 2 decimal places. `k` and `mg` are rounded to 3. The sheet shows the eight named columns; `ti` and `p` appear in the details. A positive `qz` is prefixed with a plus sign in the details.

`al + fm + c + alk` is 100. `si` may be much larger than 100 because silica is outside that sum. `k` and `mg` are ratios from 0 to 1.

## Diagnostic columns

| Column | Meaning |
| --- | --- |
| **Silica saturation** | Oversaturated, saturated, or undersaturated |
| **Alumina saturation** | Peraluminous, metaluminous, or peralkaline |
| **Mineral sum** | Sum of the catanorm cation percent, to 2 decimal places |

Silica saturation is read from the finished minerals, with a cutoff of `0.001 cat%`:

- Quartz `Q` present: oversaturated.
- No quartz, but nepheline `Ne`, leucite `Lc`, kaliophilite `Kp`, or olivine `Ol`: undersaturated.
- Otherwise: saturated.

The sign of `qz` describes silica against the Niggli base. The saturation label describes the normative minerals. Compare them with that difference in mind.

Alumina saturation uses the normalized cations, before minerals are built:

- **Peraluminous**: Al > Na + K + Ca
- **Metaluminous**: Na + K < Al ≤ Na + K + Ca
- **Peralkaline**: Al ≤ Na + K

## Normative minerals

Mineral columns are cation percent. `Plag` equals `Ab + An`. `Sum` includes `Plag` and does not add `Ab` and `An` again.

| Symbol | Mineral | Symbol | Mineral |
| --- | --- | --- | --- |
| `Q` | Quartz | `Hy` | Hypersthene |
| `C` | Corundum | `Ol` | Olivine |
| `Or` | Orthoclase | `Cs` | Calcium orthosilicate |
| `Plag` | Plagioclase | `Wo` | Wollastonite |
| `Ab` | Albite | `Mt` | Magnetite |
| `An` | Anorthite | `Hm` | Hematite |
| `Lc` | Leucite | `Il` | Ilmenite |
| `Ne` | Nepheline | `Tn` | Titanite |
| `Kp` | Kaliophilite | `Pf` | Perovskite |
| `Ac` | Acmite | `Ru` | Rutile |
| `Ns` | Sodium metasilicate | `Ap` | Apatite |
| `Ks` | Potassium metasilicate | `Fr` | Fluorite |
| `Di` | Diopside | `Py` | Pyrite |
| | | `Cc` | Calcite |

End-member ratios are written only in the calculation details.

## Calculation details

A successful row shows six sections:

1. **Classification**: silica saturation, alumina saturation, and the mineral sum. Undersaturated silica and peralkaline alumina are highlighted.
2. **Niggli numbers**: `si`, `al`, `fm`, `c`, `alk`, `ti`, `p`, `k`, `mg`, `qz`.
3. **End-members**, when the host mineral is present:
   - Plagioclase `An% = 100 × An / (Ab + An)`
   - Diopside is Wo 50%, with the other 50% split into En and Fs by the magnesium ratio
   - Olivine Fo and Fa. Fo equals the magnesium index below
   - Magnesium index `Mgr = 100 × En / (En + Fs)`, taken before hypersthene is combined into diopside
4. **Desilication steps**: quartz cat% when silica remains; otherwise each mineral consumed and whether the deficit reached zero.
5. **Main normative minerals**: minerals above `1 cat%`, largest first, excluding the duplicate `Plag` entry. Entries above `10 cat%` are highlighted.
6. **Warnings**: the current calculator warns when iron valences are estimated from FeOT.

## Calculation

### 1. Cation basis

Each oxide becomes a cation amount: `(wt% / molar mass) × cations in the formula`. SiO₂, TiO₂, FeO, MnO, MgO, CaO, CO₂, F, and S contribute 1. Al₂O₃, Fe₂O₃, Na₂O, K₂O, and P₂O₅ contribute 2.

The cation amounts are scaled to 100. That pool is the catanorm basis. A sum near zero fails the row. Oxide weight percent is not normalized to 100 in this module, so H₂O and loss on ignition stay outside the 100 when they were not entered.

### 2. Accessories and salts

Mn is added to Fe²⁺. The cation accounting is then:

| Order | Mineral | Accounting |
| --- | --- | --- |
| 1 | Calcite `Cc` | Amount = `2 × CO2`, consuming an equal amount of Ca |
| 2 | Apatite `Ap` | When P is small relative to F, `Ap = 3P` and part of the F is consumed. Otherwise the remaining F is taken into apatite |
| 3 | Fluorite `Fr` | Remaining F: `Fr = 1.5 × F`, consuming `0.5 × F` of Ca |
| 4 | Pyrite `Py` | `Py = 1.5 × S`, consuming `0.5 × S` of Fe²⁺ |
| 5 | Ilmenite `Il` | The smaller of Ti and Fe²⁺ sets the amount; the mineral is twice that quantity |

CO₂, P, F, and S take Ca or Fe²⁺ first. When those volatiles exceed the cations that can bind them, later calcic and mafic minerals are no longer meaningful; check the mineral sum and the desilication log.

### 3. Feldspars and titanium minerals

Orthoclase, albite, and anorthite are groups of 5 cations when the limiting cation is abundant.

1. K, Al, and Si form orthoclase `Or` (1 Al and 3 Si for each K). Leftover K forms potassium metasilicate `Ks`.
2. Na, Al, and Si form albite `Ab`. Leftover Na and Fe³⁺ form acmite `Ac` (1 Fe³⁺ and 2 Si for each Na). Sodium still left forms sodium metasilicate `Ns`.
3. Remaining Al and Ca form anorthite `An`. When Ca is short, Al is limiting: each limiting Al takes 0.5 Ca and 1 Si.
4. Remaining Ti, Ca, and Si form titanite `Tn` (3 cations). Ti still left is rutile `Ru`.
5. Al still left is corundum `C`.

### 4. Iron oxides and pyroxene

Fe³⁺ and Fe²⁺ form magnetite `Mt`. While Fe³⁺ is at most twice Fe²⁺, `Mt = 1.5 × Fe³⁺`. Leftover Fe³⁺ is hematite `Hm`.

Leftover Ca, Mg, and Fe²⁺ become wollastonite `Wo`, an enstatite molecule, and a ferrosilite molecule. Each mineral amount is twice the cation amount, and Si decreases by the original cation amount. Enstatite plus ferrosilite is hypersthene `Hy`. The magnesium index is stored here.

`Wo` and `Hy` then combine in equal cation portions into diopside `Di`. The smaller side is used up, `Di` is twice that portion, and the larger side remains as `Wo` or `Hy`.

### 5. Quartz, or seven desilication steps

When leftover Si is zero or positive, it becomes quartz `Q` and the norm stops.

When Si is negative, the deficit is `D = −Si`. Each step either removes the deficit and stops, or exhausts that mineral and continues. The coefficients are cation percent:

| Step | Reaction | Per 1 unit of silica deficit removed |
| --- | --- | --- |
| 1 | Hypersthene → olivine | Consume 4 `Hy`, produce 3 `Ol` |
| 2 | Titanite → perovskite | Consume 3 `Tn`, produce 2 `Pf` |
| 3 | Albite → nepheline | Consume 2.5 `Ab`, produce 1.5 `Ne` |
| 4 | Orthoclase → leucite | Consume 5 `Or`, produce 4 `Lc` |
| 5 | Leucite → kaliophilite | Consume 4 `Lc`, produce 3 `Kp` |
| 6 | Wollastonite → calcium orthosilicate | Consume 4 `Wo`, produce 3 `Cs` |
| 7 | Diopside → calcium orthosilicate + olivine | Consume 4 `Di`, produce 1.5 `Cs` and 1.5 `Ol` |

Step 5 writes the product to `Kp`. After desilication, `Plag = Ab + An`, and silica saturation is assigned by the rule above.

## Reading this next to CIPW

The same major-element row can be pasted into both modules. Keep the units separate:

- CIPW minerals are **wt%**. That module also normalizes the entered oxides to an anhydrous 100% and can carry zircon, chromite, halite, and thenardite.
- Minerals here are **cat%**. The Niggli numbers use oxide molecules. `P2O5` is grouped with the other oxides and volatiles.
- With only FeO or only Fe₂O₃, the iron assumptions differ. CIPW sets the missing valence to 0. This module treats the single column as total iron and splits it. For a direct comparison, supply both FeO and Fe₂O₃, or supply the same FeOT and the same Fe³⁺/ΣFe to both modules.

## References

- Niggli, P. (1948). *Gesteine und Minerallagerstätten*. Birkhäuser, Basel.
- Barth, T. F. W. (1948). The distribution of oxygen in the lithosphere. *The Journal of Geology*, 56(1), 41–49.
- Janoušek, V., Farrow, C. M., Erban, V. (2006). Interpretation of whole-rock geochemical data in igneous geochemistry: introducing Geochemical Data Toolkit (GCDkit). *Journal of Petrology*, 47(6), 1255–1259.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
- GCDkit: [https://www.gcdkit.org/](https://www.gcdkit.org/)
