import React from 'react';
import Translate from '@docusaurus/Translate';
import { FaCheck, FaTimes, FaExclamationTriangle } from 'react-icons/fa';
import styles from './styles.module.css';

/**
 * Rows where both columns tick spend the reader's attention proving we match the
 * built-in manager. Two are kept so the table doesn't read as rigged; the rest of
 * the space goes to the things only this package does.
 */
const features = [
  { name: <Translate id="homepage.featureComparison.sceneLoading">Async Scene Loading</Translate>, package: true, unity: true },
  { name: <Translate id="homepage.featureComparison.sceneUnloading">Async Scene Unloading</Translate>, package: true, unity: true },
  { name: <Translate id="homepage.featureComparison.sceneTransition">Async Scene Transitions</Translate>, package: true, unity: false },
  { name: <Translate id="homepage.featureComparison.sceneReloading">Async Scene Reloading</Translate>, package: true, unity: false },
  { name: <Translate id="homepage.featureComparison.multiScene">Multiple Scenes Per Call</Translate>, package: true, unity: false },
  { name: <Translate id="homepage.featureComparison.lifecycle">Operation Lifecycle States</Translate>, package: true, unity: false },
  { name: <Translate id="homepage.featureComparison.cancellation">Cancellation</Translate>, package: true, unity: false },
  { name: <Translate id="homepage.featureComparison.asyncAwait">Async/Await Support</Translate>, package: true, unity: false },
  { name: <Translate id="homepage.featureComparison.loading">Integrated Loading Screens</Translate>, package: true, unity: false },
  { name: <Translate id="homepage.featureComparison.addressables">Addressables Integration</Translate>, package: true, unity: 'limited' },
];

const FeatureIcon = ({ isSupported }) => {
  return isSupported === true ? <FaCheck className={styles.checkmark} /> : isSupported !== 'limited' ? <FaTimes className={styles.crossmark} /> : <FaExclamationTriangle className={styles.exclamation} />;
};

export default function FeatureComparison() {
  return (
    <section>
      <div className="container">
        <h2 className="text--center"><Translate id="homepage.featureTable.title">What you would write instead</Translate></h2>
        <div className={styles.tableWrapper}>
          <table className={styles.comparisonTable}>
            <thead>
              <tr>
                <th></th>
                <th className="text--center">My Scene Manager</th>
                <th className="text--center">Unity Scene Manager</th>
              </tr>
            </thead>
            <tbody>
              {features.map((feature, index) => (
                <tr key={index}>
                  <td>{feature.name}</td>
                  <td className="text--center"><FeatureIcon isSupported={feature.package} /></td>
                  <td className="text--center"><FeatureIcon isSupported={feature.unity} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
