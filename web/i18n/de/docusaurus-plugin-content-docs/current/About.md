---
sidebar_position: 6
---

# ℹ️ Über

:::info

**„Ich fand den Sportwagen meiner Träume nicht, also habe ich ihn selbst gebaut.“ — Ferdinand Porsche**

:::

**GeoChemistry Nexus** ist das erste Projekt, das ich über einen sehr langen Zeitraum pflegen möchte. Ich beabsichtige, es mindestens in den kommenden fünf Jahren fortlaufend zu aktualisieren.

Bemerkenswert ist, dass es ursprünglich **Geo-Thermometer** hieß. Anfangs wollte ich lediglich ein grundlegendes Programm für geologische Thermometer entwickeln, um Berechnungen und Zeichnungen zu erleichtern. Im weiteren Verlauf des Projekts, insbesondere nach Fertigstellung des Zeichenmoduls, wurde mir jedoch klar, dass sich ein weit größeres Ziel verwirklichen ließ. Das Projekt wurde deshalb in Konzept und Funktionsumfang erweitert und entwickelte sich von Geo-Thermometer zu GeoChemistry Nexus.

## Warum dieser Name

**GeoChemistry** steht für Geochemie. **Nexus** bedeutet Verbindungspunkt, Knoten oder Ort des Zusammentreffens.

Wir möchten verstreute geochemische Berechnungsverfahren, Werkzeuge der Datenverarbeitung, mineralchemische Auswertungen, geologische Diagramme und Weiteres in einer einheitlichen Plattform zusammenführen.

## Warum GeoChemistry Nexus benötigt wird

In der geochemischen und petrologischen Forschung sind **das Erstellen von Abbildungen** und **die Berechnung** häufig auf verschiedene Werkzeuge verteilt: Daten werden in Tabellenprogrammen aufbereitet, Basiskarten in allgemeinen Vektorprogrammen gezeichnet, und Geothermobarometer erfordern ein weiteres Skript oder ein veraltetes Programm. Zerrissene Abläufe, uneinheitliche Formate und hoher Aufwand für die Reproduktion sind Reibungen, denen viele Forschende im Alltag begegnen.

Andererseits **werden viele gebräuchliche Diskriminationsdiagramme, Geothermobarometer und zugehörige Berechnungswerkzeuge seit Langem nicht fortlaufend aktualisiert**. Neu veröffentlichte Algorithmen und Überarbeitungen klassischer Modelle lassen sich nur schwer zeitnah aufnehmen, und Forschende bleiben oft bei seit Jahren stillstehenden alten Versionen. Noch unmittelbarer ist dieses Problem: Wenn Forschende einen neuen Algorithmus oder ein neues Diagramm **selbst entwickeln**, **fehlt häufig ein bequemer und offener Weg, das Ergebnis weiterzugeben**. Die Arbeit bleibt im Anhang einer Veröffentlichung, in einem persönlichen Skript oder in einem kleinen Kreis und lässt sich im Fachgebiet nur schwer schnell verbreiten und wiederverwenden.

![plot_home_ui](/img/about-illustrations/02-research-tool-pain-points.png)

Darüber hinaus sind mir in der Forschung auch Vorgehensweisen begegnet, denen es an Sorgfalt fehlt. So zeichnen manche die Formen einer geologischen Basiskarte in CorelDRAW nur grob nach. Der visuelle Unterschied ist zwar gering, aus wissenschaftlicher Sicht widerspricht ein solches Vorgehen jedoch dem Anspruch wissenschaftlicher Sorgfalt.

Deshalb fragte ich mich: **Warum nicht selbst ein Werkzeug entwickeln?**

Ein Werkzeug, das eine große Zahl geologischer und geochemischer Diagrammvorlagen zusammenführt, verschiedene geologische Thermometer und Barometer berechnen kann und sich auf geochemische Berechnungen sowie auf derzeit verbreitete Funktionen des maschinellen Lernens erweitern lässt. Liegt keine fertige Vorlage vor, stellt es visuelle Zeichenwerkzeuge bereit, mit denen sich Vorlagen erstellen und weitergeben lassen. Ergänzt durch eine moderne Oberflächengestaltung und Mehrsprachigkeit können Forschende die grundlegenden Arbeiten des wissenschaftlichen Zeichnens und Rechnens weitgehend in einer einzigen Software erledigen.

