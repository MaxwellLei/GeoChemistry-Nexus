export const LOCALE_LIST = [
  { locale: 'zh-Hans', label: '简体中文', badge: 'zh-Hans' },
  { locale: 'zh-Hant', label: '繁體中文', badge: 'zh-Hant' },
  { locale: 'en', label: 'English', badge: 'en-US' },
  { locale: 'de-DE', label: 'Deutsch', badge: 'de-DE' },
  { locale: 'ja-JP', label: '日本語', badge: 'ja-JP' },
  { locale: 'ko-KR', label: '한국어', badge: 'ko-KR' },
  { locale: 'es-ES', label: 'Español', badge: 'es-ES' },
  { locale: 'ru-RU', label: 'Русский', badge: 'ru-RU' },
  { locale: 'fr-FR', label: 'Français', badge: 'fr-FR' },
  { locale: 'pt-BR', label: 'Português (Brasil)', badge: 'pt-BR' },
];

export const LOCALE_TO_TAS_KEY = {
  'zh-Hans': 'zhCN',
  'zh-Hant': 'zhTW',
  en: 'en',
  'de-DE': 'de',
  'ja-JP': 'ja',
  'ko-KR': 'ko',
  'es-ES': 'es',
  'ru-RU': 'ru',
  'fr-FR': 'fr',
  'pt-BR': 'pt',
};

export const TAS_LANG_BUTTONS = [
  { key: 'zhCN', label: '简体中文' },
  { key: 'zhTW', label: '繁體中文' },
  { key: 'en', label: 'English' },
  { key: 'de', label: 'Deutsch' },
  { key: 'ja', label: '日本語' },
  { key: 'ko', label: '한국어' },
  { key: 'ru', label: 'Русский' },
  { key: 'es', label: 'Español' },
  { key: 'fr', label: 'Français' },
  { key: 'pt', label: 'Português' },
];

export const TAS_MULTILINGUAL = {
  zhCN: { code: '当前词条：zh-Hans · 简体中文', l1: '玄武岩', l2: '安山岩', l3: '英安岩', l4: '流纹岩', l5: '粗面岩', l6: '响岩', lleg: '弧火山岩样品', lx: 'SiO₂ (质量分数 wt.%)', ly: 'Na₂O + K₂O (wt.%)' },
  zhTW: { code: '當前詞條：zh-Hant · 繁體中文', l1: '玄武岩', l2: '安山岩', l3: '英安岩', l4: '流紋岩', l5: '粗面岩', l6: '響岩', lleg: '弧火山岩樣品', lx: 'SiO₂ (質量分數 wt.%)', ly: 'Na₂O + K₂O (wt.%)' },
  en: { code: 'Active Locale: en-US · English', l1: 'Basalt', l2: 'Andesite', l3: 'Dacite', l4: 'Rhyolite', l5: 'Trachyte', l6: 'Phonolite', lleg: 'Arc Volcanics', lx: 'SiO₂ (wt.%) — Le Maitre (2002)', ly: 'Na₂O + K₂O (wt.%)' },
  de: { code: 'Aktive Sprache: de-DE · Deutsch', l1: 'Basalt', l2: 'Andesit', l3: 'Dazit', l4: 'Rhyolith', l5: 'Trachyt', l6: 'Phonolith', lleg: 'Bogen-Vulkanite', lx: 'SiO₂ (Gew.-%)', ly: 'Na₂O + K₂O (Gew.-%)' },
  ja: { code: '表示言語：ja-JP · 日本語', l1: '玄武岩', l2: '安山岩', l3: 'デイサイト', l4: '流紋岩', l5: '粗面岩', l6: '響岩', lleg: '島弧火山岩試料', lx: 'SiO₂ (重量% wt.%)', ly: 'Na₂O + K₂O (wt.%)' },
  ko: { code: '현재 언어: ko-KR · 한국어', l1: '현무암', l2: '안산암', l3: '데사이트', l4: '유문암', l5: '조면암', l6: '향암', lleg: '호상화산암 시료', lx: 'SiO₂ (중량% wt.%)', ly: 'Na₂O + K₂O (wt.%)' },
  ru: { code: 'Язык диаграммы: ru-RU · Русский', l1: 'Базальт', l2: 'Андезит', l3: 'Дацит', l4: 'Риолит', l5: 'Трахит', l6: 'Фонолит', lleg: 'Вулканиты дуги', lx: 'SiO₂ (мас. %)', ly: 'Na₂O + K₂O (мас. %)' },
  es: { code: 'Idioma activo: es-ES · Español', l1: 'Basalto', l2: 'Andesita', l3: 'Dacita', l4: 'Riolita', l5: 'Traquita', l6: 'Fonolita', lleg: 'Volcánicas de arco', lx: 'SiO₂ (% en peso)', ly: 'Na₂O + K₂O (% en peso)' },
  fr: { code: 'Langue active : fr-FR · Français', l1: 'Basalte', l2: 'Andésite', l3: 'Dacite', l4: 'Rhyolite', l5: 'Trachyte', l6: 'Phonolite', lleg: 'Série d’arc', lx: 'SiO₂ (% pds) — Le Maitre (2002)', ly: 'Na₂O + K₂O (% pds)' },
  pt: { code: 'Idioma ativo: pt-BR · Português', l1: 'Basalto', l2: 'Andesito', l3: 'Dacito', l4: 'Riolito', l5: 'Traquito', l6: 'Fonolito', lleg: 'Vulcânicas de arco', lx: 'SiO₂ (% em peso)', ly: 'Na₂O + K₂O (% em peso)' },
};

