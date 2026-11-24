---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Eine Diagrammvorlage erstellen

Für Diagrammvorlagen, die in der Online-Bibliothek der Diagrammvorlagen noch nicht enthalten sind, können Sie eine eigene Diagrammvorlage erstellen. Wenn Sie eine benutzerdefinierte Vorlage anlegen und zu einem Vorlagenpaket packen, können Sie sie schnell mit anderen Forschenden teilen.

Sie können Ihre Vorlage auch den Entwicklern zur Aufnahme in die Online-Bibliothek der Diagrammvorlagen anbieten. Wir danken jeder mitwirkenden Person herzlich für ihren Beitrag.

## Schnelles Anlegen

Sie können jetzt über die Menüleiste `Datei` -> `Neue Diagrammvorlage` eine neue Diagrammvorlage erstellen, wie unten gezeigt:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template.webp')}
    alt="Diagrammeinstellungen"
    style={{width: '50%'}}
  />
</div>

Nach einem Klick auf 【Neue Diagrammvorlage】 erscheint ein Dialog zum Anlegen einer neuen Diagrammvorlage:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template_interface.webp')}
    alt="Oberfläche zum Anlegen eines Diagramms"
    style={{width: '100%'}}
  />
</div>

Für eine neue benutzerdefinierte Diagrammvorlage sind vor allem drei Teile zu konfigurieren:

1.  **Standardmäßig unterstützte Sprachen**: Im Auswahlfeld rechts können Sie eingebaute Sprachkurzoptionen wählen. Wir stellen zehn Sprachen bereit, darunter vereinfachtes Chinesisch, traditionelles Chinesisch, amerikanisches Englisch, Japanisch, Russisch, Koreanisch und Deutsch.

    > Zu beachten ist: Die Standardsprache ist die Sprache, auf die zurückgefallen wird, wenn eine andere Sprache nicht übersetzt ist oder ein Fehler auftritt. Außerdem sind in der neuen Version benutzerdefinierte Sprachcodes nicht mehr erlaubt. Wenn Sie Unterstützung für Ihre Sprache benötigen, wenden Sie sich bitte an die Entwickler.

2.  **Klassifikation der Diagrammvorlage (Hierarchie)**: Wir stellen eine eingebaute Schnellstruktur der Klassifikation bereit. Diese Einstellung bestimmt die hierarchische Position Ihrer Vorlage in der Liste der Diagrammvorlagen.

3.  **Typ der Diagrammvorlage**: Derzeit werden zwei Typen unterstützt: **zweidimensionales Koordinatensystem** und Ternärdiagramm.

Klicken Sie nach Abschluss der Einstellungen auf 【OK】, um zur Oberfläche für das benutzerdefinierte Zeichnen zu gelangen. Als Nächstes liegt der Schwerpunkt der Bedienung auf der Funktionsleiste 【Bearbeiten】. Nach einem Klick auf 【Bearbeiten】 zeigt das System einen Bestätigungsdialog zum Bearbeiten des Diagramms. Nach der Bestätigung gelangen Sie in den Bearbeitungsmodus. Dort können Sie die verschiedenen Werkzeuge der Bearbeitungsleiste ansehen und verwenden.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/empty_diagram_template.webp')}
    alt="Leeres Diagramm"
    style={{width: '100%'}}
  />
</div>

## Ein Diagramm zeichnen

Im Bearbeitungsmodus sind die folgenden Vorgänge zulässig:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphical_toolbar.webp')}
    alt="Diagramm zeichnen"
    style={{width: '100%'}}
  />
</div>

* **Speichern**: Speichert die Diagrammvorlage. Nach dem Klick erzeugt das Programm standardmäßig anhand des aktuellen Zeichenzustands die entsprechende Miniaturansicht.
* **Speichern unter**: Speichert die Diagrammvorlage an einem anderen Dateiort.
* **Linie hinzufügen**: Nach dem Aktivieren wird der Modus „Linie hinzufügen“ geöffnet. Ein Klick auf den ersten Punkt im Zeichenbereich beginnt die Linie, ein Klick auf den zweiten Punkt schließt das Linienobjekt ab.
* **Text hinzufügen**: auch als Beschriftung bezeichnet. Nach dem Aktivieren wird der Modus „Text hinzufügen“ geöffnet. Ein Klick auf eine bestimmte Stelle in der Zeichnung erzeugt den Text. Der Standardtext ist `Text`. Position oder Inhalt ändern Sie im Eigenschaftenbereich des Ebenenbereichs.
* **Polygon hinzufügen**: Nach dem Aktivieren wird der Modus „Polygon hinzufügen“ geöffnet. Durch aufeinanderfolgende Klicks mit der linken Maustaste legen Sie Eckpunkte an und fügen ein geschlossenes Polygon hinzu. Ein Klick mit der rechten Maustaste schließt die Form.
* **Pfeil hinzufügen**: Nach dem Aktivieren wird der Modus „Pfeil hinzufügen“ geöffnet. Der Ablauf entspricht dem Anlegen einer Linie.
* **Funktion hinzufügen**: Nach dem Klick wird eine Standardfunktion `sin(x)` mit dem Definitionsbereich [-10, 10] hinzugefügt. Die Formel können Sie im Eigenschaftenbereich anpassen.
* **Rückgängig/Wiederholen**: Wenn kein Zeichenobjekt erzeugt oder gelöscht wurde, sind diese Funktionen deaktiviert. Standardmäßig werden in der Historie nur die letzten 10 Vorgänge gespeichert.
* **Löschen**: Löscht ein Zeichenobjekt. Wählen Sie zuerst das Objekt aus, zum Beispiel einen Text, und klicken Sie anschließend auf Löschen, um es zu entfernen.

