import {EN} from './en';

function createLocaleFromEn(overrides) {
  return {
    ...EN,
    ...overrides,
    nav: { ...EN.nav, ...(overrides.nav || {}) },
    hero: { ...EN.hero, ...(overrides.hero || {}) },
    metrics: overrides.metrics || EN.metrics,
    features: { ...EN.features, ...(overrides.features || {}) },
    toolkit: { ...EN.toolkit, ...(overrides.toolkit || {}) },
    roadmap: { ...EN.roadmap, ...(overrides.roadmap || {}) },
    cta: { ...EN.cta, ...(overrides.cta || {}) },
    footer: { ...EN.footer, ...(overrides.footer || {}) },
  };
}

export default createLocaleFromEn;
