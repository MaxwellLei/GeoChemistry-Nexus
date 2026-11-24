---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Startseite

Wir möchten, dass die Startseite ein bequemer Einstieg wird, der geowissenschaftliche Forschungsressourcen mit dem alltäglichen Arbeitsablauf verbindet. Durch zusammengeführte Kurzzugänge zu häufig genutzten Websites der Geowissenschaften und durch leichte, praktische Kleinwerkzeuge unterstützt sie die Benutzerinnen und Benutzer bei alltäglichen Vorgängen und einfachen Berechnungen. Ursprünglich getrennte Funktionsmodule, Online-Ressourcen und Website-Werkzeuge werden so organisch miteinander verbunden, und es entsteht ein vernetzter, effizienter und bequemer Zugang zu einem umfassenden Dienst.

![plot_home_ui](/img/v0.9.0/en-US/Software_main_interface.webp)

Die Startseite gliedert sich vor allem in drei Teile: **Mitteilungen**, **Kurzlinks** und **Kleinwerkzeuge**.

* **Mitteilungen**: nehmen zugehörige Hinweise und Push-Nachrichten des Servers entgegen
* **Kurzlinks**: führen Einstiege aus den Geowissenschaften zusammen und gliedern sich in zwei große Gruppen. **Empfohlene Links** und **persönliche Links**. Empfohlene Links können Sie persönlich nicht ändern; sie werden über den Server als eingebaute Empfehlungen bereitgestellt. **Wenn Sie möchten, dass Ihre Website den Empfehlungen der Startseite hinzugefügt wird, kontaktieren Sie uns bitte über die Seite „Über“.** Persönliche Links fügen Sie selbst hinzu, löschen sie und verwalten so Ihre bevorzugten Links.
* **Kleinwerkzeuge**: Die Kleinwerkzeuge sollen einfache, eigenständige und grundlegende Berechnungen oder Bedienvorgänge bereitstellen, die gebräuchlichen peripheren Werkzeuge ergänzen und den Alltag erleichtern. Später sollen sie als erweiterbare Plugins umgesetzt werden. Derzeit lassen sie sich nicht frei erweitern.

## Kleinwerkzeuge

Die Kleinwerkzeuge umfassen derzeit vor allem: **Alkalinitätsrechner, Härterechner, Schwarzkörperstrahlungsrechner, geochemisches Periodensystem, chronostratigraphische Schnellreferenzkarte, Element-/Oxid-Umrechner, Umrechner für geologisch gebräuchliche Einheiten**.

**Wir danken [Kurt Hollocher (Union College)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/) sehr für die öffentlich zugänglichen methodischen Hinweise zur Berechnung der Alkalinität, der Härte und der Schwarzkörperstrahlung.**

### Alkalinitätsrechner

Berechnet die Alkalinität einer Wasserprobe aus dem Titrationsendpunkt mit Salzsäure. Nach Eingabe der HCl-Konzentration, des Volumens der Wasserprobe und des verbrauchten HCl-Volumens wird die Alkalinität (meq/L) ausgegeben und in g/L, meq/L, molare Konzentration und ppm für die vier Darstellungen HCO₃⁻, CO₃²⁻, CaCO₃ und 0.5(CaCO₃) umgerechnet.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/alkalinity_calculator.webp')}
    alt="Alkalinitätsrechner"
    style={{width: '60%'}}
  />
</div>

### Härterechner

Berechnet die Gesamthärte des Wassers aus den Konzentrationen von Magnesium und Calcium. Nach Eingabe der Konzentrationen von Mg und Ca (ppm) wird die Härte als CaCO₃ (ppm) umgerechnet und nach der USGS-Einstufung als weiches Wasser, mittelhartes Wasser, hartes Wasser oder sehr hartes Wasser gekennzeichnet.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/hardness_calculator.webp')}
    alt="Härterechner"
    style={{width: '60%'}}
  />
</div>

### Schwarzkörperstrahlungsrechner

Berechnet die Schwarzkörperstrahlung aus der Temperatur. Nach Eingabe der Temperatur in Kelvin werden die Gesamtstrahlungsleistung nach Stefan–Boltzmann und die Wellenlänge des Wien-Maximums ausgegeben, und nach der Planck-Formel wird eine normierte Spektralkurve gezeichnet.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/black_body_radiation_calculator.webp')}
    alt="Schwarzkörperstrahlungsrechner"
    style={{width: '70%'}}
  />
</div>

### Geochemisches Periodensystem

Interaktives Periodensystem, in dem die Elemente nach geochemischen Gruppen nachgeschlagen werden können. Ein ausgewähltes Element zeigt Ordnungszahl, Atommasse, Elektronenkonfiguration, Häufigkeit in der Erdkruste, Oxidationsstufen und die Molekülmassen häufiger Oxide. Hervorheben lassen sich außerdem die Gruppen der Seltenerdelemente, der High-Field-Strength-Elemente, der Large-Ion-Lithophile-Elemente und der Platingruppenelemente sowie lithophile, siderophile, chalkophile und atmophile Elemente.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geochemical_periodic_table.webp')}
    alt="Geochemisches Periodensystem"
    style={{width: '70%'}}
  />
</div>

### Chronostratigraphische Schnellreferenzkarte

Chronostratigraphische Schnellreferenz auf Grundlage der internationalen geologischen Zeitskala der ICS. Sie können nach chinesischen und englischen Namen suchen oder ein Alter (Ma) eingeben und die zugehörige stratigraphische Einheit zurückverfolgen, entlang der Hierarchie tiefer gehen und Grenzalter sowie die offiziellen Farben der IUGS ansehen.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp')}
    alt="Chronostratigraphische Schnellreferenzkarte"
    style={{width: '60%'}}
  />
</div>

### Element-/Oxid-Umrechner

Rechnet zwischen den Massenanteilen von Element und Oxid um. Nach Auswahl einer Paarung wie Si/SiO₂, Al/Al₂O₃, Fe/Fe₂O₃ oder FeO und Eingabe der wt% auf einer Seite ergibt der stöchiometrische Faktor das Ergebnis der anderen Seite. Abgedeckt werden Hauptelemente des Gesamtgesteins und häufige Oxide von Spurenelementen.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/element_oxide_converter.webp')}
    alt="Element-/Oxid-Umrechner"
    style={{width: '60%'}}
  />
</div>

### Umrechner für geologisch gebräuchliche Einheiten

Rechnet zwischen Einheiten um, die in der geologischen Arbeit gebräuchlich sind. Abgedeckt werden Gehaltskonzentrationen (g/t, ppm, wt%, ppb, oz/t), Druck und Spannung (GPa, kbar, MPa, bar, atm, psi), Längenmaße im Mikrobereich (mm, μm, nm, Å, pm), Temperatur (℃, K, ℉) und Isotopenalter (Ga, Ma, ka, yr), ergänzt um häufige geologische Referenzwerte.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geological_common_unit_converter.webp')}
    alt="Umrechner für geologisch gebräuchliche Einheiten"
    style={{width: '60%'}}
  />
</div>
