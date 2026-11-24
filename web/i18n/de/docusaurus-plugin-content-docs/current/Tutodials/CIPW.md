---
sidebar_position: 5
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# CIPW-Normberechnung

Die **CIPW-Normberechnung** rechnet die Oxide der Hauptelemente eines Gesamtgesteins (wt%) in eine Gruppe idealisierter Normminerale um. Das Ergebnis dient dem Vergleich magmatischer Gesteine sowie der Beurteilung der SiO₂-Sättigung und der Al₂O₃-Sättigung. Die CIPW-Norm wurde 1902 von Cross, Iddings, Pirsson und Washington vorgeschlagen. Dieses Modul rechnet stapelweise in einer Excel-ähnlichen Tabelle und gibt für jede Zeile Diagnose und Massenbilanz aus.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/cipw.webp')}
    alt="CIPW-Oberfläche"
    style={{width: '100%'}}
  />
</div>

## Oberfläche

Die Seite besteht aus der oberen Leiste und dem Datenarbeitsblatt. Links in der oberen Leiste steht **Fe³⁺/ΣFe**, rechts stehen **Erläuterung**, **Beispiel**, **Exportieren**, **Leeren** und **Berechnung ausführen**.

Das Arbeitsblatt gliedert sich von links nach rechts in vier Abschnitte. Über den Spaltenköpfen stehen Gruppennamen:

| Bereich | Gruppe | Einheit | Funktion |
| --- | --- | --- | --- |
| Probe | — | — | Probenname, dient nur der Kennzeichnung und nimmt an der Berechnung nicht teil. Diese Spalte ist fixiert und bleibt beim horizontalen Scrollen sichtbar |
| Oxide | Hauptelemente; weitere Oxide / flüchtige Bestandteile | wt% | weißer Eingabebereich |
| Diagnose | Diagnose | — | `SiO2-Sättigung`, `Al2O3-Status`, `Massenbilanzfehler(%)` |
| Normminerale | CIPW-Normminerale | wt% | heller Ergebnisbereich. Der Spaltenkopf setzt sich aus Abkürzung und deutschem Namen zusammen, zum Beispiel `Q(Quarz)` |

Zwischen Eingabebereich und Ergebnisbereich liegt eine schmale Trennspalte. Die Ergebnisspalten sind vor der Berechnung leer. Die Tabelle stellt anfangs 500 Zeilen bereit. Mit einem Doppelklick auf das Ausfüllkästchen können Sie wie in Excel nach unten füllen.

### Obere Leiste

| Steuerung | Funktion |
| --- | --- |
| **Fe³⁺/ΣFe** | Nur bei Verwendung des Gesamteisens `FeOT` werden FeO und Fe₂O₃ nach diesem Verhältnis aufgeteilt. Bereich `0`–`1`, Standard `0.15` (Le Maitre, 2002), Anzeige mit zwei Dezimalstellen |
| **Erläuterung** | Öffnet die Algorithmenbeschreibung in der Software |
| **Beispiel** | Füllt drei Beispielzeilen: Granit, Basalt und Andesit, mit gemessenen Fe₂O₃, FeO und MnO |
| **Exportieren** | Speichert die aktuelle Tabelle als UTF-8-CSV, Standarddateiname `CIPW_Results.csv` |
| **Leeren** | Löscht nach Bestätigung sämtliche Eingaben, Ergebnisse und Berechnungsdetails |
| **Berechnung ausführen** | Berechnet alle Zeilen, die gültige Oxide enthalten |

### Status des Zeilenkopfs

Der Punkt neben der Zeilennummer zeigt den Status der Zeile:

| Punkt | Bedeutung |
| --- | --- |
| Grau, hohl | Es liegt eine Eingabe vor, die Berechnung wurde noch nicht ausgeführt |
| Türkis, gefüllt | Die Berechnung war erfolgreich, es gibt keine Warnung, und die Massenbilanz ist bestanden |
| Orange, gefüllt | Die Berechnung war erfolgreich, es liegt jedoch eine Warnung vor, oder der Massenbilanzfehler ist größer als `1×10⁻⁶` |
| Rot, gefüllt | Die Berechnung dieser Zeile ist fehlgeschlagen. Die Diagnosespalte enthält „Fehler“ und die Ursache |
| Orange, hohl | Die Oxide wurden nach der Berechnung geändert, das aktuelle Ergebnis ist veraltet |

