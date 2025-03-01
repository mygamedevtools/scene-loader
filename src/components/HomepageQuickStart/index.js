import React from 'react';
import Translate from '@docusaurus/Translate';
import CodeBlock from '@theme/CodeBlock';

export default function QuickStart() {
  return (
    <section>
      <div className="container">
        <h2 className="text--center">⚡ <Translate id="homepage.example.title">Quick Example</Translate></h2>
        <p className="text--center"><Translate id="homepage.example.text1">Perform scene transitions like this:</Translate></p>
        <CodeBlock language="cs">
          {`AdvancedSceneManager.TransitionAsync("my-target-scene", "my-loading-scene");`}
        </CodeBlock>
        <p className="text--center"><Translate id="homepage.example.text2">Instead of:</Translate></p>
        <CodeBlock language="cs">
          {`yield return SceneManager.LoadSceneAsync("my-loading-scene", LoadSceneMode.Additive);
yield return SceneManager.LoadSceneAsync("my-target-scene", LoadSceneMode.Additive);
SceneManager.SetActiveScene(SceneManager.GetSceneByName("my-target-scene"));
SceneManager.UnloadSceneAsync("my-loading-scene");
SceneManager.UnloadSceneAsync("my-previous-scene");`}
        </CodeBlock>
      </div>
    </section >
  );
}