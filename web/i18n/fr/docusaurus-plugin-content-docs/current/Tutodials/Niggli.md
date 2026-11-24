---
sidebar_position: 6
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Calcul de la norme de Niggli

**Le calcul de la norme de Niggli** produit, pour une même ligne d'oxydes en roche totale, deux jeux de résultats :

1. **Paramètres de Niggli** : `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz`, ainsi que `ti` et `p`, qui ne s'affichent que dans le détail du calcul.
2. **Norme moléculaire de Barth–Niggli (Catanorm)** : minéraux normatifs exprimés en pourcentage cationique (cat%), composition des pôles, et parcours de désilication lorsque la silice est insuffisante.

Catanorm a été établie par Paul Niggli (1948) et Tom F. W. Barth (1948). Elle répartit selon les équivalents cationiques et réduit le poids qu'occupent, dans le pourcentage massique, les éléments lourds tels que le fer. Elle se rapproche donc davantage, que les wt% de la norme CIPW, des relations de volume des minéraux en lame mince. Pour les minéraux normatifs en pourcentage massique, voir [Calcul de la norme CIPW](./CIPW).

L'algorithme s'inspire du processus de norme moléculaire de GCDkit et a été réimplémenté. Le produit de la désilication supplémentaire de la leucite est porté dans la kaliophilite `Kp`.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/niggli.webp')}
    alt="Interface Niggli"
    style={{width: '100%'}}
  />
</div>

## Interface

La barre supérieure est la même que pour CIPW : **Fe³⁺/ΣFe**, **Notice**, **Exemple**, **Exporter**, **Vider**, **Exécuter le calcul**. La plage de Fe³⁺/ΣFe est `0`–`1`, et la valeur par défaut est `0.15` (Le Maitre, 2002).

**Exemple** remplit trois lignes : granite, basalte et phonolite (Phonolite). Les deux premières sont des roches volcaniques courantes, relativement riches en silice ; la phonolite sert à examiner des résultats sous-saturés, du type néphéline et leucite.

La feuille se divise, de gauche à droite, en cinq sections :

| Zone | Groupe | Unité | Contenu |
| --- | --- | --- | --- |
| Échantillon | — | — | Nom de l'échantillon. Il ne participe pas au calcul et reste visible lors du défilement horizontal |
| Oxydes | Éléments majeurs ; autres oxydes / volatils | wt% | Zone de saisie |
| Paramètres de Niggli | Paramètres de Niggli | sans dimension | `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz` |
| Diagnostic | Diagnostic | — | état de saturation en silice, état de saturation en alumine, somme des minéraux |
| Minéraux normatifs | Minéraux normatifs | cat% | Pourcentage cationique. La dernière colonne est `Sum(somme)` |

La colonne d'échantillon est figée. Le tableau compte au départ 500 lignes et permet de remplir vers le bas par un double-clic sur la poignée de recopie. Après une modification de la saisie, une ligne déjà calculée passe à l'état périmé : il faut cliquer de nouveau sur **Exécuter le calcul**.

### État de l'en-tête de ligne

| Point | Signification |
| --- | --- |
| Creux gris | Des données ont été saisies, mais le calcul n'a pas encore été fait |
| Plein vert sarcelle | Calcul réussi, sans avertissement |
| Plein orange | Calcul réussi, mais l'état d'oxydation du fer a été estimé à partir de FeOT |
| Plein rouge | Le calcul a échoué. La colonne de diagnostic indique « erreur de calcul » et la cause |
| Creux orange | Les oxydes ont été modifiés après le calcul : le résultat est périmé |

Un échantillon pour lequel FeO et Fe₂O₃ ont été saisis tous les deux porte un point vert sarcelle après un calcul réussi. Lorsqu'il n'y a que FeOT, un avertissement sur l'état d'oxydation du fer apparaît, et l'en-tête de ligne est orange.

## Saisie

Chaque ligne est un échantillon. L'unité est le pourcentage massique des oxydes (wt%). Seules les cellules supérieures à 0 sont lues ; une cellule vide et un 0 sont considérés comme non fournis.

