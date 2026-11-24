---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Créer un modèle de diagramme

Pour un modèle de diagramme qui ne figure pas encore dans la bibliothèque de modèles en ligne, vous pouvez choisir d'en créer un vous-même. En personnalisant un modèle et en le regroupant dans un paquet de modèle, vous pouvez le partager rapidement avec d'autres chercheurs.

Vous pouvez aussi choisir de fournir votre modèle au développeur, afin qu'il soit intégré à la bibliothèque de modèles de diagrammes en ligne. Nous remercions sincèrement toute personne qui contribue.

## Création rapide

Vous pouvez maintenant créer un nouveau modèle de diagramme en choisissant, dans la barre de menus, `Fichier` -> `Nouveau modèle de diagramme`, comme ci-dessous :

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template.webp')}
    alt="Paramètres du diagramme"
    style={{width: '50%'}}
  />
</div>

Après avoir cliqué sur « Nouveau modèle de diagramme », une fenêtre s'ouvre pour créer le nouveau modèle de diagramme :

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template_interface.webp')}
    alt="Interface de nouveau diagramme"
    style={{width: '100%'}}
  />
</div>

Pour un nouveau modèle de diagramme personnalisé, il y a principalement trois parties à configurer :

1.  **Langues prises en charge par défaut** : vous pouvez choisir, dans la liste de droite, des raccourcis de langues intégrés. Nous en proposons dix, dont le chinois simplifié, le chinois traditionnel, l'anglais américain, le japonais, le russe, le coréen et l'allemand.

    > Il faut préciser que la langue par défaut est celle vers laquelle on revient si une autre langue n'est pas traduite, ou en cas d'erreur. Par ailleurs, dans la nouvelle version, la personnalisation du code de langue par l'utilisateur n'est plus autorisée. Si vous avez besoin de la prise en charge de votre langue, contactez le développeur.

2.  **Classification (hiérarchie) du modèle de diagramme** : nous proposons une structure de classification rapide intégrée. Ce réglage détermine la position hiérarchique de votre modèle dans la liste des modèles de diagrammes.

3.  **Type de modèle de diagramme** : deux types sont actuellement pris en charge : le **repère à deux dimensions** et le diagramme ternaire.

Une fois les réglages terminés, cliquez sur « OK » pour entrer dans l'interface de dessin personnalisé. Ensuite, nous concentrons les opérations sur la barre de fonctions « Édition ». Après un clic sur « Édition », le système affiche une boîte de dialogue de confirmation pour modifier le diagramme. Après confirmation, vous entrez en mode édition : vous pouvez y consulter et utiliser les divers outils de la barre de fonctions d'édition.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/empty_diagram_template.webp')}
    alt="Diagramme vide"
    style={{width: '100%'}}
  />
</div>

## Dessiner le diagramme

En mode édition, les opérations suivantes sont permises :

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphical_toolbar.webp')}
    alt="Dessiner le diagramme"
    style={{width: '100%'}}
  />
</div>

* **Enregistrer** : enregistre le modèle de diagramme. Après le clic, le programme génère par défaut la vignette correspondante selon l'état actuel du dessin.
* **Enregistrer sous** : enregistre le modèle de diagramme à un autre emplacement de fichier.
* **Ajouter une ligne** : une fois activé, entre dans le mode « ajouter une ligne ». Cliquez sur le premier point de la zone de dessin pour commencer le trait, puis sur le second point pour terminer l'objet ligne.
* **Ajouter du texte** : appelé aussi annotation. Une fois activé, entre dans le mode « ajouter du texte ». Cliquez à un endroit précis du dessin pour le créer. Le texte par défaut est `Text`. Vous pouvez modifier la position ou le contenu dans la partie propriétés du panneau des calques.
* **Ajouter un polygone** : une fois activé, entre dans le mode « ajouter un polygone ». Ajoutez un polygone fermé en créant les sommets par des clics gauches successifs, puis fermez la forme par un clic droit.
* **Ajouter une flèche** : une fois activé, entre dans le mode « ajouter une flèche ». Le processus d'ajout ressemble à la création d'une ligne.
* **Ajouter une fonction** : après le clic, ajoute une fonction par défaut `sin(x)`, dont le domaine de définition est [-10, 10]. Vous pouvez personnaliser la formule dans le panneau des propriétés.
* **Annuler/Rétablir** : s'il n'y a pas eu de création ni de suppression d'objet de dessin, ces fonctions sont désactivées. Par défaut, l'historique ne conserve que les 10 dernières opérations.
* **Supprimer** : supprime un objet de dessin. Sélectionnez d'abord l'objet (par exemple un texte), puis cliquez sur Supprimer pour le retirer.