### Linie hinzufügen

Unten sehen Sie ein Beispiel für den Eigenschaftenbereich beim Hinzufügen einer Linie. Über den Eigenschaftenbereich können Sie Position und weitere Eigenschaften der Linie genau anpassen.

**Die Schaltfläche 【Koordinate aufnehmen】 rechts neben jeder Koordinate erlaubt es, die Koordinate im Zeichenbereich neu auszurichten und einzufangen.** Nach dem Auslösen übernimmt der nächste Klick mit der linken Maustaste im Zeichenbereich automatisch die Koordinate der angeklickten Stelle.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_line.webp')}
    alt="Linie hinzufügen"
    style={{width: '100%'}}
  />
</div>

### Pfeil hinzufügen

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_arrow.webp')}
    alt="Pfeil hinzufügen"
    style={{width: '100%'}}
  />
</div>

### Polygon hinzufügen

**Nachdem Sie das Hinzufügen eines Polygons aktiviert haben, bestätigen Sie die Eckpunkte des Polygons im Zeichenbereich durch aufeinanderfolgende Klicks mit der linken Maustaste. Währenddessen sehen Sie den Vorschaubereich des Polygons. Ein Druck der rechten Maustaste schließt das Anlegen des Polygons ab.** Ein Polygonobjekt besitzt eine Liste der Eckpunkte. Sie können den jeweiligen Eckpunkt jederzeit auswählen, ändern und löschen.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_polygon.webp')}
    alt="Polygon hinzufügen"
    style={{width: '100%'}}
  />
</div>

### Text hinzufügen

Nachdem Sie das Hinzufügen von Text aktiviert haben, erzeugt ein beliebiger Klick mit der linken Maustaste im Zeichenbereich an der entsprechenden Stelle ein Textobjekt. In der Statusleiste am unteren Rand der Zeichenfläche können Sie sehen, zu welcher Sprache der Inhalt des aktuellen Textobjekts gehört.

Da Diagramme Mehrsprachigkeit von Haus aus unterstützen, wird die Übersetzung mehrsprachiger Inhalte weiter unten ausführlich erläutert.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_text.webp')}
    alt="Text hinzufügen"
    style={{width: '100%'}}
  />
</div>

### Funktion hinzufügen

Das Hinzufügen einer Funktion unterscheidet sich von den vorherigen Objekten. **Nach einem Klick auf die Schaltfläche Funktion hinzufügen wird sofort ein standardmäßiges Funktionsobjekt hinzugefügt.** Die Standardfunktion ist `sin(x)`. Im Eigenschaftenbereich können Sie die Funktionsformel ändern und damit Ihre Funktion festlegen.

Für die Eigenschaften eines Funktionsobjekts sind zwei Parameter am wichtigsten: **Definitionsbereich** und **Abtastpunkte**.

* **Der Definitionsbereich legt den Anzeigebereich der Funktion auf der Zeichenfläche fest.**
* **Die Abtastpunkte steuern die Zeichengenauigkeit der Funktion auf der Zeichenfläche** und beeinflussen damit die Genauigkeit des Algorithmus, mit dem die Maus die Auswahl einfängt. Der Standardwert ist `1000`. Je höher die Genauigkeit, desto feiner die Darstellung, allerdings steigt auch die Renderlast des Programms.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_function.webp')}
    alt="Funktion hinzufügen"
    style={{width: '100%'}}
  />
</div>

## Die Diagrammvorlage vervollständigen

Nach dem Zeichnen der grundlegenden Grafik benötigt eine vollständige Vorlage außerdem:

1.  **Daten und Skript**: Damit werden die Eingabedaten der Diagrammvorlage und der Algorithmus der Datenberechnung festgelegt, also angegeben, wie die Daten berechnet und in das Diagramm eingetragen werden.
2.  **Diagrammdokument**: Damit wird der Hilfeinhalt erläutert, der dieses Diagramm beschreibt.
3.  **Mehrsprachigkeit**: Wenn die Diagrammvorlage auf mehr als zwei Sprachen eingestellt ist, muss der mehrsprachige Teil übersetzt werden. Dazu gehören das zuvor genannte 【Diagrammdokument】 sowie Textobjekte auf der Diagrammfläche und weitere Inhalte.

Die Diagrammvorlage lässt sich zwar auch in der Bedienoberfläche des Diagramms vervollständigen. Wir empfehlen jedoch, dies im Bearbeitungsfenster des Diagramms zu tun, weil es dort bequemer ist.

