---
sidebar_position: 1
title: Cenas de Carregamento
description: Aprenda com o Exemplo 'Loading Scene Examples'.
---

# Exemplos de Cena de Carregamento

Este exemplo consiste em duas salas e uma lista de exemplos. Cada item da lista é **uma linha da API do pacote**, mostrada ao lado da linha que a executa — telas de carregamento a partir de uma cena, de um prefab e de um documento UI Toolkit, carregamentos de várias cenas, recarregamentos e transições aguardadas. Um HUD persistente reporta a fase e o progresso de cada operação enquanto ela roda.

## Instalação {/* #installation */}

Importe o exemplo através do **Package Manager**.

1. Abra `Window/Package Manager`.
2. Selecione `My Scene Manager` na lista `In Project`.
3. No painel direito, selecione a aba **Samples**.
4. Clique no botão `Import` do item `Loading Scene Examples`.

Os arquivos do exemplo serão instalados em `Samples/My Scene Manager/<version>/Loading Scene Examples`.

## Compatibilidade com Scriptable Render Pipeline {/* #scriptable-render-pipeline-compatibility */}

Ao importar o exemplo em um projeto com um **Scriptable Render Pipeline** ativo, uma caixa de diálogo aparecerá perguntando se você deseja atualizar automaticamente os materiais do exemplo.
Isso atualizará os materiais tanto para **URP** quanto para **HDRP**.

## Adicionando cenas às Build Settings {/* #adding-scenes-to-build-settings */}

O exemplo carrega suas cenas pelo nome, então elas precisam estar nas **Build Settings**.
Nada é gravado na importação: as Build Settings valem para o projeto inteiro, então o exemplo pede permissão antes.

Abra **SceneA** ou **SceneB** e entre no modo de jogo. Se alguma das cenas do exemplo estiver faltando, a sala é substituída por um aviso:

- **Add them, and exit Play Mode** (adicioná-las e sair do modo de jogo) adiciona as cenas que faltam. As Build Settings são lidas quando o modo de jogo começa, então o exemplo sai do modo de jogo — entre nele de novo e o exemplo vai rodar.
- **Leave without changing anything** (sair sem alterar nada) sai do modo de jogo e deixa as Build Settings como estão.

Para remover as cenas de novo, use o botão **Remove the sample's scenes from Build Settings, and exit Play Mode** (remover as cenas do exemplo das Build Settings e sair do modo de jogo) na UI da sala. Somente as cenas do exemplo são removidas; todo o resto fica como está.

## Testando o Exemplo {/* #playing-the-sample */}

O exemplo contém **duas** salas, **duas** cenas de carregamento e **duas** cenas auxiliares:

- **SceneA** e **SceneB** — as salas. Cada uma faz a transição para a outra.
- **Loading_Screen** — uma cena de carregamento uGUI construída a partir dos próprios componentes do pacote.
- **Loading_Animated** — uma cena de carregamento UI Toolkit com uma animação de painéis deslizantes.
- **Extra** — uma cena com um objeto girando, carregada junto com uma sala pelo exemplo de várias cenas.
- **SceneListenerHUD** — o HUD persistente. Carregado sob demanda pela sala em que você começar e nunca descarregado.

Comece em qualquer uma das salas. A lista contém **oito** exemplos; clique em um para executá-lo e leia a linha de código logo abaixo para ver o que rodou:

![Loading Scene Examples](@site/docs/img/sample_loading-scene-examples.jpg)