### Ajouter une ligne

Voici un exemple de panneau des propriétés pour l'ajout d'une ligne. Par le panneau des propriétés, vous pouvez régler avec précision la position de la ligne et ses autres propriétés.

**Le bouton de capture des coordonnées, à droite de chaque coordonnée, permet de réajuster et de capturer les coordonnées dans la zone de dessin**. Une fois déclenché, le prochain clic gauche dans la zone de dessin prend automatiquement comme coordonnées l'endroit cliqué.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_line.webp')}
    alt="Ajouter une ligne"
    style={{width: '100%'}}
  />
</div>

### Ajouter une flèche

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_arrow.webp')}
    alt="Ajouter une flèche"
    style={{width: '100%'}}
  />
</div>

### Ajouter un polygone

**Après avoir activé l'ajout de polygone, dans la zone du canevas, confirmez les sommets du polygone par des clics gauches successifs. Pendant ce temps, vous voyez la zone d'aperçu du polygone. Un clic droit termine la création du polygone**. L'objet polygone possède une liste de sommets : vous pouvez à tout moment sélectionner, modifier et supprimer le sommet correspondant.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_polygon.webp')}
    alt="Ajouter un polygone"
    style={{width: '100%'}}
  />
</div>

### Ajouter du texte

Après avoir activé l'ajout de texte, cliquez avec le bouton gauche n'importe où dans la zone du canevas pour créer un objet texte à l'endroit correspondant. Il faut préciser que, dans la barre d'état au bas du canevas, vous pouvez voir à quelle langue appartient le contenu de l'objet texte actuel.

Comme le diagramme prend en charge plusieurs langues dès l'origine, la traduction des contenus multilingues sera expliquée en détail plus loin.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_text.webp')}
    alt="Ajouter du texte"
    style={{width: '100%'}}
  />
</div>

### Ajouter une fonction

L'ajout d'une fonction diffère des objets précédents : **après un clic sur le bouton d'ajout de fonction, un objet fonction par défaut est ajouté immédiatement**. La fonction par défaut est `sin(x)`. Vous pouvez modifier la formule de la fonction dans le panneau des propriétés pour définir votre fonction.

Pour les propriétés de l'objet fonction, les deux paramètres les plus importants sont le **domaine de définition** et les **points d'échantillonnage**.

* **Le domaine de définition fixe l'étendue d'affichage de la fonction sur le canevas**.
* **Les points d'échantillonnage contrôlent la précision du tracé de la fonction sur le canevas**, et influent donc sur l'exactitude de l'algorithme de capture et de sélection à la souris. La valeur par défaut est `1000`. Plus la précision est élevée, plus le rendu de la fonction est fin, mais cela alourdit aussi le rendu par le programme.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_function.webp')}
    alt="Ajouter une fonction"
    style={{width: '100%'}}
  />
</div>

## Compléter le modèle de diagramme

Une fois les formes de base dessinées, un modèle complet exige encore :

1.  **Données et script** : servent à définir les données d'entrée du modèle de diagramme et l'algorithme de calcul des données, c'est-à-dire à indiquer comment les données sont calculées et comment le report des points est réalisé.
2.  **Documentation du diagramme** : sert à rédiger le contenu d'aide qui explique ce diagramme.
3.  **Multilingue** : si le modèle de diagramme est réglé sur plus de deux langues, il faut traduire la partie multilingue. Cela comprend la « documentation du diagramme » précédente, ainsi que les objets texte du canevas du diagramme, entre autres contenus.

On peut compléter le modèle de diagramme dans l'interface de travail du diagramme, mais nous vous recommandons tout de même de le faire dans la fenêtre d'édition du diagramme : ce sera plus commode.

### Données et script

Les données et le script sont la partie essentielle du modèle de diagramme, car ils définissent comment les données du diagramme sont calculées, puis finalement présentées sur la figure.

