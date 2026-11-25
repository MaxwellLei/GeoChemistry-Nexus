// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import {themes as prismThemes} from 'prism-react-renderer';
import fs from 'node:fs';
import path from 'node:path';

/**
 * Per-locale homepage copy.
 * Docusaurus builds each locale separately, so we alias `@gn/homeCopy` to the
 * current locale's file only. The other 9 languages never reach the browser.
 * @type {import('@docusaurus/types').PluginModule}
 */
function homeCopyAliasPlugin(context) {
  const locale = context.i18n.currentLocale;
  const dir = path.join(context.siteDir, 'src', 'data', 'home');
  const target = fs.existsSync(path.join(dir, `${locale}.js`))
    ? path.join(dir, `${locale}.js`)
    : path.join(dir, 'en.js');
  return {
    name: 'gn-home-copy-alias',
    configureWebpack() {
      return {resolve: {alias: {'@gn/homeCopy': target}}};
    },
  };
}

const GA_ID = 'G-XKH32ZMLQP';

/**
 * Deferred Google Analytics + Cloudflare Web Analytics (no third-party script blocks hydration).
 * The official gtag plugin puts preconnect + <script async> in <head>, which
 * stalls first paint where Google is unreachable (e.g. mainland China).
 * Here <head> only gets a tiny stub that queues gtag() calls; the real gtag.js
 * is injected after window "load" + browser idle time (max ~4 s later).
 * @type {import('@docusaurus/types').PluginModule}
 */
function deferredGtagPlugin(context) {
  if (process.env.NODE_ENV !== 'production') return {name: 'gn-deferred-gtag'};
  const loader = `
window.dataLayer=window.dataLayer||[];
function gtag(){dataLayer.push(arguments);}
window.gtag=gtag;
gtag('js',new Date());
gtag('config','${GA_ID}');
(function(){
  var loaded=false;
  function load(){
    if(loaded)return; loaded=true;
    var s=document.createElement('script');
    s.async=true;
    s.src='https://www.googletagmanager.com/gtag/js?id=${GA_ID}';
    document.head.appendChild(s);
    var b=document.createElement('script');
    b.defer=true;
    b.src='https://static.cloudflareinsights.com/beacon.min.js';
    b.setAttribute('data-cf-beacon','{"token": "c8b9a5804d4d43d4bcb7bd59266e7eab"}');
    document.head.appendChild(b);
  }
  function schedule(){
    if('requestIdleCallback' in window){requestIdleCallback(load,{timeout:4000});}
    else{setTimeout(load,2500);}
  }
  if(document.readyState==='complete'){schedule();}
  else{window.addEventListener('load',schedule,{once:true});}
})();`;
  return {
    name: 'gn-deferred-gtag',
    getClientModules() {
      return [path.join(context.siteDir, 'src', 'clientModules', 'gtagRoute.js')];
    },
    injectHtmlTags() {
      return {headTags: [{tagName: 'script', innerHTML: loader}]};
    },
  };
}

/**
 * First-visit language redirect (browser language based).
 * Runs only on the site root ("/"), which is the English default locale.
 *  - same-origin referrer (user is navigating inside the site)  -> never redirect
 *  - localStorage "gn-locale-pref" (set by the homepage language menu) -> honour it
 *  - otherwise map navigator.languages (first entry that is supported) to a locale
 *  - English as first preference -> stay
 * Explicit locale URLs such as /zh-Hans/ are never touched.
 * @type {import('@docusaurus/types').PluginModule}
 */
