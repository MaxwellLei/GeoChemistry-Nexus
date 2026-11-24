---
sidebar_position: 6
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Niggli-Normberechnung

Die **Niggli-Normberechnung** erzeugt für dieselbe Zeile der Gesamtgesteinsoxide zwei Ergebnissätze:

1. **Niggli-Parameter**: `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz`, außerdem `ti` und `p`, die nur in den Berechnungsdetails angezeigt werden.
2. **Molekulare Barth-Niggli-Norm (Catanorm)**: in Kationenprozent (cat%) ausgedrückte Normminerale, Endgliedzusammensetzungen sowie der Entkieselungsverlauf bei Siliciumuntersättigung.

Die Catanorm wurde von Paul Niggli (1948) und Tom F. W. Barth (1948) aufgestellt. Sie verteilt nach Kationenäquivalenten und verringert so das Gewicht schwerer Elemente wie Eisen in den Massenprozenten. Deshalb liegt sie näher an den Volumenbeziehungen der Minerale im Dünnschliff als die wt% der CIPW-Norm. Die Normminerale in Massenprozent siehe [CIPW-Normberechnung](./CIPW).

Der Algorithmus folgt dem Ablauf der molekularen Norm von GCDkit und wurde neu implementiert. Das Produkt der weiteren Entkieselung von Leucit wird dabei als Kaliophilit `Kp` geführt.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/niggli.webp')}
    alt="Niggli-Oberfläche"
    style={{width: '100%'}}
  />
</div>

## Oberfläche

Die obere Leiste entspricht derjenigen der CIPW-Berechnung: **Fe³⁺/ΣFe**, **Erläuterung**, **Beispiel**, **Exportieren**, **Leeren**, **Berechnung ausführen**. Der Bereich von Fe³⁺/ΣFe ist `0`–`1`, der Standard ist `0.15` (Le Maitre, 2002).

**Beispiel** füllt drei Zeilen: Granit, Basalt und Phonolith (Phonolite). Die ersten beiden Zeilen sind häufige vulkanische Gesteine mit höherem Siliciumgehalt. Der Phonolith dient dazu, untersättigte Ergebnisse wie Nephelin und Leucit anzusehen.

Das Arbeitsblatt gliedert sich von links nach rechts in fünf Abschnitte:

| Bereich | Gruppe | Einheit | Inhalt |
| --- | --- | --- | --- |
| Probe | — | — | Probenname, nimmt an der Berechnung nicht teil und bleibt beim horizontalen Scrollen sichtbar |
| Oxide | Hauptelemente; weitere Oxide / flüchtige Bestandteile | wt% | Eingabebereich |
| Niggli-Parameter | Niggli-Parameter | dimensionslos | `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz` |
| Diagnose | Diagnose | — | Zustand der Si-Sättigung, Zustand der Al-Sättigung, Mineralsumme |
| Normminerale | Normminerale | cat% | Kationenprozent. Die letzte Spalte ist `Sum(Summe)` |

Die Probenspalte ist fixiert. Die Tabelle hat anfangs 500 Zeilen und unterstützt das Füllen nach unten durch einen Doppelklick auf das Ausfüllkästchen. Nach einer Änderung der Eingabe werden bereits berechnete Zeilen in den Zustand veraltet versetzt. Sie müssen erneut auf **Berechnung ausführen** klicken.

### Status des Zeilenkopfs

| Punkt | Bedeutung |
| --- | --- |
| Grau, hohl | Es liegt eine Eingabe vor, die Berechnung wurde noch nicht ausgeführt |
| Türkis, gefüllt | Die Berechnung war erfolgreich, es gibt keine Warnung |
| Orange, gefüllt | Die Berechnung war erfolgreich, die Eisenwertigkeit wurde jedoch aus FeOT geschätzt |
| Rot, gefüllt | Die Berechnung ist fehlgeschlagen. Die Diagnosespalte enthält „Berechnungsfehler“ und die Ursache |
| Orange, hohl | Die Oxide wurden nach der Berechnung geändert, das Ergebnis ist veraltet |