Deux paramètres sont nécessaires : les **paramètres de variables** du modèle de diagramme et l'**algorithme de calcul (script)**, comme ci-dessous :

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_data_and_script.webp')}
    alt="Diagramme – données et script"
    style={{width: '100%'}}
  />
</div>

Pour les **paramètres de variables** du modèle de diagramme, vous pouvez ajouter en haut les colonnes correspondantes, qui représentent les colonnes d'entrée exigées par ce diagramme. Il y a deux paramètres facultatifs 

1. Unit indique l'unité de la colonne d'entrée. Il ne sert qu'à l'affichage et à l'explication, et n'influe pas sur l'algorithme des données.
2. Group indique le groupe de la colonne d'entrée. Il ne sert qu'à l'affichage et à l'explication, et n'influe pas sur l'algorithme des données.

Il faut préciser que **les titres de colonnes que vous définissez dans le tableau de données sont, par défaut, des variables déjà enregistrées, que le script ci-dessous peut appeler directement**. Cela signifie que vous n'avez pas à redéfinir, dans le script, les variables des colonnes d'entrée.

Pour l'**algorithme de calcul (script)**, l'écriture se fait par défaut en langage `JavaScript`. Le contenu d'exemple du script par défaut est `return [X,Y]` : il renvoie aux axes de coordonnées à deux dimensions les colonnes d'entrée `X` et `Y` définies dans le tableau de données.

Par exemple, pour un diagramme TAS, les paramètres doivent être : `SiO2, Na2O, K2O`. Le contenu du script sera :

```javascript
// Calcul à l'aide des variables K2O + Na2O
var result1 = K2O + Na2O;
// Utilisation de SiO2
var result2 = SiO2;
// Renvoie deux valeurs calculées. Attention : pour l'image en coordonnées à deux dimensions par défaut, il n'y a que deux valeurs de retour.
// La première position correspond à la valeur de retour X, la seconde à la valeur de retour Y.
return [result2, result1]
```

Vous pouvez aussi écrire le script comme suit :

```javascript
var result = K2O + Na2O
return [SiO2, result]
```

Notez que la position des valeurs de retour est fixe. Dans `[x, y]`, la première valeur est toujours X (axe du bas), et la seconde est Y (axe de gauche).

:::info

Pour un diagramme ternaire, le format de retour final est `[A, B, C]` : la première valeur est A (axe du bas), la seconde est B (axe de gauche), et la troisième est C (axe de droite).

:::

### Documentation du diagramme

La documentation du diagramme est une étape nécessaire pour aider les autres chercheurs à comprendre rapidement les informations de base et l'usage de ce diagramme.

Rédigez la documentation du diagramme sur la page de la documentation d'aide. Nous proposons une barre d'outils simple, suffisante pour les besoins courants d'un document. Bien entendu, vous pouvez aussi rédiger le contenu dans `Office Word`, puis le coller ici : la très grande majorité des mises en forme sont compatibles.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_help_document.webp')}
    alt="Diagramme – documentation d'aide"
    style={{width: '100%'}}
  />
</div>

Pour la forme de la documentation du diagramme, nous recommandons de suivre la norme ci-dessous :

*   **Introduction** : explique le concept de base et la fonction du modèle de diagramme, afin d'aider l'utilisateur à comprendre rapidement.
*   **Format des données** : précise le format des données d'entrée et les titres de colonnes nécessaires à une lecture valide des données.
*   **Références** : liste les références utilisées pour créer le modèle de diagramme et son contenu.
*   **Contributeurs** : noms des personnes qui ont participé à la réalisation du modèle de diagramme. Il est même encouragé d'y inclure votre site personnel.

### Multilingue

Nous avons établi deux méthodes pour régler plusieurs langues :

La première consiste à utiliser l'option **Changer de langue** dans la barre de fonctions en haut de l'interface du diagramme. Elle permet de définir un contenu particulier pour la seconde langue.

La seconde méthode consiste à modifier le contenu multilingue dans la fenêtre d'édition du diagramme, comme le montre la figure ci-dessous.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/illustrated_multilingual.webp')}
    alt="Diagramme – données et script"
    style={{width: '100%'}}
  />
</div>
