import React from "react";
import Link from "@docusaurus/Link";
import Translate from "@docusaurus/Translate";
import useBaseUrl from "@docusaurus/useBaseUrl";
import styles from "./styles.module.css";

export default function CaseStudy() {
  return (
    <section>
      <div className="container">
        <h2 className="text--center">
          🎮 <Translate id="homepage.caseStudy.title">In a Real Project</Translate>
        </h2>
        <div className={styles.caseStudy}>
          <video
            className={styles.video}
            controls
            muted
            loop
            playsInline
            src={useBaseUrl("/img/3d-game-kit.mp4")}
          />
          <div className={styles.text}>
            <p>
              <Translate id="homepage.caseStudy.text1">
                Unity's 3D Game Kit ships with its own coroutine-driven scene loader: fade out, load in single mode, teleport the player, fade in.
              </Translate>
            </p>
            <p>
              <Translate id="homepage.caseStudy.text2">
                Replacing it took one TransitionAsync call. The loading screen became a scene, the after-load setup moved onto the operation's events, and the sample HUD stayed loaded through every transition without DontDestroyOnLoad.
              </Translate>
            </p>
            <Link className="button button--primary" to="/docs/next/case-studies/unity-3d-game-kit">
              <Translate id="homepage.caseStudy.link">Read the case study</Translate>
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
