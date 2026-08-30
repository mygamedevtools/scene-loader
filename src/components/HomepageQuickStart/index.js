import React from 'react';
import Translate from '@docusaurus/Translate';
import CodeBlock from '@theme/CodeBlock';
import styles from './styles.module.css';

export default function QuickStart() {
  return (
    <section>
      <div className="container">
        <h2 className="text--center"><Translate id="homepage.example.title">One line instead of five</Translate></h2>
        <p className={styles.caption}><Translate id="homepage.example.text1">Perform scene transitions like this:</Translate></p>
        <CodeBlock language="cs">
          {`MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");`}
        </CodeBlock>
        <p className={styles.caption}><Translate id="homepage.example.text2">Instead of:</Translate></p>
        <CodeBlock language="cs">
          {`yield return SceneManager.LoadSceneAsync("my-loading-scene", LoadSceneMode.Additive);
yield return SceneManager.LoadSceneAsync("my-target-scene", LoadSceneMode.Additive);
SceneManager.SetActiveScene(SceneManager.GetSceneByName("my-target-scene"));
SceneManager.UnloadSceneAsync("my-loading-scene");
SceneManager.UnloadSceneAsync("my-previous-scene");`}
        </CodeBlock>
        <p className={styles.caption}>
          <Translate id="homepage.example.text3">
            That same line works whether the scene comes from your Build Settings or from Addressables.
          </Translate>
        </p>
      </div>
    </section >
  );
}
