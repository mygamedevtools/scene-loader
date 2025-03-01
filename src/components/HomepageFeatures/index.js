import React from "react";
import clsx from "clsx";
import Translate from '@docusaurus/Translate';
import { FaRocket, FaCogs, FaSyncAlt, FaCode, FaBolt, FaBoxes } from "react-icons/fa";
import styles from './styles.module.css';

const features = [
  {
    title: <Translate>Simple Scene Transitions</Translate>,
    description: <Translate>Load and switch scenes effortlessly, with support for loading screens.</Translate>,
    icon: FaRocket,
  },
  {
    title: <Translate>Addressables Support</Translate>,
    description: <Translate>Manage Addressable and non-Addressable scenes in a unified and intuitive way.</Translate>,
    icon: FaCogs,
  },
  {
    title: <Translate>Integrated Async/Await</Translate>,
    description: <Translate>Load and unload scenes asynchronously using async/await for cleaner code.</Translate>,
    icon: FaSyncAlt,
  },
  {
    title: <Translate id="homepage.feature.loadingScreen.title">Loading Screens</Translate>,
    description: <Translate id="homepage.feature.loadingScreen.subtitle">Easily build loading screens with built-in components.</Translate>,
    icon: FaBolt,
  },
  {
    title: <Translate>Simple & Powerful API</Translate>,
    description: <Translate>A clean API that makes integration and maintenance easy.</Translate>,
    icon: FaCode,
  },
  {
    title: <Translate>Fully Modular</Translate>,
    description: <Translate>Pick only the components you need and customize them as you like.</Translate>,
    icon: FaBoxes,
  },
];

export default function Features() {
  return (
    <section>
      <div className="container">
        <div className="row">
          {features.map((feature, idx) => (
            <div key={idx} className={clsx("col col--4 margin-vert--sm")}>
              <div className="text--center">
                <feature.icon size={50} className={styles.featureIcon} />
              </div>
              <h3 className="text--center">{feature.title}</h3>
              <p className="text--center">{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
