---
sidebar_position: 5
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Calcul de la norme CIPW

**Le calcul de la norme CIPW** convertit les oxydes des éléments majeurs d'une roche totale (wt%) en un ensemble de minéraux normatifs idéalisés. Les résultats servent à la comparaison des roches ignées, ainsi qu'à la discrimination de la saturation en silice et de la saturation en alumine. La norme CIPW a été proposée en 1902 par Cross, Iddings, Pirsson et Washington. Ce module calcule par lots dans un tableau de type Excel, et fournit, pour chaque ligne, un diagnostic et un bilan de masse.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/cipw.webp')}
    alt="Interface CIPW"
    style={{width: '100%'}}
  />
</div>

## Interface

La page se compose d'une barre supérieure et d'une feuille de données. À gauche de la barre supérieure se trouve **Fe³⁺/ΣFe** ; à droite se trouvent **Notice**, **Exemple**, **Exporter**, **Vider** et **Exécuter le calcul**.

La feuille se divise, de gauche à droite, en quatre sections. Au-dessus des en-têtes de colonnes figurent les noms de groupes :

| Zone | Groupe | Unité | Rôle |
| --- | --- | --- | --- |
| Échantillon | — | — | Nom de l'échantillon. Il sert seulement d'identifiant et ne participe pas au calcul. Cette colonne est figée et reste visible lors du défilement horizontal |
| Oxydes | Éléments majeurs ; autres oxydes / volatils | wt% | Zone de saisie blanche |
| Diagnostic | Diagnostic | — | `saturation en SiO2`, `état de Al2O3`, `erreur de bilan de masse (%)` |
| Minéraux normatifs | Minéraux normatifs CIPW | wt% | Zone de résultats en teinte claire. L'en-tête associe l'abréviation et le nom, par exemple `Q(quartz)` |

Une étroite colonne de séparation se trouve entre la zone de saisie et la zone de résultats. Les colonnes de résultats sont vides avant le calcul. Le tableau propose au départ 500 lignes. Un double-clic sur la poignée de recopie remplit vers le bas, comme dans Excel.

### Barre supérieure

| Contrôle | Rôle |
| --- | --- |
| **Fe³⁺/ΣFe** | Seulement lorsque le fer total `FeOT` est utilisé, répartit FeO et Fe₂O₃ selon ce rapport. Plage `0`–`1`, valeur par défaut `0.15` (Le Maitre, 2002), affichée avec deux décimales |
| **Notice** | Ouvre l'explication de l'algorithme dans le logiciel |
| **Exemple** | Remplit trois lignes d'exemple : granite, basalte et andésite, avec Fe₂O₃, FeO et MnO mesurés |
| **Exporter** | Enregistre le tableau actuel en CSV UTF-8. Le nom de fichier par défaut est `CIPW_Results.csv` |
| **Vider** | Après confirmation, efface toutes les saisies, les résultats et le détail du calcul |
| **Exécuter le calcul** | Calcule toutes les lignes qui contiennent des oxydes valides |

### État de l'en-tête de ligne

Le point à côté du numéro de ligne indique l'état de la ligne :

| Point | Signification |
| --- | --- |
| Creux gris | Des données ont été saisies, mais le calcul n'a pas encore été fait |
| Plein vert sarcelle | Calcul réussi, sans avertissement, et bilan de masse également validé |
| Plein orange | Calcul réussi, mais il existe un avertissement, ou l'erreur de bilan de masse est supérieure à `1×10⁻⁶` |
| Plein rouge | Le calcul de cette ligne a échoué. La colonne de diagnostic indique « erreur » et la cause |
| Creux orange | Les oxydes ont été modifiés après le calcul : le résultat actuel est périmé |

Les données d'exemple contiennent toutes du MnO. MnO est incorporé à FeO et produit un avertissement ; les lignes d'exemple sont donc orange après le calcul, et c'est le résultat attendu. Modifier le nom de l'échantillon ne périme pas le résultat ; modifier n'importe quelle saisie d'oxyde le périme.

## Saisie

Chaque ligne est un échantillon de roche totale. On y indique le pourcentage massique des oxydes (wt%). Le logiciel ne lit que les cellules **supérieures à 0** ; une cellule vide et un 0 sont tous deux considérés comme non fournis. Les lignes vides sont ignorées automatiquement.

On peut coller directement depuis un tableur. L'ordre des colonnes est fixe : lors du collage, alignez-vous sur les en-têtes ci-dessous.

### Colonnes d'oxydes

