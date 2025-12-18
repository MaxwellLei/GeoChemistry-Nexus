---
sidebar_position: 2
---

# 🎨 Illustriertes Plotten

Dieser Abschnitt stellt das integrierte **Geowissenschaftliche Illustrierte Plot-Modul** der Software vor. Er behandelt die Klassifizierung, Verwaltung und Erweiterungsmechanismen (JSON/ZIP) der **Vorlagenbibliothek**, bietet eine detaillierte Analyse des Layouts der **Plot-Oberfläche** (Menüleiste, Symbolleiste, Ebenenliste, Eigenschaftenbereich) und ihrer Kernfunktionen (wie Datenimport, Ebenenbearbeitung, visuelle Einstellungen und Integration von Drittanbietersoftware) und bietet einen **Vollständigen Workflow-Leitfaden** – von der Auswahl einer Vorlage bis zum Export des endgültigen Plots. 🌍

## Ziele

Auf der Seite "Illustriertes Plotten" werden wir weitere grundlegende geowissenschaftliche Vorlagen integrieren, einschließlich, aber nicht beschränkt auf: Diagramme zur Diskriminierung tektonischer Umgebungen, Gesteinsklassifikationsdiagramme und grundlegende Geothermometer-Diagramme. **Unser ultimatives Ziel ist es, ein umfassendes Plot-Toolkit für Geowissenschaften zu erstellen, um Forschern maximalen Komfort zu bieten.** 🧪

Die Klassifizierungslogik für Vorlagen ist derzeit nach akademischen Disziplinen organisiert:

![Illustrated Template Classification](./imgs/Illustrated_Template_Classification.png)

:::info

Da Vorlagen aktualisiert werden, können sich einige Klassifizierungsstrukturen ändern.

Wir begrüßen wertvolles Feedback während Ihrer Nutzung, um die Benutzerfreundlichkeit und Bequemlichkeit der Software zu verbessern. 🌹

:::

## Schnellstart

### Vorlagenbibliothek

#### Hauptseite

Standardmäßig zeigt das Plot-Modul beim Aufrufen die integrierte Geowissenschaftliche Vorlagenbibliothek an (sofern keine benutzerdefinierten Vorlagen definiert sind). Die Oberfläche ist in drei Hauptbereiche unterteilt:

* **Links - Vorlagenliste**: Zeigt alle Vorlagenhierarchien und entsprechenden Vorlagen an, einschließlich Listen benutzerdefinierter Vorlagen.
* **Oben Rechts - Navigationsleiste**: Aktualisiert sich basierend auf der ausgewählten Hierarchie in der Vorlagenliste, um verschiedene Ebenen von Inhalten anzuzeigen.
* **Unten Rechts - Vorlagenkarten**: Zeigt die Plot-Karten unter der aktuellen Hierarchie an, einschließlich Namen und Vorschaubildern.

![Illustrated Template Classification](./imgs/Plot_Template_Library.png)

Wählen Sie eine Vorlagenkarte aus und klicken Sie darauf, um die spezifische Plot-Oberfläche aufzurufen.

**Diese Vorlagen sind hochgradig erweiterbar.** Wir verwenden das `JSON`-Format, um Kerninformationen der Vorlage zu speichern, und das `ZIP`-Format, um vollständige Ressourcenpakete zu verpacken. Mit diesem Design können **Forscher nicht nur ihre eigenen Vorlagen erstellen, sondern sie auch verpacken und mit anderen teilen, um sie schnell wiederzuverwenden.**

**Das System unterstützt derzeit serverseitige Speicherung**, sodass die Vorlagenliste dynamisch aktualisiert werden kann, ohne dass ein Software-Update erforderlich ist.

Unter lokalen Internetbedingungen können Benutzer manuell über die Menüleiste nach Updates für die integrierte Vorlagenliste suchen oder die automatische Überprüfung in den Einstellungen aktivieren, um die neuesten Ressourcen sicherzustellen.

#### Menüleiste

Die Funktionen der Menüleiste sind in zwei Hauptkategorien unterteilt:

1. **Datei**: Hauptsächlich zum Erstellen, Öffnen und Importieren von Vorlagen.
   1. **Neue Vorlage**: Wird verwendet, um benutzerdefinierte Diagramme zu erstellen; Klicken öffnet ein interaktives Popup.
   2. **Vorlage öffnen**: Wird verwendet, um eine Vorlage vorübergehend zu öffnen; unterstützt `json`-Dateien und `zip`-Ressourcenpakete.
   3. **Vorlage importieren**: Wird verwendet, um externe Vorlagenpakete (`zip`) in die lokale benutzerdefinierte Vorlagenliste zu importieren.