**GeoChemistry Nexus** soll diese Kette an einem Ort zusammenführen: vom Import der Probendaten über Vorverarbeitung und Korrektur bis zum Zeichnen von Diskriminationsdiagrammen, zur Berechnung von Geothermometern und zur CIPW-Normberechnung und weiter zum Export in Formate wie PNG und SVG, möglichst in derselben Oberfläche. Zugleich sollen Diagramme und Algorithmen über ein **Ökosystem von Cloud-Vorlagen** und **Mechanismen der Zusammenarbeit in der Community** fortlaufend weiterentwickelt werden und mehr Forschende schnell erreichen.

## Der ursprüngliche Entwicklungsgedanke

Zufällig beherrsche ich Programmiersprachen wie C# und Python und die Entwicklung von Desktop-Software. Im Dezember 2024 begann ich deshalb mit der Entwicklung eines Demos. In der ersten Phase der Technikauswahl habe ich sorgfältig überlegt: **Welche Sprache und welche Architektur verwirklichen dieses Ziel am besten?**

Mein erster Gedanke war Python. Damit ließe sich schnell ein Prototyp erstellen, was für Entwickler günstig ist. Aus Sicht der Benutzer hat es jedoch entscheidende Nachteile. Erstens ist die Ausführungsgeschwindigkeit gering. Bei kleinen Datenmengen fällt das wenig ins Gewicht, ein Leistungsunterschied bleibt dennoch. Zweitens entsteht ein sehr großes Installationspaket. Ich habe einmal ein einfaches, ursprünglich in WPF geschriebenes Programm in Python neu aufgebaut. Selbst wenn es in einer vollständig neuen virtuellen Umgebung gepackt wurde, umfasste das Ergebnis mehrere hundert MB, während das mit .NET gepackte Programm nur etwas mehr als zehn MB groß war. Der Unterschied ist erheblich. **Benutzerinnen und Benutzer zu zwingen, für eine einfache Funktion eine Installation von mehreren hundert MB oder sogar mehreren GB und eine aufwendige Konfiguration hinzunehmen, ist keine gute Lösung.**

Danach habe ich die Webentwicklung erwogen. Der Vorteil liegt auf der Hand: keine Installation, sofortige Nutzung. Das Problem ist, dass die dauerhaft hohen Kosten eines Servers meine persönlichen Möglichkeiten der Pflege übersteigen. Deshalb habe ich den Schwerpunkt wieder auf die Client-Entwicklung gelegt.

![plot_home_ui](/img/about-illustrations/03-technology-choice-path.png)

Für die Client-Entwicklung gibt es viele technische Wege. Im Vergleich mit dem auf C++ beruhenden Qt bin ich mit .NET jedoch vertrauter. Zusammen mit der hohen Ausführungsgeschwindigkeit von .NET und der Verbreitung von Windows habe ich .NET schließlich als Hauptentwicklungsplattform gewählt. Anfangs habe ich auch Avalonia erwogen, um Windows, Linux und macOS plattformübergreifend zu unterstützen. Während der Entwicklung stellte ich fest, dass einige Projektabhängigkeiten mit Avalonia nicht vereinbar sind und die Kosten einer Migration sehr hoch wären. Mit meinen eigenen Kräften lassen sich diese Module in einer frühen Phase kaum neu aufbauen. Deshalb konzentriert sich meine Arbeit zumindest gegenwärtig weiter auf die Windows-Plattform. Eine plattformübergreifende Version kann an einem späteren Tag entstehen.

## Kontakt

Wenn Sie sich für die Softwareentwicklung interessieren und mit mir zusammenarbeiten möchten, oder wenn Sie Vorschläge und Rückmeldungen, Diagrammvorlagen, Unterstützung bei Algorithmen oder auch eine geschäftliche Zusammenarbeit anbieten möchten, erreichen Sie mich jederzeit unter **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)**.

*Wenn Ihnen bei der Verwendung der Software Mängel oder Probleme auffallen, geben Sie bitte jederzeit Rückmeldung. Da der Funktionsumfang stetig wächst und komplexer wird, können Tests kaum alle Fälle abdecken, und es können Lücken oder Ungenauigkeiten bleiben. Ich werde mich bemühen, sie zu beheben und zu verbessern, damit Sie eine stabilere und zuverlässigere Nutzung erhalten.*

![plot_home_ui](/img/about-illustrations/04-long-term-maintenance.png)