| Exemplo | O que executa | O que mostra |
|---|---|---|
| **Direct** | `TransitionAsync("SceneB")` | Uma troca direta, sem tela de carregamento. |
| **Loading scene** | `TransitionAsync("SceneB", "Loading_Screen")` | Uma cena com um `LoadingBehavior` e um `LoadingFader`, construída com uGUI. |
| **Prefab screen** | `TransitionAsync(target, new PrefabLoadingScreen(prefab))` | A mesma tela como um prefab: sem cena extra, sem entrada nas Build Settings. |
| **UI Toolkit screen** | `TransitionAsync(target, new UIDocumentLoadingScreen(uxml, panel))` | Um documento UXML que é dono do seu próprio `LoadingProgress`, sem `LoadingBehavior` em lugar nenhum. |
| **Animated screen** | `TransitionAsync("SceneB", "Loading_Animated")` | Uma cena de carregamento UI Toolkit cujos gates ficam retidos até cada deslize terminar. |
| **Reload this scene** | `ReloadActiveSceneAsync(loadingScreen)` | Recarrega a cena ativa, seja ela qual for, então o mesmo botão funciona nas duas salas. |
| **Two scenes at once** | `TransitionAsync(new[] { target, extra }, loadingScreen)` | Uma operação, duas cenas, a primeira delas definida como ativa. |
| **Await the handle** | `SceneResult result = await TransitionAsync(target, loadingScreen)` | A operação é aguardável; o resultado traz as cenas que ela produziu. |

Toda tela de carregamento do exemplo permanece visível por pelo menos **dois segundos**, por mais rápido que o carregamento seja, para que dê tempo de ler o que ela está mostrando.

### O HUD de operação {/* #the-operation-hud */}

A barra no topo da tela é a cena `SceneListenerHUD`. Ela mostra a cena ativa, o tipo de operação em execução e seu progresso, e acende cada fase do ciclo de vida da operação conforme ela avança:

`Resolving → ScreenIn → Unloading → Loading → Activating → ScreenOut → Completed`

Um botão **Cancel** aparece enquanto uma operação roda. Cancele uma no meio do caminho e a linha do tempo termina em `Canceled`.

O HUD vive na sua própria cena porque a UI de uma sala é descarregada no meio de uma transição, então ela nunca conseguiria reportar uma do início ao fim. Nada nele usa `DontDestroyOnLoad`: `TransitionAsync` só descarrega a cena **ativa**, então uma cena carregada aditivamente simplesmente sobrevive.

## Entendendo os Exemplos {/* #understanding-the-examples */}

### Iniciando uma transição {/* #starting-a-transition */}

A lista de exemplos é um único componente `TransitionExamples`, compartilhado pelas duas salas como um prefab — só a cena de destino é diferente. Cada linha combina o código que exibe com o código que executa:

```cs
new Example(
    "Loading scene", "SCENE",
    "A scene with a LoadingBehavior and a LoadingFader, built with uGUI.",
    $"TransitionAsync(\"{_targetScene}\", \"{_loadingScene}\")",
    () => MySceneManager.TransitionAsync(_targetScene, _loadingScene)),
```

Nada nele reporta progresso ou observa fases — o HUD faz isso para toda operação que o exemplo inicia. Esse é o arranjo que vale a pena copiar: o código que inicia uma transição não precisa saber nada sobre o código que a exibe.

Duas das linhas fazem um pouco mais. O exemplo de várias cenas constrói um `SceneParameters` a partir de um array — a primeira cena vira a ativa, a menos que os parâmetros indiquem outra:

```cs
SceneParameters parameters = new[] { _targetScene, _additiveScene };
MySceneManager.TransitionAsync(parameters, _loadingScene);
```

E o exemplo aguardado é `async void`, o mesmo formato que um handler de botão no seu próprio projeto teria. A operação é cancelada se o objeto for destruído no meio do caminho, e é para isso que `CancelWith` serve:

```cs
async void AwaitTransition()
{
    SceneResult result = await MySceneManager
        .TransitionAsync(_targetScene, _loadingScene)
        .CancelWith(destroyCancellationToken);

    Debug.Log($"Transition finished with {result.GetScenes().Length} scene(s) loaded.");
}
```

:::info
O exemplo acessa `MySceneManager` a partir de `Start`, nunca de `Awake` ou `OnEnable`. O manager estático é criado depois que a primeira cena termina de carregar, então tentar acessá-lo antes disso lança uma exceção quando a cena em questão é justamente a primeira.
:::

