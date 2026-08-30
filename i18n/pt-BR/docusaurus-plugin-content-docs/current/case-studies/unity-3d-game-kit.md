---
sidebar_position: 1
description: Uma integração real, passo a passo — substituindo o carregador por corrotina do 3D Game Kit pelo My Scene Manager.
---

# Unity 3D Game Kit

O [3D Game Kit](https://assetstore.unity.com/packages/templates/tutorials/3d-game-kit-115747) da Unity é um jogo pequeno e completo com carregamento de cenas próprio: uma corrotina em `SceneController` que faz fade para um overlay de carregamento, chama `SceneManager.LoadSceneAsync` em modo single, teleporta o jogador para a entrada certa e faz o fade de volta. É a forma a que a maioria dos projetos chega por conta própria, o que o torna um bom assunto para mostrar o que uma integração realmente toca — e o que não toca.

Esta página percorre essa integração na Unity `6000.5`, render pipeline built-in, uGUI, sem Addressables. Toda cena está nas Build Settings, então uma `string` simples é tudo de que uma referência precisa.

:::tip
Duas das correções da 5.0 do pacote saíram exatamente desta integração: o fader rodando em tempo escalado e sem limite, e o `MinimumDisplayTime` vivendo no exemplo em vez de no pacote. A página descreve o resultado na 5.0, onde nenhuma das duas precisa de contorno.
:::

O resultado, antes dos detalhes:

<video controls muted loop playsInline width="100%" src="/img/3d-game-kit.mp4" />

## O que o Game Kit tinha {/* #what-the-game-kit-had */}

`ScreenFader.prefab` fica em `Resources`, se instancia sozinho, é marcado como `DontDestroyOnLoad` e carrega três canvases de overlay: `BlackFader`, `GameOverCanvas` e `LoadingCanvas`. `SceneController.Transition` os comandava de uma corrotina:

```
SaveAllData → ReleaseControl → ScreenFader.FadeSceneOut(Loading) → ClearPersisters
→ SceneManager.LoadSceneAsync(name)                       (modo single)
→ LoadAllData → teleportar o jogador para a entrada → ScreenFader.FadeSceneIn → GainControl
```

`Scenes/UI/Loading.unity` estava nas Build Settings mas sem uso — uma câmera, um `EventSystem`, pós-processamento e cópias antigas dos canvases.

Três coisas se destacam. O visual do carregamento vive num overlay que precisa sobreviver às cenas, então ele é `DontDestroyOnLoad`. Cada passo depois do fade precisa ser sequenciado à mão. E o carregamento em modo single destrói tudo, então qualquer coisa que deva persistir também precisa ser `DontDestroyOnLoad`.

## Instalando {/* #installing */}

`Packages/manifest.json` recebe o registro do OpenUPM e a dependência:

```json
"dependencies": { "com.mygamedevtools.scene-loader": "5.0.0", … },
"scopedRegistries": [
  { "name": "Open UPM", "url": "https://package.openupm.com", "scopes": [ "com.mygamedevtools" ] }
]
```

O assembly de runtime é auto-referenciado, então os scripts do `Assembly-CSharp` do Game Kit podem usar `using MyGameDevTools.SceneLoading;` sem nenhum trabalho de asmdef. Veja [Instalação](../getting-started/installation.mdx) para as outras formas de entrar.

## A tela de carregamento como cena {/* #the-loading-screen-as-a-scene */}

A subárvore `LoadingCanvas` foi retirada do `ScreenFader.prefab` para um prefab próprio, `LoadingScreen.prefab`, e `Loading.unity` foi esvaziada até uma única instância dele. Ela mantém o visual do Game Kit — o fundo, as barras pretas, o `LoadingText` e o `LoadingChomper` animado por sprites — e ganha os componentes do pacote:

| Componente | Papel |
|---|---|
| `Canvas` — Screen Space Overlay | Renderiza sem câmera, então a cena não tem uma. Ela é carregada aditivamente sobre a cena que está saindo, exatamente como a `Loading_Screen` do exemplo. |
| `CanvasGroup` | O que o fader controla. |
| `LoadingBehavior` | **Obrigatório.** Ancora o `LoadingProgress` pelo qual a transição espera. Sem ele, uma cena de carregamento é exibida exatamente pelo tempo que o carregamento leva. |
| `LoadingFader` — `fadeInTime` e `fadeOutTime` em `0.5` | Retém os dois gates pela duração de cada fade. |
| `MinimumDisplayTime` — `seconds` em `1.5` | Retém a conclusão, então um carregamento rápido — `Level2 → Start` — não faz a tela piscar. |

Nada é ligado entre eles no Inspector: todo componente encontra o `LoadingBehavior` nos pais. A ordem de sorting do `Canvas` foi definida em `50`, abaixo do painel do HUD do exemplo em `100`, para que o HUD (abaixo) fique por cima da tela enquanto ela é exibida.

Com o visual do carregamento nas mãos da cena, o `ScreenFader` perde o fade `Loading`. Os fades `Black` e `GameOver` ficam: o fade-in inicial na primeira cena e a tela de morte não são transições de cena.

```diff title="ScreenFader.cs"
 public enum FadeType
 {
-    Black, Loading, GameOver,
+    Black, GameOver,
 }

 public CanvasGroup faderCanvasGroup;
-public CanvasGroup loadingCanvasGroup;
 public CanvasGroup gameOverCanvasGroup;

 public static IEnumerator FadeSceneOut(FadeType fadeType = FadeType.Black)
 {
     CanvasGroup canvasGroup;
     switch (fadeType)
     {
-        case FadeType.Black:
-            canvasGroup = Instance.faderCanvasGroup;
-            break;
         case FadeType.GameOver:
             canvasGroup = Instance.gameOverCanvasGroup;
             break;
         default:
-            canvasGroup = Instance.loadingCanvasGroup;
+            canvasGroup = Instance.faderCanvasGroup;
             break;
     }
```

:::info
Apague o caminho antigo em vez de deixá-lo dormente. Uma única fonte da verdade para o visual do carregamento é o objetivo; dois caminhos que funcionam é como a próxima pessoa escolhe o errado.
:::

## A transição {/* #the-transition */}

`SceneController.Transition` continua sendo uma corrotina — quem a chama no Game Kit são corrotinas — mas o sequenciamento passou para a operação. Cada linha do original continua lá; o que mudou é *quem* a executa e *quando*:

```diff title="SceneController.cs"
+public const string LoadingSceneName = "Loading";
+
+// Um único lugar, para que mudanças de zona, reinícios e o reload da timeline usem a mesma tela.
+public static LoadingScreen CreateLoadingScreen() => new SceneLoadingScreen(LoadingSceneName);
+
 protected IEnumerator Transition(string newSceneName, DestinationTag destinationTag, TransitionType transitionType)
 {
     m_Transitioning = true;
     PersistentDataManager.SaveAllData();

     if (m_PlayerInput == null)
         m_PlayerInput = FindObjectOfType<PlayerInput>();
     if (m_PlayerInput) m_PlayerInput.ReleaseControl();
-    yield return StartCoroutine(ScreenFader.FadeSceneOut(ScreenFader.FadeType.Loading));
-    PersistentDataManager.ClearPersisters();
-    yield return SceneManager.LoadSceneAsync(newSceneName);
-    m_PlayerInput = FindObjectOfType<PlayerInput>();
-    if (m_PlayerInput) m_PlayerInput.ReleaseControl();
-    PersistentDataManager.LoadAllData();
-    SceneTransitionDestination entrance = GetDestination(destinationTag);
-    SetEnteringGameObjectLocation(entrance);
-    SetupNewScene(transitionType, entrance);
-    if (entrance != null)
-        entrance.OnReachDestination.Invoke();
-    yield return StartCoroutine(ScreenFader.FadeSceneIn());
+
+    SceneOperation operation = MySceneManager.TransitionAsync(newSceneName, CreateLoadingScreen());
+
+    // A tela está opaca e a cena antiga está prestes a sair — o momento que o fade-out marcava antes.
+    operation.StateChanged += op =>
+    {
+        if (op.State == SceneOperationState.Unloading)
+            PersistentDataManager.ClearPersisters();
+    };
+
+    // Dispara enquanto a tela ainda está opaca, então o jogador está no lugar antes do fade-out.
+    void OnSceneLoaded(Scene scene)
+    {
+        if (scene.name != newSceneName)
+            return;
+
+        m_PlayerInput = FindObjectOfType<PlayerInput>();
+        if (m_PlayerInput) m_PlayerInput.ReleaseControl();
+        PersistentDataManager.LoadAllData();
+        SceneTransitionDestination entrance = GetDestination(destinationTag);
+        SetEnteringGameObjectLocation(entrance);
+        SetupNewScene(transitionType, entrance);
+        if (entrance != null)
+            entrance.OnReachDestination.Invoke();
+    }
+    operation.SceneLoaded += OnSceneLoaded;
+
+    try
+    {
+        // Relança se a operação falhar — uma cena fora das Build Settings, por exemplo.
+        yield return operation.ToCoroutine();
+    }
+    finally
+    {
+        operation.SceneLoaded -= OnSceneLoaded;
+        m_Transitioning = false;
+    }
+
     if (m_PlayerInput)
         m_PlayerInput.GainControl();
-
-    m_Transitioning = false;
 }
```

Cada um dos passos antigos tem uma fase à qual pertence:

| Passo antigo | Para onde foi | Por que ali |
|---|---|---|
| `FadeSceneOut(Loading)` | `ScreenIn` — o pacote | O fade-in do fader. Sua retenção de exibição mantém a cena antiga viva até a tela ficar opaca. |
| `ClearPersisters()` depois do fade | `StateChanged` → `Unloading` | Mesmo momento: tela opaca, cena antiga prestes a sair. |
| `LoadAllData`, teleporte, `OnReachDestination` | `SceneLoaded`, filtrado para o alvo | Roda durante `Loading`, antes de `ScreenOut`, então não há salto quando a tela some. |
| `FadeSceneIn()` | `ScreenOut` — o pacote | O fade-out do fader. Sua retenção de ocultação mantém a tela até terminar. |
| `m_Transitioning = false` | `finally` | Também é resetado quando a operação falha. |

`SceneLoaded` é filtrado por nome porque uma transição carrega a cena de carregamento também — ele dispara para as duas.

### Reinícios e reloads {/* #restarts-and-reloads */}

`RestartZone` já passava por `Transition` com a zona atual como alvo, então não precisou de nada. O `SceneReloaderBehaviour` da timeline carregava por índice de build em modo single, o que teria destruído toda cena carregada aditivamente:

```diff title="SceneReloaderBehaviour.cs"
 public void ReloadScene(GameObject sceneGameObject)
 {
-    SceneManager.LoadSceneAsync(sceneGameObject.scene.buildIndex);
+    MySceneManager.ReloadActiveSceneAsync(SceneController.CreateLoadingScreen());
 }
```

O reload agora mostra a mesma tela de carregamento de todas as outras transições, e mantém o HUD (abaixo) vivo durante ele.

## Um HUD persistente sem `DontDestroyOnLoad` {/* #a-persistent-hud-without-dontdestroyonload */}

A cena `SceneListenerHUD` do exemplo é um documento do UI Toolkit que se inscreve em `OperationStarted` e renderiza a fase, o progresso e um botão de cancelar de cada operação. Ela não tem câmera, `EventSystem` nem `DontDestroyOnLoad` — sobrevive a `Start → Level1 → Level2 → Start` porque uma transição descarrega **apenas a cena ativa**.

Um componente pequeno na cena do menu garante que ela esteja lá:

```cs title="MenuSceneHud.cs"
public class MenuSceneHud : MonoBehaviour
{
    [SceneName] public string hudSceneName = "SceneListenerHUD";

    // Start, não Awake: MySceneManager.Default é criado depois que a primeira cena carregou.
    void Start()
    {
        if (IsSceneLoadedOrLoading(hudSceneName))
            return;

        MySceneManager.LoadAsync(hudSceneName);
    }

    static bool IsSceneLoadedOrLoading(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        return false;
    }
}
```

Duas escolhas que valem explicação. `Start` em vez de `Awake`, porque `MySceneManager.Default` é criado por um `RuntimeInitializeOnLoadMethod` que roda depois que a primeira cena carregou, então ele ainda não existe no `Awake` dessa cena. E o loop sobre `SceneManager.sceneCount` em vez de `TryGetLoadedSceneByName`: a proteção precisa enxergar uma cena que ainda está **carregando** — o menu pode ser reaberto enquanto o HUD está a caminho — e a consulta do manager só conhece cenas que terminaram. `SceneManager.GetSceneByName` também não serve: ele retorna um handle válido para qualquer cena que as Build Settings conheçam, carregada ou não.

Esse é o padrão geral para qualquer coisa que deva sobreviver às transições: carregue aditivamente uma vez, nunca a torne ativa, e deixe o manager em paz com ela.

## Checklist para o seu projeto {/* #checklist-for-your-project */}

1. Adicione o registro do OpenUPM e a dependência; confirme que o pacote resolveu em `Library/PackageCache`.
2. Construa a tela de carregamento como um prefab: `Canvas` + `CanvasGroup` + **`LoadingBehavior`** + `LoadingFader`, mais `MinimumDisplayTime` e os componentes de feedback que quiser. Sem câmera, sem `EventSystem`. Coloque a cena dela nas Build Settings.
3. Substitua a corrotina de carregamento por um único `MySceneManager.TransitionAsync(target, new SceneLoadingScreen("Loading"))`. Mova o trabalho de "depois que a tela subiu" para `StateChanged == Unloading` e o de "antes de a tela sair" para `SceneLoaded`. `yield return op.ToCoroutine()` ou `await op`, e resete a flag de ocupado no `finally`.
4. Apague o caminho antigo de overlay.
5. Qualquer coisa que deva sobreviver às transições: carregue aditivamente uma vez e nunca a torne ativa — sem `DontDestroyOnLoad`.
6. Qualquer coisa que acesse `MySceneManager.Default` a partir da primeira cena faz isso no `Start`, não no `Awake`.
7. Assista aos fades num editor com janela, não num em batchmode — não há backbuffer para vê-los.

A página do exemplo, [Loading Scene Examples](../samples/loading-scene-examples.md), tem o HUD e toda forma de tela de carregamento como referências executáveis, e [Criando Telas de Carregamento](../getting-started/loading-screens.md) cobre os gates e retenções sobre os quais os componentes acima são construídos.