export const MARQUEE_ROW_1 = [
  { key: 'zhCN', badge: 'zh-Hans', native: '简体中文 · 火成岩分类与构造判别', terms: '玄武岩 · 安山岩 · 流纹岩' },
  { key: 'en', badge: 'en-US', native: 'English · TAS & Tectonic Setting', terms: 'Basalt · Andesite · Rhyolite' },
  { key: 'de', badge: 'de-DE', native: 'Deutsch · TAS-Klassifikation & Geotektonik', terms: 'Basalt · Andesit · Rhyolith' },
  { key: 'ja', badge: 'ja-JP', native: '日本語 · 火成岩 TAS 分類・構造環境判別', terms: '玄武岩 · 安山岩 · 流紋岩' },
  { key: 'fr', badge: 'fr-FR', native: 'Français · Classification TAS & Géodynamique', terms: 'Basalte · Andésite · Rhyolite' },
  { key: 'ru', badge: 'ru-RU', native: 'Русский · Классификация TAS и геодинамика', terms: 'Базальт · Андезит · Риолит' },
  { key: 'es', badge: 'es-ES', native: 'Español · Clasificación TAS y Tectónica', terms: 'Basalto · Andesita · Riolita' },
  { key: 'ko', badge: 'ko-KR', native: '한국어 · 화성암 TAS 분류 및 지구조 판별', terms: '현무암 · 안산암 · 유문암' },
  { key: 'pt', badge: 'pt-BR', native: 'Português · Classificação TAS e Tectônica', terms: 'Basalto · Andesito · Riolito' },
  { key: 'zhTW', badge: 'zh-Hant', native: '繁體中文 · 火成岩分類與構造環境判別', terms: '玄武岩 · 安山岩 · 流紋岩' },
];

export const MARQUEE_ROW_2 = [
  { key: 'en', badge: 'en-US', native: 'Geothermobarometry & CIPW Norm', terms: 'Olivine · Zircon · Biotite' },
  { key: 'zhCN', badge: 'zh-Hans', native: '地质温压计与电子探针晶体化学式', terms: '橄榄石 · 锆石 · 黑云母' },
  { key: 'ja', badge: 'ja-JP', native: '地質温度圧力計・EPMA 鉱物構造式', terms: 'カンラン石 · ジルコン · 黒雲母' },
  { key: 'de', badge: 'de-DE', native: 'Geothermobarometrie & Normminerale', terms: 'Olivin · Zirkon · Biotit' },
  { key: 'ru', badge: 'ru-RU', native: 'Геотермобарометрия и формулы EPMA', terms: 'Оливин · Циркон · Биотит' },
  { key: 'fr', badge: 'fr-FR', native: 'Géothermobarométrie & Norme CIPW', terms: 'Olivine · Zircon · Biotite' },
  { key: 'ko', badge: 'ko-KR', native: '지질온도압력계 및 EPMA 광물 화학식', terms: '감람석 · 저어콘 · 흑운모' },
  { key: 'es', badge: 'es-ES', native: 'Geotermobarometría y Norma CIPW', terms: 'Olivino · Circón · Biotita' },
  { key: 'pt', badge: 'pt-BR', native: 'Geotermobarometria e Minerais EPMA', terms: 'Olivina · Zircão · Biotita' },
  { key: 'zhTW', badge: 'zh-Hant', native: '地質溫壓計與標準礦物白盒解算', terms: '橄欖石 · 鋯石 · 黑雲母' },
];
