---
sidebar_position: 2
---

# 🎨 Diagrammerstellung

Dieser Abschnitt stellt das integrierte **Geowissenschaftliche Diagrammerstellungsmodul** der Software vor. Es behandelt die Klassifizierung, Verwaltung und Erweiterungsmechanismen (JSON/ZIP) der **Vorlagenbibliothek**, bietet eine detaillierte Analyse des Layouts der **Plot-Oberfläche** (Menüleiste, Symbolleiste, Ebenenliste, Eigenschaftsfenster) und seiner Kernfunktionen (wie Datenimport, Ebenenbearbeitung, visuelle Einstellungen und Integration von Drittanbietersoftware) und bietet einen **Vollständigen Workflow-Leitfaden** – von der Auswahl einer Vorlage bis zum Export des endgültigen Plots. 🌍

## Ziele

Auf der Seite Illustrierte Diagrammerstellung werden wir weitere grundlegende geowissenschaftliche Vorlagen integrieren, einschließlich, aber nicht beschränkt auf: Tektonische Umgebungsdiskriminierungsdiagramme, Gesteinsklassifizierungsdiagramme und grundlegende Geothermometerdiagramme. **Unser ultimatives Ziel ist es, ein umfassendes Plot-Toolkit für die Geowissenschaften zu erstellen, um Forschern maximalen Komfort zu bieten.** 🧪

Die Klassifizierungslogik für Vorlagen ist derzeit nach akademischer Disziplin organisiert:

![tutorial_plot1](/img/v0.6.1/tutorial_plot1.png)

:::info

Da Vorlagen aktualisiert werden, können sich einige Klassifizierungsstrukturen ändern.

Wir begrüßen wertvolles Feedback während Ihrer Nutzung, um die Benutzerfreundlichkeit und Bequemlichkeit der Software zu verbessern. 🌹

:::

## Schnellstart

### Vorlagenbibliothek

Wir kategorisieren Diagrammvorlagen in zwei Haupttypen: **Offizielle integrierte Vorlagen** und **Persönliche benutzerdefinierte Vorlagen**.

**Offizielle integrierte Vorlagen** werden von uns kontinuierlich aktualisiert und gepflegt. Benutzer können auf die neuesten Versionen zugreifen, ohne die Software zu aktualisieren, um sicherzustellen, dass sie immer über die umfassendsten und maßgeblichsten Vorlagenressourcen verfügen.

**Persönliche benutzerdefinierte Vorlagen** eignen sich für Szenarien, in denen die erforderliche Vorlage nicht in der offiziellen Bibliothek gefunden wird oder wenn benutzerdefinierte Vorlagen für spezifische Forschungsbedürfnisse erstellt werden müssen. Benutzer können diese Vorlagen nicht nur selbst erstellen, sondern sie auch exportieren, um sie einfach mit anderen Forschern zu teilen, was den akademischen Austausch und die Verbreitung erleichtert.

> *In Zukunft planen wir den Aufbau einer dedizierten Diagrammvorlagen-Community, in der Benutzer verschiedene **Persönliche benutzerdefinierte Vorlagen** einfach erstellen, hochladen, teilen und herunterladen können, was die Flexibilität und Skalierbarkeit des Systems weiter verbessert.*

#### Hauptseite

Standardmäßig zeigt das Plot-Modul beim Betreten die integrierte Geowissenschaftliche Vorlagenbibliothek an. Die Oberfläche ist in drei Hauptabschnitte unterteilt:

* **Links - Vorlagenliste**: Zeigt alle Vorlagenhierarchien und entsprechenden Vorlagen an, einschließlich benutzerdefinierter Vorlagenlisten.
* **Oben Rechts - Navigationsleiste**: Aktualisiert sich basierend auf der ausgewählten Hierarchie in der Vorlagenliste, um verschiedene Inhaltsebenen anzuzeigen.
* **Unten Rechts - Vorlagenkarten**: Zeigt die Plot-Karten unter der aktuellen Hierarchie an, einschließlich Namen und Vorschaubildern.

![tutorial_plot2](/img/v0.6.1/tutorial_plot2.png)

Wählen Sie eine Vorlagenkarte aus und klicken Sie darauf, um die spezifische Plot-Oberfläche aufzurufen.