Éléments majeurs, jusqu'à `K2O` inclus :

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`

Autres oxydes / volatils :

`CO2`, `P2O5`, `F`, `S`

Ce module ne lit pas `ZrO2`, `Cr2O3`, `Cl` ni `SO3`. Si ces quatre éléments doivent entrer dans les minéraux normatifs, utilisez le module CIPW.

### Traitement du fer

Le fer est traité avant la conversion en cations. Lors de la répartition, le fer est d'abord réduit à des atomes de Fe : le nombre de moles de fer total = masse du fer / masse molaire de FeO, puis il est séparé en fer ferrique et en fer ferreux selon Fe³⁺/ΣFe. La moitié du fer ferrique est écrite dans Fe₂O₃.

| Situation de saisie | Traitement |
| --- | --- |
| `Fe2O3` et `FeO` sont tous deux présents | Les valeurs mesurées sont utilisées. Même si `FeOT` figure à côté, il n'y a plus de répartition |
| `FeOT` est présent, et Fe₂O₃ et FeO ne sont pas tous deux disponibles | `FeOT` est réparti selon le rapport actuel, et un avertissement est écrit dans le détail |
| Seulement `Fe2O3`, sans `FeO` ni `FeOT` | Ce Fe₂O₃ est pris comme fer total, puis réparti selon le rapport en Fe₂O₃ et FeO |
| Seulement `FeO`, sans `Fe2O3` ni `FeOT` | Ce FeO est pris comme fer total, puis réparti selon le rapport en Fe₂O₃ et FeO |

Par conséquent, si seul FeO ou seul Fe₂O₃ a été dosé, le résultat varie avec le rapport de la barre supérieure. Si ces deux valeurs sont des mesures, remplissez les deux colonnes.

`MnO` est conservé jusqu'à la conversion en cations. Il entre dans `fm` et `mg` des paramètres de Niggli et, une fois dans Catanorm, il est incorporé à Fe²⁺.

## Paramètres de Niggli

Les paramètres se calculent à partir du **nombre moléculaire** des oxydes : nombre moléculaire = wt% / masse molaire. Ici, `al` est le nombre moléculaire de Al₂O₃ : une part de Al₂O₃ compte pour 1. Ce n'est pas la même base que dans Catanorm, plus loin, où « une part de Al₂O₃ compte pour 2 Al ».

```text
al  = n(Al2O3)
fm  = n(FeO) + 2 n(Fe2O3) + n(MnO) + n(MgO)
c   = n(CaO)
alk = n(Na2O) + n(K2O)
base = al + fm + c + alk

