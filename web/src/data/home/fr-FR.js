import createLocaleFromEn from './createLocaleFromEn';

const FR = createLocaleFromEn({
  metaTitle: 'Plateforme Intégrée de Calcul Géochimique et de Diagrammes Scientifiques',
  nav: { stage: 'Atelier', features: 'Fonctionnalités', toolkit: 'Portail & Outils', roadmap: 'Feuille de route', docs: 'Documentation ↗', docsMobile: 'Documentation', download: 'Télécharger', downloadMobile: 'Télécharger v0.9.0' },
  hero: {
    eyebrow: 'v0.9.0 disponible : ajout de la chimie minérale EPMA, des normes CIPW & Niggli et de nouveaux modèles cloud →',
    titleLine1: 'Relier calcul géochimique quantitatif et tracé scientifique dans un',
    titleLine2: 'Atelier de Recherche Intégré Nouvelle Génération',
    lead: 'Conçu pour la géochimie, la pétrologie, la minéralogie et la métallogénie. Du calcul boîte blanche à l’export vectoriel SVG multilingue prêt pour les revues internationales.',
    primaryBtn: 'Télécharger pour Windows',
    secondaryBtn: 'Guide de démarrage en 3 minutes',
    tutorialLink: 'Voir le tutoriel du module →',
  },
  features: {
    title: 'Conçu pour la rigueur éditoriale scientifique et la reproductibilité',
    tasTitle: 'Un diagramme, 10+ langues : bascule instantanée des légendes',
    svgTitle: 'Export SVG éditorial et liaison directe vers les logiciels vectoriels',
    svgBridgeTag: 'SVG Direct',
  },
  cta: {
    title: 'Adoptez un flux de recherche fluide ou co-construisez l’écosystème géoscientifique',
    primaryBtn: 'Télécharger la dernière version Windows (v0.9.0)',
    secondaryBtn: 'Lire la lettre d’invitation à collaborer →',
    copiedText: '✓ Copié dans le presse-papiers',
  },
});

export default FR;
