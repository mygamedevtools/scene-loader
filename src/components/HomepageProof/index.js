import React from "react";
import Translate from "@docusaurus/Translate";
import styles from "./styles.module.css";

const facts = [
  { label: <Translate id="homepage.proof.license">License</Translate>, value: "MIT" },
  { label: <Translate id="homepage.proof.unity">Unity</Translate>, value: "6.0+" },
  {
    label: <Translate id="homepage.proof.dependencies">Dependencies</Translate>,
    value: <Translate id="homepage.proof.dependencies.value">None</Translate>,
  },
  {
    label: <Translate id="homepage.proof.addressables">Addressables</Translate>,
    value: <Translate id="homepage.proof.addressables.value">Optional</Translate>,
  },
  { label: <Translate id="homepage.proof.pipelines">Render pipelines</Translate>, value: "Built-in · URP · HDRP" },
];

export default function Proof() {
  return (
    <section className={styles.section}>
      <div className="container">
        <div className={styles.strip}>
          {facts.map((fact, idx) => (
            <div key={idx} className={styles.fact}>
              <span className={styles.label}>{fact.label}</span>
              <span className={styles.value}>{fact.value}</span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
