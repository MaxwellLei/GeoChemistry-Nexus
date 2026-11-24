---
sidebar_position: 3
---

# 🛣️ Feuille de route

L'objectif de GeoChemistry Nexus est de constituer progressivement une chaîne d'outils de recherche intégrée en géochimie et en pétrologie : **données · diagrammes · calcul · collaboration**.

## ✅ Déjà réalisé


### Flux de travail principal
- [x] **Accueil** : liens rapides, widgets utiles
- [x] **Diagrammes de discrimination** : un grand nombre de diagrammes de discrimination intégrés à partir de la bibliothèque de modèles, y compris, sans s'y limiter, les diagrammes de discrimination des contextes tectoniques, les diagrammes de classification des roches, etc. ; en outre, prise en charge des diagrammes en toile d'araignée d'éléments en traces et de terres rares, avec un grand nombre de schémas de normalisation intégrés ; prise en charge du report des points sur des diagrammes de Harker en planches multiples ;
- [x] **Géothermobaromètres** : modèles en ligne de calcul géothermobarométrique pour plusieurs minéraux, et fonctions personnalisées de type Excel
- [x] **Calcul minéral par EPMA** : modèles de calcul EPMA pour différents minéraux, mis à jour en temps réel en ligne, prêts à l'emploi dès le téléchargement.
- [x] **Calcul de la norme CIPW** : calcul normatif des données d'éléments majeurs en roche totale et exportation des résultats
- [x] **Calcul de la norme de Niggli** : calcul normatif des données d'éléments majeurs en roche totale et exportation des résultats
### Capacités de la plateforme
- [x] **Empreinte d'exécution** : occupation mémoire extrêmement faible, espace d'installation extrêmement réduit ; sans le runtime `.NET`, le logiciel local n'occupe que 9 MB.
- [x] **Écosystème de modèles** : mise à jour dynamique des modèles en ligne dans le cloud ; création, importation et partage empaqueté de modèles personnalisés ; consultation du détail du calcul pour en faciliter la vérification.
- [x] **Multilingue** : localisation de l'interface (chinois / anglais / allemand / russe / japonais / coréen, etc.) ; bascule en un clic de la langue des modèles de diagrammes
- [x] **Exportation et intégration** : exportation PNG / JPG / BMP / SVG ; collaboration avec des logiciels tiers tels que CorelDRAW, Inkscape et Adobe Illustrator

## 📅 Prochains travaux
### Diagrammes de géochronologie isotopique
- [ ] Diagramme concordia U-Pb (Concordia)
- [ ] Droites isochrones Rb-Sr, Sm-Nd, Lu-Hf et U-Pb
- [ ] Diagrammes liés aux rapports isotopiques initiaux (par exemple (⁸⁷Sr/⁸⁶Sr)ᵢ vs εNd(t))
- [ ] Et d'autres encore


## 🔭 Vision à moyen et long terme
### Analyse assistée par apprentissage automatique
Introduire les démarches courantes d'apprentissage automatique, pour aider à la discrimination et à la classification des données géochimiques, et produire l'évaluation des modèles, l'importance des variables ainsi que des résultats visuels tels que les courbes ROC et les matrices de confusion — **sans que l'utilisateur ait à configurer lui-même un environnement Python**.
### Modèles de discrimination de type « nouveau diagramme »
Prendre en charge le chargement de modèles de discrimination par apprentissage automatique extensibles (par exemple de nouveaux diagrammes de discrimination entraînés sur de grands jeux de données), afin que le chercheur applique directement l'analyse par « nouveau diagramme », sans avoir à maîtriser toute la pile de développement ML.
### Assistant de recherche par IA (par étapes)
1. **Première étape** : questions-réponses par RAG, fondées sur la documentation d'aide intégrée et une base de connaissances, qui proposent des suggestions d'utilisation selon les mots-clés et la situation  
2. **Deuxième étape** : aide supplémentaire au traitement des données et au déroulement de l'analyse, afin de simplifier les opérations répétitives
### Multiplateforme et autres points
- [ ] Évaluer des solutions telles qu'Avalonia, afin de prendre progressivement en charge Linux / macOS (aujourd'hui, Windows est la plateforme principale)
- [ ] Continuer d'optimiser les performances, le format des modèles et les interfaces d'extension pour les développeurs
