---
sidebar_position: 4
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# EPMA-Mineralberechnung

**Die EPMA-Mineralberechnung** verarbeitet Mineralzusammensetzungen aus der Elektronenstrahl-Mikroanalyse. Die Oxidgehalte einer Reihe von Messpunkten werden mit der gewählten Mineralmethode in mineralchemische Ergebnisse umgerechnet. Diese dienen der Mineralbestimmung, dem Zusammensetzungsvergleich und der weiteren geologischen Interpretation.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/epma_mineral_calculation.webp')}
    alt="EPMA-Oberfläche"
    style={{width: '100%'}}
  />
</div>

## Oberfläche

Die EPMA-Mineralberechnung besteht aus zwei Oberflächen: der **Vorlagenbibliothek für EPMA-Mineralberechnungen** und der **EPMA-Berechnungsoberfläche**.

In der Vorlagenbibliothek können Sie suchen, filtern und eine passende Mineral-EPMA-Berechnung auswählen. Im Beschreibungsbereich ganz rechts sehen Sie die Grundinformationen zu diesen Vorlagen. Mit **Berechnung öffnen** starten Sie die Berechnung.

## Beispiel

Wählen Sie zum Beispiel eine Olivin-EPMA-Vorlage und öffnen Sie die Berechnungsoberfläche. Die Seite ist in einen oberen und einen unteren Bereich geteilt. In der oberen Leiste können Sie die ausführliche Hilfe zu dieser Vorlage ansehen oder Beispieldaten laden.

Geben Sie die von der Tabelle benötigten Daten ein und klicken Sie in der oberen Leiste auf **Berechnung ausführen**.

Der weißgraue Bereich ist der Eingabebereich, der blauviolette Bereich zeigt die Ergebnisse. Sie können die Ausgabe frei ansehen oder mit **Prüfen** die Zwischenschritte der Berechnung öffnen.

Der farbige Punkt vor jeder Zeilenüberschrift zeigt den Berechnungsstatus dieser Zeile:

* **Grau**: Die Zeile enthält Daten, wurde aber noch nicht berechnet
* **Grün**: Die Zeile wurde normal abgeschlossen, ohne Warnung und ohne Fehler
* **Orange**: Die Berechnung ist abgeschlossen, es gibt aber eine Warnung
* **Rot**: Die Berechnung dieser Zeile ist fehlgeschlagen
