---
sidebar_position: 2
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Diagramme

Im Diagrammmodul führen wir weitere grundlegende geowissenschaftliche Diagrammvorlagen zusammen, einschließlich, aber nicht beschränkt auf Diskriminationsdiagramme tektonischer Milieus, Gesteinsklassifikationsdiagramme und weitere. **Unser endgültiges Ziel ist, für die Geowissenschaften einen umfassenden Werkzeugkasten aus Diagrammrahmen und Zeichnen zu schaffen und den Forschenden die größtmögliche Erleichterung zu bieten.** 🧪

:::info

Wenn Sie in der Forschung zu geologischen Diagrammen und verwandten Gebieten arbeiten und Ihre Diagrammergebnisse bekannt machen möchten, nehmen Sie gern Kontakt mit uns auf. Wir helfen dabei, dass Ihre Diagrammergebnisse einen weiteren Kreis der Forschung erreichen, und steigern so Reichweite und Wirkung der wissenschaftlichen Ergebnisse.

:::

## Schnellstart

Das Diagrammmodul unserer Software besteht vor allem aus zwei Teilen: der Bibliothek der Diagrammvorlagen und der Bedienoberfläche des Diagramms. Sie können zuerst in der Bibliothek der Diagrammvorlagen den benötigten Diagrammtyp wählen und anschließend in die entsprechende Bedienoberfläche gelangen, um Dateneingabe, Bearbeitung des Diagramms, Export der Ergebnisse und weitere Vorgänge abzuschließen.

### Vorlagenbibliothek

Wir gliedern das Diagrammmodul in drei große Gruppen: **Online-Cloud-Vorlagen**, **Meine Vorlagen** und **Werkzeug-Diagrammvorlagen**.

**Online-Cloud-Vorlagen**: werden von uns fortlaufend aktualisiert und gepflegt. Sie müssen die Software nicht aktualisieren. Bei bestehender Netzwerkverbindung erhalten Sie aktualisierte Diagrammvorlagen und verfügen so stets über die umfassendsten und maßgeblichen Vorlagenressourcen.

**Meine Vorlagen**: eignet sich, wenn für einen bestimmten Forschungsbedarf eigene Vorlagen erstellt werden sollen. Sie können diese Vorlagen nicht nur selbst erstellen, sondern auch exportieren, um sie anderen Forschenden leicht weiterzugeben und so den wissenschaftlichen Austausch und die Verbreitung zu fördern.

> *Künftig planen wir, eine eigene Community für Diagrammvorlagen aufzubauen. Benutzerinnen und Benutzer können dort verschiedene **persönliche benutzerdefinierte Vorlagen** leicht erstellen, hochladen, teilen und herunterladen und so Flexibilität und Erweiterbarkeit des Systems weiter stärken.*

**Werkzeug-Diagrammvorlagen**: feste, übliche Diagramme werden gesondert herausgelöst. Derzeit enthalten sind **Spiderdiagramm der Seltenerdelemente, Spiderdiagramm der Spurenelemente, Harker-Mehrfachdiagramm** sowie die üblichen zweidimensionalen und ternären Streudiagramme.

#### Oberfläche

Standardmäßig zeigt das Zeichenmodul beim Öffnen die eingebaute Bibliothek geowissenschaftlicher Vorlagen. Die Oberfläche gliedert sich in drei Hauptteile:

*   **Links – Diagrammklassifikation**: zeigt alle Vorlagenhierarchien und die entsprechenden Vorlagen, einschließlich der Liste benutzerdefinierter Vorlagen.
*   **Rechts – Diagrammkarten**: zeigt die Diagrammkarten der aktuellen Hierarchie, einschließlich Name und Vorschaubild.
*   **Oben – Navigationsleiste**: Die Software verwendet ein rahmenloses Design, daher übernimmt die Navigationsleiste oben einen Teil der Funktionen jeder Seite. In der Diagrammoberfläche zeigt die obere Navigationsleiste den aktuellen Pfad der Diagrammvorlage sowie Funktionen wie die Suche nach Diagrammvorlagen.

![graphical_interface](/img/v0.9.0/en-US/graphical_interface.webp)

