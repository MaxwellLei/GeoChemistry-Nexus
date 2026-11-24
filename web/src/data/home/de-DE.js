import createLocaleFromEn from './createLocaleFromEn';

const DE = createLocaleFromEn({
  metaTitle: 'Integrierte Forschungs-Workbench für Geochemie & Wissenschaftliche Diagramme',
  nav: { stage: 'Arbeitsbereich', features: 'Kernfunktionen', toolkit: 'Portal & Tools', roadmap: 'Roadmap', docs: 'Doku ↗', docsMobile: 'Dokumentation', download: 'Download', downloadMobile: 'Download v0.9.0' },
  hero: {
    eyebrow: 'v0.9.0 veröffentlicht: Neue Module für EPMA-Mineralchemie, CIPW- & Niggli-Norm sowie wachsende Cloud-Vorlagen →',
    titleLine1: 'Quantitative Geochemie & Diagrammerstellung in einer',
    titleLine2: 'Integrierten Forschungs-Workbench der nächsten Generation',
    lead: 'Entwickelt für Geochemie, Petrologie, Mineralogie und Lagerstättenkunde. Von der Datenaufbereitung und White-Box-Berechnung bis zum mehrsprachigen publikationsreifen SVG-Export in einer einzigen klaren Arbeitsumgebung.',
    primaryBtn: 'Für Windows herunterladen',
    secondaryBtn: '3-Minuten-Schnellstart',
    tutorialLink: 'Modul-Anleitung ansehen →',
  },
  metrics: [
    { val: '50', suffix: '+', title: 'Klassifikations- & Tektonik-Plots', lbl: 'Kontinuierlich per Cloud erweitert' },
    { val: '20', suffix: '+', title: 'Geothermobarometrie & EPMA', lbl: 'Live-Updates ohne Neuinstallation' },
    { val: '60', suffix: '', title: 'CIPW- & Niggli-Normminerale', lbl: '10⁻⁶ Massenbilanz-Prüfung' },
    { val: '10', suffix: '+', title: 'Sprachen für UI & Diagramme', lbl: 'Verlustfreier Sprachwechsel per Klick' },
  ],
  features: {
    title: 'Präzision für wissenschaftliche Publikationen und Reproduzierbarkeit',
    sub: 'Systematisch entwickelt für internationale Fachzeitschriften, White-Box-Prüfung und offene Erweiterbarkeit.',
    tasTitle: 'Ein Diagramm, 10+ Sprachen: Sofortiger Wechsel der Beschriftungen',
    svgTitle: 'Publikationsreifes SVG & Direktanbindung an Vektorprogramme',
    svgBridgeTag: 'SVG Direkt',
    card3Title: 'White-Box-Zeileninspektor',
    card4Title: 'Excel-ähnliche Bedienung & Freie Formeln',
    card5Title: 'Offenes Cloud-Ökosystem & Team-Sharing',
  },
  toolkit: {
    title: 'Integriertes Geowissenschafts-Portal & Mikro-Werkzeuge',
    docBtn: 'Portal & Werkzeuge entdecken →',
  },
  roadmap: {
    title: 'Mehr als ein Plot-Programm: Ein wachsendes digitales Fundament',
  },
  cta: {
    title: 'Erleben Sie einen durchgängigen Forschungs-Workflow oder gestalten Sie mit',
    primaryBtn: 'Neueste Windows-Version laden (v0.9.0)',
    secondaryBtn: 'Einladung zur Zusammenarbeit lesen →',
    copiedText: '✓ In die Zwischenablage kopiert',
  },
});

export default DE;
