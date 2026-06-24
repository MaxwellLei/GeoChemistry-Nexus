---
sidebar_position: 3
---

# 🛣️ Roadmap

Das Ziel von GeoChemistry Nexus ist nicht nur eine „Diagrammsoftware“, sondern schrittweise eine integrierte Werkzeugkette für geochemische und petrographische Forschung mit den Säulen **Daten · Diagramme · Berechnungen · Zusammenarbeit**.

## ✅ Abgeschlossen


### Kern-Workflow
- [x] **Startseite**: Schnelllinks, praktische Widgets
- [x] **Datenvorverarbeitung**: Erkennung von Hauptoxid-Spalten, Fe-Valenz-Schätzung / Rückrechnung, Strategien für Ausreißer, fehlende Werte und Nachweisgrenzen
- [x] **Diskriminative Diagramme**: Erstellung von Ternary-, Streu- und Spinnendiagrammen usw. auf Basis der Template-Bibliothek; Unterstützung für Datenprojektion und Stilbearbeitung
- [x] **Geothermometer (GTM)**: Integrierte Berechnungsvorlagen für mehrere Minerale sowie Excel-ähnliche benutzerdefinierte Funktionen
- [x] **CIPW-Standardmineralberechnung**: Norm-Berechnung aus Ganzgesteins-Hauptelementdaten und Ergebnisexport
### Plattformfähigkeiten
- [x] **Template-Ökosystem**: Dynamische Cloud-Updates offizieller Vorlagen; Erstellung, Import und Paketierung benutzerdefinierter Vorlagen im JSON- / ZIP-Format zum Teilen
- [x] **Mehrsprachigkeit**: Oberflächenlokalisierung (Chinesisch / Englisch / Deutsch usw.); Ein-Klick-Sprachwechsel für Diagrammvorlagen
- [x] **Export und Integration**: Export als PNG / JPG / BMP / SVG; Zusammenarbeit mit Drittanbieter-Software wie CorelDRAW, Inkscape, Adobe Illustrator



## 🔥 In Arbeit
- [ ] **Fortlaufende Erweiterung der Diagrammvorlagen-Bibliothek**  
  Schwerpunkt auf geochemischen diskriminativen Diagrammen wie tektonischer Umgebungs- und Gesteinsklassifikationsdiagrammen.
- [ ] **Weiterentwicklung des Geothermometer-Moduls**  
  Erweiterung gängiger geologischer Thermometer- / Barometer-Vorlagen.
- [ ] **Dokumentation und Lokalisierung**  
  Synchronisierte Aktualisierung der Dokumentation in Chinesisch / Englisch / Deutsch usw., um den Einstieg für neue Nutzer zu erleichtern.

## 📅 Kurzfristige Planung
### Isotopen-Datierung und Diagramme
- [ ] U-Pb-Konkordia-Diagramm (Concordia)
- [ ] Isochron-Diagramme für Rb-Sr, Sm-Nd, Lu-Hf, U-Pb usw.
- [ ] Diagramme zu Anfangsisotopenverhältnissen (z. B. (⁸⁷Sr/⁸⁶Sr)ᵢ vs. εNd(t))


## 🔭 Mittel- und langfristige Vision
### Machine-Learning-gestützte Analyse
Einführung gängiger ML-Workflows zur Unterstützung geochemischer Datendiskrimination und -klassifikation mit Ausgabe von Modellbewertung, Variablenwichtigkeit sowie ROC-Kurven, Konfusionsmatrizen usw. — **ohne dass Nutzer selbst eine Python-Umgebung einrichten müssen**.
### „Neue Diagramme“ als diskriminative Modelle
Unterstützung für erweiterbare ML-Diskriminationsmodelle (z. B. auf Basis großer Datensätze trainierte neue Diagramme), damit Forschende „neue Diagramme“ direkt anwenden können, ohne den vollständigen ML-Entwicklungsstack zu beherrschen.
### KI-Forschungsassistent (in Phasen)
1. **Phase 1**: RAG-basierte Fragen und Antworten auf Basis integrierter Hilfedokumentation und Wissensbasis, mit Handlungsempfehlungen nach Schlüsselwörtern und Szenario  
2. **Phase 2**: Weitere Unterstützung bei Datenverarbeitung und Analyse-Workflows zur Vereinfachung repetitiver Schritte
### Plattformübergreifend und Sonstiges
- [ ] Bewertung von Avalonia u. a., schrittweise Unterstützung für Linux / macOS (derzeit primär Windows)
- [ ] Fortlaufende Optimierung von Performance, Template-Format und Entwickler-Erweiterungsschnittstellen
