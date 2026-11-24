---
sidebar_position: 3
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Geothermobarometer

In frühen Versionen wurde dieses Modul kurz **GTM** genannt. Später wurde es zum Geothermometer und trägt heute den förmlichen Namen Geothermobarometer.

Frühe Versionen enthielten Geothermometer-Berechnungen für Minerale wie Zirkon, Sphalerit, Quarz, Xenotim, Chlorit und Biotit. Das Geothermobarometer der neuen Version wird noch angepasst und aktualisiert und wird in Kürze nachgezogen.

## Oberfläche

Das Modul Geothermobarometer gliedert sich insgesamt in zwei Teile:

1. Links befindet sich die **Liste der Geothermobarometer-Vorlagen**. Hier können Sie eine für Sie passende Geothermobarometer-Vorlage suchen, oder Sie können eine eigene Berechnungsvorlage für ein Geothermobarometer erstellen.
2. Rechts befindet sich die **Hilfedokumentation der derzeit ausgewählten Geothermobarometer-Vorlage**.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geothermobarometer_interface.webp')}
    alt="Oberfläche des Geothermobarometers"
    style={{width: '100%'}}
  />
</div>

## Funktionen

Das Modul Geothermobarometer ist von Anfang an **der Form von Excel nachempfunden**, um die Einstiegshürde zu senken. **Geothermobarometer werden vor allem in zwei Formen verwendet**:

1. **Online-Geothermobarometer-Vorlagen**: Die standardmäßigen **Online-Geothermobarometer-Vorlagen** können Sie nicht ändern, die Umsetzungslogik des Algorithmus aber einsehen. Sie können, ohne die Software zu aktualisieren, bei bestehender Netzwerkverbindung weitere neueste Vorlagen für Geothermobarometer beziehen. Forschende können suchen, filtern und eine für sie passende Vorlage wählen und die Daten gemäß dem Inhalt der Anleitung eintragen und berechnen. **Dieser Weg ist unmittelbarer und schneller und erfordert keine Programmierkenntnisse.**
2. **Eigenständiges Berechnungsfenster**: Forschende können über die obere Menüleiste **Datei -> Eigenständige Tabelle öffnen** ein eigenständiges Berechnungsfenster öffnen. Im eigenständigen Fenster dürfen Forschende selbst programmieren und verschiedene Geothermobarometer zu einer kombinierten Berechnung verwenden. Die Funktionen, die den einzelnen Geothermobarometern entsprechen, finden Sie im Teil der fortgeschrittenen Tutorials. **Dieser Weg ist komplexer und vergleichsweise flexibel.**

Natürlich erlaubt das Modul Geothermobarometer den Forschenden auch, ein eigenes benutzerdefiniertes Geothermobarometer zu erstellen. Eine fertig erstellte Geothermobarometer-Vorlage lässt sich exportieren und an andere Forschende verteilen, damit diese sie schnell verwenden können.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/export_geothermobarometer_template.webp')}
    alt="Geothermobarometer exportieren"
    style={{width: '45%'}}
  />
</div>

## Anwendungsbeispiel

### Eine Online-Geothermobarometer-Vorlage verwenden

Filtern und wählen Sie zunächst im Modul Geothermobarometer in der linken Seitenleiste die entsprechende Berechnungsvorlage:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/select_and_apply_thermobarometer_template.webp')}
    alt="Geothermobarometer auswählen"
    style={{width: '100%'}}
  />
</div>

Im rechten Bereich können Sie die Erläuterungsdokumentation dieser Vorlage ansehen. Die obere Leiste zeigt außerdem Titel und Schlagwörter der derzeit ausgewählten Geothermobarometer-Vorlage. Nachdem Sie die passende Geothermobarometer-Vorlage gewählt haben, klicken Sie in der oberen Leiste auf die Schaltfläche **Anwenden**, um zur Berechnungsoberfläche des Geothermobarometers zu gelangen.

In der Oberfläche des Geothermobarometers sehen Sie die entsprechenden Eingabespalten. Sie können auch in der oberen Leiste auf die Schaltfläche **Beispiel** klicken, um Beispieldaten zu laden. Sie müssen nur die Daten entsprechend den Anforderungen der Spaltenüberschriften eintragen und anschließend **die Berechnungsformel kopieren** oder **das Ausfüllkästchen durch „Klicken und Ziehen“ verwenden** (ähnlich wie in Excel), um die Berechnung automatisch abzuschließen (wie in der folgenden Animation gezeigt).

> Zu beachten ist: Die Spalte `Sample` nimmt an der Berechnung nicht teil. Sie dient nur als Kommentar zur Kennzeichnung der Datenprobe.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/calculate_thermobarometer_using_formula.webp')}
    alt="Geothermobarometer mit einer Formel berechnen"
    style={{width: '80%'}}
  />
</div>

Die Geothermobarometer der aktuellen Version unterstützen außerdem das Ansehen der internen Berechnungsdetails.

Sie können die entsprechende Datenzeile mit der Maus auswählen, oben auf die Schaltfläche **Prüfen** klicken und im eingeblendeten Seitenbereich die Details der Zwischenergebnisse ansehen.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_calculation_details.webp')}
    alt="Berechnungsdetails des Geothermobarometers"
    style={{width: '100%'}}
  />
</div>

### Eigenständiges Fenster – benutzerdefinierte Funktionen

Wie zuvor beschrieben, öffnen Sie über die Menüleiste **Datei -> Freie Tabelle öffnen** ein eigenständiges freies Fenster.

Die Tabelle des eigenständigen Fensters ist eine leere Tabelle und enthält einige einfache Schaltflächen der Symbolleiste.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_free_form.webp')}
    alt="Freie Berechnungstabelle des Geothermobarometers"
    style={{width: '100%'}}
  />
</div>

Die Tabelle des eigenständigen Fensters verwenden Sie wie eine **alltägliche Excel-Tabelle: Sie können beliebige Zahlen eingeben und eine Funktion aufrufen, indem Sie `=Funktionsname(Übergabeparameter)` eingeben**.

Der Unterschied besteht darin, dass unsere Geothermobarometer-Berechnungen als entsprechende Berechnungsfunktionen registriert sind. Forschende können den Inhalt der Berechnung nach Bedarf flexibler selbst festlegen. Sie können sogar **in derselben Tabelle die Ergebnisse mehrerer Geothermobarometer vergleichen**.

:::note

Zu beachten ist: **In der Tabelle des eigenständigen Fensters lässt sich der Berechnungsverlauf der Zwischenschritte der entsprechenden Datenzeile nicht ansehen.**

:::