Proben, bei denen FeO und Fe₂O₃ zugleich eingetragen sind, zeigen nach erfolgreicher Berechnung einen türkisen Punkt. Liegt nur FeOT vor, erscheint eine Warnung zur Eisenwertigkeit, und der Zeilenkopf ist orange.

## Eingabe

Jede Zeile ist eine Probe. Die Einheit ist das Massenprozent des Oxids (wt%). Gelesen werden nur Zellen größer als 0. Leere Zellen und 0 gelten als nicht angegeben.

Hauptelemente, bis einschließlich `K2O`:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`

Weitere Oxide / flüchtige Bestandteile:

`CO2`, `P2O5`, `F`, `S`

Dieses Modul liest `ZrO2`, `Cr2O3`, `Cl` und `SO3` nicht. Sollen diese vier Größen in die Normminerale eingehen, verwenden Sie das CIPW-Modul.

### Behandlung des Eisens

Das Eisen wird behandelt, bevor es in Kationen umgerechnet wird. Bei der Aufteilung wird das Eisen zuerst in Fe-Atome umgerechnet: Molzahl des Gesamteisens = Masse des Eisens / Molmasse von FeO. Anschließend wird nach Fe³⁺/ΣFe in dreiwertiges und zweiwertiges Eisen geteilt. Die Hälfte des dreiwertigen Eisens wird in Fe₂O₃ geschrieben.

| Eingabesituation | Behandlung |
| --- | --- |
| `Fe2O3` und `FeO` liegen zugleich vor | Die Messwerte werden verwendet. Auch wenn daneben noch `FeOT` steht, wird nicht weiter aufgeteilt |
| `FeOT` liegt vor, und Fe₂O₃ und FeO liegen nicht zugleich vor | `FeOT` wird nach dem aktuellen Verhältnis aufgeteilt, und in den Details wird eine Warnung ausgegeben |
| Nur `Fe2O3`, weder `FeO` noch `FeOT` | Dieses Fe₂O₃ wird als Gesamteisen behandelt und anschließend nach dem Verhältnis in Fe₂O₃ und FeO aufgeteilt |
| Nur `FeO`, weder `Fe2O3` noch `FeOT` | Dieses FeO wird als Gesamteisen behandelt und anschließend nach dem Verhältnis in Fe₂O₃ und FeO aufgeteilt |

Wurde nur FeO oder nur Fe₂O₃ analysiert, ändern sich die Ergebnisse deshalb mit dem Verhältnis in der oberen Leiste. Sind beide Größen Messwerte, tragen Sie bitte beide Spalten ein.

`MnO` bleibt bis zur Umrechnung in Kationen erhalten. Es geht in die Niggli-Parameter `fm` und `mg` ein und wird nach dem Eintritt in die Catanorm dem Fe²⁺ zugeschlagen.

## Niggli-Parameter

Die Parameter werden mit den **Molekülzahlen** der Oxide berechnet. Molekülzahl = wt% / Molmasse. Das `al` hier ist die Molekülzahl von Al₂O₃. Ein Anteil Al₂O₃ zählt als 1 und ist nicht dieselbe Basis wie in der späteren Catanorm, in der ein Anteil Al₂O₃ als 2 Al zählt.

```text
al  = n(Al2O3)
fm  = n(FeO) + 2 n(Fe2O3) + n(MnO) + n(MgO)
c   = n(CaO)
alk = n(Na2O) + n(K2O)
base = al + fm + c + alk

