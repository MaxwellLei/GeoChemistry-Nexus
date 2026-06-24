---
sidebar_position: 6
---

# ℹ️ Über

:::info

**"Ich konnte den Sportwagen meiner Träume nicht finden, also habe ich ihn selbst gebaut." —— Ferdinand Porsche**

:::

**GeoChemistry Nexus** ist mein erstes Projekt, das für eine extrem langfristige Wartung geplant ist – ich beabsichtige, es mindestens in den nächsten fünf Jahren kontinuierlich zu aktualisieren.

Interessanterweise hieß es ursprünglich **Geo-Thermometer**. Anfangs wollte ich einfach ein grundlegendes Programm im Zusammenhang mit geologischen Thermometern entwickeln, um Berechnungen und Darstellungen zu erleichtern. Doch im Laufe des Projekts, insbesondere nach Fertigstellung des Plotting-Moduls, wurde mir plötzlich klar, dass ein viel größeres Ziel erreichbar war. Folglich erweiterte sich das Projekt sowohl im Konzept als auch in der Funktionalität und entwickelte sich von Geo-Thermometer zu GeoChemistry Nexus.

Meiner Ansicht nach sind Geologie und Geochemie relativ nischenhafte Disziplinen. In diesem Bereich habe ich festgestellt, dass viele Softwaretools entweder verfallen sind und seit langem nicht mehr aktualisiert werden oder zwar noch aktualisiert werden, aber eine extrem hohe Einstiegshürde haben, was viele Forscher behindert. Darüber hinaus habe ich in meiner Forschung einige weniger rigorose Praktiken gesehen – zum Beispiel verwenden manche Leute CorelDRAW, um Formen grob zu skizzieren, wenn sie geologische Diagramm-Basiskarten zeichnen. Obwohl es visuell keinen großen Unterschied gibt, verstößt dieser Ansatz aus wissenschaftlicher Forschungsperspektive gegen den rigorosen Geist der Wissenschaft.

Also dachte ich: **Warum nicht selbst ein Werkzeug entwickeln?**

Ein Werkzeug, das eine große Anzahl geologischer und geochemischer Diagrammvorlagen integrieren kann, verschiedene Geothermometer-/Barometerberechnungen unterstützt und auf geochemische Berechnungen und aktuell beliebte Funktionen für maschinelles Lernen erweitert werden kann. Wenn keine fertigen Vorlagen verfügbar sind, bietet es visuelle Zeichenwerkzeuge, um Erstellung und Austausch zu erleichtern. Ergänzt durch modernes Oberflächendesign und Mehrsprachenunterstützung ermöglicht es Forschern, den Großteil der grundlegenden Arbeiten des wissenschaftlichen Plottens und Rechnens innerhalb einer einzigen Software zu erledigen.

Eine visuelle Oberfläche kann die Bedienungsschwierigkeiten erheblich verringern, während Mehrsprachenunterstützung die Lernkosten senkt; integrierte reichhaltige Vorlagen und langfristige Updates werden dieses Programm zu einem wirklich wertvollen und nachhaltigen wissenschaftlichen Forschungswerkzeug machen.

Zufälligerweise bin ich mit Programmiersprachen wie C# und Python vertraut und beherrsche auch Entwicklungstechnologien wie WPF. Daher begann ich im Dezember 2024 mit der Entwicklung einer Demo. In der anfänglichen Technologieauswahlphase habe ich ernsthaft überlegt: Welche Sprache und Architektur würde dieses Ziel am besten erreichen?

Mein erster Gedanke war Python – es würde mir erlauben, schnell Prototypen zu erstellen, was eine gute Nachricht für Entwickler ist. Aus der Benutzerperspektive hat es jedoch fatale Nachteile: erstens geringe Ausführungseffizienz (obwohl die Auswirkungen bei kleinen Datenmengen gering sind, gibt es dennoch eine Leistungslücke); zweitens bringt es ein massives Installationspaket mit sich. Ich habe einmal ein einfaches Programm, das ursprünglich in WPF geschrieben war, mit Python refaktoriert. Selbst wenn es in einer frischen virtuellen Umgebung verpackt war, war das fertige Produkt Hunderte von MB groß, während das .NET-gepackte Programm nur ein Dutzend MB groß war – ein riesiger Unterschied. Benutzer zu zwingen, Hunderte von MB oder sogar GB an Installation und komplexer Konfiguration für einfache Funktionen zu ertragen, ist nicht die beste Wahl.

Dann habe ich Webentwicklung in Betracht gezogen. Die Vorteile liegen auf der Hand – keine Installation erforderlich, öffnen und verwenden. Aber das Problem liegt darin: Einerseits ist meine Beherrschung der Webtechnologie nicht umfassend genug; andererseits übersteigen die langfristig hohen Kosten für Server meine persönliche Wartungskapazität. Daher habe ich meinen Fokus letztendlich wieder auf die clientseitige Entwicklung gelegt.

Es gibt viele technische Lösungen für die clientseitige Entwicklung, aber im Vergleich zu QT auf C++-Basis bin ich mit .NET vertrauter. Durch die Kombination der hohen Ausführungseffizienz von .NET mit der Allgegenwart von Windows habe ich mich schließlich für .NET als Hauptentwicklungsplattform entschieden. Anfangs habe ich auch die Verwendung von Avalonia in Betracht gezogen, um plattformübergreifende Unterstützung für Windows, Linux und MacOS zu erreichen, aber während des Entwicklungsprozesses stellte ich fest, dass einige Projektabhängigkeiten nicht mit Avalonia kompatibel waren, was zu sehr hohen Migrationskosten führte. Mit meiner persönlichen Kraft ist es schwierig, diese Module in den frühen Stadien zu refaktorieren. Daher konzentriert sich meine Arbeit zumindest im aktuellen Stadium immer noch auf die Windows-Plattform, und plattformübergreifende Versionen könnten eines Tages in der Zukunft realisiert werden.

## Kontaktieren Sie mich

Wenn Sie an Softwareentwicklung interessiert sind und mit mir zusammenarbeiten möchten oder Vorschläge und Feedback, Diagrammvorlagen, Algorithmenunterstützung geben oder sogar eine kommerzielle Zusammenarbeit durchführen möchten, können Sie mich gerne über **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)** kontaktieren.

*Wenn Sie bei der Verwendung der Software Mängel oder Probleme feststellen, geben Sie bitte Feedback. Aufgrund der zunehmenden Funktionalität und Komplexität der Software ist es schwierig, alle Situationen zu testen, und es kann zu Auslassungen oder Ungenauigkeiten kommen. Ich werde mein Bestes tun, um es zu korrigieren und zu optimieren, um Ihnen ein stabileres und zuverlässigeres Benutzererlebnis zu bieten.*