### Observando toda operação {/* #watching-every-operation */}

`OperationHud` se inscreve uma única vez, e toda operação que o exemplo inicia se reporta a ele:

```cs
void Start()
{
    _manager = MySceneManager.Default;

    _manager.OperationStarted += OnOperationStarted;
    _manager.ActiveSceneChanged += OnActiveSceneChanged;
}

void OnOperationStarted(SceneOperation operation)
{
    operation.Progressed += OnProgressed;
    operation.StateChanged += OnStateChanged;
    operation.Completed += OnCompleted;
}
```

`operation.State` é comparado com a linha do tempo para acender os indicadores de fase, e `operation.Cancel()` é o que o botão **Cancel** chama. Veja [Operação de Cena](../advanced-usage/scene-operation.md) para a API completa do handle.

### A cena de carregamento {/* #the-loading-scene */}

`Loading_Screen` é o guia [Criando Telas de Carregamento](../getting-started/loading-screens.md) em forma de cena, construída inteiramente com componentes do pacote:

- `LoadingBehavior` na raiz do canvas, ancorando o `LoadingProgress`.
- `LoadingFader` no mesmo `CanvasGroup`, retendo a transição pela duração de cada fade.
- `LoadingFeedbackSlider` e `LoadingFeedbackText` exibindo o progresso.
- `MinimumDisplayTime`, um componente do exemplo que mantém a tela visível por dois segundos.

Nenhum deles é ligado aos outros no Inspector. Todo componente abaixo do `LoadingBehavior` o encontra nos pais, e cada um que precisa que a transição espere faz sua própria **retenção** nos gates do progresso. A transição espera até o último deles liberar.

### A tela em prefab {/* #the-prefab-screen */}

`PrefabLoadingScreen` mostra que a *cena* de carregamento nunca foi a questão. O `LoadingScreen.prefab` que ele instancia tem exatamente a mesma hierarquia de `Loading_Screen` — a cena é construída a partir do prefab — então os dois são idênticos por construção:

```cs
public class PrefabLoadingScreen : LoadingScreen
{
    readonly GameObject _prefab;
    GameObject _instance;

    public PrefabLoadingScreen(GameObject prefab)
    {
        _prefab = prefab != null ? prefab : throw new System.ArgumentNullException(nameof(prefab));
    }

    public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
    {
        _instance = Object.Instantiate(_prefab);
        // Para a cena hospedeira, para que sobreviva ao descarregamento da cena de saída.
        host.Adopt(_instance);

        BindProgress(LoadingBehaviorRegistry.TryGet(_instance, out LoadingBehavior behavior) ? behavior.Progress : null);

        return SceneOperationPump.Completed(operation);
    }

    public override void Dispose()
    {
        if (_instance != null)
            Object.Destroy(_instance);

        _instance = null;
        base.Dispose();
    }
}
```

`PrepareAsync` e `Dispose` são tudo o que ele implementa. Um `LoadingBehavior` em qualquer lugar do prefab é detectado por meio do `LoadingBehaviorRegistry` e controla a transição; sem um, a tela não retém nada.

### A tela UI Toolkit {/* #the-ui-toolkit-screen */}

`UIDocumentLoadingScreen` vai um passo além: sem cena, sem prefab e sem `LoadingBehavior` em lugar nenhum. Ele cria seu próprio `LoadingProgress` e controla a transição por meio dele — tudo o que uma tela de carregamento precisa fazer se expressa por um `LoadingProgress`, e um objeto C# comum pode ter um.

