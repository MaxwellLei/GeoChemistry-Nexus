---
sidebar_position: 5
---

# CIPW-Standardmineralberechnung

Das Modul **CIPW-Standardmineralberechnung** wandelt Ganzgesteins-Hauptelement-Oxidanalysen in idealisierte Standardmineral-Assemblagen um. Es wird häufig für die Klassifikation magmatischer Gesteine, petrographische Vergleiche sowie die Beurteilung von Silizium- und Aluminium-Sättigung eingesetzt.

Die CIPW-Norm (Cross-Iddings-Pirsson-Washington-Standardminerale) wurde 1902 von vier Petrologen vorgeschlagen. GeoChemistry Nexus implementiert den vollständigen Berechnungsablauf in einer Excel-ähnlichen Tabellenoberfläche mit Stapelberechnung, Diagnoseansicht und CSV-Export.

![cipw_ui](/img/v0.7.1/cipw1.webp)

## Modulfunktionen

- Zeilenweise Verarbeitung von Hauptelement-Oxid-Daten (wt%)
- Normalisierung der Oxide auf wasserfreie Basis
- Behandlung der Eisenverteilung (FeO / Fe₂O₃ / FeOT)
- Zuordnung der Oxide zu Standardmineralen in fester Prioritätsreihenfolge
- Ausgabe von Silizium-Sättigung, Aluminium-Sättigungsstatus und Massenbilanz-Diagnose
- Export der Ergebnisse für Diagramme oder weitere Analysen

## Oberflächenüberblick

Die CIPW-Seite gliedert sich in drei Bereiche:

1. **Obere Symbolleiste** — Parametereinstellungen und Aktionsschaltflächen
2. **Datentabelle** — Eingabe der Oxide, Diagnosespalten und Mineralergebnisse
3. **Unteres Diagnosepanel** — Detaillierte Berechnungsinformationen der aktuell gewählten Zeile

### Symbolleisten-Aktionen

| Schaltfläche | Beschreibung |
| --- | --- |
| **Anleitung** | Öffnet das integrierte Fenster zur Algorithmusbeschreibung |
| **Beispiel** | Füllt drei Beispielzeilen (Granit, Basalt, Andesit) |
| **Export** | Exportiert die aktuelle Tabelle als CSV-Datei |
| **Löschen** | Entfernt alle Eingaben und Berechnungsergebnisse |
| **Berechnung ausführen** | Führt die CIPW-Berechnung für alle Zeilen mit gültigen Daten aus |

### Fe³⁺/Fe-Verhältnis

Liegt nur **FeOT** (Gesamteisen, als FeO-Äquivalent) vor, teilt die Software es anhand des eingestellten **Fe³⁺/Fe**-Verhältnisses in FeO und Fe₂O₃ auf.

- Gültiger Bereich: `0` – `1`
- Standardwert: `0.15` (Le Maitre, 2002)

Sind gleichzeitig gemessene FeO- und Fe₂O₃-Werte vorhanden, verwendet die Software diese direkt und ignoriert FeOT.

## Anforderungen an Eingabedaten

Jede Zeile steht für eine Ganzgesteinsprobe; tragen Sie die **Massenprozent (wt%)** der Oxide in den Eingabespalten ein.

### Unterstützte Eingabeoxide

| Oxid | Beschreibung |
| --- | --- |
| `SiO2`, `TiO2`, `Al2O3`, `MgO`, `CaO`, `Na2O`, `K2O`, `P2O5` | Kern-Hauptelemente |
| `Fe2O3`, `FeO`, `FeOT` | Siehe Eisen-Regeln unten |
| `MnO` | Wird nach Mol-Verhältnis in FeO-Äquivalent umgerechnet |
| `ZrO2`, `Cr2O3` | Nebenmineral- / Spurenelement-Oxide |
| `CO2`, `S`, `F`, `Cl`, `SO3` | Flüchtige Bestandteile |

:::tip

Sie können zuerst im Modul **Datenvorverarbeitung** wasserfreie Normalisierung, Fe-Valenz-Schätzung usw. durchführen und die bearbeitete Tabelle anschließend in das CIPW-Modul einfügen.

:::

### Regeln für Eiseneingabe

