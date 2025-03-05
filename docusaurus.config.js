// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import { themes as prismThemes } from 'prism-react-renderer';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'My Scene Manager',
  tagline: 'Enhance your scene loading experience',
  favicon: 'img/favicon.ico',

  // Set the production url of your site here
  url: 'https://scene-loader.mygamedevtools.com',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'mygamedevtools', // Usually your GitHub org/user name.
  projectName: 'scene-loader', // Usually your repo name.
  deploymentBranch: 'gh-pages',
  trailingSlash: false,

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en', 'pt-BR'],
  },

  markdown: {
    mermaid: true
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: './sidebars.js',
          editUrl:
            'https://github.com/mygamedevtools/scene-loader/tree/docs/',
          versions:
          {
            current: {
              label: '4.0.0 🚧',
              banner: 'unreleased'
            },
          }
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themes: ['@docusaurus/theme-mermaid'],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      image: 'img/social-card.jpg',
      navbar: {
        title: 'My Scene Manager',
        logo: {
          alt: 'My GameDev Tools Logo',
          src: 'img/logo.svg',
        },
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'documentationSidebar',
            position: 'left',
            label: 'Docs',
          },
          {
            type: 'docsVersionDropdown',
            position: 'right'
          },
          {
            type: 'localeDropdown',
            position: 'right'
          },
          {
            href: 'https://github.com/mygamedevtools/scene-loader',
            position: 'right',
            className: 'header-github-link',
            'aria-label': 'GitHub Repository',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Docs',
            items: [
              {
                label: 'Welcome',
                to: '/docs/intro',
              },
              {
                label: 'Installation',
                to: '/docs/getting-started/installation'
              },
              {
                label: 'Basic Usage',
                to: '/docs/getting-started/basic-usage'
              },
            ],
          },
          {
            title: 'Upgrade',
            items: [
              {
                label: 'From 3.x to 4.x',
                to: 'docs/next/upgrades/from-3-to-4'
              },
              {
                label: 'From 2.x to 3.x',
                to: 'docs/upgrades/from-2-to-3'
              },
            ],
          },
          {
            title: 'Support',
            items: [
              {
                label: 'Troubleshooting',
                to: '/docs/troubleshooting'
              },
              {
                label: 'Report an Issue',
                href: 'https://github.com/mygamedevtools/scene-loader/issues/new',
              },
            ],
          },
        ],
        logo: {
          alt: "My GameDev Tools Logo",
          src: '/img/logo_full.svg',
          href: "https://github.com/mygamedevtools",
          height: 54
        },
        copyright: `Copyright © ${new Date().getFullYear()} My GameDev Tools. Built with Docusaurus.`,
      },
      prism: {
        theme: prismThemes.oneLight,
        darkTheme: prismThemes.oneDark,
        additionalLanguages: ['csharp', 'bash', 'diff']
      },
      colorMode: {
        defaultMode: "dark",
        respectPrefersColorScheme: true
      },
      mermaid: {
        theme: { light: "default", dark: "dark" }
      }
    }),
};

export default config;