**Diese Vorlagen sind hochgradig erweiterbar.** Wir verwenden das `JSON`-Format, um Kernvorlageninformationen zu speichern, und das `ZIP`-Format, um vollständige Ressourcenpakete zu packen. Mit diesem Design können **Forscher nicht nur ihre eigenen Vorlagen erstellen, sondern sie auch packen und mit anderen teilen, um sie schnell wiederzuverwenden.**

**Das System unterstützt derzeit serverseitige Speicherung**, sodass die Vorlagenliste dynamisch aktualisiert werden kann, ohne dass ein Software-Update erforderlich ist.

Unter lokalen Internetbedingungen können Benutzer die integrierte Vorlagenliste manuell über die Menüleiste überprüfen und aktualisieren oder die automatische Überprüfung in den Einstellungen aktivieren, um die neuesten Ressourcen sicherzustellen.

#### Grundlegende Symbolleiste

Die Funktionen der Menüleiste sind in zwei Hauptkategorien unterteilt:

1. **Datei**: Hauptsächlich zum Erstellen, Öffnen und Importieren von Vorlagen.
   1. **Neue Vorlage**: Wird verwendet, um benutzerdefinierte Diagramme zu erstellen; ein Klick hierauf öffnet ein interaktives Popup.
   2. **Vorlage öffnen**: Wird verwendet, um eine Vorlage vorübergehend zu öffnen; unterstützt `json`-Dateien und `zip`-Ressourcenpakete.
   3. **Vorlage importieren**: Wird verwendet, um externe Vorlagenpakete (`zip`) in die lokale benutzerdefinierte Vorlagenliste zu importieren.
2. **Vorlagen**: Hauptsächlich für Updates integrierter Vorlagen. **Auf Updates für integrierte Vorlagen prüfen**: Wird verwendet, um die neuesten Vorlagenlisten und Updates abzurufen.

### Plot-Oberfläche

#### Layout

Die Plot-Oberfläche ist in fünf Hauptteile unterteilt:

- **Symbolleiste**: Enthält Schnellzugriffsschaltflächen und drei Funktionsregisterkarten: Plotten, Daten und Bearbeiten.
- **Ebenenliste (Objekte)**: Eine Liste von Zeichnungselementen auf der Vorlage. Durch Klicken auf ein Element können Sie dessen Eigenschaften ändern.
- **Zeichenfläche**: Der zentrale Bereich zum Anzeigen des Plots, Importieren von Daten, visuellen Einstellungen und Anzeigen von Vorlagenanweisungen.
- **Statusleiste**: Zeigt grundlegende Plot-Informationen an, einschließlich der aktuellen Diagrammsprache und Koordinateninformationen.
- **Eigenschaftsfenster**: Zeigt die Attribute des ausgewählten Zeichnungselements (z. B. Farbe, Größe) an, um den gewünschten visuellen Effekt zu erzielen.

![tutorial_plot3](/img/v0.6.1/tutorial_plot3.png)

#### Plot-Symbolleiste

Die Symbolleiste besteht aus **Schnellzugriffsschaltflächen** und einer **Menüleiste**. **Schnellzugriffsschaltflächen** sind für häufige Operationen gedacht, während die **Menüleiste** spezifische spezialisierte Funktionen bietet.

Standardmäßig zeigt das System die Plot-Symbolleiste an. Allgemeine Benutzer müssen normalerweise nicht die **Bearbeitungs-Symbolleiste** verwenden – dies ist ein fortgeschrittenes Werkzeug, das zum Erstellen und Erweitern von Vorlagen verwendet wird.

![tutorial_plot4](/img/v0.6.1/tutorial_plot4.png)

### Ebenenliste

Zeichnungselemente sind in 7 Haupttypen unterteilt:

- **Linie (Line)**: Definiert grundlegende Kartengrenzen oder Segmente.
- **Text (Text)**: Beschriftungen und Anmerkungen.
- **Polygon (Polygon)**: Geschlossene Formen innerhalb des Plots.
- **Pfeil (Arrow)**: Gerichtete Zeichnungsobjekte.
- **Funktion (Function)**: Ermöglicht Benutzern die Eingabe benutzerdefinierter mathematischer Funktionen und Definitionsbereiche.
- **Achsen (Axes)**: Koordinatenachsen für den Plot.
- **Datenpunkt (Data Point)**: Elemente, die importierte Daten darstellen.

**Standard-Renderreihenfolge (Oben nach Unten): `Text > Pfeil > Punkt > Funktion > Linie > Polygon > Achsen`**.