| Eingabesituation | Behandlung |
| --- | --- |
| `FeO` und `Fe2O3` gleichzeitig | Gemessene Eisenverteilung wird verwendet |
| Nur `FeO` oder nur `Fe2O3` | Fehlender Wert gilt als 0; Warnung |
| Nur `FeOT` | Aufteilung in FeO / Fe₂O₃ nach Fe³⁺/Fe-Verhältnis |
| `FeOT` zusammen mit `FeO` oder `Fe2O3` | Inkonsistente Eingabe; FeOT wird ignoriert; Warnung |
| Keine Eisendaten | Wert 0; Warnung |

`MnO` wird vor der Berechnung stets in FeO-Äquivalent umgewandelt.

## Empfohlener Ablauf

1. **Daten vorbereiten**  
   Stellen Sie sicher, dass Oxid-Überschriften den Eingabespalten entsprechen und Werte nicht-negative wt% sind.

2. **Daten eingeben**  
   Einfügen oder manuelle Eingabe — eine Probe pro Zeile; leere Zeilen werden übersprungen.

3. **Fe³⁺/Fe einstellen** (bei Verwendung von FeOT)  
   Passen Sie das Verhältnis in der Symbolleiste an, wenn Ihr Datensatz andere Oxidationsannahmen erfordert.

4. **„Berechnung ausführen“ klicken**  
   Die Software verarbeitet alle gültigen Zeilen und schreibt Ergebnisse in die rechten Spalten.

5. **Diagnose prüfen**  
   Wählen Sie eine Ergebniszeile und prüfen Sie im unteren Panel Silizium-/Aluminium-Sättigung, Eisen-Behandlungsmodus, Hauptmineralzusammensetzung und Warnungen.

6. **Ergebnisse exportieren**  
   Exportieren Sie die vollständige Tabelle (Eingabe + Diagnose + Minerale) als CSV zur Archivierung oder weiteren Analyse.

## Beschreibung der Ausgabespalten

Berechnungsergebnisse erscheinen rechts neben dem Eingabebereich, getrennt durch die Spalte `│`.

### Diagnosespalten

| Spaltenname | Beschreibung |
| --- | --- |
| **Silizium-Sättigung** | Übersättigt / gesättigt / undersättigt |
| **Aluminium-Sättigungsstatus** | Peralkalisch / peraluminös / normal aluminös |
| **Massenbilanzfehler** | Abweichung der Mineralmasse-Summe von 100 % |

### Standardmineral-Spalten

Mineralabkürzungen folgen der CIPW-Konvention; in der deutschen Oberfläche werden deutsche Mineralnamen angezeigt. Gängige Minerale:

| Abkürzung | Mineral |
| --- | --- |
| `Q` | Quarz |
| `Or`, `Ab`, `An` | Orthoklas, Albit, Anorthit |
| `Le`, `Ne`, `Kp` | Leucit, Nephelin, Pseudoleucit |
| `Di`, `Hd`, `Wo` | Diopsid, Hedenbergit, Wollastonit |
| `En`, `Fs`, `Fo`, `Fa` | Enstatit, Ferrosilit, Forsterit, Fayalit |
| `Mt`, `Hm`, `Ilm` | Magnetit, Hämatit, Ilmenit |
| `Cc`, `Ap`, `Z` | Calcit, Apatit, Zirkon |

Pro Zeile werden nur Minerale angezeigt, deren Gehalt über dem Anzeigeschwellenwert liegt.

## Diagnosepanel

Nach der Berechnung können Sie eine beliebige Ergebniszeile anklicken, um Details einzusehen:

- **Silizium-Sättigung** — bei Undersättigung hervorgehoben
- **Aluminium-Sättigung** — bei Peralkalinität hervorgehoben
- **Eisen-Behandlungsmodus** — gemessen, geschätzt oder fehlend
- **Massenbilanzfehler** — bei größerer Abweichung hervorgehoben
- **Hauptmineralzusammensetzung** — absteigend nach Gehalt sortiert
- **Warnungen** — z. B. fehlende Eisendaten, inkonsistente FeOT-Eingabe

Über die Schaltfläche rechts in der Statusleiste können Sie das Diagnosepanel ein- und ausklappen oder maximieren.

## Berechnungsalgorithmus

Die Berechnung folgt dem klassischen CIPW-Standardmineral-Ablauf:

