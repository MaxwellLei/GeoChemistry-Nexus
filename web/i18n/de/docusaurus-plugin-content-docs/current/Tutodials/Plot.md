---
sidebar_position: 3
---

# Diagrammvorlagen

Dieser Abschnitt stellt das integrierte **Geowissenschaftliche Diagrammmodul** der Software vor. Er behandelt die Klassifizierung, Verwaltung und Erweiterungsmechanismen (JSON/ZIP) der **Vorlagenbibliothek**, analysiert detailliert das Layout der **Diagrammoberfläche** (Menüleiste, Symbolleiste, Ebenenliste, Eigenschaftsfenster) und deren Kernfunktionen (wie Datenimport, Ebenenbearbeitung, visuelle Einstellungen und Integration von Drittanbietersoftware) und bietet einen **vollständigen Workflow-Leitfaden** von der Vorlagenauswahl bis zum Export des endgültigen Diagramms. 🌍

## Ziele

Auf der Diagrammzeichnungsseite werden wir weitere grundlegende geowissenschaftliche Vorlagen integrieren, einschließlich, aber nicht beschränkt auf: Tektonische Umgebungsdiskriminierungsdiagramme, Gesteinsklassifizierungsdiagramme und grundlegende Geothermometerdiagramme. **Unser ultimatives Ziel ist es, ein umfassendes Zeichenwerkzeug für die Geowissenschaften zu schaffen und Forschenden maximalen Komfort zu bieten.** 🧪

Die Klassifizierungslogik für Vorlagen ist derzeit nach Fachdisziplin organisiert:

![tutorial_plot1](/img/v0.6.1/tutorial_plot1.png)

:::info

Da Vorlagen aktualisiert werden, können sich bestimmte Klassifizierungsstrukturen ändern.

Wir begrüßen wertvolles Feedback während der Nutzung, um die Benutzerfreundlichkeit und den Komfort der Software zu verbessern. 🌹

:::

## Schnellstart

### Vorlagenbibliothek

Wir unterteilen das Diagrammmodul in drei Kategorien: **Integrierte Cloud-Vorlagen**, **Persönliche benutzerdefinierte Vorlagen** und **Integrierte Werkzeugvorlagen**.

**Integrierte Cloud-Vorlagen**: Werden von uns kontinuierlich aktualisiert und gepflegt. Nutzer müssen die Software nicht aktualisieren; bei Internetverbindung können sie aktualisierte Diagrammvorlagen abrufen und stets über die umfassendsten und maßgeblichsten Vorlagenressourcen verfügen.

**Persönliche benutzerdefinierte Vorlagen**: Geeignet für Szenarien, in denen die benötigte Vorlage nicht in der offiziellen Bibliothek gefunden wird oder benutzerdefinierte Vorlagen für spezifische Forschungsbedürfnisse erstellt werden müssen. Nutzer können diese Vorlagen nicht nur selbst erstellen, sondern auch exportieren, um sie einfach mit anderen Forschenden zu teilen und den akademischen Austausch zu fördern.

> *In Zukunft planen wir den Aufbau einer dedizierten Diagrammvorlagen-Community, in der Nutzer verschiedene **persönliche benutzerdefinierte Vorlagen** einfach erstellen, hochladen, teilen und herunterladen können, um die Flexibilität und Erweiterbarkeit des Systems weiter zu steigern.*

**Integrierte Werkzeugvorlagen**: Nicht standardmäßige Cloud-Diagrammwerkzeuge werden separat ausgelagert. Derzeit umfasst dies: **REE-Spinnendiagramm, Spurenelement-Spinnendiagramm und Harker-Diagramm**.

#### Oberfläche

Standardmäßig zeigt das Zeichenmodul beim Betreten die integrierte geowissenschaftliche Vorlagenbibliothek an. Die Oberfläche ist in drei Hauptbereiche unterteilt:

*   **Links – Vorlagenliste**: Zeigt alle Vorlagenhierarchien und entsprechende Vorlagen an, einschließlich der benutzerdefinierten Vorlagenliste.
*   **Oben rechts – Navigationsleiste**: Aktualisiert sich basierend auf der in der Vorlagenliste gewählten Hierarchie, um Inhalte auf verschiedenen Ebenen anzuzeigen.
*   **Unten rechts – Vorlagenkarten**: Zeigt die Diagrammkarten der aktuellen Ebene an, einschließlich Name und Vorschaubild.