```cs
public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
{
    _instance = new GameObject(nameof(UIDocumentLoadingScreen));
    host.Adopt(_instance);

    UIDocument document = _instance.AddComponent<UIDocument>();
    document.panelSettings = _panelSettings;
    document.visualTreeAsset = _visualTree;
    document.sortingOrder = 50;

    _root  = document.rootVisualElement;
    _value = _root?.Q<Label>("value");
    _fill  = _root?.Q<VisualElement>("fill");

    LoadingProgress progress = new();
    progress.Progressed += OnProgressed;
    progress.LoadingCompleted += FadeOut;
    BindProgress(progress);

    // Retidos antes que a transição possa ler os gates, e liberados quando cada fade termina.
    progress.HoldShow(this);
    progress.HoldHide(this);

    // Atrasa o sinal em vez do gate, para que a tela permaneça visível pelo seu tempo mínimo.
    if (_minimumSeconds > 0)
    {
        progress.HoldCompletion(this);
        _root?.schedule.Execute(() => progress.ReleaseCompletion(this))
              .StartingIn((long)(_minimumSeconds * 1000f));
    }

    Fade(0, 1, () => progress.ReleaseShow(this));

    return SceneOperationPump.Completed(operation);
}
```

Os fades rodam pelo próprio scheduler do UI Toolkit, então a tela não precisa de nenhum `MonoBehaviour` para rodar uma coroutine.

### A tela animada {/* #the-animated-screen */}

`Loading_Animated` é uma **cena** de carregamento que não é uGUI. `AnimatedLoadingScreen` é um `LoadingScreenComponent` — a base para qualquer coisa que vive em uma tela de carregamento e conduz o seu `LoadingProgress`, ou espera por ele. Ele encontra o `LoadingBehavior` no mesmo objeto e, uma vez vinculado, retém os dois gates até cada deslize terminar:

```cs
[RequireComponent(typeof(UIDocument))]
public class AnimatedLoadingScreen : LoadingScreenComponent
{
    protected override void OnBound()
    {
        Progress.HoldShow(this);
        Progress.HoldHide(this);

        Progress.Progressed += OnProgressed;
        Progress.LoadingCompleted += SlideOut;

        SlideIn();
    }

    void SlideIn()
    {
        // ...adiciona as classes "closed" para que a transição USS rode...
        _left.schedule.Execute(() => Progress.ReleaseShow(this)).StartingIn(Milliseconds());
    }

    void SlideOut()
    {
        // ...remove elas de novo...
        _left.schedule.Execute(() => Progress.ReleaseHide(this)).StartingIn(Milliseconds());
    }
}
```

O gate abre quando o deslize termina, não quando começa, então a cena de saída nunca é descarregada atrás de uma cortina que ainda está se abrindo.

### Tempo mínimo de exibição {/* #minimum-display-time */}

`MinimumDisplayTime` é o menor `LoadingScreenComponent` do exemplo, e ele faz uma distinção que vale a pena conhecer:

```cs
public class MinimumDisplayTime : LoadingScreenComponent
{
    [SerializeField]
    float _seconds = 2f;

    float _shownAt;

    protected override void OnBound()
    {
        _shownAt = Time.unscaledTime;
        Progress.HoldCompletion(this);
    }

    void Update()
    {
        if (Progress == null || Time.unscaledTime - _shownAt < _seconds)
            return;

        Progress.ReleaseCompletion(this);
        enabled = false;
    }
}
```

Ele retém a **conclusão**, não o gate de ocultação. Reter o gate de ocultação atrasa a transição depois que a tela já recebeu o aviso para sair, então o fade roda até o fim e o resto da espera acontece numa tela vazia. Reter a conclusão atrasa o próprio sinal `LoadingCompleted`, então a tela permanece visível e a animação de saída, seja ela qual for, começa na hora certa.

## Conclusão {/* #wrap-up */}

Com este exemplo, você pôde executar, a partir de uma única lista, todas as formas que uma transição pode assumir, observar cada uma delas pelo mesmo HUD e ler três telas de carregamento — uma cena, um prefab e um documento UI Toolkit — que controlam a mesma transição da mesma maneira.
Use os scripts `PrefabLoadingScreen`, `UIDocumentLoadingScreen` e `MinimumDisplayTime` como ponto de partida para criar suas próprias experiências de carregamento ✨.
