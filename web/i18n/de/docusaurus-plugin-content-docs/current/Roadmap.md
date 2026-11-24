---
sidebar_position: 3
---

# 🛣️ Fahrplan

Das Ziel von GeoChemistry Nexus ist, schrittweise eine integrierte wissenschaftliche Werkzeugkette für Geochemie und Petrologie aus **Daten · Diagrammen · Berechnung · Zusammenarbeit** aufzubauen.

## ✅ Abgeschlossen


### Kernablauf
- [x] **Startseite**: Kurzlinks und praktische Kleinwerkzeuge
- [x] **Diskriminationsdiagramme**: Auf der Vorlagenbibliothek beruht eine große Zahl eingebauter Diskriminationsdiagramme, einschließlich, aber nicht beschränkt auf Diskriminationsdiagramme tektonischer Milieus, Gesteinsklassifikationsdiagramme und weitere. Darüber hinaus werden Spiderdiagramme für Spurenelemente und Seltenerdelemente unterstützt, und es ist eine große Zahl von Normierungsschemata eingebaut. Das Plotten von Harker-Diagrammen als Mehrfachdiagramme wird unterstützt.
- [x] **Geothermobarometer**: Eingebaute, online verfügbare Berechnungsvorlagen für Geothermobarometer mehrerer Minerale sowie Excel-ähnliche benutzerdefinierte Funktionen
- [x] **EPMA-Mineralberechnung**: Eingebaute, online in Echtzeit aktualisierte EPMA-Berechnungsvorlagen für verschiedene Minerale, nach dem Download sofort verwendbar
- [x] **CIPW-Normberechnung**: Normierung von Hauptelementdaten des Gesamtgesteins und Export der Ergebnisse
- [x] **Niggli-Normberechnung**: Normierung von Hauptelementdaten des Gesamtgesteins und Export der Ergebnisse
### Fähigkeiten der Plattform
- [x] **Leistungsabhängigkeit**: sehr geringer Speicherbedarf und sehr geringer Installationsumfang. Die lokale Software ohne `.NET`-Laufzeitumgebung umfasst nur 9 MB.
- [x] **Vorlagenökosystem**: dynamische Aktualisierung der Online-Vorlagen aus der Cloud; Erstellen, Importieren und paketiertes Teilen benutzerdefinierter Vorlagen; Berechnungsdetails sind zur Prüfung einsehbar.
- [x] **Mehrsprachigkeit**: Lokalisierung der Oberfläche (Chinesisch / Englisch / Deutsch / Russisch / Japanisch / Koreanisch und weitere); Sprachwechsel der Diagrammvorlagen mit einem Klick
- [x] **Export und Einbindung**: Export als PNG / JPG / BMP / SVG; Zusammenarbeit mit Drittanbietersoftware wie CorelDRAW, Inkscape und Adobe Illustrator

## 📅 Pläne für die nähere Zukunft
### Abbildungen der Isotopengeochronologie
- [ ] U-Pb-Konkordia-Diagramm (Concordia)
- [ ] Isochronendiagramme für Rb-Sr, Sm-Nd, Lu-Hf, U-Pb und weitere
- [ ] Diagramme zu initialen Isotopenverhältnissen, zum Beispiel (⁸⁷Sr/⁸⁶Sr)ᵢ gegen εNd(t)
- [ ] und weitere


## 🔭 Mittelfristige und langfristige Perspektive
### Analyse mit Unterstützung durch maschinelles Lernen
Einführung gebräuchlicher Abläufe des maschinellen Lernens zur Unterstützung von Diskrimination und Klassifikation geochemischer Daten. Ausgegeben werden Modellbewertung, Variablenwichtigkeit sowie visuelle Ergebnisse wie ROC-Kurven und Konfusionsmatrizen, **ohne dass Benutzerinnen und Benutzer selbst eine Python-Umgebung einrichten müssen**.
### Diskriminationsmodelle im Stil „neuer Diagramme“
Es soll möglich sein, erweiterbare Diskriminationsmodelle des maschinellen Lernens zu laden, etwa neuartige Diskriminationsdiagramme, die auf großen Datenmengen trainiert wurden. Forschende können „neue Diagramme“ dann unmittelbar anwenden, ohne den vollständigen Entwicklungsstack des maschinellen Lernens beherrschen zu müssen.
### KI-Forschungsassistent (in Stufen)
1. **Erste Stufe**: RAG-Fragen und -Antworten auf Grundlage der eingebauten Hilfedokumentation und Wissensbasis; Handlungsvorschläge anhand von Schlüsselwörtern und Anwendungssituationen
2. **Zweite Stufe**: weitere Unterstützung bei der Datenverarbeitung und bei Analyseabläufen, Vereinfachung wiederkehrender Vorgänge
### Plattformübergreifende Nutzung und Weiteres
- [ ] Bewertung von Ansätzen wie Avalonia und schrittweise Unterstützung von Linux / macOS (derzeit liegt der Schwerpunkt auf Windows)
- [ ] Fortlaufende Verbesserung von Leistung, Vorlagenformat und Erweiterungsschnittstellen für Entwickler