### 1. Datenvorverarbeitung und Normalisierung

- Wasserfreie Normalisierung der eingegebenen Hauptelement-Oxide (auf 100 %)
- Eisenverteilung: Aufteilung von FeOT in FeO und Fe₂O₃ gemäß Fe³⁺/Fe-Verhältnis
- Zusammenführung von MnO in FeO nach Mol-Verhältnis
- Umrechnung der Oxid-Massenprozente in Molzahlen

### 2. Bildung flüchtiger Minerale

Verbrauch flüchtiger Bestandteile in Prioritätsreihenfolge:

- Calcit (Cc): CO₂ + CaO
- Fluorit (Fl): F + CaO
- Pyrit (Py): S + FeO
- Halit (Hl): Cl + Na₂O
- Thenardit (Th): SO₃ + Na₂O

### 3. Bildung von Nebenmineralen

- Zirkon (Z): ZrO₂ + SiO₂
- Apatit (Ap): P₂O₅ + CaO
- Chromit (Cm): Cr₂O₃ + FeO
- Ilmenit (Ilm): TiO₂ + FeO
- Titanit (Tn): TiO₂ + CaO + SiO₂
- Rutil (Ru): verbleibendes TiO₂

### 4. Beurteilung des Aluminium-Sättigungsstatus

- **Peralkalisch (Peralkaline)**: Na₂O + K₂O > Al₂O₃
- **Normal aluminös (Metaluminous)**: Al₂O₃ ≤ CaO + Na₂O + K₂O
- **Peraluminös (Peraluminous)**: Al₂O₃ > CaO + Na₂O + K₂O

### 5. Bildung von Feldspäten und alkalischen Silikaten

- Orthoklas (Or): K₂O + Al₂O₃ + 6SiO₂
- Albit (Ab): Na₂O + Al₂O₃ + 6SiO₂
- Anorthit (An): CaO + Al₂O₃ + 2SiO₂
- Korund (Cor): verbleibendes Al₂O₃ (peraluminös)
- Aegirin (Ac): Na₂O + Fe₂O₃ + 4SiO₂ (peralkalisch)
- Restliche alkalische Silikate (ns, ks)

### 6. Bildung von Eisenoxiden

- Magnetit (Mt): Fe₂O₃ + FeO
- Hämatit (Hm): verbleibendes Fe₂O₃

### 7. Bildung dunkler Silikatminerale

- Diopsid (Di): CaO + MgO + 2SiO₂ (Mg-Endglied)
- Hedenbergit (Hd): CaO + FeO + 2SiO₂ (Fe-Endglied)
- Enstatit (En): MgO + SiO₂
- Ferrosilit (Fs): FeO + SiO₂
- Wollastonit (Wo): verbleibendes CaO + SiO₂

### 8. Silizium-Sättigungskorrektur

- **Übersättigt** (SiO₂ übrig): Bildung von Quarz (Q)
- **Gesättigt** (SiO₂ verbraucht): kein Quarz, keine Feldspathoide
- **Undersättigt** (SiO₂ fehlt): Umwandlung in Prioritätsreihenfolge —
  - Orthopyroxen (En + Fs) → Olivin (Fo + Fa)
  - Orthoklas (Or) → Leucit (Le)
  - Leucit (Le) → Pseudoleucit (Kp)
  - Albit (Ab) → Nephelin (Ne)

### 9. Ergebnisausgabe

- Multiplikation der Molzahlen mit Molmasse → Mineralmasse
- Normalisierung auf Massenprozent (wt%) als Ausgabe
- Diagnose zu Silizium-Sättigung, Aluminium-Sättigungsstatus und Massenbilanzfehler

:::info

Über die Schaltfläche **Anleitung** in der Symbolleiste können Sie dieselbe Algorithmusbeschreibung in der Software einsehen.

:::

## Literatur

- Cross, W., Iddings, J.P., Pirsson, L.V., Washington, H.S. (1902). *A Quantitative Chemico-Mineralogical Classification and Nomenclature of Igneous Rocks.*
- Le Maitre, R.W. (2002). *Igneous Rocks: A Classification and Glossary of Terms.* Cambridge University Press.
- Kelsey, C.H. (1965). Calculation of the CIPW norm. *Mineralogical Magazine*, 34, 276–282.
