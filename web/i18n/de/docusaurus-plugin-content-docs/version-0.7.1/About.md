---
sidebar_position: 6
---

# ℹ️ Über uns

:::info

**„Ich habe meinen Traum-Sportwagen nicht gefunden, also habe ich einen selbst gebaut.“ — Ferdinand Porsche**

:::

**GeoChemistry Nexus** ist mein erstes Projekt, das ich langfristig pflegen möchte — ich beabsichtige, es mindestens fünf Jahre lang kontinuierlich weiterzuentwickeln.

Interessanterweise hieß es ursprünglich **Geo-Thermometer**. Anfangs wollte ich lediglich ein grundlegendes Programm für geologische Thermometer entwickeln, um Berechnungen und Diagramme zu erleichtern. Im Laufe des Projekts, insbesondere nach Abschluss des Diagrammmoduls, wurde mir jedoch klar, dass ein weitaus größeres Ziel erreichbar ist. Das Projekt wurde daher konzeptionell und funktional erweitert und entwickelte sich von Geo-Thermometer zu GeoChemistry Nexus.

## Warum GeoChemistry Nexus?

In der geochemischen und petrographischen Forschung sind **Diagrammerstellung** und **Berechnungen** oft auf verschiedene Werkzeuge verteilt: Datenaufbereitung in Tabellenkalkulationen, Basiskartenerstellung in allgemeinen Vektorprogrammen, Thermobarometer-Berechnungen in separaten Skripten oder veralteten Programmen. Fragmentierte Workflows, uneinheitliche Formate und hohe Reproduzierbarkeitskosten sind alltägliche Reibungspunkte für viele Forschende.

Gleichzeitig fehlt bei vielen gängigen diskriminativen Diagrammen, Thermobarometern und zugehörigen Berechnungstools **eine kontinuierliche Weiterentwicklung** — neu publizierte Algorithmen und überarbeitete klassische Modelle lassen sich nur schwer zeitnah integrieren; Forschende sind oft auf jahrelang unveränderte Versionen angewiesen. Ein weiteres praktisches Problem: Wenn Wissenschaftler **eigene** Algorithmen oder Diagramme entwickeln, fehlt häufig ein **bequemer, offener Kanal zur Verbreitung** — Ergebnisse bleiben in Anhängen, persönlichen Skripten oder kleinen Kreisen und lassen sich in der Fachgemeinschaft nur schwer schnell verbreiten und wiederverwenden.

![plot_home_ui](/img/about-illustrations/02-research-tool-pain-points.png)

Darüber hinaus habe ich in der Forschung auch weniger sorgfältige Vorgehensweisen beobachtet — etwa wenn geologische Basiskarten mit CorelDRAW grob skizziert werden. Visuell mag der Unterschied gering sein, doch aus wissenschaftlicher Sicht widerspricht dies dem Anspruch an rigorose Forschung.

Also dachte ich: **Warum entwickle ich nicht einfach selbst ein Werkzeug?**

Ein Tool, das zahlreiche geologische und geochemische Diagrammvorlagen integriert, verschiedene geologische Thermometer/Barometer unterstützt und sich auf geochemische Berechnungen sowie aktuelle Machine-Learning-Funktionen ausweitet. Fehlen passende Vorlagen, bietet es visuelle Zeichenwerkzeuge zum Erstellen und Teilen. Mit modernem Interface-Design und Mehrsprachigkeit sollen Forschende die meisten grundlegenden wissenschaftlichen Diagramm- und Berechnungsaufgaben in einer Software erledigen können.

**GeoChemistry Nexus** möchte diese Kette an einem Ort bündeln — vom Import und der Vorverarbeitung von Probedaten über diskriminative Diagramme, Geothermometer-Berechnungen und CIPW-Standardmineralberechnungen bis zum Export in PNG / SVG — möglichst in derselben Oberfläche; gleichzeitig sollen **Cloud-Template-Ökosystem** und **Community-Kollaboration** Diagramme und Algorithmen kontinuierlich weiterentwickeln und schneller mehr Forschenden zugänglich machen.

## Die ursprüngliche Entwicklungsidee

Zufällig beherrsche ich Programmiersprachen wie C# und Python sowie Desktop-Softwareentwicklung. Im Dezember 2024 begann ich daher mit der Entwicklung eines Demos. In der anfänglichen Technologieauswahl habe ich sorgfältig abgewogen: **Welche Sprache und Architektur eignen sich am besten für dieses Ziel?**

Mein erster Gedanke war Python — schnelle Prototypen sind für Entwickler vorteilhaft. Aus Nutzersicht hat es jedoch entscheidende Nachteile: geringe Ausführungseffizienz (bei kleinen Datenmengen weniger spürbar, aber dennoch ein Performance-Unterschied) und sehr große Installationspakete. Ich habe ein einfaches Programm, ursprünglich in WPF geschrieben, einmal mit Python neu implementiert. Selbst in einer frischen virtuellen Umgebung betrug das Paket mehrere hundert MB, während das .NET-Paket nur etwa ein Dutzend MB umfasste — ein enormer Unterschied. **Nutzer zu zwingen, für einfache Funktionen hunderte MB oder sogar GB zu installieren und komplexe Konfigurationen durchzuführen, ist keine gute Lösung.**

Als Nächstes erwog ich Webentwicklung. Die Vorteile liegen auf der Hand — keine Installation, sofort einsatzbereit. Problematisch war jedoch: Einerseits beherrsche ich Webtechnologien nicht umfassend; andererseits überstiegen die langfristigen Serverkosten meine persönlichen Wartungsmöglichkeiten. Daher verlagerte ich den Schwerpunkt zurück auf Client-Entwicklung.

![plot_home_ui](/img/about-illustrations/03-technology-choice-path.png)

Für Client-Entwicklung gibt es viele Optionen; im Vergleich zu C++-basiertem Qt bin ich mit .NET vertrauter. In Verbindung mit der hohen Ausführungseffizienz von .NET und der Verbreitung von Windows wählte ich .NET als Hauptplattform. Anfangs erwog ich auch Avalonia für plattformübergreifende Unterstützung unter Windows, Linux und macOS; während der Entwicklung stellte sich jedoch heraus, dass einige Projektabhängigkeiten mit Avalonia inkompatibel sind, was hohe Migrationskosten verursacht. Mit meinen persönlichen Ressourcen ist es in der Frühphase schwer, diese Module umzubauen. Daher konzentriert sich meine Arbeit derzeit weiterhin auf Windows; eine plattformübergreifende Version könnte irgendwann folgen.

## Kontakt

Wenn Sie sich für Softwareentwicklung interessieren und mit mir zusammenarbeiten möchten — oder Vorschläge und Feedback, Diagrammvorlagen, Algorithmen oder sogar eine geschäftliche Zusammenarbeit anbieten — kontaktieren Sie mich gerne unter **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)**.

*Wenn Sie beim Einsatz der Software Mängel oder Probleme feststellen, geben Sie uns bitte jederzeit Feedback. Da die Funktionalität stetig wächst und komplexer wird, ist es schwer, alle Szenarien im Test abzudecken; es können Lücken oder Ungenauigkeiten bleiben. Ich werde mich bemühen, diese zu beheben und zu optimieren, um Ihnen ein stabileres und zuverlässigeres Nutzererlebnis zu bieten.*

![plot_home_ui](/img/about-illustrations/04-long-term-maintenance.png)