si  = 100 × n(SiO2) / base
al, fm, c und alk werden ebenfalls als Anteile von 100 an base ausgedrückt
ti  = 100 × n(TiO2) / base
p   = 100 × n(P2O5) / base
k   = n(K2O) / (n(Na2O) + n(K2O))
mg  = n(MgO) / (n(MgO) + n(FeO) + 2 n(Fe2O3) + n(MnO))
```

Quarzindex `qz`:

```text
wenn al > alk     qz = si − (100 + 4 al)
wenn al ≤ alk     qz = si − (100 + 3 al + alk)
```

Die zweite Zeile gilt für Proben, bei denen Aluminium die Alkalien nicht übersteigt. Das Fenster „Erläuterung“ in der Software führt nur die erste Zeile auf.

`si`, `al`, `fm`, `c`, `alk`, `ti`, `p` und `qz` behalten 2 Dezimalstellen, `k` und `mg` behalten 3 Dezimalstellen. Die Tabelle enthält nur die acht Spalten der ersten Gruppe. `ti` und `p` stehen in den Berechnungsdetails. Ist `qz` positiv, wird in den Details ein Pluszeichen ergänzt.

`al + fm + c + alk` soll in der Tabelle 100 ergeben. `si` kann weit über 100 liegen, weil Silicium nicht in diesem Anteil von 100 enthalten ist. `k` und `mg` sind Verhältnisse von 0 bis 1.

## Diagnosespalten

| Spalte | Bedeutung |
| --- | --- |
| **Zustand der Si-Sättigung** | Si-übersättigt, Si-gesättigt, Si-untersättigt |
| **Zustand der Al-Sättigung** | peraluminös, metaluminös, peralkalisch |
| **Mineralsumme** | Summe der Kationenprozente der Catanorm, mit 2 Dezimalstellen |

Der Zustand der Si-Sättigung wird anhand der endgültigen Minerale beurteilt. Der Schwellenwert ist `0.001 cat%`:

- Quarz `Q` ist vorhanden: Si-übersättigt.
- Kein Quarz, aber Nephelin `Ne`, Leucit `Lc`, Kaliophilit `Kp` oder Olivin `Ol`: Si-untersättigt.
- Übrige Fälle: Si-gesättigt.

Das Vorzeichen von `qz` zeigt, ob Silicium gegenüber der Niggli-Basis im Überschuss oder im Defizit ist. Der „Zustand der Si-Sättigung“ in der Tabelle betrachtet die Normminerale. Beide Spalten lassen sich gegenüberstellen. Ihre Beurteilungsgrundlagen unterscheiden sich.

Der Zustand der Al-Sättigung wird anhand der normierten Kationen beurteilt und liegt vor der Mineralzuteilung:

- **Peraluminös**: Al > Na + K + Ca
- **Metaluminös**: Na + K < Al ≤ Na + K + Ca
- **Peralkalisch**: Al ≤ Na + K

## Normminerale

Die Einheit der Mineralspalten ist das Kationenprozent. `Plag` ist gleich `Ab + An`. Die Summe `Sum` zählt `Plag` und addiert `Ab` und `An` nicht ein zweites Mal. Die Geschlossenheit (Abschluss der Kationensumme) prüfen Sie an `Sum` oder an der Mineralsumme der Diagnosespalte.

| Abkürzung | Mineral | Abkürzung | Mineral |
| --- | --- | --- | --- |
| `Q` | Quarz | `Hy` | Orthopyroxen |
| `C` | Korund | `Ol` | Olivin |
| `Or` | Orthoklas | `Cs` | Rankinit |
| `Plag` | Plagioklas | `Wo` | Wollastonit |
| `Ab` | Albit | `Mt` | Magnetit |
| `An` | Anorthit | `Hm` | Hämatit |
| `Lc` | Leucit | `Il` | Ilmenit |
| `Ne` | Nephelin | `Tn` | Titanit |
| `Kp` | Kaliophilit | `Pf` | Perowskit |
| `Ac` | Ägirin | `Ru` | Rutil |
| `Ns` | Natriummetasilikat | `Ap` | Apatit |
| `Ks` | Kaliummetasilikat | `Fr` | Fluorit |
| `Di` | Diopsid | `Py` | Pyrit |
| | | `Cc` | Calcit |

Die Endgliedverhältnisse belegen keine eigenen Ergebnisspalten. Sie stehen sämtlich in den Berechnungsdetails.

## Berechnungsdetails

Nachdem Sie eine erfolgreiche Zeile ausgewählt haben, zeigt der Prüfer rechts sechs Abschnitte:

1. **Gesteinsklassifikation und Sättigungszustand**: Zustand der Si-Sättigung, Zustand der Al-Sättigung, Summe der Normminerale. Bei Untersättigung wird die Si-Sättigung hervorgehoben, bei peralkalischem Charakter die Al-Sättigung.
2. **Niggli-Parameter**: `si`, `al`, `fm`, `c`, `alk`, `ti`, `p`, `k`, `mg`, `qz`.
3. **Mineralendglieder**, nur wenn das entsprechende Mineral vorliegt:
   - Plagioklas `An% = 100 × An / (Ab + An)`
   - Diopsid ist fest auf Wo 50 % gesetzt, die übrigen 50 % werden nach dem Magnesium-Eisen-Verhältnis in En und Fs geteilt
   - Olivin Fo und Fa; Fo entspricht der unten stehenden Magnesiumzahl
   - Magnesiumzahl `Mgr = 100 × En / (En + Fs)`, berechnet bevor Orthopyroxen in Diopsid eingeht
4. **Verlauf der Entkieselungsreaktionen**: Ist Silicium übrig, werden die cat% des Quarzes genannt. Bei einem Siliciumdefizit wird schrittweise aufgeschrieben, welches Mineral verbraucht wurde und ob das Defizit bereits beseitigt ist.
5. **Wichtigste Normminerale**: Minerale über `1 cat%`, von hoch nach niedrig, ohne das wiederholte `Plag`. Einträge über `10 cat%` werden hervorgehoben.
6. **Warnungen**: Die aktuelle Umsetzung schreibt eine Warnung, wenn die Eisenwertigkeit aus FeOT geschätzt wird.

## Berechnungsverlauf

### 1. Kationenbasis

Jedes Oxid wird zuerst in eine Kationenmenge umgewandelt: `(wt% / Molmasse) × Kationenzahl in der chemischen Formel`. Die Kationenzahl von SiO₂, TiO₂, FeO, MnO, MgO, CaO, CO₂, F und S ist 1. Diejenige von Al₂O₃, Fe₂O₃, Na₂O, K₂O und P₂O₅ ist 2.

Alle Kationen werden addiert und anschließend auf 100 skaliert. Das ist der Zuteilungspool der Catanorm. Liegt die Summe nahe bei 0, schlägt diese Zeile fehl, mit dem Hinweis, dass die Kationensumme unzureichend ist. In diesem Schritt werden die Oxid-wt% **nicht mehr** auf 100 normiert. Nicht eingetragenes H₂O und der Glühverlust gehen nicht in diese 100 ein.

### 2. Akzessorien und Salze

Mn wird zuerst dem Fe²⁺ zugeschlagen. Anschließend gilt die Kationenstöchiometrie:

| Reihenfolge | Mineral | Stöchiometrischer Kern |
| --- | --- | --- |
| 1 | Calcit `Cc` | Mineralmenge = `2 × CO2`, verbraucht die gleiche Menge Ca |
| 2 | Apatit `Ap` | Ist P gegenüber F geringer, gilt `Ap = 3P`, und ein Teil von F wird verbraucht; andernfalls wird das gesamte verbleibende F in den Apatit aufgenommen |
| 3 | Fluorit `Fr` | Verbleibendes F: `Fr = 1.5 × F`, verbraucht `0.5 × F` an Ca |
| 4 | Pyrit `Py` | `Py = 1.5 × S`, verbraucht `0.5 × S` an Fe²⁺ |
| 5 | Ilmenit `Il` | Die geringere der beiden Größen Ti und Fe²⁺ bestimmt die Ausbeute, die Mineralmenge ist das 2-Fache dieser Größe |

CO₂, P, F und S belegen vorrangig Ca oder Fe²⁺. Liegen diese flüchtigen Bestandteile deutlich über den Kationen, an die sie gebunden werden können, werden die nachfolgenden Calcium-Magnesium-Minerale verfälscht. Prüfen Sie dies anhand der Mineralsumme und der Entkieselungsaufzeichnung.

### 3. Feldspäte, Vorstufen der Feldspatoide und Titanminerale

Die Einheit der Zuteilung ist das Kation. Orthoklas, Albit und Anorthit bilden bei ausreichenden Endgliedern jeweils eine Gruppe aus 5 Kationen.

1. K bildet mit Al und Si Orthoklas `Or` (jedem K sind 1 Al und 3 Si zugeordnet). Verbleibendes K bildet Kaliummetasilikat `Ks`.
2. Na bildet mit Al und Si Albit `Ab`. Verbleibendes Na bildet anschließend mit Fe³⁺ Ägirin `Ac` (jedem Na sind 1 Fe³⁺ und 2 Si zugeordnet). Weiter verbleibendes Na bildet Natriummetasilikat `Ns`.
3. Verbleibendes Al bildet mit Ca Anorthit `An`. Reicht Ca nicht aus, begrenzt Aluminium die Bildung. Jedem begrenzenden Al entsprechen 0,5 Ca und 1 Si.
4. Verbleibendes Ti bildet mit Ca und Si Titanit `Tn` (3 Kationen). Weiter verbleibendes Ti ist Rutil `Ru`.
5. Das zuletzt verbleibende Al ist Korund `C`.

### 4. Eisenoxide und Pyroxen

Fe³⁺ und Fe²⁺ bilden Magnetit `Mt`: Übersteigt Fe³⁺ das Doppelte von Fe²⁺ nicht, gilt `Mt = 1.5 × Fe³⁺`. Verbleibendes Fe³⁺ ist Hämatit `Hm`.

Verbleibendes Ca, Mg und Fe²⁺ werden zu Wollastonit `Wo`, zum Enstatit-Molekül und zum Ferrosilit-Molekül, jeweils das 2-Fache der Kationenzahl, und verbrauchen je einen gleichen Anteil Si. Enstatit- und Ferrosilit-Molekül addiert ergeben Orthopyroxen `Hy`. Zu diesem Zeitpunkt wird die Magnesiumzahl berechnet.

`Wo` und `Hy` verbinden sich anschließend bei gleichen Kationen zu Diopsid `Di`: Die geringere Seite geht vollständig in `Di` ein, die Kationenzahl von `Di` ist das 2-Fache davon. Die größere Seite lässt `Wo` oder `Hy` zurück.

### 5. Quarz oder Entkieselung in sieben Schritten

Ist verbleibendes Si ≥ 0, wird es vollständig zu Quarz `Q`, und die Berechnung endet hier.

Ist Si < 0, beträgt das Defizit `D = −Si`. Jeder der folgenden Schritte verringert das Defizit entweder auf 0 und endet, oder er verbraucht dieses Mineral vollständig und geht zum nächsten Schritt über. Die Koeffizienten sind Kationenprozente:

| Schritt | Reaktion | Je beseitigtem Anteil des Si-Defizits |
| --- | --- | --- |
| 1 | Orthopyroxen → Olivin | verbraucht 4 `Hy`, erzeugt 3 `Ol` |
| 2 | Titanit → Perowskit | verbraucht 3 `Tn`, erzeugt 2 `Pf` |
| 3 | Albit → Nephelin | verbraucht 2.5 `Ab`, erzeugt 1.5 `Ne` |
| 4 | Orthoklas → Leucit | verbraucht 5 `Or`, erzeugt 4 `Lc` |
| 5 | Leucit → Kaliophilit | verbraucht 4 `Lc`, erzeugt 3 `Kp` |
| 6 | Wollastonit → Rankinit | verbraucht 4 `Wo`, erzeugt 3 `Cs` |
| 7 | Diopsid → Rankinit + Olivin | verbraucht 4 `Di`, erzeugt je 1.5 `Cs` und 1.5 `Ol` |

Das Produkt von Schritt 5 ist `Kp`. Nach Abschluss der Entkieselung gilt Plagioklas `Plag = Ab + An`. Anschließend wird der Zustand der Si-Sättigung nach den zuvor genannten Regeln beurteilt.

## Literatur

- Niggli, P. (1948). *Gesteine und Minerallagerstätten*. Birkhäuser, Basel.
- Barth, T. F. W. (1948). The distribution of oxygen in the lithosphere. *The Journal of Geology*, 56(1), 41–49.
- Janoušek, V., Farrow, C. M., Erban, V. (2006). Interpretation of whole-rock geochemical data in igneous geochemistry: introducing Geochemical Data Toolkit (GCDkit). *Journal of Petrology*, 47(6), 1255–1259.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
- GCDkit: [https://www.gcdkit.org/](https://www.gcdkit.org/)
