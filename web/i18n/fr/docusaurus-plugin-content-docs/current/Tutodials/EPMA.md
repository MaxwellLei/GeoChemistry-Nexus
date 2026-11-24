---
sidebar_position: 4
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Calcul minéral EPMA

**Le calcul minéral EPMA** traite les compositions minérales obtenues par microsonde électronique. Les teneurs en oxydes d'une série de points d'analyse sont converties, selon la méthode minérale choisie, en résultats de chimie minérale. Ils servent à l'identification des minéraux, à la comparaison des compositions et à l'interprétation géologique ultérieure.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/epma_mineral_calculation.webp')}
    alt="Interface EPMA"
    style={{width: '100%'}}
  />
</div>

## Interface

Le calcul minéral EPMA se divise en deux interfaces : la **bibliothèque de modèles de calcul minéral EPMA** et l'**interface de calcul EPMA**.

Dans la bibliothèque, vous pouvez rechercher, filtrer et choisir un calcul EPMA adapté. Le panneau d'explication à l'extrême droite présente les informations de base de ces modèles. Cliquez sur **Entrer dans le calcul** pour lancer le calcul.

## Exemple

Par exemple, choisissez un modèle de calcul EPMA de l'olivine, puis ouvrez l'interface de calcul. La page est divisée en une partie haute et une partie basse. Dans la barre supérieure, vous pouvez consulter la documentation détaillée du modèle ou charger des données d'exemple.

Saisissez les données demandées par le tableau, puis cliquez sur **Exécuter le calcul** dans la barre supérieure.

La zone gris-blanc est la zone de saisie et la zone bleu-violet est la zone de résultats. Vous pouvez consulter librement les résultats, ou cliquer sur **Inspecter** pour voir les étapes intermédiaires du calcul.

Le point coloré devant l'en-tête de chaque ligne indique l'état de calcul de cette ligne :

* **Gris** : la ligne contient des données, mais n'a pas encore été calculée
* **Vert** : la ligne a été calculée normalement, sans avertissement ni erreur
* **Orange** : le calcul est terminé, mais un avertissement est présent
* **Rouge** : le calcul de cette ligne a échoué