Wählen Sie eine passende Diagrammvorlage. Ein Klick auf die Karte der Diagrammvorlage öffnet die Bedienoberfläche des Diagramms. Bei der Karte einer Diagrammvorlage können Sie auch mit der rechten Maustaste weitere Vorgänge ausführen, etwa Exportieren, Löschen und Favorit.

:::tip

Mit der rechten <kbd>Alt</kbd>-Taste lassen sich Diagrammvorlagen schnell löschen oder als Favorit setzen.

:::

#### Obere Menüleiste

Die obere Menüleiste **Datei** enthält:

1.  **Neues Diagramm**: dient dem Erstellen einer benutzerdefinierten Diagrammvorlage.
2.  **Diagramm öffnen**: dient dem vorübergehenden Öffnen einer Diagrammvorlage; unterstützt werden nur Dateien `.diag.zip`.
3.  **Diagramm importieren**: dient dem Import eines externen Pakets einer Diagrammvorlage (`.diag.zip`) in die lokale Liste Meine Vorlagen.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphic_menu_bar.webp')}
    alt="Menüleiste der Diagramme"
    style={{width: '50%'}}
  />
</div>

### Bedienoberfläche des Diagramms

#### Oberfläche

Die Diagrammoberfläche gliedert sich in fünf Hauptteile:

-   **Symbolleiste**: umfasst Kurzschaltflächen und drei Funktionsregisterkarten: Zeichnen, Daten und Bearbeiten.
-   **Ebenenliste (Objekte)**: Liste der Zeichenelemente auf der Vorlage. Ein Klick auf ein Element erlaubt es, seine Eigenschaften zu ändern.
-   **Zeichenfläche**: der zentrale Bereich zum Ansehen des Diagramms, zum Importieren von Daten, zu visuellen Einstellungen und zum Ansehen der Vorlagenerläuterung.
-   **Statusleiste**: zeigt grundlegende Zeicheninformationen, einschließlich der aktuellen Diagrammsprache und der Koordinateninformationen.
-   **Eigenschaftenbereich**: zeigt die Eigenschaften des ausgewählten Zeichenelements, etwa Farbe und Größe, um die gewünschte visuelle Wirkung zu erreichen.

![graphical_operation_interface](/img/v0.9.0/en-US/graphical_operation_interface.webp)

#### Diagramm-Symbolleiste

Die Symbolleiste besteht aus einer Reihe von **Kurzschaltflächen**. Vor allem sind dies:

1. Zurück zur Bibliothek der Diagrammvorlagen
2. Wechsel des Bedienzustands (Diagrammmodus, Datenmodus, Bearbeitungsmodus)
3. Verschiedene Eigenschaftenbereiche und Bedienwerkzeuge des Diagramms

Die Funktionen im Einzelnen zeigt die folgende Abbildung.

![graphical_toolbar](/img/v0.9.0/en-US/graphical_toolbar.webp)

### Ebenenliste

Die Zeichenelemente gliedern sich in 7 große Gruppen:

-   **Line (Linie)**: legt die grundlegende Diagrammgrenze oder ein Liniensegment fest.
-   **Text (Text)**: Beschriftungen und Anmerkungen.
-   **Polygon (Polygon)**: geschlossene Formen innerhalb des Diagramms.
-   **Arrow (Pfeil)**: gerichtete Zeichenobjekte.
-   **Function (Funktion)**: erlaubt die Eingabe einer benutzerdefinierten mathematischen Funktion und eines Definitionsbereichs.
-   **Axes (Koordinatenachsen)**: die Koordinatenachsen des Diagramms.
-   **Data Point (Datenpunkt)**: Elemente, die importierte Daten darstellen.

**Standardmäßige Renderreihenfolge (von oben nach unten): `Datenpunkt > Text > Pfeil > Funktion > Linie > Polygon > Koordinatenachsen`.**

![tutorial_plot5](/img/v0.9.0/en-US/diagrammatic_object_selection_example.webp)

Wenn Sie im Ebenenbereich ein Element auswählen, wird es auf der Zeichenfläche hervorgehoben, während die übrigen Elemente halbtransparent werden, um Störungen zu verringern. Der Eigenschaftenbereich zeigt anschließend die zugehörigen Eigenschaften dieses Elements. 🔍

