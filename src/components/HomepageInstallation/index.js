import React from "react";
import Translate from '@docusaurus/Translate';
import Link from "@docusaurus/Link";
import clsx from "clsx";
import styles from "./styles.module.css"

export default function Installation() {
  return (
    <section>
      <div className="container">
        <h2 className="text--center">📥 <Translate id="homepage.installation.title">Installation</Translate></h2>
        <p className="text--center"><Translate id="homepage.installation.text">Choose your preferred installation method.</Translate></p>
        <div className={styles.installationButtons}>
          <Link to="/docs/getting-started/installation#openupm" className={clsx("button button--lg margin--sm", styles.buttonOpenUpm)}>
            OpenUPM
          </Link>
          <Link to="/docs/getting-started/installation#git" className={clsx("button button--lg margin--sm", styles.buttonGit)}>
            Git
          </Link>
          <Link to="/docs/getting-started/installation#tarball" className={clsx("button button--lg margin--sm", styles.buttonTarball)}>
            Tarball
          </Link>
        </div>
      </div>
    </section>
  );
};