![tutorial_plot2](/img/v0.7.1/tutorial_plot2.webp)

Wählen Sie eine Vorlagenkarte mit der Maus aus und klicken Sie darauf, um die spezifische Diagrammoberfläche aufzurufen. Sie können auch per Rechtsklick auf eine Diagrammkarte das entsprechende Diagramm exportieren, um es mit anderen Forschenden zu teilen. Oder favorisieren Sie ein Diagramm für den schnellen Zugriff beim nächsten Mal. Persönliche benutzerdefinierte Diagramme können per Rechtsklick gelöscht werden.

:::tip

Sie können die <kbd>Strg</kbd>-Taste gedrückt halten, um Diagrammkarten schnell zu löschen oder zu favorisieren.

:::

Unter lokalen Netzwerkbedingungen können Nutzer die integrierte Vorlagenliste manuell über die Menüleiste prüfen und aktualisieren oder in den Einstellungen die automatische Prüfung aktivieren, um stets die neuesten Ressourcen zu erhalten.

#### Obere Menüleiste

Die Menüleistenfunktionen sind in zwei Kategorien unterteilt:

1.  **Datei**: Hauptsächlich zum Erstellen, Öffnen und Importieren von Vorlagen.
    1.  **Neue Vorlage**: Zum Erstellen benutzerdefinierter Diagramme; ein Klick öffnet ein interaktives Dialogfenster.
    2.  **Vorlage öffnen**: Zum vorübergehenden Öffnen einer Vorlage; unterstützt `json`-Dateien und `zip`-Ressourcenpakete.
    3.  **Vorlage importieren**: Zum Importieren externer Vorlagenpakete (`zip`) in die lokale benutzerdefinierte Vorlagenliste.
2.  **Vorlage**: Hauptsächlich für Updates integrierter Vorlagen. **Integrierte Cloud-Vorlagen auf Updates prüfen**: Zum Abrufen der neuesten Vorlagenliste und Updates.

:::tip

Sie können Diagrammvorlagendateien auch direkt an die vorgesehene Stelle ziehen, um Diagrammvorlagen zu importieren.

:::

### Diagrammoberfläche

#### Oberfläche

Die Diagrammoberfläche ist in fünf Hauptbereiche unterteilt:

-   **Symbolleiste**: Enthält Schnellzugriffsschaltflächen und drei Funktionsregisterkarten: Zeichnen, Daten und Bearbeiten.
-   **Ebenenliste (Objekte)**: Liste der Zeichnungselemente auf der Vorlage. Durch Klicken auf ein Element können Sie dessen Eigenschaften ändern.
-   **Zeichenfläche**: Zentraler Bereich zum Anzeigen des Diagramms, Importieren von Daten, visuellen Einstellungen und Anzeigen der Vorlagendokumentation.
-   **Statusleiste**: Zeigt grundlegende Zeicheninformationen an, einschließlich der aktuellen Diagrammsprache und Koordinateninformationen.
-   **Eigenschaftsfenster**: Zeigt die Eigenschaften des ausgewählten Zeichnungselements (z. B. Farbe, Größe) an, um den gewünschten visuellen Effekt zu erzielen.

![tutorial_plot3](/img/v0.7.1/tutorial_plot3.webp)

#### Diagramm-Symbolleiste

Die Symbolleiste besteht aus einer Reihe von **Schnellzugriffsschaltflächen**. Dazu gehören unter anderem:

1. Zurück zur Diagrammvorlagenbibliothek
2. Diagrammstatus wechseln (Diagrammmodus, Datenmodus, Bearbeitungsmodus)
3. Verschiedene Diagramm-Eigenschaftspanels und Bedienwerkzeuge usw.

Detaillierte Funktionen siehe Abbildung unten.

![tutorial_plot4](/img/v0.6.1/tutorial_plot4.png)

### Ebenenliste

Zeichnungselemente sind in 7 Kategorien unterteilt:

-   **Line (Linie)**: Definiert grundlegende Kartenbegrenzungen oder Liniensegmente.
-   **Text (Text)**: Beschriftungen und Anmerkungen.
-   **Polygon (Polygon)**: Geschlossene Formen innerhalb des Diagramms.
-   **Arrow (Pfeil)**: Gerichtete Zeichnungsobjekte.
-   **Function (Funktion)**: Ermöglicht die Eingabe benutzerdefinierter mathematischer Funktionen und Definitionsbereiche.
-   **Axes (Achsen)**: Koordinatenachsen des Diagramms.
-   **Data Point (Datenpunkt)**: Elemente, die importierte Daten darstellen.

