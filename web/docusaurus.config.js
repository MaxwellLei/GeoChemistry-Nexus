// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import {themes as prismThemes} from 'prism-react-renderer';


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

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en', 'zh-Hans', 'zh-Hant', 'de-DE', 'ja-JP', 'ko-KR'],
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
        label: '中文',
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
    }
  },



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
              label: 'v0.7.1',
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
  plugins: [
    [
      '@docusaurus/plugin-google-gtag',
      {
        trackingID: 'G-JKS3Q36GRE',
        anonymizeIP: true,
      },
    ],
  ],
};



export default config;
