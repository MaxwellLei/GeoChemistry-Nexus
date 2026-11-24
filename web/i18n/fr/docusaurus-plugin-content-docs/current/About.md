---
sidebar_position: 6
---

# ℹ️ À propos

:::info

**« Je ne trouvais pas la voiture de sport de mes rêves, alors je l'ai construite moi-même. » — Ferdinand Porsche**

:::

**GeoChemistry Nexus** est le premier projet que je prévois de maintenir sur le très long terme : je compte continuer à le mettre à jour pendant au moins les cinq prochaines années.

Fait curieux, il s'appelait à l'origine **Geo-Thermometer**. Au début, je voulais seulement développer un programme de base consacré aux géothermomètres, afin de faciliter le calcul et le tracé. Cependant, à mesure que le projet avançait, et surtout une fois le module de diagrammes terminé, j'ai soudain compris qu'un objectif bien plus vaste était à portée de main. Le projet s'est donc élargi, dans son concept comme dans ses fonctions, et Geo-Thermometer est devenu GeoChemistry Nexus.

## Pourquoi ce nom

**GeoChemistry** désigne la géochimie ; **Nexus** signifie point de connexion, pivot ou centre de convergence ;

Nous souhaitons réunir, sur une seule plateforme, des méthodes de calcul géochimique dispersées, des outils de traitement des données, l'analyse minéralochimique, les diagrammes géologiques, et le reste.

## Pourquoi il faut GeoChemistry Nexus

En géochimie et en pétrologie, le **tracé des figures** et le **calcul** sont souvent dispersés entre plusieurs outils : la mise en forme des données se fait dans un tableur, le dessin du fond de figure dans un outil vectoriel généraliste, et le calcul des géothermobaromètres exige encore d'ouvrir un script ou un ancien programme. La rupture du flux de travail, l'absence d'uniformité des formats et le coût élevé de la reproductibilité sont des frictions que beaucoup de chercheurs rencontrent au quotidien.

D'autre part, de nombreux diagrammes de discrimination, géothermobaromètres et outils de calcul d'usage courant **manquent depuis longtemps de mises à jour suivies** : les algorithmes nouvellement publiés et les révisions des modèles classiques peinent à être intégrés à temps, et les chercheurs en sont souvent réduits à conserver d'anciennes versions, à l'arrêt depuis des années. Le problème est encore plus concret : lorsqu'un chercheur **développe lui-même** un nouvel algorithme ou un nouveau diagramme, il **manque souvent d'un canal de diffusion commode et ouvert**. Les résultats restent dans une annexe d'article, un script personnel ou un partage restreint, et peinent à se diffuser et à être réutilisés rapidement dans la même spécialité.

![plot_home_ui](/img/about-illustrations/02-research-tool-pain-points.png)

J'ai aussi observé, dans la recherche, certaines pratiques insuffisamment rigoureuses. Par exemple, certaines personnes esquissent grossièrement les formes avec CorelDRAW lorsqu'elles dessinent le fond d'une carte géologique. L'écart visuel est faible, mais, du point de vue de la recherche scientifique, cette pratique va à l'encontre de l'esprit de rigueur scientifique.

C'est pourquoi je me suis demandé : **pourquoi ne pas développer moi-même un outil ?**

Un outil capable d'intégrer un grand nombre de modèles de diagrammes géologiques et géochimiques, de prendre en charge divers calculs de géothermomètres et de géobaromètres, et de s'étendre aux calculs géochimiques ainsi qu'aux fonctions d'apprentissage automatique aujourd'hui en vogue. S'il n'existe pas de modèle tout prêt, il fournit des outils de dessin visuels pour faciliter la création et le partage. Avec une interface moderne et la prise en charge de plusieurs langues, il permet aux chercheurs d'accomplir, dans un seul logiciel, l'essentiel du travail de base de dessin scientifique et de calcul.

**GeoChemistry Nexus** vise à rassembler cette chaîne en un seul endroit : de l'importation des données d'échantillons, du prétraitement et des corrections, jusqu'au tracé des diagrammes de discrimination, au calcul des géothermomètres et au calcul de la norme CIPW, puis à l'exportation aux formats PNG / SVG, autant que possible dans une même interface. En même temps, grâce à l'**écosystème de modèles dans le cloud** et au **mécanisme de collaboration communautaire**, les diagrammes et les algorithmes peuvent être itérés en continu et parvenir rapidement à un plus grand nombre de chercheurs.

## L'idée de départ du développement

Il se trouve que je connais des langages de programmation tels que C# et Python, et que je maîtrise les techniques de développement de logiciels de bureau. C'est pourquoi, en décembre 2024, j'ai commencé à développer la démonstration. Lors du premier choix technique, je me suis sérieusement demandé : **quel langage et quelle architecture permettraient le mieux d'atteindre cet objectif ?**

Ma première idée a été Python. Il me permettait de prototyper rapidement, ce qui est une bonne nouvelle pour le développeur. Du point de vue de l'utilisateur, cependant, il a des défauts rédhibitoires. D'abord, l'efficacité d'exécution est faible (l'effet reste limité lorsque le volume de données est petit, mais l'écart de performance demeure). Ensuite, il entraîne un programme d'installation considérable. J'ai autrefois réécrit en Python un programme simple d'abord écrit en WPF. Même empaqueté dans un environnement virtuel entièrement neuf, le produit fini pesait plusieurs centaines de MB, alors que le programme empaqueté avec .NET n'en faisait qu'une dizaine : l'écart est immense. **Obliger l'utilisateur, pour une fonction simple, à supporter une installation de plusieurs centaines de MB, voire de plusieurs GB, et une configuration complexe, n'est pas le meilleur choix**.

J'ai ensuite envisagé le développement web. Les avantages sont évidents : aucune installation, utilisation immédiate. Le problème tient au coût élevé et durable d'un serveur, qui dépassait mes capacités personnelles de maintenance. J'ai donc finalement recentré l'effort sur le développement du client.

![plot_home_ui](/img/about-illustrations/03-technology-choice-path.png)

Le développement client offre de nombreuses solutions techniques, mais, par rapport à QT fondé sur C++, je connais mieux .NET. En réunissant la forte efficacité d'exécution de .NET et la large diffusion de Windows, j'ai finalement choisi .NET comme principale plateforme de développement. Au début, j'avais aussi envisagé Avalonia pour prendre en charge Windows, Linux et MacOS. Au cours du développement, j'ai constaté que certaines dépendances du projet étaient incompatibles avec Avalonia, ce qui rendait le coût de migration très élevé. Avec mes seules forces, il était difficile de réécrire ces modules à un stade précoce. C'est pourquoi, du moins à ce stade, mon travail reste concentré sur la plateforme Windows. Une version multiplateforme pourra être réalisée un jour.

## Me contacter

Si le développement logiciel vous intéresse et que vous souhaitez collaborer avec moi, ou si vous voulez proposer des suggestions et des retours, des modèles de diagrammes, un appui sur les algorithmes, voire une coopération commerciale, n'hésitez pas à me contacter à l'adresse **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)**.

*Si vous constatez une insuffisance ou un problème en utilisant le logiciel, n'hésitez pas à en faire part. Les fonctions du logiciel se multiplient et se complexifient, les tests peinent à couvrir tous les cas, et des oublis ou des inexactitudes peuvent subsister. Je ferai de mon mieux pour corriger et optimiser le logiciel, afin de vous offrir une expérience plus stable et plus fiable.*

![plot_home_ui](/img/about-illustrations/04-long-term-maintenance.png)