Éléments majeurs :

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`, `P2O5`

Autres oxydes / volatils :

`ZrO2`, `Cr2O3`, `CO2`, `S`, `F`, `Cl`, `SO3`

`FeOT` est le fer total exprimé en équivalent FeO. `S`, `F` et `Cl` se saisissent en pourcentage massique de l'élément.

### Traitement du fer

| Situation de saisie | Traitement |
| --- | --- |
| `FeO` et `Fe2O3` sont tous deux présents, sans `FeOT` | La répartition mesurée est utilisée. Le mode de traitement du fer est « valeur mesurée » |
| Seulement `FeO`, ou seulement `Fe2O3` | L'élément manquant est pris égal à 0. Le mode est « hypothèse partielle », et un avertissement est émis |
| Seulement `FeOT` | Répartition selon Fe³⁺/ΣFe. Le mode est « estimation à partir de FeOT » |
| `FeOT` apparaît en même temps que `FeO` ou `Fe2O3` | `FeOT` est ignoré. Le mode est « entrée incohérente », et un avertissement est émis |
| Aucune donnée de fer | FeO et Fe₂O₃ sont tous deux pris égaux à 0. Le mode est « manquant », et un avertissement est émis |

Lorsque seul `FeOT` est présent, la répartition est :

```text
Fe2O3 = FeOT × (Fe³⁺/ΣFe) × 1.11134
FeO   = FeOT × (1 − Fe³⁺/ΣFe)
```

Le coefficient `1.11134` correspond au rapport des masses molaires de Fe₂O₃ et de deux fois FeO. Il sert à ramener le fer ferrique, exprimé en équivalent FeO, au pourcentage massique de Fe₂O₃.

Avant le calcul, `MnO` est converti en équivalent FeO selon le rapport des masses molaires, puis ajouté à FeO :

```text
équivalent FeO = MnO × 71.844 / 70.937
```

Dès que MnO est renseigné, le détail du calcul indique « MnO a été converti en équivalent FeO selon le rapport molaire ».

## Résultats

### Colonnes de diagnostic

| Colonne | Valeurs |
| --- | --- |
| **Saturation en SiO2** | sursaturé, saturé, sous-saturé |
| **État de Al2O3** | peralcalin, métalumineux, peralumineux |
| **Erreur de bilan de masse (%)** | Écart de fermeture entre la masse des minéraux et les oxydes saisis. En dessous de `0.01`, la notation scientifique est utilisée ; sinon, 4 décimales sont conservées |

Signification des trois saturations :

- **Sursaturé** : il reste du SiO₂ à la fin de la répartition. Il est porté en quartz `Q`.
- **Saturé** : le SiO₂ est exactement épuisé. Il n'y a ni quartz, ni besoin de désilication.
- **Sous-saturé** : le SiO₂ est insuffisant, et l'on entre dans les réactions de désilication ci-dessous. Si toute la chaîne de réactions ne parvient pas à éliminer le déficit de silice, un avertissement supplémentaire est ajouté, qui indique le déficit résiduel (mol/100 g). Les minéraux normatifs sont alors peu fiables.

L'état de saturation en alumine est jugé avant la répartition des feldspaths, à partir du nombre de moles d'oxydes encore disponibles à ce moment :

- **Peralcalin** : Na₂O + K₂O > Al₂O₃
- **Peralumineux** : Al₂O₃ / (CaO + Na₂O + K₂O) > 1
- **Métalumineux** : les autres cas

L'apatite, la calcite, la fluorite, la halite et la thénardite ont déjà consommé le CaO ou le Na₂O correspondants ; on utilise donc ici les quantités restantes.

### Minéraux normatifs

Les résultats sont rangés dans l'ordre ci-dessous. Les noms correspondent aux en-têtes du logiciel.

| Abréviation | Minéral | Abréviation | Minéral |
| --- | --- | --- | --- |
| `Q` | quartz | `Fo` | forstérite |
| `Or` | orthose | `Fa` | fayalite |
| `Ab` | albite | `Mt` | magnétite |
| `An` | anorthite | `Hm` | hématite |
| `Le` | leucite | `Ilm` | ilménite |
| `Ne` | néphéline | `Cm` | chromite |
| `Kp` | pseudoleucite | `Ru` | rutile |
| `Cor` | corindon | `Tn` | titanite |
| `Ac` | aegyrine | `Pf` | pérovskite |
| `Di` | diopside | `Z` | zircon |
| `Hd` | hédenbergite | `Ap` | apatite |
| `Wo` | wollastonite | `Cc` | calcite |
| `Cs` | larnite | `Py` | pyrite |
| `En` | enstatite | `Fl` | fluorite |
| `Fs` | ferrosilite | `Hl` | halite |
| `ns` | métasilicate de sodium | `Th` | thénardite |
| `ks` | métasilicate de potassium | | |

L'orthose, l'albite et l'aegyrine sont comptées en interne en unités de formule : 1 mol de K₂O produit 2 mol de `Or`, et 1 mol de Na₂O produit 2 mol de `Ab` ou de `Ac`. Ce qui est écrit est toujours le pourcentage massique.

## Détail du calcul

Lorsqu'une ligne calculée avec succès est sélectionnée, l'inspecteur de droite affiche :

- Le nom de l'échantillon. Si la colonne d'échantillon est vide, « ligne N » s'affiche.
- La saturation en silice et la saturation en alumine. En cas de sous-saturation, la saturation en silice est mise en évidence.
- Le **mode de traitement du fer**. Il est mis en évidence dès qu'il n'est pas « valeur mesurée ».
- L'**erreur de bilan de masse**, conservée avec 6 décimales. Elle est mise en évidence lorsqu'elle dépasse `1×10⁻⁶`.
- Le **total des minéraux**, c'est-à-dire la somme des wt% des minéraux normatifs, conservée avec 4 décimales. L'oxygène libéré lors de la formation de la pyrite, de la fluorite et de la halite n'entre pas dans ce total, mais il entre dans le bilan de masse.
- Les **minéraux principaux** : minéraux dont la teneur est supérieure à `0.01 wt%`, rangés du plus abondant au moins abondant ; les entrées supérieures à `10 wt%` sont mises en évidence.
- Les **avertissements** : données de fer manquantes ou incohérentes, estimation à partir de FeOT, conversion de MnO, déficit résiduel de silice, etc.

Le coin supérieur droit de l'inspecteur permet d'exporter ce détail en CSV, ou de le copier dans le presse-papiers.

## Déroulement du calcul

L'ordre réel du code de calcul actuel est décrit ci-dessous. L'apatite occupe le calcium en premier, puis seulement se forment les minéraux de volatils ; la titanite et le rutile sont placés après l'anorthite.

Les masses molaires des minéraux sont toutes déduites des masses molaires des oxydes selon la formule chimique, afin d'éviter qu'un arrondi manuel ne déséquilibre la masse. La tolérance numérique utilisée pour le calcul est `1×10⁻¹²`. Si une étape rend nettement négatif un oxyde autre que SiO₂, la ligne échoue.

### 1. Normalisation anhydre, fer et manganèse

Tous les oxydes fournis et non négatifs sont normalisés à 100 %. Le fer est traité comme dans la section précédente, puis MnO est converti en équivalent FeO, et l'on **normalise une seconde fois**. Si le total n'est pas supérieur à 0, la ligne échoue.

### 2. Conversion en nombre de moles

Le nombre de moles de chaque oxyde = wt% normalisé / masse molaire. La répartition ultérieure se fait entièrement à l'échelle mol/100 g.

### 3. Apatite

L'apatite occupe le calcium en premier. 1 mol de `P2O5` se combine à `10/3` mol de `CaO` (sans OH ni F) :

```text
Ap = min(P2O5, CaO / (10/3))
```

### 4. Minéraux de volatils

Dans l'ordre ci-dessous, on en forme autant que la combinaison le permet :

| Minéral | Stœchiométrie |
| --- | --- |
| calcite `Cc` | `min(CO2, CaO)` |
| fluorite `Fl` | `min(F/2, CaO)`, c'est-à-dire que 1 mol de CaO se combine à 2 mol de F |
| pyrite `Py` | `min(S/2, FeO)`, c'est-à-dire que 1 mol de FeO se combine à 2 mol de S |
| halite `Hl` | `min(Cl, 2×Na2O)` |
| thénardite `Th` | `min(SO3, Na2O)` |

### 5. Zircon, chromite et ilménite

- Zircon `Z` : la totalité de `ZrO2` et une quantité égale de `SiO2`.
- Chromite `Cm` : `min(Cr2O3, FeO)`.
- Ilménite `Ilm` : `min(TiO2, FeO)`.

Cette étape ne consomme que le titane qui peut se combiner à FeO. Le TiO₂ restant est reporté après l'anorthite.

### 6. Feldspaths, corindon et alcalins restants

On enregistre d'abord l'état de saturation en alumine, puis on répartit :

| Minéral | Conditions et produits |
| --- | --- |
| orthose `Or` | `min(K2O, Al2O3)`, consomme 6 fois SiO₂, et le nombre de moles du produit est le double de cette valeur |
| albite `Ab` | `min(Na2O, Al2O3)`, consomme également 6 fois SiO₂, et le produit est le double |
| anorthite `An` | `min(CaO, Al2O3)`, consomme 2 fois SiO₂ |
| corindon `Cor` | Al₂O₃ restant |
| aegyrine `Ac` | `min(Na2O restant, Fe2O3)`, consomme 4 fois SiO₂, et le produit est le double |
| métasilicate de sodium `ns` | Na₂O restant et une quantité égale de SiO₂ |
| métasilicate de potassium `ks` | K₂O restant et une quantité égale de SiO₂ |

Pendant l'étape des feldspaths, SiO₂ peut d'abord être compté négatif : la désilication ultérieure le traitera de façon unifiée.

### 7. Titanite et rutile

La titanite n'utilise que le CaO restant après l'anorthite, et **le reste de SiO₂ du moment ne sert pas à limiter la titanite** :

```text
Tn = min(TiO2 restant, CaO restant)
```

`Tn` consomme en même temps une quantité égale de SiO₂. Le TiO₂ qui reste encore à la fin est porté en rutile `Ru`.

### 8. Oxydes de fer

```text
Mt = min(Fe2O3, FeO)
Hm = Fe2O3 restant
```

### 9. Pyroxènes et wollastonite

On pose `MgFe = MgO + FeO`. La quantité totale de clinopyroxène est `min(CaO, MgFe)`, puis elle est scindée, selon le rapport magnésium/fer, en diopside `Di` et en hédenbergite `Hd`. Chaque part de pyroxène consomme 2 parts de SiO₂.

Le MgO et le FeO restants deviennent entièrement de l'enstatite `En` et de la ferrosilite `Fs`, qui consomment chacune 1 part de SiO₂. Une fois le magnésium et le fer épuisés, s'il reste du CaO, il devient entièrement de la wollastonite `Wo`.

### 10. Saturation en silice et désilication

L'ordre de désilication est : hypersthène → titanite → albite → orthose → wollastonite → diopside/hédenbergite → leucite. Chaque étape ne consomme que le SiO₂ encore manquant à ce moment.

| Ordre | Réaction | SiO₂ libéré |
| --- | --- | --- |
| 1 | 2 enstatite/ferrosilite → 1 forstérite/fayalite | 1 mol libérée pour 2 mol de pyroxène |
| 2 | titanite `Tn` → pérovskite `Pf` | 1 mol de `Tn` libère 1 mol |
| 3 | albite `Ab` → néphéline `Ne` | 1 mol de `Ab` libère 2 mol |
| 4 | orthose `Or` → leucite `Le` | 1 mol de `Or` libère 1 mol |
| 5 | 2 wollastonite `Wo` → 1 larnite `Cs` | 1 mol libérée pour 2 mol de `Wo` |
| 6 | diopside/hédenbergite → larnite + olivine | 1 mol de pyroxène libère 1 mol ; le magnésium entre dans `Fo`, le fer entre dans `Fa` |
| 7 | leucite `Le` → pseudoleucite `Kp` | 1 mol de `Le` libère 1 mol |

Lorsqu'il reste du SiO₂, du quartz se forme, et l'état est sursaturé. Lorsque SiO₂ est exactement nul, l'état est saturé. L'entrée dans le tableau ci-dessus correspond à l'état sous-saturé.

### 11. Pourcentage massique et bilan de masse

La masse d'un minéral normatif = nombre de moles × masse molaire déduite de la formule chimique, puis convertie en wt% par rapport au total des oxydes normalisés.

Dans la pyrite, la fluorite et la halite, S, F et Cl ont remplacé l'oxygène des oxydes. L'oxygène libéré n'entre dans aucun minéral ; on le rajoute lors du bilan de masse :

```text
nombre de moles d'oxygène libéré = Fl + Py + Hl / 2
erreur de bilan de masse = |100 − (somme des wt% des minéraux + wt% de l'oxygène libéré)|
```

Lorsque l'erreur est supérieure à `1×10⁻⁶`, la ligne est marquée d'un avertissement.

## Références

- Cross, W., Iddings, J. P., Pirsson, L. V., Washington, H. S. (1902). A quantitative chemico-mineralogical classification and nomenclature of igneous rocks. *The Journal of Geology*, 10, 555–690.
- Kelsey, C. H. (1965). Calculation of the C.I.P.W. norm. *Mineralogical Magazine*, 34, 276–282.
- Verma, S. P., Torres-Alvarado, I. S., Velasco-Tapia, F. (2003). A revised CIPW norm. *Schweizerische Mineralogische und Petrographische Mitteilungen*, 83, 197–216.
- Philpotts, A. R., Ague, J. J. (2009). *Principles of Igneous and Metamorphic Petrology* (2nd ed.). Cambridge University Press.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
