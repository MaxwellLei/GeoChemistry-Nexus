---
sidebar_position: 2
---

# Datenvorverarbeitung

Die Seite **Datenvorverarbeitung (Data Preprocessing)** dient der schnellen Bereinigung, Normalisierung und Standardisierung von Ganzgesteins-Hauptelementdaten vor nachgelagerten Berechnungen (z. B. CIPW, Diagramme und geochemische Indizes).

![plot_home_ui](/img/v0.7.1/data_preprocessing1.webp)

## Modulfunktionen

Das Modul deckt gängige geochemische Vorverarbeitungsaufgaben ab:

- Import von Tabellendaten und Erkennung von Oxid-Spalten
- Normalisierung der Hauptelemente auf wasserfreie Basis
- Schätzung der Fe-Valenzverteilung (FeO / Fe2O3)
- Berechnung gängiger geochemischer Indizes (z. B. Mg#, A/CNK, A/NK)
- Export der Ergebnisse für nachfolgende Workflows

## Empfohlener Ablauf

1. **Datentabelle vorbereiten**  
   Empfohlen sind standardisierte Oxid-Überschriften wie `SiO2`, `TiO2`, `Al2O3`, `FeOT`, `FeO`, `Fe2O3`, `MgO`, `CaO`, `Na2O`, `K2O` usw.

2. **Daten laden**  
   Sie können Daten direkt in die Tabelle einfügen oder zuerst Beispieldaten laden, um den Ablauf zu prüfen.

3. **Vorverarbeitungsoptionen konfigurieren**  
   Aktivieren oder deaktivieren Sie Module nach Bedarf:
   - Wasserfreie Normalisierung
   - Methode zur Fe-Valenz-Schätzung
   - Berechnung geochemischer Indizes

4. **Verarbeitung ausführen**  
   Führen Sie die Vorverarbeitung aus und prüfen Sie den Inhalt des Ausgabe-Arbeitsblatts.

5. **Ergebnisse exportieren**  
   Exportieren Sie die Ergebnisse als `.csv` für weitere Analysen oder Diagramme.

## Wichtige Parameter

### 1) Wasserfreie Normalisierung (Anhydrous Normalization)

Nach Aktivierung werden flüchtige Bestandteile (z. B. LOI, H2O, CO2) abgezogen und Hauptoxide auf 100 % neu skaliert.

Für vergleichbare Hauptelement-Analysen über Proben hinweg wird diese Option empfohlen.

### 2) Fe-Valenz-Schätzung (Iron Valence Estimation)

Liegen nur Gesamteisen-Daten (`FeOT`) vor, kann die Verteilung auf FeO und Fe2O3 nach Strategie geschätzt werden:

- Automatische Schätzung aus Gesamteisen
- Rückrechnung aus vorhandenen FeO- / Fe2O3-Werten
- Korrektur mit empirischem Verhältnis `Fe3+/Fe`

Wählen Sie die Methode passend zu Datenqualität und Forschungsgegenstand.

### 3) Geochemische Indizes (Geochemical Indices)

Das Modul unterstützt die Berechnung von:

- `Mg#`
- `A/CNK`
- `A/NK`

Diese Indizes werden häufig zur Interpretation magmatischer Evolution und Aluminium-Sättigung verwendet.

## Eingabe und Ausgabe

### Eingabe

- Arbeitsblatt mit Probennummern und Oxid-Spalten
- Empfohlenes Zahlenformat: wt.%

### Ausgabe

- Bereinigte / normalisierte Spalten (abhängig von aktivierten Modulen)
- Spalten zur Fe-Valenz-Schätzung (wenn aktiviert)
- Spalten geochemischer Indizes (wenn aktiviert)
- Verarbeitungszusammenfassung (zur Nachverfolgung von Parametern und Ergebnissen)

## Nutzungshinweise

- Bewahren Sie Rohdaten in einem separaten Arbeitsblatt auf, um Überschreibungen zu vermeiden.
- Prüfen Sie vor der Verarbeitung die Spaltennamen, um Erkennungsfehler zu reduzieren.
- Für Publikationen oder Berichte dokumentieren Sie die gewählten Vorverarbeitungsparameter im Methodenteil.

---

Wenn Sie neue Vorverarbeitungsstrategien wünschen, reichen Sie gerne ein Issue oder eine Discussion im Projekt-Repository ein.