function langRedirectPlugin(context) {
  // Only the default-locale build (served at "/") needs this; other locales have baseUrl "/<locale>/".
  if (context.i18n.currentLocale !== context.i18n.defaultLocale) {
    return {name: 'gn-lang-redirect'};
  }
  const root = '/';
  const script = `
(function(){try{
  var p=location.pathname;
  if(p!=='${root}'&&p!=='${root}index.html')return;
  var r=document.referrer;
  if(r){try{if(new URL(r).origin===location.origin)return;}catch(e){}}
  if(sessionStorage.getItem('gn-lang-redirected'))return;
  var ok={'zh-Hans':1,'zh-Hant':1,'de-DE':1,'ja-JP':1,'ko-KR':1,'es-ES':1,'ru-RU':1,'fr-FR':1,'pt-BR':1};
  var target=null,pref=null;
  try{pref=localStorage.getItem('gn-locale-pref');}catch(e){}
  if(pref){ if(ok[pref])target=pref; }
  else{
    var m={de:'de-DE',ja:'ja-JP',ko:'ko-KR',es:'es-ES',ru:'ru-RU',fr:'fr-FR',pt:'pt-BR'};
    var langs=(navigator.languages&&navigator.languages.length)?navigator.languages:[navigator.language||''];
    for(var i=0;i<langs.length;i++){
      var l=String(langs[i]).toLowerCase().split(';')[0].replace('_','-'),b=l.split('-')[0];
      if(b==='en')break;
      if(b==='zh'){target=/(^|-)(tw|hk|mo|hant)(-|$)/.test(l)?'zh-Hant':'zh-Hans';break;}
      if(m[b]){target=m[b];break;}
    }
  }
  if(!target)return;
  sessionStorage.setItem('gn-lang-redirected','1');
  location.replace('${root}'+target+'/'+location.search+location.hash);
}catch(e){}})();`;
  return {
    name: 'gn-lang-redirect',
    injectHtmlTags() {
      return {headTags: [{tagName: 'script', innerHTML: script}]};
    },
  };
}

/**
 * Homepage screenshots on object storage (COS), chosen per locale:
 *   zh-Hans      -> Beijing bucket   (mainland China users)
 *   all others   -> Hong Kong bucket
 * Only images live there: <base>/img/v0.9.0/en-US/...  (src/pages/index.js reads customFields.cdnBases).
 * JS / CSS are served by the site itself (Cloudflare Pages), as usual.
 * Set env GN_CDN_OFF=1 to serve the images from the site itself as well.
 */