2. **Vorlagen**: Hauptsächlich für Updates integrierter Vorlagen.
   1. **Nach Updates für integrierte Vorlagen suchen**: Wird verwendet, um die neuesten Vorlagenlisten und Updates abzurufen.
   2. **Nach Updates für Klassifizierungsstrukturen suchen**: Bietet empfohlene integrierte Klassifizierungsstrukturen beim Erstellen neuer Vorlagen.

### Plot-Oberfläche

#### Layout

Die Plot-Oberfläche ist in vier Hauptteile unterteilt:

- **Symbolleiste**: Enthält Schnellzugriffsschaltflächen und drei funktionale Registerkarten: Plotten, Daten und Bearbeiten.
- **Ebenenliste (Objekte)**: Eine Liste von Zeichnungselementen auf der Vorlage. Durch Klicken auf ein Element können Sie dessen Eigenschaften ändern.
- **Plot-Leinwand**: Der zentrale Bereich zum Anzeigen des Plots, Importieren von Daten, visuellen Einstellungen und Anzeigen von Vorlagenanweisungen.
- **Eigenschaftenbereich**: Zeigt die Attribute des ausgewählten Zeichnungselements (z. B. Farbe, Größe) an, um den gewünschten visuellen Effekt zu erzielen.

![Plot_Main_View](imgs/Plot_Main_View.png)

#### Plot-Symbolleiste

Die Symbolleiste besteht aus **Schnellzugriffsschaltflächen** und einer **Menüleiste**. **Schnellzugriffsschaltflächen** sind für häufige Operationen gedacht, während die **Menüleiste** spezifische spezialisierte Funktionen bietet.

Standardmäßig zeigt das System die Plot-Symbolleiste an. Allgemeine Benutzer müssen normalerweise nicht die **Bearbeitungssymbolleiste** verwenden – sie ist ein fortgeschrittenes Werkzeug zum Erstellen und Erweitern von Vorlagen.

![plot_toolbar](imgs/plot_toolbar.png)

* **Schnellzugriffe**
  * **In Zwischenablage kopieren**: Eine schnelle Aktion oben links, um den aktuellen Plot als Bild zu kopieren.
  * **Einrasten**: Standardmäßig aktiviert; hebt Objekte hervor, wenn die Maus darüber schwebt, um die Auswahl zu erleichtern. Wenn deaktiviert, werden beim Klicken auf Objekte oder Achsen deren Eigenschaften nicht automatisch angezeigt.
  * **Hilfe**: Zeigt den "Leitfaden" für die aktuelle Vorlage an (falls im Paket enthalten). Er wird standardmäßig in der aktuellen Sprache der Software angezeigt und fällt auf Englisch zurück, wenn nicht verfügbar.
* **Daten**
  * **Daten importieren**: Wechselt zur Registerkarte Daten. Sie können auch manuell auf die Registerkarte Daten klicken.
  * **Daten löschen**: Löscht alle geplotteten Datenpunkte, ohne die tatsächlichen Daten in der Tabelle zu löschen.
* **Ansicht**
  * **Ansicht zurücksetzen**: Setzt die Ansicht der Leinwandkoordinaten auf das optimale Zentrum zurück.
  * **Koordinate**: Zeigt den Koordinaten-Tracker an/versteckt ihn. Wenn aktiviert, zeigt er die Echtzeit-Mauskoordinaten an. Dies ist standardmäßig deaktiviert, da die Statusleiste diese Informationen jetzt bereitstellt.
* **Auswahl**
  * **Auswahl aufheben**: Löscht die aktuelle Auswahl. Sie können auch mit der rechten Maustaste auf die Leinwand klicken, um dies auszulösen.
* **Exportieren**
  * **Exportieren**: Speichert die aktuelle Leinwand in Formaten wie `.png`, `.jpg`, `.bmp`, `.webp` und `.svg`. Für Forschungsarbeiten empfehlen wir dringend die Verwendung des **SVG**-Vektorformats.
* **Einstellungen**: Passt Leinwandeigenschaften an.
  * **Legendeneinstellungen**: Passt Legendenposition, Anordnung und Sichtbarkeit an.
  * **Plot-Einstellungen**: Passt Plot-Titel, Achsenbeschriftungen, Schriftarten und Farben an.
  * **Skripteinstellungen**: Verwaltet Datenberechnungsregeln für die Vorlage. Standardbenutzer müssen dies normalerweise nicht ändern.
  * **Gittereinstellungen**: Konfiguriert die Gittereigenschaften der Leinwand.
* **Sprache**: Ermöglicht das Umschalten der Vorlagensprache in Echtzeit, um Anforderungen für nationale und internationale Veröffentlichungen zu erfüllen.
* **Drittanbieter**: Unterstützt die direkte Integration mit Designsoftware von Drittanbietern für erweiterte Nachbearbeitung. Derzeit werden **Inkscape**, **CorelDRAW** und **Adobe Illustrator** unterstützt. Sie können die Anwendungspfade in den Einstellungen festlegen.
