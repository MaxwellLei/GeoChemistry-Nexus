---
sidebar_position: 2
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Diagrammes

Dans le module des diagrammes, nous intégrerons davantage de modèles de diagrammes fondamentaux des géosciences, notamment les diagrammes de discrimination des contextes tectoniques, les diagrammes de classification des roches, et d'autres encore. **Notre objectif final est de créer, pour les géosciences, une boîte à outils complète de cadres graphiques et de tracé, et d'offrir aux chercheurs le plus de commodité possible.** 🧪

:::info

Si vous êtes un chercheur travaillant sur les diagrammes géologiques ou dans un domaine voisin, et que vous souhaitez faire connaître vos propres diagrammes, n'hésitez pas à nous contacter ! Nous aiderons vos diagrammes à atteindre un public scientifique plus large, et à renforcer encore l'efficacité de diffusion et l'influence de vos résultats.

:::

## Démarrage rapide

Le module de diagrammes de notre logiciel se compose principalement de deux parties : la bibliothèque de modèles de diagrammes et l'interface de travail des diagrammes. Vous pouvez d'abord choisir, dans la bibliothèque, le type de diagramme voulu, puis entrer dans l'interface de travail correspondante pour saisir les données, traiter le diagramme et exporter les résultats.

### Bibliothèque de modèles

Nous répartissons le module de diagrammes en trois grandes catégories : les **modèles cloud en ligne**, **mes modèles** et les **modèles de diagrammes outils**.

**Modèles cloud en ligne** : nous les mettons à jour et les maintenons en continu. Sans mettre à jour le logiciel, et dès lors que vous êtes connecté, vous pouvez obtenir les modèles de diagrammes mis à jour, et disposer ainsi en permanence des ressources de modèles les plus complètes et les plus faisant autorité.

**Mes modèles** : pour les cas où il faut créer un modèle personnalisé répondant à un besoin de recherche particulier. Vous pouvez non seulement créer ces modèles vous-même, mais aussi les exporter afin de les partager facilement avec d'autres chercheurs, et ainsi favoriser l'échange et la diffusion scientifiques.

> *À l'avenir, nous prévoyons de créer une communauté dédiée aux modèles de diagrammes, où les utilisateurs pourront facilement créer, téléverser, partager et télécharger divers **modèles personnels personnalisés**, afin de renforcer encore la souplesse et l'extensibilité du système.*

**Modèles de diagrammes outils** : les diagrammes courants et fixes en sont extraits séparément. Ils comprennent pour l'instant le **diagramme en toile d'araignée des terres rares, le diagramme en toile d'araignée des éléments en traces, le diagramme de Harker en planches multiples**, ainsi que les diagrammes de dispersion bidimensionnels et ternaires habituels.

#### Interface

Par défaut, à l'entrée dans le module de tracé, la bibliothèque intégrée de modèles des géosciences s'affiche. L'interface se divise en trois parties principales :

*   **Gauche – classification des diagrammes** : affiche tous les niveaux de modèles et les modèles correspondants, y compris la liste des modèles personnalisés.
*   **Droite – cartes de diagrammes** : affiche les cartes de diagrammes du niveau actuel, avec le nom et l'image d'aperçu.
*   **Haut – barre de navigation** : le logiciel adopte une conception sans bordure ; la barre de navigation du haut reprend donc une partie des fonctions de chaque page. Dans l'interface des diagrammes, elle affiche le chemin du modèle de diagramme actuel, ainsi que la recherche de modèles de diagrammes et d'autres fonctions.

![graphical_interface](/img/v0.9.0/en-US/graphical_interface.webp)

Choisissez le modèle de diagramme approprié, puis cliquez sur sa carte pour entrer dans l'interface de travail du diagramme. Sur la carte d'un modèle, vous pouvez aussi faire un clic droit pour exporter, supprimer, ajouter aux favoris, et effectuer d'autres opérations.

:::tip

Vous pouvez appuyer sur la touche <kbd>Alt</kbd> droite pour supprimer rapidement un modèle de diagramme ou l'ajouter aux favoris.

:::

#### Barre de menus supérieure

La barre de menus **Fichier**, en haut, contient :

1.  **Nouveau diagramme** : sert à créer un modèle de diagramme personnalisé.
2.  **Ouvrir un diagramme** : sert à ouvrir temporairement un modèle de diagramme ; seuls les fichiers `.diag.zip` sont pris en charge.
3.  **Importer un diagramme** : sert à importer un paquet externe de modèle de diagramme (`.diag.zip`) dans la liste locale Mes modèles.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphic_menu_bar.webp')}
    alt="Barre de menus des diagrammes"
    style={{width: '50%'}}
  />
</div>

### Interface de travail du diagramme

#### Interface

L'interface du diagramme se divise en cinq parties principales :

-   **Barre d'outils** : comprend des boutons rapides et trois onglets de fonctions : Tracé, Données et Édition.
-   **Liste des calques (objets)** : liste des éléments dessinés sur le modèle. Cliquer sur un élément permet d'en modifier les propriétés.
-   **Canevas de tracé** : zone centrale, pour voir le diagramme, importer les données, régler l'apparence et consulter la notice du modèle.
-   **Barre d'état** : affiche les informations de base du tracé, notamment la langue actuelle du diagramme et les informations de coordonnées.
-   **Panneau des propriétés** : affiche les propriétés de l'élément de tracé sélectionné (couleur, taille, etc.), afin d'obtenir l'effet visuel voulu.

