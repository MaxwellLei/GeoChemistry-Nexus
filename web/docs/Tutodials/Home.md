---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Home

We want the home page to be a convenient entrance that connects geoscience research resources with everyday workflows. It brings together shortcuts to websites commonly used in the geosciences and offers light, practical widgets that help with everyday operations and simple calculations. Features, online resources, and web tools that used to sit apart are linked into one connected, efficient service entrance.

![plot_home_ui](/img/v0.9.0/en-US/Software_main_interface.webp)

The home page has three parts: **Announcements**, **Quick links**, and **Widgets**.

* **Announcements**: receive notices and push notifications related to the server
* **Quick links**: bring together entrances to the geosciences, in two groups. **Recommended links** and **Personal links**. Recommended links cannot be edited by an individual. They are built-in recommendations from the server. **If you want your website added to the recommended links on the home page, contact us through the About page.** Personal links are favorites that you add, delete, and manage yourself.
* **Widgets**: widgets are meant to provide simple, independent, basic calculations or operations, as a supplement to everyday peripheral tools. They will later take the form of extensible plugins. They cannot be extended freely yet.

## Widgets

The widgets today are mainly: **alkalinity calculator, hardness calculator, blackbody radiation calculator, geochemical periodic table, chronostratigraphic quick-reference card, element/oxide converter, and common geological unit converter**.

**We are especially grateful to [Kurt Hollocher (Union College)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/) for publishing the reference methods for alkalinity, hardness, and blackbody radiation.**

### Alkalinity calculator

Calculate the alkalinity of a water sample from the hydrochloric acid titration end point. After you enter the HCl concentration, the sample volume, and the volume of HCl consumed, it reports alkalinity (meq/L) and converts that result to g/L, meq/L, molar concentration, and ppm for four expressions: HCO₃⁻, CO₃²⁻, CaCO₃, and 0.5(CaCO₃).

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/alkalinity_calculator.webp')}
    alt="Alkalinity calculator"
    style={{width: '60%'}}
  />
</div>

### Hardness calculator

Calculate the total hardness of water from magnesium and calcium concentrations. After you enter the Mg and Ca concentrations (ppm), it converts them to hardness as CaCO₃ (ppm) and labels the water soft, moderately hard, hard, or very hard on the USGS scale.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/hardness_calculator.webp')}
    alt="Hardness calculator"
    style={{width: '60%'}}
  />
</div>

### Blackbody radiation calculator

Calculate blackbody radiation from temperature. After you enter the temperature in kelvin, it reports the Stefan–Boltzmann total radiated power and the Wien peak wavelength, and plots a normalized spectral curve from the Planck function.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/black_body_radiation_calculator.webp')}
    alt="Blackbody radiation calculator"
    style={{width: '70%'}}
  />
</div>

### Geochemical periodic table

An interactive periodic table for looking up elements by geochemical group. Select an element to see its atomic number, atomic weight, electron configuration, crustal abundance, oxidation states, and the molecular weights of common oxides. You can also highlight the rare earths, high field strength elements, large-ion lithophile elements, the platinum-group elements, and the lithophile, siderophile, chalcophile, and atmophile groups.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geochemical_periodic_table.webp')}
    alt="Geochemical periodic table"
    style={{width: '70%'}}
  />
</div>

### Chronostratigraphic quick-reference card

A chronostratigraphic quick reference based on the ICS International Chronostratigraphic Chart. Search by Chinese or English name, or enter an age (Ma) to find the stratigraphic unit, then drill down the hierarchy to see boundary ages and the official IUGS colors.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp')}
    alt="Chronostratigraphic quick-reference card"
    style={{width: '60%'}}
  />
</div>

### Element/oxide converter

Convert between element and oxide mass fractions. After you choose a pair such as Si/SiO₂, Al/Al₂O₃, Fe/Fe₂O₃, or FeO and enter wt% on one side, the stoichiometric factor gives the result on the other side. It covers whole-rock major elements and common trace oxides.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/element_oxide_converter.webp')}
    alt="Element/oxide converter"
    style={{width: '60%'}}
  />
</div>

### Common geological unit converter

Convert among units commonly used in geological work. It covers grade and concentration (g/t, ppm, wt%, ppb, oz/t), pressure and stress (GPa, kbar, MPa, bar, atm, psi), micro-scale length (mm, μm, nm, Å, pm), temperature (℃, K, ℉), and isotope age (Ga, Ma, ka, yr), with common geological reference values.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geological_common_unit_converter.webp')}
    alt="Common geological unit converter"
    style={{width: '60%'}}
  />
</div>
