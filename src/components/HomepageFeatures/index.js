import React from "react";
import clsx from "clsx";
import Translate from '@docusaurus/Translate';
import { FaCompressArrowsAlt, FaCogs, FaSyncAlt, FaCode, FaBolt, FaSearch } from "react-icons/fa";
import styles from './styles.module.css';

const features = [
  {
    title: <Translate id="homepage.feature.api.title">Four methods, not sixty-four</Translate>,
    description: <Translate id="homepage.feature.api.text">Load, unload, transition, get. Version 5 collapsed the whole API into them.</Translate>,
    icon: FaCompressArrowsAlt,
  },
  {
    title: <Translate id="homepage.feature.addressables.title">One string, either source</Translate>,
    description: <Translate id="homepage.feature.addressables.text">The same call finds a scene in your Build Settings or in Addressables. No second API to learn.</Translate>,
    icon: FaCogs,
  },
  {
    title: <Translate id="homepage.feature.await.title">Await it your way</Translate>,
    description: <Translate id="homepage.feature.await.text">Await it directly, bridge it to a Task, or yield return it from a coroutine. Same operation either way.</Translate>,
    icon: FaSyncAlt,
  },
  {
    title: <Translate id="homepage.feature.handle.title">A handle for every operation</Translate>,
    description: <Translate id="homepage.feature.handle.text">Progress, lifecycle state, per-scene events and cancellation — available after the call, not registered before it.</Translate>,
    icon: FaCode,
  },
  {
    title: <Translate id="homepage.feature.loadingScreen.title">Loading screens beyond scenes</Translate>,
    description: <Translate id="homepage.feature.loadingScreen.subtitle">Drive one from a scene, a prefab or a UI Toolkit document, with a built-in component for each.</Translate>,
    icon: FaBolt,
  },
  {
    title: <Translate id="homepage.feature.observable.title">Watch it work</Translate>,
    description: <Translate id="homepage.feature.observable.text">A logging layer reports each step, so a transition that stalls is diagnosable instead of mysterious.</Translate>,
    icon: FaSearch,
  },
];

export default function Features() {
  return (
    <section>
      <div className="container">
        <div className="row">
          {features.map((feature, idx) => (
            <div key={idx} className={clsx("col col--4 margin-vert--md")}>
              <feature.icon size={34} className={styles.featureIcon} />
              <h3 className={styles.featureTitle}>{feature.title}</h3>
              <p className={styles.featureText}>{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
