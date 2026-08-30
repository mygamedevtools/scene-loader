import React from "react";
import Translate from "@docusaurus/Translate";
import CodeBlock from "@theme/CodeBlock";
import styles from "./styles.module.css";

const points = [
  {
    title: <Translate id="homepage.operation.progress.title">Progress</Translate>,
    text: (
      <Translate id="homepage.operation.progress.text">
        A single number for the whole operation, not one AsyncOperation per scene to average yourself.
      </Translate>
    ),
  },
  {
    title: <Translate id="homepage.operation.states.title">States</Translate>,
    text: (
      <Translate id="homepage.operation.states.text">
        Resolving, ScreenIn, Unloading, Loading, Activating, ScreenOut, Completed — each one reported as it happens.
      </Translate>
    ),
  },
  {
    title: <Translate id="homepage.operation.cancel.title">Cancellation</Translate>,
    text: (
      <Translate id="homepage.operation.cancel.text">
        Call Cancel on the handle. No token to thread through your call sites in advance.
      </Translate>
    ),
  },
];

export default function Operation() {
  return (
    <section>
      <div className="container">
        <h2 className="text--center">
          <Translate id="homepage.operation.title">Every call hands back a handle</Translate>
        </h2>
        <p className={`text--center ${styles.lead}`}>
          <Translate id="homepage.operation.lead">
            Not a callback you registered before the call — an object you hold after it.
          </Translate>
        </p>
        <div className={styles.layout}>
          <div className={styles.code}>
            <CodeBlock language="cs">
              {`SceneOperation op = MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");

op.Progressed   += progress => bar.value = progress;
op.StateChanged += o => { if (o.State == SceneOperationState.ScreenIn) BeginIntro(); };

SceneResult result = await op;   // or op.Cancel(), or yield return op.ToCoroutine()`}
            </CodeBlock>
          </div>
          <ul className={styles.points}>
            {points.map((point, idx) => (
              <li key={idx}>
                <h3 className={styles.pointTitle}>{point.title}</h3>
                <p className={styles.pointText}>{point.text}</p>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}