const CDN_BASES = {
  'zh-Hans': 'https://geochemistrynexus-cn-1303234197.cos.ap-beijing.myqcloud.com/Web',
  default: 'https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/Web',
};
const CDN_ENABLED = !process.env.GN_CDN_OFF;
// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'GeoChemistry Nexus',
  tagline: 'Next-Gen Geochemistry & Petrology Discrimination Diagrams and Calculation Tool — A Geoscientist\'s Best Friend',
  favicon: 'img/logo.ico',

  // Set the production url of your site here
  url: 'https://geochemistry-nexus.pages.dev',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'MaxwellLei',
  projectName: 'GeoChemistry-Nexus',

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Cloudflare Web Analytics 已移入 deferredGtagPlugin 一并延迟加载：
  // 放在 <head> 的 defer 脚本会排在 main.js 之前执行，国内一旦卡住就会阻塞 React 激活（点击无响应）。

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en', 'zh-Hans', 'zh-Hant', 'de-DE', 'ja-JP', 'ko-KR', 'es-ES', 'ru-RU', 'fr-FR', 'pt-BR'],
    path: 'i18n',
    localeConfigs: {
      en: {
        label: 'English',
        direction: 'ltr',
        htmlLang: 'en-US',
        calendar: 'gregory',
        path: 'en',
      },
      'zh-Hans': {
        label: '简体中文',
        direction: 'ltr',
        htmlLang: 'zh-cn',
        calendar: 'gregory',
        path: 'zh',
      },
      'zh-Hant': {
        label: '繁體中文',
        direction: 'ltr',
        htmlLang: 'zh-tw',
        calendar: 'gregory',
        path: 'zh-Hant',
      },
      'de-DE': {
        label: 'Deutsch',
        direction: 'ltr',
        htmlLang: 'de-DE',
        calendar: 'gregory',
        path: 'de',
      },
      'ja-JP': {
        label: '日本語',
        direction: 'ltr',
        htmlLang: 'ja-JP',
        calendar: 'gregory',
        path: 'ja',
      },
      'ko-KR': {
        label: '한국어',
        direction: 'ltr',
        htmlLang: 'ko-KR',
        calendar: 'gregory',
        path: 'ko',
      },
      'es-ES': {
        label: 'Español',
        direction: 'ltr',
        htmlLang: 'es-ES',
        calendar: 'gregory',
        path: 'es',
      },
      'ru-RU': {
        label: 'Русский',
        direction: 'ltr',
        htmlLang: 'ru-RU',
        calendar: 'gregory',
        path: 'ru',
      },
      'fr-FR': {
        label: 'Français',
        direction: 'ltr',
        htmlLang: 'fr-FR',
        calendar: 'gregory',
        path: 'fr',
      },
      'pt-BR': {
        label: 'Português (Brasil)',
        direction: 'ltr',
        htmlLang: 'pt-BR',
        calendar: 'gregory',
        path: 'pt-BR',
      },
    }
  },



  plugins: [homeCopyAliasPlugin, deferredGtagPlugin, langRedirectPlugin],

  // Empty string => assets are served from this site itself (default).
  customFields: {cdnBases: CDN_ENABLED ? CDN_BASES : {}},

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: './sidebars.js',
          editUrl: 'https://github.com/MaxwellLei/GeoChemistry-Nexus/edit/main/web/',
          editLocalizedFiles: true,
          lastVersion: 'current',
          versions: {
            current: {
              label: 'v0.9.0',
            },
            '0.7.1': {
              label: 'v0.7.1',
              path: '0.7.1',
            },
            '0.6.1': {
              label: 'v0.6.1',
              path: '0.6.1',
            },
            '0.6.0': {
              label: 'v0.6.0',
              path: '0.6.0',
            },
            '0.5.0.0': {
              label: 'v0.5.0.0',
              path: '0.5.0.0',
            },
            '0.3.1.2': {
              label: 'v0.3.1.2',
              path: '0.3.1.2',
            },
          },
        },
        blog: {
          showReadingTime: true,
          feedOptions: {
            type: ['rss', 'atom'],
            xslt: true,
          },
          editUrl: 'https://github.com/MaxwellLei/GeoChemistry-Nexus/edit/main/web/',
          // Useful options to enforce blogging best practices
          onInlineTags: 'warn',
          onInlineAuthors: 'warn',
          onUntruncatedBlogPosts: 'warn',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
        // Google Analytics 4 (数据流: geochemistry-nexus.pages.dev)
        // 已改为 deferredGtagPlugin 延迟加载，不再使用内置 gtag 预设（会在 <head> 同步预连接 Google）。
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      // Replace with your project's social card
      image: 'img/docusaurus-social-card.jpg',
      navbar: {
        title: 'GeoChemistry Nexus',
        logo: {
          alt: 'My Site Logo',
          src: 'img/logo.ico',
        },
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'tutorialSidebar',
            position: 'left',
            label: 'Tutorial',
          },
          // {to: '/blog', label: 'Blog', position: 'left'},
          {
            type: 'docsVersionDropdown', // 添加版本下拉菜单
            position: 'right',           // 你可以根据喜好调整位置, 'left' 或 'right'
          },
          {
            href: 'https://github.com/MaxwellLei/GeoChemistry-Nexus',
            label: 'GitHub',
            position: 'right',
          },
          {
            type: 'localeDropdown',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
        //   {
        //     title: 'Docs',
        //     items: [
        //       {
        //         label: 'Tutorial',
        //         to: '/docs/intro',
        //       },
        //     ],
        //   },
        //   // {
        //   //   title: 'Community',
        //   //   items: [
        //   //     {
        //   //       label: 'Stack Overflow',
        //   //       href: 'https://stackoverflow.com/questions/tagged/docusaurus',
        //   //     },
        //   //     {
        //   //       label: 'Discord',
        //   //       href: 'https://discordapp.com/invite/docusaurus',
        //   //     },
        //   //     {
        //   //       label: 'X',
        //   //       href: 'https://x.com/docusaurus',
        //   //     },
        //   //   ],
        //   // },
        //   {
        //     title: 'More',
        //     items: [
        //       {
        //         label: 'Blog',
        //         to: '/blog',
        //       },
        //       {
        //         label: 'GitHub',
        //         href: 'https://github.com/facebook/docusaurus',
        //       },
        //     ],
        //   },
        ],
        copyright: 'Copyright © 2024-2026 GeoChemistry Nexus',
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
      },
      tableOfContents: {
        minHeadingLevel: 2,
        maxHeadingLevel: 4,
      },
    }),
};



export default config;
