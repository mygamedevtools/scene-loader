import React from "react";
import Translate from '@docusaurus/Translate';
import Link from "@docusaurus/Link";
import styles from "./styles.module.css"

export default function Installation() {
  return (
    <section>
      <div className="container">
        <h2 className="text--center"><Translate id="homepage.installation.title">Install it</Translate></h2>
        <p className="text--center"><Translate id="homepage.installation.text">Choose your preferred installation method.</Translate></p>
        <div className={styles.installationButtons}>
          <Link to="/docs/getting-started/installation#openupm" className="button button--lg margin--sm">
            OpenUPM
          </Link>
          <Link to="/docs/getting-started/installation#git" className="button button--lg margin--sm">
            Git
          </Link>
          <Link to="/docs/getting-started/installation#tarball" className="button button--lg margin--sm">
            Tarball
          </Link>
          <Link to="/docs/getting-started/installation#asset-store" className="button button--lg margin--sm">
            Asset Store
          </Link>
        </div>
      </div>
    </section>
  );
};