---
sidebar_position: 4
---

# Geothermobarometer

In frühen Versionen wurde dieses Modul kurz als **GTM** bezeichnet. Später hieß es Geothermometer und trägt nun den offiziellen Namen Geothermobarometer.

Frühe Versionen umfassten geologische Thermometer für Zirkon, Sphalerit, Quarz, Xenotim, Chlorit und Biotit. Das neue Geothermobarometer-Modul wird derzeit noch angepasst und wird in Kürze synchronisiert.

## 🌟 Merkmale

Das Geothermobarometer-Modul orientiert sich von Anfang an **am Excel-Prinzip**, um die Einarbeitung zu erleichtern. **Es gibt zwei Hauptnutzungsformen:**

1. **Integrierte Cloud-Vorlagen**: Standardmäßig sind **Cloud-Vorlagen** für Nutzer nicht editierbar und werden offiziell gepflegt. Bei Internetverbindung erhalten Sie die neuesten Geothermobarometer-Vorlagen ohne Software-Update. Forschende können passende Vorlagen suchen, Daten gemäß Anleitung eintragen und berechnen. **Diese Methode ist direkter und schneller — Programmierkenntnisse sind nicht erforderlich.**
2. **Unabhängiges Fenster**: Über die Menüleiste **Datei → Unabhängige Tabelle öffnen** lässt sich ein separates Fenster öffnen. Dort können Forschende benutzerdefinierte Berechnungen mit verschiedenen Geothermobarometern kombinieren. Die zugehörigen Funktionen finden Sie in den fortgeschrittenen Anleitungen. **Diese Methode ist komplexer, aber flexibler.**

Das Modul erlaubt zudem das Erstellen eigener Geothermobarometer. Als Paket exportiert können Sie diese an andere Forschende weitergeben.

![gtm_ui](/img/v0.7.1/geothermobarometer1.webp)

## Anwendungsbeispiel Geothermobarometer

### Integrierte Vorlagen verwenden

Wählen Sie zunächst im Geothermometer-Modul über die Menüleiste die gewünschte Berechnungsvorlage:

![gtm_ui](/img/v0.7.1/geothermobarometer2.webp)

Anschließend erscheint eine seitliche Bestätigungsleiste; nach Bestätigung wird die vorherige Vorlagenberechnung zurückgesetzt und überschrieben (falls zuvor ein anderes Thermobarometer gewählt wurde).

![gtm_ui](/img/v0.7.1/geothermobarometer3.webp)

Als Nächstes erscheint eine Beispielvorlage für ein Thermometer. Tragen Sie die entsprechenden Daten ein und nutzen Sie den „Klicken und Ziehen“-Füllgriff (ähnlich wie in Excel), um Berechnungen automatisch auszuführen (siehe Animation unten).

![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/V0_5_0_0/Example.gif)

Neuere Versionen unterstützen zudem die Anzeige interner Berechnungsdetails. Markieren Sie eine Datenzeile mit der Maus, um Zwischenergebnisse im unteren Bereich einzusehen.

![gtm_ui](/img/v0.7.1/geothermobarometer4.webp)

### Unabhängiges Fenster — benutzerdefinierte Funktionen

Wie oben beschrieben, öffnen Sie über **Datei → Unabhängige Tabelle öffnen** ein separates Fenster.

Die unabhängige Tabelle ist leer und enthält einige einfache Symbolleisten-Schaltflächen.

![gtm_ui](/img/v0.7.1/geothermobarometer5.webp)

Die Nutzung entspricht dem **täglichen Arbeiten mit Excel: Beliebige Werte eingeben und mit `=Funktionsname(Übergabeparameter)` Funktionen aufrufen.**

Der Unterschied: Geothermobarometer-Berechnungen sind als Funktionen registriert; Forschende können Berechnungen flexibel anpassen — u. a. Vergleiche mehrerer Thermobarometer. In Standardvorlagen lassen sich nicht gleichzeitig mehrere Ergebnisse berechnen (außer nacheinander pro Vorlage). Im unabhängigen Fenster können Sie mehrere Geothermobarometer-Funktionen in derselben Tabelle definieren und nutzen.

:::note

**Im unabhängigen Fenster können Zwischenberechnungen einzelner Datenzeilen nicht angezeigt werden.**

:::

---

Wir laden Mitwirkende herzlich ein, gemeinsam ein besseres Software-Erlebnis zu schaffen. 🤝😊