Sie können diese Eigenschaften ändern, um den gewünschten visuellen Stil zu erreichen.

:::tip

Um die Auswahl aller Elemente aufzuheben, **klicken Sie mit der rechten Maustaste auf eine beliebige Stelle der Zeichenfläche** oder verwenden Sie die Schaltfläche **Auswahl aufheben** in der Symbolleiste.

:::

## Anwendungsbeispiel

### Beispiel

1. Wählen Sie in der **Bibliothek der Diagrammvorlagen** eine Diagrammvorlage und gelangen Sie auf die Diagramm-Arbeitsseite. Zum Beispiel: Wählen Sie das TAS-Diagramm.
2. Wenn Daten eingegeben werden sollen, wechseln Sie vom Diagrammmodus in den Datenmodus.
   
   ![tutorial_plot6](/img/v0.9.0/en-US/diagram_data_input.webp)


Für das TAS-Diagramm müssen vier Datenspalten eingegeben werden: `Category`, `SiO2`, `K2O` und `Na2O`. Die Überschriftsspalte `Category` dient dazu, die Daten zu gruppieren und eine Legende zu erzeugen. Die Spalte `Sample` kennzeichnet die Datenprobe und nimmt an keiner Berechnung teil (sie darf leer sein). Die Einheit der übrigen Elemente ist `wt.%`. Wenn Ihnen nicht klar ist, welche Daten dieses Diagramm erwartet, welche Einheit gilt und wie das Diagramm zu verwenden ist, klicken Sie auf die Hilfe-Schaltfläche des Diagramms oben rechts im Diagrammbereich und sehen Sie die Erläuterungsdokumentation zu diesem Diagramm an.

3. Geben Sie nach dem Verständnis der Anforderungen Ihre Daten ein und klicken Sie auf die Plot-Schaltfläche oben rechts im Datenbereich, um die Datenpunkte zu zeichnen. Wenn die Dateneingabe fehlerhaft ist, ändern Sie die Daten und klicken Sie erneut auf Plotten.![tutorial_plot8](/img/v0.9.0/en-US/data_plotting.webp)
4. **Diagramm exportieren**. Wir stellen eine **Kurzschaltfläche bereit, die das Plottergebnis des Diagramms in die Zwischenablage kopiert**. Außerdem gibt es eine **reguläre Exportfunktion**. Im Bereich des regulären Exports unterstützen wir die Bildformate `jpg`, `png`, `bmp` und `svg`. Wenn für wissenschaftliche Abbildungen eine Vektorgrafik benötigt wird, empfehlen wir das Format `svg`. Zugleich unterstützen wir auch, **das Ergebnis unmittelbar an eine Zeichensoftware eines Drittanbieters zu übergeben**, ohne den Zwischenschritt des Exportierens und erneuten Importierens. Als Drittanbietersoftware fest eingebaut und empfohlen werden: **CorelDRAW, Inkscape, Adobe Illustrator**. Für die nachträgliche wissenschaftliche Überarbeitung der Abbildung empfehlen wir die Anbindung an Inkscape. Natürlich unterstützen wir auch, eine andere Zeichensoftware eines Drittanbieters selbst festzulegen, vorausgesetzt, diese Software unterstützt das Vektorformat `svg`. Die Forschenden müssen in den Einstellungen lediglich den Programmpfad der Zeichensoftware angeben.
   ![tutorial_plot8](/img/v0.9.0/en-US/export_diagram.webp)

### Ergänzende Erläuterung

Die Diagrammvorlage selbst unterstützt den Wechsel der Sprache, das heißt: Ein Diagramm lässt sich in mehrere Sprachversionen umschalten, um den Bedarf der Internationalisierung zu erfüllen.

Zum Beispiel benötigt ein Diagramm für eine Veröffentlichung in einer chinesischen Zeitschrift die chinesische Sprachversion, für eine Veröffentlichung in einer englischsprachigen Zeitschrift wie SCI/IE die englische Version des Diagramms. Sie können die Sprachversion des Diagramms mit einem Klick über das Bedienelement oben rechts in der Diagramm-Symbolleiste umschalten.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/switch_graphic_language.webp')}
    alt="Diagrammsprache wechseln"
    style={{width: '60%'}}
  />
</div>