**Standard-Renderreihenfolge (von oben nach unten): `Text > Pfeil > Punkt > Funktion > Linie > Polygon > Achsen`**.

![tutorial_plot5](/img/v0.6.1/tutorial_plot5.png)

Wenn Sie ein Element im Ebenenpanel auswählen, wird es auf der Zeichenfläche hervorgehoben, während andere Elemente halbtransparent werden, um Ablenkungen zu reduzieren. Das Eigenschaftsfenster zeigt anschließend die relevanten Eigenschaften dieses Elements an. 🔍

Passen Sie diese Eigenschaften an, um Ihren gewünschten visuellen Stil zu erreichen.

:::tip

Um alle Elemente abzuwählen, klicken Sie einfach mit der rechten Maustaste irgendwo auf die Zeichenfläche oder verwenden Sie die Schaltfläche **Abwählen** in der Symbolleiste.

:::

### Eigenschaftsfenster & Diagrammleitfaden

## Anwendungsbeispiel Diagramm

### Beispiel

1. Wählen Sie eine Vorlage aus der **Vorlagenbibliothek**, um die Zeichenseite aufzurufen. Beispiel: TAS-Diagramm auswählen.
2. Wenn Daten eingegeben werden müssen, wechseln Sie vom Diagrammmodus in den Datenmodus.
   
   ![tutorial_plot6](/img/v0.7.1/tutorial_plot6.webp)


   Für das TAS-Diagramm sind vier Datenspalten erforderlich: `Category`, `SiO2`, `K2O` und `Na2O`. Die Einheit ist `wt.%`. Die Spalte `Category` dient der Gruppierung der Daten und der Erstellung der Legende. Wenn Sie unsicher sind, welche Daten eingegeben werden sollen, welche Einheiten gelten oder wie das Diagramm verwendet wird, können Sie auf die Diagrammhilfe-Schaltfläche oben rechts im Diagrammbereich klicken, um die Dokumentation zu diesem Diagramm anzuzeigen.

   ![tutorial_plot7](/img/v0.6.1/tutorial_plot7.png)
3. Nachdem Sie die Anforderungen verstanden haben, geben Sie Ihre Daten ein und klicken Sie auf die Projektionsschaltfläche oben rechts im Datenpanel, um Datenpunkte zu zeichnen. Bei fehlerhaften Eingaben klicken Sie nach der Korrektur einfach erneut auf Projektion.
   
   ![tutorial_plot8](/img/v0.7.1/tutorial_plot8.webp)
   
4. **Diagramm exportieren**. Wir bieten eine **Schnellschaltfläche zum Kopieren des Diagrammergebnisses in die Zwischenablage**. Außerdem gibt es eine **formale Exportfunktion**. Im formalen Exportpanel unterstützen wir die Bildformate `jpg`, `png`, `bmp` und `svg`. Für wissenschaftliche Abbildungen mit Vektorgrafiken empfehlen wir das `svg`-Format. Wir unterstützen auch die **direkte Verknüpfung der Ergebnisse mit Drittanbieter-Zeichensoftware**, ohne den Zwischenschritt Export und erneuter Import. Empfohlene integrierte Drittanbieter-Software: **CorelDRAW, Inkscape, Adobe Illustrator**. Für die nachträgliche wissenschaftliche Bearbeitung empfehlen wir die Verknüpfung mit Inkscape. Selbstverständlich unterstützen wir auch benutzerdefinierte Drittanbieter-Zeichensoftware, sofern diese das `svg`-Vektorformat unterstützt. Forschende müssen nur in den Einstellungen den Programmpfad der Zeichensoftware angeben.
   ![tutorial_plot8](/img/v0.7.1/tutorial_plot10.webp)

### Zusätzliche Hinweise

Diagrammvorlagen unterstützen selbst den mehrsprachigen Wechsel, d. h. ein Diagramm kann in mehrere Sprachversionen umgeschaltet werden, um Internationalisierungsbedürfnisse zu erfüllen.

Beispiel: Für Veröffentlichungen in chinesischen Fachzeitschriften wird die chinesische Version benötigt, für englische SCI/IE-Zeitschriften die englische Version. Sie können über die Sprachschaltfläche oben rechts in der Diagramm-Symbolleiste zur entsprechenden Sprachversion wechseln.

![tutorial_plot9](/img/v0.7.1/tutorial_plot9.webp)