si  = 100 × n(SiO2) / base
al, fm, c et alk sont de même convertis en parts de base ramenées à 100
ti  = 100 × n(TiO2) / base
p   = 100 × n(P2O5) / base
k   = n(K2O) / (n(Na2O) + n(K2O))
mg  = n(MgO) / (n(MgO) + n(FeO) + 2 n(Fe2O3) + n(MnO))
```

Indice de quartz `qz` :

```text
lorsque al > alk     qz = si − (100 + 4 al)
lorsque al ≤ alk     qz = si − (100 + 3 al + alk)
```

La seconde ligne s'applique aux échantillons dont l'aluminium ne dépasse pas les alcalins. La fenêtre « Notice » du logiciel ne liste que la première.

`si`, `al`, `fm`, `c`, `alk`, `ti`, `p` et `qz` conservent 2 décimales ; `k` et `mg` en conservent 3. Le tableau ne place que les huit colonnes du premier groupe ; `ti` et `p` figurent dans le détail du calcul. Lorsque `qz` est positif, le détail lui ajoute un signe plus.

Dans le tableau, `al + fm + c + alk` doit valoir 100. `si` peut être très supérieur à 100, car la silice n'entre pas dans cette quote-part de 100. `k` et `mg` sont des rapports compris entre 0 et 1.

## Colonnes de diagnostic

| Colonne | Signification |
| --- | --- |
| **État de saturation en silice** | sursaturé en silice, saturé en silice, sous-saturé en silice |
| **État de saturation en alumine** | peralumineux, métalumineux, peralcalin |
| **Somme des minéraux** | somme des pourcentages cationiques de Catanorm, conservée avec 2 décimales |

L'état de saturation en silice est jugé d'après les minéraux finaux. Le seuil est `0.001 cat%` :

- Présence de quartz `Q` : sursaturé en silice.
- Absence de quartz, mais présence de néphéline `Ne`, de leucite `Lc`, de kaliophilite `Kp` ou d'olivine `Ol` : sous-saturé en silice.
- Les autres cas : saturé en silice.

Le signe de `qz` indique si la silice est en excès ou en déficit par rapport à la base de Niggli. Dans le tableau, l'« état de saturation en silice » se juge sur les minéraux normatifs. Les deux colonnes peuvent être confrontées : leurs bases de jugement diffèrent.

L'état de saturation en alumine se juge sur les cations normalisés, avant la répartition des minéraux :

- **Peralumineux** : Al > Na + K + Ca
- **Métalumineux** : Na + K < Al ≤ Na + K + Ca
- **Peralcalin** : Al ≤ Na + K

## Minéraux normatifs

L'unité des colonnes de minéraux est le pourcentage cationique. `Plag` est égal à `Ab + An`. La somme `Sum` compte `Plag` et n'ajoute pas `Ab` et `An` une seconde fois. Pour vérifier le degré de fermeture, regardez `Sum` ou la somme des minéraux de la colonne de diagnostic.

| Abréviation | Minéral | Abréviation | Minéral |
| --- | --- | --- | --- |
| `Q` | quartz | `Hy` | orthopyroxène |
| `C` | corindon | `Ol` | olivine |
| `Or` | orthose | `Cs` | rankinite |
| `Plag` | plagioclase | `Wo` | wollastonite |
| `Ab` | albite | `Mt` | magnétite |
| `An` | anorthite | `Hm` | hématite |
| `Lc` | leucite | `Il` | ilménite |
| `Ne` | néphéline | `Tn` | titanite |
| `Kp` | kaliophilite | `Pf` | pérovskite |
| `Ac` | aegyrine | `Ru` | rutile |
| `Ns` | métasilicate de sodium | `Ap` | apatite |
| `Ks` | métasilicate de potassium | `Fr` | fluorite |
| `Di` | diopside | `Py` | pyrite |
| | | `Cc` | calcite |

Les proportions des pôles n'occupent pas de colonne de résultats : elles sont toutes écrites dans le détail du calcul.

## Détail du calcul

Lorsqu'une ligne réussie est sélectionnée, l'inspecteur de droite affiche six sections :

1. **Classification de la roche et état de saturation** : état de saturation en silice, état de saturation en alumine, somme des minéraux normatifs. En cas de sous-saturation, la saturation en silice est mise en évidence ; dans le cas peralcalin, la saturation en alumine est mise en évidence.
2. **Paramètres de Niggli** : `si`, `al`, `fm`, `c`, `alk`, `ti`, `p`, `k`, `mg`, `qz`.
3. **Pôles minéraux**, qui n'apparaissent que lorsque le minéral correspondant existe :
   - plagioclase `An% = 100 × An / (Ab + An)`
   - le diopside est fixé à Wo 50 %, et les 50 % restants sont répartis en En et Fs selon le rapport magnésium/fer
   - olivine Fo, Fa ; Fo est égal à l'indice magnésien ci-dessous
   - indice magnésien `Mgr = 100 × En / (En + Fs)`, calculé avant que l'orthopyroxène ne soit incorporé au diopside
4. **Parcours des réactions de désilication** : lorsqu'il reste de la silice, le cat% du quartz est indiqué ; en cas de déficit de silice, on écrit pas à pas quel minéral a été consommé, et si le déficit a déjà été éliminé.
5. **Principaux minéraux normatifs** : minéraux supérieurs à `1 cat%`, du plus abondant au moins abondant, sans le `Plag` en double. Les entrées supérieures à `10 cat%` sont mises en évidence.
6. **Avertissements** : la mise en œuvre actuelle écrit un avertissement lorsque l'état d'oxydation du fer est estimé à partir de FeOT.

## Déroulement du calcul

### 1. Base cationique

Chaque oxyde est d'abord converti en quantité de cations : `(wt% / masse molaire) × nombre de cations dans la formule chimique`. Le nombre de cations de SiO₂, TiO₂, FeO, MnO, MgO, CaO, CO₂, F et S est 1 ; celui de Al₂O₃, Fe₂O₃, Na₂O, K₂O et P₂O₅ est 2.

Tous les cations sont additionnés, puis ramenés à 100, ce qui constitue la réserve de répartition de Catanorm. Lorsque le total est proche de 0, la ligne échoue, avec l'indication que la somme cationique est insuffisante. Cette étape ne normalise **plus** les wt% des oxydes à 100. L'H₂O non renseigné et la perte au feu n'entrent pas dans ce 100.

### 2. Minéraux accessoires et sels

Mn est d'abord incorporé à Fe²⁺. Ensuite, selon la stœchiométrie cationique :

| Ordre | Minéral | Point de stœchiométrie |
| --- | --- | --- |
| 1 | calcite `Cc` | quantité de minéral = `2 × CO2`, et consommation d'une quantité égale de Ca |
| 2 | apatite `Ap` | lorsque P est faible par rapport à F, `Ap = 3P`, et une partie de F est consommée ; sinon, tout le F restant est incorporé à l'apatite |
| 3 | fluorite `Fr` | F restant : `Fr = 1.5 × F`, et consommation de `0.5 × F` de Ca |
| 4 | pyrite `Py` | `Py = 1.5 × S`, et consommation de `0.5 × S` de Fe²⁺ |
| 5 | ilménite `Il` | le plus faible entre Ti et Fe²⁺ détermine la production ; la quantité de minéral est le double de cette quantité |

CO₂, P, F et S occupent en priorité Ca ou Fe²⁺. Lorsque ces volatils sont nettement plus abondants que les cations auxquels ils peuvent se combiner, les minéraux calciques et magnésiens ultérieurs seront faussés. Il faut alors vérifier avec la somme des minéraux et l'enregistrement de la désilication.

### 3. Feldspaths, précurseurs des feldspathoïdes et minéraux titanés

L'unité de répartition est le cation. L'orthose, l'albite et l'anorthite forment chacune un groupe de 5 cations lorsque le pôle est suffisant.

1. K, avec Al et Si, forme l'orthose `Or` (chaque K est associé à 1 Al et à 3 Si). Le K restant forme le métasilicate de potassium `Ks`.
2. Na, avec Al et Si, forme l'albite `Ab`. Le Na restant forme ensuite, avec Fe³⁺, l'aegyrine `Ac` (chaque Na est associé à 1 Fe³⁺ et à 2 Si). Le Na qui reste encore forme le métasilicate de sodium `Ns`.
3. L'Al restant forme, avec Ca, l'anorthite `An`. Lorsque Ca est insuffisant, la limite est l'aluminium : chaque Al limitant correspond à 0.5 Ca et à 1 Si.
4. Le Ti restant forme, avec Ca et Si, la titanite `Tn` (3 cations). Le Ti qui reste encore est le rutile `Ru`.
5. L'Al qui reste à la fin est le corindon `C`.

### 4. Oxydes de fer et pyroxènes

Fe³⁺ et Fe²⁺ forment la magnétite `Mt` : lorsque Fe³⁺ ne dépasse pas le double de Fe²⁺, `Mt = 1.5 × Fe³⁺`. Le Fe³⁺ restant est l'hématite `Hm`.

Le Ca, le Mg et le Fe²⁺ restants deviennent respectivement la wollastonite `Wo`, la molécule d'enstatite et la molécule de ferrosilite, chacune égale au double du nombre de cations, et consomment une part égale de Si. La somme des molécules d'enstatite et de ferrosilite est l'orthopyroxène `Hy`. L'indice magnésien est calculé à ce moment.

`Wo` et `Hy` se combinent ensuite, à cations égaux, en diopside `Di` : le moins abondant des deux entre entièrement dans `Di`, et le nombre de cations de `Di` est le double de celui-ci ; le plus abondant laisse `Wo` ou `Hy`.

### 5. Quartz, ou désilication en sept étapes

Lorsque le Si restant est ≥ 0, il devient entièrement du quartz `Q`, et le calcul s'arrête là.

Lorsque Si < 0, le déficit est `D = −Si`. Chaque étape ci-dessous soit ramène le déficit à 0 et s'arrête, soit épuise ce minéral puis passe à l'étape suivante. Les coefficients sont des pourcentages cationiques :

| Étape | Réaction | Pour éliminer 1 part de déficit de Si |
| --- | --- | --- |
| 1 | orthopyroxène → olivine | consomme 4 `Hy`, produit 3 `Ol` |
| 2 | titanite → pérovskite | consomme 3 `Tn`, produit 2 `Pf` |
| 3 | albite → néphéline | consomme 2.5 `Ab`, produit 1.5 `Ne` |
| 4 | orthose → leucite | consomme 5 `Or`, produit 4 `Lc` |
| 5 | leucite → kaliophilite | consomme 4 `Lc`, produit 3 `Kp` |
| 6 | wollastonite → rankinite | consomme 4 `Wo`, produit 3 `Cs` |
| 7 | diopside → rankinite + olivine | consomme 4 `Di`, et produit 1.5 `Cs` et 1.5 `Ol` |

Le produit de l'étape 5 est `Kp`. Une fois la désilication terminée, le plagioclase `Plag = Ab + An`, puis l'état de saturation en silice est jugé selon les règles énoncées plus haut.

## Références

- Niggli, P. (1948). *Gesteine und Minerallagerstätten*. Birkhäuser, Basel.
- Barth, T. F. W. (1948). The distribution of oxygen in the lithosphere. *The Journal of Geology*, 56(1), 41–49.
- Janoušek, V., Farrow, C. M., Erban, V. (2006). Interpretation of whole-rock geochemical data in igneous geochemistry: introducing Geochemical Data Toolkit (GCDkit). *Journal of Petrology*, 47(6), 1255–1259.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
- GCDkit: [https://www.gcdkit.org/](https://www.gcdkit.org/)
