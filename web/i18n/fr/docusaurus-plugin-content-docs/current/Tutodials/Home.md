---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Accueil

Nous souhaitons que la page d'accueil soit une entrée commode entre les ressources de la recherche en géosciences et le travail quotidien. En regroupant des accès rapides aux sites couramment utilisés en géosciences, et en proposant de petits outils légers et utiles, elle aide l'utilisateur à accomplir les opérations quotidiennes et les calculs simples. Elle relie ainsi, de façon organique, des modules, des ressources en ligne et des outils web auparavant dispersés, et constitue une porte d'entrée de services complète, interconnectée, efficace et commode.

![plot_home_ui](/img/v0.9.0/en-US/Software_main_interface.webp)

La page d'accueil se divise principalement en trois parties : les **annonces**, les **liens rapides** et les **widgets**.

* **Annonces** : réception des notifications et des envois liés au serveur
* **Liens rapides** : regroupement des entrées du domaine des géosciences, en deux grandes catégories. Les **liens recommandés** et les **liens personnels**. Les liens recommandés ne peuvent pas être modifiés par l'utilisateur : ce sont des recommandations intégrées, fournies par le serveur. Bien entendu, **si vous souhaitez que votre site soit ajouté aux recommandations de la page d'accueil, contactez-nous par la page « À propos »**. Les liens personnels sont ceux que l'utilisateur ajoute, supprime et gère lui-même, selon ses préférences.
* **Widgets** : les widgets ont pour objectif de fournir des fonctions de calcul ou d'opération simples, autonomes et élémentaires, en complément des outils périphériques courants, pour un usage quotidien. Ils prendront plus tard la forme de plug-ins extensibles ; pour l'instant, on ne peut pas les étendre librement.

## Widgets

Les widgets comprennent pour l'instant principalement : **calculateur d'alcalinité, calculateur de dureté, calculateur de rayonnement du corps noir, tableau périodique géochimique, fiche de consultation chronostratigraphique, convertisseur élément/oxyde, convertisseur d'unités géologiques courantes**.

**Nous remercions tout particulièrement [Kurt Hollocher(Union College)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/) pour les méthodes de calcul de l'alcalinité, de la dureté et du rayonnement du corps noir qu'il a rendues publiques, et qui nous ont servi de référence**.

### Calculateur d'alcalinité

Calcule l'alcalinité d'un échantillon d'eau à partir du point final du titrage par l'acide chlorhydrique. Après saisie de la concentration en HCl, du volume d'échantillon et du volume de HCl consommé, il donne l'alcalinité (meq/L), puis la convertit en g/L, meq/L, concentration molaire et ppm pour quatre formes d'expression : HCO₃⁻, CO₃²⁻, CaCO₃ et 0.5(CaCO₃).

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/alkalinity_calculator.webp')}
    alt="Calculateur d'alcalinité"
    style={{width: '60%'}}
  />
</div>

### Calculateur de dureté

Calcule la dureté totale de l'eau à partir des concentrations en magnésium et en calcium. Après saisie des concentrations en Mg et en Ca (ppm), il les convertit en dureté exprimée en CaCO₃ (ppm), et indique, selon la classification de l'USGS, une eau douce, une eau moyennement dure, une eau dure ou une eau très dure.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/hardness_calculator.webp')}
    alt="Calculateur de dureté"
    style={{width: '60%'}}
  />
</div>

### Calculateur de rayonnement du corps noir

Calcule le rayonnement du corps noir à partir de la température. Après saisie de la température en kelvins, il donne la puissance totale rayonnée selon Stefan–Boltzmann et la longueur d'onde du pic de Wien, et trace une courbe spectrale normalisée selon la formule de Planck.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/black_body_radiation_calculator.webp')}
    alt="Calculateur de rayonnement du corps noir"
    style={{width: '70%'}}
  />
</div>

### Tableau périodique géochimique

Tableau périodique interactif, qui permet de consulter les éléments par groupes géochimiques. En sélectionnant un élément, on peut voir le numéro atomique, la masse atomique, la configuration électronique, l'abondance crustale, les degrés d'oxydation et la masse moléculaire des oxydes courants. On peut aussi mettre en évidence les groupes des terres rares, des éléments à haute force de champ, des éléments lithophiles à grands ions et des éléments du groupe du platine, ainsi que les éléments lithophiles, sidérophiles, chalcophiles et atmophiles.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geochemical_periodic_table.webp')}
    alt="Tableau périodique géochimique"
    style={{width: '70%'}}
  />
</div>

### Fiche de consultation chronostratigraphique

Consultation chronostratigraphique fondée sur la charte internationale des temps géologiques de l'ICS. On peut rechercher par nom en chinois ou en anglais, ou saisir un âge (Ma) pour retrouver l'unité stratigraphique correspondante, puis descendre dans la hiérarchie et consulter les âges des limites ainsi que les couleurs officielles de l'IUGS.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp')}
    alt="Fiche de consultation chronostratigraphique"
    style={{width: '60%'}}
  />
</div>

### Convertisseur élément/oxyde

Convertit entre la fraction massique d'un élément et celle de l'oxyde. Après avoir choisi un couple tel que Si/SiO₂, Al/Al₂O₃, Fe/Fe₂O₃ ou FeO, saisissez le wt% d'un côté : le facteur stœchiométrique donne le résultat de l'autre côté. Le convertisseur couvre les éléments majeurs en roche totale et les oxydes traces courants.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/element_oxide_converter.webp')}
    alt="Convertisseur élément/oxyde"
    style={{width: '60%'}}
  />
</div>

### Convertisseur d'unités géologiques courantes

Convertit entre les unités courantes du travail géologique. Il couvre les teneurs (g/t, ppm, wt%, ppb, oz/t), la pression et la contrainte (GPa, kbar, MPa, bar, atm, psi), les échelles microscopiques (mm, μm, nm, Å, pm), la température (℃, K, ℉) et les âges isotopiques (Ga, Ma, ka, yr), et il joint des valeurs géologiques de référence courantes.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geological_common_unit_converter.webp')}
    alt="Convertisseur d'unités géologiques courantes"
    style={{width: '60%'}}
  />
</div>
