# My Scene Manager Documentation

This repository contains the documentation for **My Scene Manager**, a Unity package designed to streamline scene transitions, async operations, and addressable scene management.
This website is built using [Docusaurus](https://docusaurus.io/), a modern static website generator.

## Prerequisites

To run and build this documentation site locally, you need to have **Node.js 18+** installed.

You can download the latest version of Node.js from [here](https://nodejs.org/en/download/).

## Running the Website Locally

1. Clone the repository (if you haven’t already).
2. Install the necessary dependencies:
  ```bash
  npm install
  ```
3. To run the site locally:
  ```bash
  npx docusaurus start
  ```
  This will start a local development server at `http://localhost:3000`.
4. To run the site locally in a different locale (e.g., Português Brasileiro):
  ```bash
  npm run start -- --locale pt-BR
  ```
  This will start the site with the specified locale.

## Building the Website Locally

If you want to validate the site build before pushing, run the following command:

```bash
npm run build
```

This will generate the static files in the `build` directory.

## Deployment

The deployment of the website is automated through a **GitHub Actions** workflow. Every time changes are pushed to the `docs` branch, GitHub Actions will automatically build and deploy the updated documentation to **GitHub Pages**.

You don't need to manually deploy the site, as the process is handled by the GitHub Actions pipeline.

## Contributing

Feel free to contribute by opening issues or submitting pull requests with updates to the documentation.