![graphical_operation_interface](/img/v0.9.0/en-US/graphical_operation_interface.webp)

#### Barre d'outils du diagramme

La barre d'outils se compose d'une série de **boutons rapides**. Principalement :

1. Revenir à la bibliothèque de modèles de diagrammes
2. Changer l'état de travail (mode diagramme, mode données, mode édition)
3. Divers panneaux de propriétés du diagramme, outils d'opération, etc.

Le détail des fonctions est donné sur la figure ci-dessous.

![graphical_toolbar](/img/v0.9.0/en-US/graphical_toolbar.webp)

### Liste des calques

Les éléments de tracé se répartissent en 7 grandes catégories :

-   **Line (ligne)** : définit le contour de base du diagramme ou un segment.
-   **Text (texte)** : étiquettes et annotations.
-   **Polygon (polygone)** : forme fermée à l'intérieur du diagramme.
-   **Arrow (flèche)** : objet de tracé orienté.
-   **Function (fonction)** : permet à l'utilisateur de saisir une fonction mathématique personnalisée et son domaine de définition.
-   **Axes (axes)** : axes de coordonnées du diagramme.
-   **Data Point (point de données)** : élément qui représente les données importées.

**Ordre de rendu par défaut (de haut en bas) : `points de données > texte > flèches > fonctions > lignes > polygones > axes`**.

![tutorial_plot5](/img/v0.9.0/en-US/diagrammatic_object_selection_example.webp)

Lorsque vous sélectionnez un élément dans le panneau des calques, il est mis en évidence sur le canevas, tandis que les autres éléments deviennent semi-transparents afin de réduire les interférences. Le panneau des propriétés affiche ensuite les propriétés liées à cet élément. 🔍

Vous pouvez modifier ces propriétés pour obtenir le style visuel que vous souhaitez.

:::tip

Pour désélectionner tous les éléments, il suffit de **faire un clic droit n'importe où sur le canevas**, ou d'utiliser le bouton **Désélectionner** de la barre d'outils.

:::

## Exemple d'utilisation

### Exemple

1. Dans la **bibliothèque de modèles de diagrammes**, choisissez un modèle de diagramme et entrez dans la page d'utilisation du diagramme. Par exemple : choisir le diagramme TAS.
2. Lorsque nous devons saisir des données, il faut passer du mode diagramme au mode données.
   
   ![tutorial_plot6](/img/v0.9.0/en-US/diagram_data_input.webp)


Pour le diagramme TAS, il faut saisir quatre colonnes de données : `Category`, `SiO2`, `K2O` et `Na2O`. La colonne de titre `Category` sert à regrouper les données et à produire la légende ; la colonne `Sample` sert à identifier l'étiquette de l'échantillon et ne participe à aucun calcul (elle peut être vide) ; l'unité des autres éléments est `wt.%`. Bien entendu, si vous ne savez pas quelles données ce diagramme attend, dans quelle unité, ni comment l'utiliser, vous pouvez cliquer sur le bouton d'aide du diagramme, dans le coin supérieur droit de la zone du diagramme, pour consulter la documentation de ce diagramme.

3. Une fois les besoins compris, saisissez vos données et cliquez sur le bouton de report des points, dans le coin supérieur droit du panneau de données, pour tracer les points. Si la saisie pose problème, modifiez les données, puis cliquez de nouveau sur le report des points.![tutorial_plot8](/img/v0.9.0/en-US/data_plotting.webp)
4. **Exporter le diagramme**. Nous proposons un **bouton de copie rapide, vers le presse-papiers, du résultat du report sur le diagramme**. Il existe aussi une **fonction d'exportation formelle**. Dans le panneau d'exportation formelle, nous prenons en charge l'exportation des images aux formats `jpg`, `png`, `bmp` et `svg`. Lorsque le dessin scientifique exige une figure vectorielle, nous recommandons le format `svg`. Nous prenons aussi en charge **l'envoi direct du résultat vers un logiciel de dessin tiers**, sans passer par l'étape intermédiaire d'exportation puis d'importation. Les logiciels tiers recommandés d'office sont : **CorelDRAW, Inkscape, Adobe Illustrator**. Nous recommandons plutôt la liaison avec Inkscape pour la retouche scientifique ultérieure. Nous permettons aussi de définir un autre logiciel de dessin tiers, à condition que ce logiciel prenne en charge le format vectoriel `svg`. Il suffit au chercheur d'indiquer, dans les paramètres, le chemin du programme du logiciel de dessin.
   ![tutorial_plot8](/img/v0.9.0/en-US/export_diagram.webp)

### Complément

Le modèle de diagramme lui-même permet de changer de langue : un même diagramme peut être affiché dans plusieurs versions linguistiques, afin de répondre aux besoins de l'internationalisation.

Par exemple : un diagramme destiné à un article dans une revue chinoise doit utiliser la version en chinois, tandis qu'une publication dans une revue SCI/IE en anglais exige la version anglaise du diagramme. Vous pouvez basculer en un clic vers la version linguistique correspondante du diagramme en cliquant sur le bouton de changement de langue, dans le coin supérieur droit de la barre d'outils du diagramme.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/switch_graphic_language.webp')}
    alt="Changer la langue du diagramme"
    style={{width: '60%'}}
  />
</div>
