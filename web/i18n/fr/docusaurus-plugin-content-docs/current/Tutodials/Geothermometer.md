---
sidebar_position: 3
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Géothermobaromètres

Dans les premières versions, ce module était appelé en abrégé **GTM**. Il a ensuite été renommé géothermomètre, puis a reçu son nom définitif actuel : géothermobaromètre.

Les premières versions comprenaient le calcul de géothermomètres pour des minéraux tels que le zircon, la sphalérite, le quartz, le xénotime, la chlorite et la biotite. La nouvelle version des géothermobaromètres est encore en cours de mise à jour et d'adaptation ; elle sera bientôt synchronisée.

## Interface

Le module des géothermobaromètres se divise globalement en deux parties :

1. À gauche, la **liste des modèles de géothermobaromètres**. Vous pouvez y chercher le modèle qui vous convient, ou créer votre propre modèle de calcul de géothermobaromètre.
2. À droite, la **documentation d'aide du modèle de géothermobaromètre actuellement sélectionné**

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geothermobarometer_interface.webp')}
    alt="Interface des géothermobaromètres"
    style={{width: '100%'}}
  />
</div>

## Fonctions

Dès sa conception, le module des géothermobaromètres **imite la forme d'Excel**, afin d'abaisser le coût de prise en main. **L'utilisation des géothermobaromètres se fait principalement de deux façons** :

1. **Modèles de géothermobaromètres en ligne** : par défaut, l'utilisateur ne peut pas modifier les **modèles de géothermobaromètres en ligne**, mais il peut consulter la logique de mise en œuvre de l'algorithme. Vous pouvez obtenir en ligne davantage de modèles de géothermobaromètres récents sans mettre à jour le logiciel. Les chercheurs peuvent rechercher, filtrer et choisir le modèle qui leur convient, puis saisir les données et lancer le calcul en suivant le guide. **Cette façon est plus directe et plus rapide, et elle ne demande aucune connaissance en programmation**.
2. **Fenêtre de calcul indépendante** : dans la barre de menus supérieure, les chercheurs peuvent ouvrir la fenêtre de calcul indépendante par **Fichier -> Ouvrir un tableau indépendant**. Dans la fenêtre indépendante, ils sont autorisés à programmer eux-mêmes et à combiner différents géothermobaromètres pour le calcul. Pour les fonctions qui correspondent aux différents géothermobaromètres, voir la partie des tutoriels avancés. **Cette façon est plus complexe, et relativement plus souple**.

Bien entendu, le module des géothermobaromètres permet aussi aux chercheurs de créer leurs propres géothermobaromètres personnalisés. Le modèle de géothermobaromètre ainsi préparé peut être exporté et diffusé, pour être utilisé rapidement par d'autres chercheurs.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/export_geothermobarometer_template.webp')}
    alt="Exporter un géothermobaromètre"
    style={{width: '45%'}}
  />
</div>

## Exemple d'utilisation

### Utiliser un modèle de géothermobaromètre en ligne

D'abord, dans le module des géothermomètres, filtrez et choisissez le modèle de calcul correspondant dans la barre latérale gauche :

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/select_and_apply_thermobarometer_template.webp')}
    alt="Choisir un géothermobaromètre"
    style={{width: '100%'}}
  />
</div>

Vous pouvez consulter la documentation de ce modèle dans le panneau de droite. La barre supérieure affiche aussi le titre et les étiquettes du modèle de géothermobaromètre actuellement sélectionné. Une fois le modèle choisi, vous pouvez cliquer sur le bouton **Appliquer** de la barre supérieure pour entrer dans l'interface de calcul du géothermobaromètre.

Dans l'interface du géothermobaromètre, on voit les colonnes de saisie correspondantes. Vous pouvez aussi cliquer sur le bouton **Exemple** de la barre supérieure pour charger des données d'exemple. Il vous suffit de saisir les données selon les exigences de l'en-tête de colonne, puis de **copier la formule de calcul** ou d'**utiliser la poignée de recopie « cliquer et faire glisser »** (comme dans Excel) pour terminer automatiquement le calcul (comme le montre l'animation ci-dessous).

> Il faut préciser que la colonne `Sample` ne participe pas au calcul : elle ne sert que de commentaire pour identifier l'échantillon des données

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/calculate_thermobarometer_using_formula.webp')}
    alt="Calculer un géothermobaromètre avec une formule"
    style={{width: '80%'}}
  />
</div>

Bien entendu, la version actuelle des géothermobaromètres permet déjà de consulter le détail interne du calcul.

Vous pouvez sélectionner la ligne de données correspondante avec la souris, cliquer sur le bouton **Inspecter** en haut, et consulter le détail des résultats intermédiaires dans la barre latérale qui s'ouvre.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_calculation_details.webp')}
    alt="Détail du calcul du géothermobaromètre"
    style={{width: '100%'}}
  />
</div>

### Fenêtre indépendante et fonctions personnalisées

Comme indiqué plus haut, on peut, dans la barre de menus, **Fichier -> Ouvrir un tableau libre**, ouvrir la fenêtre libre indépendante.

Le tableau de la fenêtre indépendante est un tableau vide, qui comporte quelques boutons simples de barre d'outils.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_free_form.webp')}
    alt="Tableau de calcul libre du géothermobaromètre"
    style={{width: '100%'}}
  />
</div>

Utiliser le tableau de la fenêtre indépendante, c'est comme **utiliser au quotidien un tableau Excel : on peut saisir n'importe quelle valeur, et appeler une fonction en saisissant `=NomFonction(paramètres)`**.

La différence est que nos calculs de géothermobaromètres sont enregistrés comme des fonctions de calcul correspondantes. Les chercheurs peuvent ainsi personnaliser plus souplement le contenu du calcul selon leurs besoins. Vous pouvez même **comparer, dans un même tableau, les résultats de plusieurs géothermobaromètres**.

:::note

Il faut noter que, **dans le tableau de la fenêtre indépendante, on ne peut pas consulter le déroulement intermédiaire du calcul de la ligne de données correspondante**.

:::