### Daten und Skript

Daten und Skript sind der entscheidende Teil der Diagrammvorlage, weil sie festlegen, wie die Daten im Diagramm berechnet und schließlich dargestellt werden.

Zwei Angaben werden benötigt: die **Variablenparameter** der Diagrammvorlage und der **Berechnungsalgorithmus (Skript)**, wie unten gezeigt:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_data_and_script.webp')}
    alt="Diagramm – Daten und Skript"
    style={{width: '100%'}}
  />
</div>

Für die **Variablenparameter** der Diagrammvorlage können Sie oben die entsprechenden Spalten hinzufügen, die die für dieses Diagramm benötigten Eingabespalten darstellen. Dabei gibt es zwei optionale Parameter.

1. Unit gibt die Einheit der Eingabespalte an. Sie dient nur der Anzeige und Erläuterung und beeinflusst den Datenalgorithmus nicht.
2. Group gibt die Gruppierung der Eingabespalte an. Sie dient nur der Anzeige und Erläuterung und beeinflusst den Datenalgorithmus nicht.

Zu beachten ist: **Die Spaltenüberschriften, die Sie in der Datentabelle festlegen, gelten standardmäßig als registrierte Variablen, die das unten stehende Skript unmittelbar aufrufen kann.** Sie müssen die Variablen der Eingabespalten im Skript also nicht erneut definieren.

Der **Berechnungsalgorithmus (Skript)** wird standardmäßig in `JavaScript` geschrieben. Der standardmäßige Beispielinhalt des Skripts lautet `return [X,Y]`. Damit werden die in der Datentabelle definierten Eingabespalten `X` und `Y` an die zweidimensionalen Koordinatenachsen zurückgegeben.

Für ein TAS-Diagramm sollten die Parameter zum Beispiel `SiO2, Na2O, K2O` sein. Der Skriptinhalt lautet dann:

```javascript
// Berechnung anhand der Variablen K2O + Na2O
var result1 = K2O + Na2O;
// SiO2 verwenden
var result2 = SiO2;
// Zwei berechnete Werte zurückgeben. Hinweis: Für das standardmäßige zweidimensionale Koordinatendiagramm gibt es nur zwei Rückgabewerte.
// Die erste Position ist der Rückgabewert für X, die zweite der Rückgabewert für Y.
return [result2, result1]
```

Sie können das Skript auch so schreiben:

```javascript
var result = K2O + Na2O
return [SiO2, result]
```

Beachten Sie, dass die Position der Rückgabewerte fest ist. In `[x, y]` ist der erste Wert stets X (untere Achse) und der zweite Y (linke Achse).

:::info

Für ein Ternärdiagramm lautet das endgültige Rückgabeformat `[A, B, C]`. Der erste Wert ist A (untere Achse), der zweite B (linke Achse) und der dritte C (rechte Achse).

:::

### Diagrammdokument

Das Diagrammdokument ist ein notwendiger Schritt, damit andere Forschende die Grundinformationen und die Verwendung dieses Diagramms schnell verstehen.

Das Diagrammdokument wird auf der Seite der Hilfedokumentation verfasst. Wir stellen eine einfache Symbolleiste bereit, die den üblichen Dokumentbedarf abdeckt. Sie können den Inhalt natürlich auch in `Office Word` verfassen und ihn anschließend hier einfügen. Der größte Teil der Formatierung ist beiderseits unmittelbar verwendbar.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_help_document.webp')}
    alt="Diagramm – Hilfedokument"
    style={{width: '100%'}}
  />
</div>

Für den Aufbau des Diagrammdokuments empfehlen wir, den folgenden Standard einzuhalten:

*   **Einführung**: Erläutert Grundkonzept und Funktion der Diagrammvorlage und hilft den Benutzerinnen und Benutzern, sie schnell zu verstehen.
*   **Datenformat**: Gibt das Eingabedatenformat und die Spaltenüberschriften an, die für ein gültiges Einlesen der Daten erforderlich sind.
*   **Literatur**: Führt die Literatur auf, die zum Erstellen der Diagrammvorlage und ihres Inhalts verwendet wurde.
*   **Mitwirkende**: Namen der Personen, die an der Erstellung der Diagrammvorlage mitgewirkt haben. Es ist sogar erwünscht, dass Sie Ihre persönliche Website angeben.

### Mehrsprachigkeit

Wir haben zwei Wege eingerichtet, Mehrsprachigkeit festzulegen:

Der erste Weg ist die Option **Sprache wechseln** in der oberen Funktionsleiste der Diagrammoberfläche. Damit können Sie für eine zweite Sprache bestimmte Inhalte festlegen.

Der zweite Weg ist, mehrsprachige Inhalte im Bearbeitungsfenster des Diagramms zu bearbeiten, wie in der folgenden Abbildung gezeigt.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/illustrated_multilingual.webp')}
    alt="Diagramm – Daten und Skript"
    style={{width: '100%'}}
  />
</div>