Alle Beispieldaten enthalten MnO. MnO wird in FeO einbezogen und erzeugt eine Warnung. Die Beispielzeilen sind nach der Berechnung deshalb orange. Das ist das erwartete Ergebnis. Eine Änderung des Probennamens lässt das Ergebnis nicht veralten; die Änderung einer beliebigen Oxideingabe tut dies.

## Eingabe

Jede Zeile ist eine Gesamtgesteinsprobe. Tragen Sie die Massenprozente der Oxide (wt%) ein. Die Software liest nur Zellen, die **größer als 0** sind. Leere Zellen und 0 gelten als nicht angegeben. Leere Zeilen werden automatisch übersprungen.

Sie können unmittelbar aus einem Tabellenprogramm einfügen. Die Spaltenreihenfolge ist fest. Richten Sie die Daten beim Einfügen an den folgenden Spaltenköpfen aus.

### Oxidspalten

Hauptelemente:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`, `P2O5`

Weitere Oxide / flüchtige Bestandteile:

`ZrO2`, `Cr2O3`, `CO2`, `S`, `F`, `Cl`, `SO3`

`FeOT` ist das Gesamteisen, ausgedrückt als FeO-Äquivalent. `S`, `F` und `Cl` werden als Massenprozent des Elements eingetragen.

### Behandlung des Eisens

| Eingabesituation | Behandlung |
| --- | --- |
| `FeO` und `Fe2O3` liegen zugleich vor, `FeOT` nicht | Die gemessene Aufteilung wird verwendet. Der Eisenbehandlungsmodus ist „Messwerte“ |
| Nur `FeO` oder nur `Fe2O3` | Die fehlende Größe wird als 0 gesetzt. Der Modus ist „teilweise angenommen“, und es wird eine Warnung ausgegeben |
| Nur `FeOT` | Aufteilung nach Fe³⁺/ΣFe. Der Modus ist „aus FeOT geschätzt“ |
| `FeOT` tritt zugleich mit `FeO` oder `Fe2O3` auf | `FeOT` wird ignoriert. Der Modus ist „widersprüchliche Eingabe“, und es wird eine Warnung ausgegeben |
| Es liegen keine Eisendaten vor | FeO und Fe₂O₃ werden beide als 0 gesetzt. Der Modus ist „fehlend“, und es wird eine Warnung ausgegeben |

Die Aufteilung, wenn nur `FeOT` vorliegt, lautet:

```text
Fe2O3 = FeOT × (Fe³⁺/ΣFe) × 1.11134
FeO   = FeOT × (1 − Fe³⁺/ΣFe)
```

Der Koeffizient `1.11134` entspricht dem Verhältnis der Molmassen von Fe₂O₃ und dem Doppelten von FeO. Er rechnet das dreiwertige Eisen aus dem FeO-Äquivalent in die Massenprozente von Fe₂O₃ zurück.

`MnO` wird vor der Berechnung nach dem Verhältnis der Molmassen in FeO-Äquivalent umgerechnet und zu FeO addiert:

```text
FeO-Äquivalent = MnO × 71.844 / 70.937
```

Sobald MnO eingetragen ist, erscheint in den Berechnungsdetails „MnO wurde nach dem Molverhältnis in FeO-Äquivalent umgerechnet“.

## Ausgabe

### Diagnosespalten

| Spalte | Werte |
| --- | --- |
| **SiO2-Sättigung** | übersättigt, gesättigt, untersättigt |
| **Al2O3-Status** | peralkalisch, metaluminös, peraluminös |
| **Massenbilanzfehler(%)** | Abweichung der Geschlossenheit von Mineralmassen und eingegebenen Oxiden. Unter `0.01` wird die wissenschaftliche Schreibweise verwendet, andernfalls werden 4 Dezimalstellen beibehalten |

Die Bedeutung der drei Sättigungszustände:

- **Übersättigt**: Nach Abschluss der Zuteilung ist noch SiO₂ vorhanden. Es wird als Quarz `Q` geführt.
- **Gesättigt**: SiO₂ ist genau aufgebraucht. Es gibt weder Quarz, noch ist eine Entkieselung erforderlich.
- **Untersättigt**: SiO₂ reicht nicht aus. Es folgen die unten stehenden Entkieselungsreaktionen. Wenn die gesamte Reaktionskette das Siliciumdefizit immer noch nicht beseitigt, wird eine zusätzliche Warnung ausgegeben und das Restdefizit (mol/100 g) genannt. Die Normminerale sind dann nicht zuverlässig.

Der Zustand der Aluminiumsättigung wird vor der Zuteilung der Feldspäte anhand der zu diesem Zeitpunkt verbleibenden Oxidmolzahlen beurteilt:

- **Peralkalisch**: Na₂O + K₂O > Al₂O₃
- **Peraluminös**: Al₂O₃ / (CaO + Na₂O + K₂O) > 1
- **Metaluminös**: die übrigen Fälle

Apatit, Calcit, Fluorit, Halit und Thenardit haben das entsprechende CaO oder Na₂O bereits vorher verbraucht. Hier wird deshalb die verbleibende Menge verwendet.

### Normminerale

Die Ergebnisse sind in der folgenden Reihenfolge angeordnet. Die deutschen Namen stimmen mit den Spaltenköpfen der Software überein.

| Abkürzung | Mineral | Abkürzung | Mineral |
| --- | --- | --- | --- |
| `Q` | Quarz | `Fo` | Forsterit |
| `Or` | Orthoklas | `Fa` | Fayalit |
| `Ab` | Albit | `Mt` | Magnetit |
| `An` | Anorthit | `Hm` | Hämatit |
| `Le` | Leucit | `Ilm` | Ilmenit |
| `Ne` | Nephelin | `Cm` | Chromit |
| `Kp` | Pseudoleucit | `Ru` | Rutil |
| `Cor` | Korund | `Tn` | Titanit |
| `Ac` | Ägirin | `Pf` | Perowskit |
| `Di` | Diopsid | `Z` | Zirkon |
| `Hd` | Hedenbergit | `Ap` | Apatit |
| `Wo` | Wollastonit | `Cc` | Calcit |
| `Cs` | Larnit | `Py` | Pyrit |
| `En` | Enstatit | `Fl` | Fluorit |
| `Fs` | Ferrosilit | `Hl` | Halit |
| `ns` | Natriummetasilikat | `Th` | Thenardit |
| `ks` | Kaliummetasilikat | | |

Orthoklas, Albit und Ägirin werden intern in Einheiten der chemischen Formel gezählt: 1 mol K₂O erzeugt 2 mol `Or`, 1 mol Na₂O erzeugt 2 mol `Ab` oder `Ac`. Ausgegeben werden stets Massenprozente.

## Berechnungsdetails

Nachdem Sie eine erfolgreich berechnete Zeile ausgewählt haben, zeigt der Prüfer rechts:

- den Probennamen. Ist die Probenspalte leer, wird „Zeile N“ angezeigt.
- Si-Sättigung und Al-Sättigung. Bei Untersättigung wird die Si-Sättigung hervorgehoben.
- den **Eisenbehandlungsmodus**. Sobald er nicht „Messwerte“ lautet, wird er hervorgehoben.
- den **Massenbilanzfehler**, mit 6 Dezimalstellen. Oberhalb von `1×10⁻⁶` wird er hervorgehoben.
- die **Mineralgesamtmenge**, also die Summe der wt% der einzelnen Normminerale, mit 4 Dezimalstellen. Der Sauerstoff, der bei der Bildung von Pyrit, Fluorit und Halit freigesetzt wird, geht nicht in diese Gesamtmenge ein, wohl aber in die Massenbilanz.
- die **Hauptminerale**: Minerale mit einem Gehalt von mehr als `0.01 wt%`, von hoch nach niedrig geordnet. Einträge über `10 wt%` werden hervorgehoben.
- **Warnungen**: fehlende oder widersprüchliche Eisendaten, Schätzung aus FeOT, Umrechnung von MnO, verbleibendes Siliciumdefizit und Weiteres.

Über die rechte obere Ecke des Prüfers können Sie diese eine Detailansicht als CSV exportieren oder in die Zwischenablage kopieren.

## Berechnungsverlauf

Im Folgenden wird die tatsächliche Reihenfolge des aktuellen Berechnungscodes erläutert. Apatit belegt das Calcium zuerst, erst danach bilden sich die Minerale der flüchtigen Bestandteile. Titanit und Rutil stehen nach dem Anorthit.

Die Molmassen der Minerale werden sämtlich aus den Molmassen der Oxide nach der chemischen Formel abgeleitet. Damit wird eine Massenunbilanz durch manuelles Runden vermieden. Die bei der Berechnung verwendete numerische Toleranz ist `1×10⁻¹²`. Wird ein Oxid außer SiO₂ in einem Schritt deutlich negativ, schlägt diese Zeile fehl.

### 1. Wasserfreie Normierung sowie Eisen und Mangan

Alle angegebenen und nicht negativen Oxide werden auf 100 % normiert. Das Eisen wird wie im vorherigen Abschnitt behandelt, anschließend wird MnO in FeO-Äquivalent umgerechnet, und danach wird **erneut normiert**. Ist die Gesamtmenge nicht größer als 0, schlägt diese Zeile fehl.

### 2. Umrechnung in Molzahlen

Die Molzahl jedes Oxids = normierte wt% / Molmasse. Die anschließende Zuteilung erfolgt sämtlich auf der Skala mol/100 g.

### 3. Apatit

Apatit belegt das Calcium zuerst. 1 mol `P2O5` bindet `10/3` mol `CaO` (ohne OH oder F):

```text
Ap = min(P2O5, CaO / (10/3))
```

### 4. Minerale der flüchtigen Bestandteile

In der folgenden Reihenfolge wird so viel gebildet, wie sich binden lässt:

| Mineral | Stöchiometrie |
| --- | --- |
| Calcit `Cc` | `min(CO2, CaO)` |
| Fluorit `Fl` | `min(F/2, CaO)`, das heißt 1 mol CaO bindet 2 mol F |
| Pyrit `Py` | `min(S/2, FeO)`, das heißt 1 mol FeO bindet 2 mol S |
| Halit `Hl` | `min(Cl, 2×Na2O)` |
| Thenardit `Th` | `min(SO3, Na2O)` |

### 5. Zirkon, Chromit und Ilmenit

- Zirkon `Z`: das gesamte `ZrO2` mit der gleichen Menge `SiO2`.
- Chromit `Cm`: `min(Cr2O3, FeO)`.
- Ilmenit `Ilm`: `min(TiO2, FeO)`.

In diesem Schritt wird nur das Titan verbraucht, das sich mit FeO binden lässt. Verbleibendes TiO₂ bleibt bis nach dem Anorthit erhalten.

### 6. Feldspäte, Korund und verbleibende Alkalien

Zuerst wird der Zustand der Aluminiumsättigung festgehalten, danach wird zugeteilt:

| Mineral | Bedingung und Produkt |
| --- | --- |
| Orthoklas `Or` | `min(K2O, Al2O3)`, verbraucht das 6-Fache an SiO₂, die Molzahl des Produkts ist das 2-Fache dieses Werts |
| Albit `Ab` | `min(Na2O, Al2O3)`, verbraucht ebenfalls das 6-Fache an SiO₂, das Produkt ist das 2-Fache |
| Anorthit `An` | `min(CaO, Al2O3)`, verbraucht das 2-Fache an SiO₂ |
| Korund `Cor` | verbleibendes Al₂O₃ |
| Ägirin `Ac` | `min(verbleibendes Na2O, Fe2O3)`, verbraucht das 4-Fache an SiO₂, das Produkt ist das 2-Fache |
| Natriummetasilikat `ns` | verbleibendes Na₂O mit der gleichen Menge SiO₂ |
| Kaliummetasilikat `ks` | verbleibendes K₂O mit der gleichen Menge SiO₂ |

In der Feldspatstufe darf SiO₂ zunächst als negativer Wert geführt werden. Die Entkieselung behandelt dies später einheitlich.

### 7. Titanit und Rutil

Titanit verwendet nur das nach dem Anorthit verbleibende CaO und **begrenzt den Titanit nicht durch den zu diesem Zeitpunkt verbleibenden SiO₂-Rest**:

```text
Tn = min(verbleibendes TiO2, verbleibendes CaO)
```

`Tn` verbraucht zugleich die gleiche Menge SiO₂. Das zuletzt noch verbleibende TiO₂ wird als Rutil `Ru` geführt.

### 8. Eisenoxide

```text
Mt = min(Fe2O3, FeO)
Hm = verbleibendes Fe2O3
```

### 9. Pyroxen und Wollastonit

Es sei `MgFe = MgO + FeO`. Die Gesamtmenge des Klinopyroxens ist `min(CaO, MgFe)` und wird nach dem Magnesium-Eisen-Verhältnis in Diopsid `Di` und Hedenbergit `Hd` geteilt. Jeder Anteil Pyroxen verbraucht 2 Anteile SiO₂.

Das verbleibende MgO und FeO werden vollständig zu Enstatit `En` und Ferrosilit `Fs` und verbrauchen jeweils 1 Anteil SiO₂. Sind Magnesium und Eisen beide aufgebraucht und ist noch CaO vorhanden, wird es vollständig zu Wollastonit `Wo`.

### 10. Si-Sättigung und Entkieselung

Die Reihenfolge der Entkieselung ist: Hypersthen → Titanit → Albit → Orthoklas → Wollastonit → Diopsid/Hedenbergit → Leucit. Jeder Schritt verbraucht nur das zu diesem Zeitpunkt noch fehlende SiO₂.

| Reihenfolge | Reaktion | Freigesetztes SiO₂ |
| --- | --- | --- |
| 1 | 2 Enstatit/Ferrosilit → 1 Forsterit/Fayalit | je 2 mol Pyroxen wird 1 mol freigesetzt |
| 2 | Titanit `Tn` → Perowskit `Pf` | 1 mol `Tn` setzt 1 mol frei |
| 3 | Albit `Ab` → Nephelin `Ne` | 1 mol `Ab` setzt 2 mol frei |
| 4 | Orthoklas `Or` → Leucit `Le` | 1 mol `Or` setzt 1 mol frei |
| 5 | 2 Wollastonit `Wo` → 1 Larnit `Cs` | je 2 mol `Wo` wird 1 mol freigesetzt |
| 6 | Diopsid/Hedenbergit → Larnit + Olivin | 1 mol Pyroxen setzt 1 mol frei; Magnesium geht in `Fo`, Eisen in `Fa` |
| 7 | Leucit `Le` → Pseudoleucit `Kp` | 1 mol `Le` setzt 1 mol frei |

Ist SiO₂ übrig, wird Quarz gebildet, und der Zustand ist übersättigt. Ist SiO₂ genau null, liegt Sättigung vor. Wird die oben stehende Tabelle erreicht, liegt Untersättigung vor.

### 11. Massenprozente und Massenbilanz

Masse des Normminerals = Molzahl × die aus der chemischen Formel abgeleitete Molmasse, anschließend umgerechnet in wt% bezogen auf die Summe der normierten Oxide.

S, F und Cl in Pyrit, Fluorit und Halit haben den Sauerstoff in den Oxiden ersetzt. Der freigesetzte Sauerstoff geht in kein Mineral ein und wird bei der Massenbilanz wieder hinzugezählt:

```text
Molzahl des freigesetzten Sauerstoffs = Fl + Py + Hl / 2
Massenbilanzfehler = |100 − (Summe der Mineral-wt% + wt% des freigesetzten Sauerstoffs)|
```

Ist der Fehler größer als `1×10⁻⁶`, wird diese Zeile als Warnung geführt.

## Literatur

- Cross, W., Iddings, J. P., Pirsson, L. V., Washington, H. S. (1902). A quantitative chemico-mineralogical classification and nomenclature of igneous rocks. *The Journal of Geology*, 10, 555–690.
- Kelsey, C. H. (1965). Calculation of the C.I.P.W. norm. *Mineralogical Magazine*, 34, 276–282.
- Verma, S. P., Torres-Alvarado, I. S., Velasco-Tapia, F. (2003). A revised CIPW norm. *Schweizerische Mineralogische und Petrographische Mitteilungen*, 83, 197–216.
- Philpotts, A. R., Ague, J. J. (2009). *Principles of Igneous and Metamorphic Petrology* (2nd ed.). Cambridge University Press.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
