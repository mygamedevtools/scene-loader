---
sidebar_position: 3
title: Telas de Carregamento
description: Como criar telas de carregamento com o pacote.
---

# Criando Telas de Carregamento

Durante as transições de cena, você pode fornecer uma tela de carregamento — uma splash screen animada ou uma barra de progresso, por exemplo.

Uma tela de carregamento é um `LoadingScreen`. A mais simples é uma cena, e basta passar o nome de uma cena para ter uma de graça:

```cs
MySceneManager.TransitionAsync("target", "loading");                                  // uma cena
MySceneManager.TransitionAsync("target", new PrefabLoadingScreen(prefab));            // um prefab
MySceneManager.TransitionAsync("target", new UIDocumentLoadingScreen(uxml, panel));   // um documento UI Toolkit
```

Nome de cena, caminho, endereço, índice de build, `Scene` ou `AssetReference`: todos são convertidos implicitamente em uma tela de carregamento baseada em cena, então você só escreve o seu próprio `LoadingScreen` quando quer algo que *não* seja uma cena.

Seja qual for a tela, ela segura a transição da mesma forma, por meio de um `LoadingProgress`. Esta página começa com uma cena de carregamento construída a partir dos componentes do pacote, explica os gates que esses componentes usam e, em seguida, mostra como os mesmos gates conduzem um prefab ou um documento UI Toolkit.

## Uma cena de carregamento {/* #a-loading-scene */}

Tome como exemplo a hierarquia de cena de carregamento a seguir — é a cena `Loading_Screen` do exemplo [Loading Scene Examples](../samples/loading-scene-examples.md):

* Loading Screen - ([Canvas], [CanvasScaler], [CanvasGroup], `LoadingBehavior`, `LoadingFader`, `MinimumDisplayTime`)
  * Backdrop - ([Image])
  * Card - ([Image])
    * Value - ([Text], `LoadingFeedbackText`)
    * Track - ([Slider], `LoadingFeedbackSlider`)
      * Fill - ([Image])

Com essa hierarquia na sua cena de carregamento, ela faz fade in, mostra uma barra e uma porcentagem de progresso, permanece visível por pelo menos alguns segundos e faz fade out assim que a cena de destino terminar de carregar.

Nada precisa ser ligado no Inspector: cada componente abaixo do `LoadingBehavior` o encontra nos pais, e cada um que precisa que a transição espere — o fader, o tempo mínimo de exibição — mantém sua própria retenção. A transição espera por quem liberar por último.

Você pode testar essa cena passando seu nome, caminho ou índice de build como segundo argumento de `TransitionAsync`.

:::tip
A cena de carregamento não precisa ser uGUI. A cena `Loading_Animated` do exemplo é um `UIDocument` de UI Toolkit com o mesmo `LoadingBehavior` — veja [Componentes personalizados](#custom-components) abaixo.
:::

## Componentes de Carregamento {/* #loading-components */}

### O Loading Behavior {/* #the-loading-behavior */}

O `LoadingBehavior` é um componente [MonoBehaviour] que ancora o `LoadingProgress` da tela. Coloque um na raiz da sua tela de carregamento e todo o resto — feedbacks, fades, animações — se conecta ao seu `Progress`:

```cs
public class LoadingProgress : IProgress<float>
{
  public event Action<float> Progressed;
  public event Action LoadingCompleted;

  public bool IsShown { get; }
  public bool IsHidden { get; }

  public void HoldShow(object owner);
  public void ReleaseShow(object owner);
  public void HoldHide(object owner);
  public void ReleaseHide(object owner);
  public void HoldCompletion(object owner);
  public void ReleaseCompletion(object owner);
}
```

O evento `Progressed` envia um parâmetro `float`, de 0 a 1, para reportar o progresso da operação de carregamento de cena.
O evento `LoadingCompleted` notifica quando a operação de carregamento de cena foi concluída, mas a tela de carregamento ainda está ativa — é o sinal para a tela começar a se ocultar.

:::info[Como ele é encontrado]
Um `LoadingBehavior` se registra quando é **habilitado**, sob a cena em que vive — ou, para uma tela em prefab, sob a hierarquia em que foi instanciado. Duas consequências que vale a pena conhecer:

* Um `LoadingBehavior` em um GameObject **desabilitado** nunca é encontrado, e a transição roda sem feedback e sem espera, em vez de reportar um problema.
* **Um por tela de carregamento.** Se uma cena contiver dois, a transição emite um aviso no log e conduz o primeiro que se registrou.
:::

:::note
Um `LoadingBehavior` é **opcional**. Uma cena de carregamento sem ele continua funcionando como tela de carregamento — você só não recebe feedback de progresso, e a tela fica visível exatamente pelo tempo que o carregamento levar.
:::

### Gates e retenções {/* #gates-and-holds */}

A transição espera em dois **gates**: o gate de *exibição*, antes de descarregar a cena de onde você veio, e o gate de *ocultação*, antes de considerar que a tela de carregamento se foi. Ambos ficam **abertos, a menos que algo os esteja segurando fechados**.

Qualquer coisa que precise que a transição espere — um fade, uma animação, um script — chama `HoldShow` ou `HoldHide` passando a si mesma como dona, e libera quando termina. O gate abre quando o último detentor libera, e é isso que permite que vários componentes segurem a mesma transição sem que nenhum deles saiba da existência dos outros.

```cs
void Awake()
{
    // Faça as retenções antes que a transição possa ler os gates.
    _loadingBehavior.Progress.HoldShow(this);
    _loadingBehavior.Progress.HoldHide(this);
    _loadingBehavior.Progress.LoadingCompleted += PlayOut;

    PlayIn();
}

void OnPlayInFinished()  => _loadingBehavior.Progress.ReleaseShow(this);
void OnPlayOutFinished() => _loadingBehavior.Progress.ReleaseHide(this);
```

As retenções são identificadas pelo dono, então reter duas vezes ou liberar duas vezes é inofensivo. Faça as retenções em `Awake` ou `OnEnable`: uma retenção feita mais tarde pode chegar depois de a transição já ter lido o gate.

Existe uma terceira retenção, `HoldCompletion`, que atrasa o **sinal** `LoadingCompleted` em vez de um gate. Reter o gate de ocultação atrasa a *transição*, mas a tela já foi avisada para sair: um fade out roda até o fim e o resto da espera acontece em uma tela vazia. Reter a conclusão mantém a tela visível, e o que quer que a leve embora começa na hora certa. É disso que um [tempo mínimo de exibição](#minimum-display-time) precisa.

:::note
Para esperar pelos gates por conta própria, use `WaitForShowAsync()` e `WaitForHideAsync()`, ou leia as propriedades `IsShown` / `IsHidden`.
:::

:::warning
Se você fizer uma retenção e nunca a liberar, a transição espera. Isso não falha silenciosamente: depois de 10 segundos, um development build informa qual é o detentor e continua esperando. Um detentor destruído sem liberar é descartado, em vez de bloquear para sempre.
:::

### O Feedback de Carregamento {/* #the-loading-feedback */}

Com um `LoadingBehavior` no lugar, adicione componentes de feedback para mostrar o progresso.
Esse pacote vem com **três** feedbacks:

* `LoadingFeedbackSlider`: adicione a um [UI Slider] para mostrar o feedback de carregamento como uma barra de progresso.
* `LoadingFeedbackTextMeshPro`: adicione a um [UI Text Mesh Pro] para mostrar o feedback de carregamento em forma de texto normalizado de 0 a 100.
* `LoadingFeedbackText` _(também conhecido como Legacy)_: adicione a um [UI Legacy Text] para mostrar o feedback de carregamento em forma de texto normalizado de 0 a 100.

Você pode usar uma combinação desses componentes de feedback na cena de carregamento.
O campo `LoadingBehavior` deles é opcional: quando deixado vazio, ele é obtido do mesmo objeto ou do pai mais próximo que tiver um. Atribua-o somente quando o feedback estiver em outro lugar da hierarquia.

### O Loading Fader {/* #the-loading-fader */}

O componente `LoadingFader` realiza transições de **fade in/out**.
Adicione-o a um [GameObject] com [UI Canvas Group] para controlar o valor de alpha do grupo durante as transições visuais.
Você pode definir os tempos de fade in e fade out separadamente, limitar o quanto um único frame pode avançar um fade com `maxFrameStep` e personalizar as curvas de animação do fade in/out como preferir.

Ele retém os dois gates durante cada fade, então basta adicionar o componente para declarar que a transição deve esperar pelos fades — não há nada para habilitar no `LoadingBehavior`.

Os dois fades rodam em tempo **não escalado e limitado**. Não escalado porque uma transição iniciada a partir de um jogo pausado — voltar ao menu por uma tela de pausa com `timeScale = 0` — nunca avançaria o fade e nunca abriria o gate que ele está retendo. Limitado porque o frame em que uma cena é ativada costuma ser longo o bastante para consumir um fade inteiro antes que qualquer coisa seja desenhada, deixando o primeiro frame que o jogador vê já quase transparente; `maxFrameStep` (1/30 s por padrão) é o máximo que um frame pode contar.

### Componentes personalizados {/* #custom-components */}

Os feedbacks e o fader estendem `LoadingScreenComponent`, a base de qualquer coisa que vive em uma tela de carregamento e conduz o seu `LoadingProgress`, ou espera por ele. Ela resolve o `LoadingBehavior` para você e chama `OnBound` assim que o `Progress` estiver disponível — é ali que você se inscreve nos eventos e faz suas retenções.

Um feedback cabe em poucas linhas:

```cs
public class LoadingFeedbackImageFill : LoadingScreenComponent
{
    Image _image;

    protected override void Awake()
    {
        _image = GetComponent<Image>();
        base.Awake();
    }

    protected override void OnBound()
    {
        Progress.Progressed += progress => _image.fillAmount = progress;
    }
}
```

Uma animação pela qual a transição precisa esperar tem a mesma forma, mais as retenções. É o caso da cena `Loading_Animated` do exemplo — um documento UI Toolkit cujos painéis deslizam até se encontrar e voltam a deslizar para fora assim que o carregamento termina:

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
        // ...inicia a transição USS...
        _left.schedule.Execute(() => Progress.ReleaseShow(this)).StartingIn(Milliseconds());
    }

    void SlideOut()
    {
        // ...inicia ela ao contrário...
        _left.schedule.Execute(() => Progress.ReleaseHide(this)).StartingIn(Milliseconds());
    }
}
```

Cada gate é liberado quando o respectivo deslize **termina**, não quando começa, então a cena de saída nunca é descarregada atrás de uma cortina que ainda está se abrindo.

### Tempo mínimo de exibição {/* #minimum-display-time */}

Uma cena que carrega em dois frames produz uma tela de carregamento que apenas pisca, o que parece um bug. O componente `MinimumDisplayTime` mantém a tela visível por pelo menos um tempo definido, medido no relógio não escalado, e é a razão de `HoldCompletion` existir:

```cs
[AddComponentMenu("Scene Loading/Minimum Display Time")]
public class MinimumDisplayTime : LoadingScreenComponent
{
    [Min(0)]
    public float seconds = 2f;

    float _shownAt;

    protected override void OnBound()
    {
        _shownAt = Time.unscaledTime;
        Progress.HoldCompletion(this);
    }

    void Update()
    {
        if (Progress == null || Time.unscaledTime - _shownAt < seconds)
            return;

        Progress.ReleaseCompletion(this);
        enabled = false;
    }
}
```

Coloque-o junto de um `LoadingBehavior` e o sinal `LoadingCompleted` espera por ele — o fader, ou o que quer que leve a tela embora, começa assim que o carregamento e o temporizador tiverem terminado. Um carregamento que já durou mais do que `seconds` não é atrasado.

## Telas de carregamento que não são cenas {/* #loading-screens-that-are-not-scenes */}

Uma cena é um jeito pesado de mostrar uma barra de progresso. `LoadingScreen` é a abstração que permite que um prefab ou um documento UI Toolkit façam o mesmo trabalho:

```cs
public abstract class LoadingScreen : IDisposable
{
  protected LoadingProgress Progress { get; }
  protected void BindProgress(LoadingProgress progress);

  public abstract SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation);
  public virtual SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation);
  public virtual void ReportProgress(float progress);
  public virtual SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation);
  public virtual void Dispose();
}
```

`PrepareAsync` é o único membro que uma tela precisa escrever, mais `Dispose` se ela tiver construído alguma coisa. Exibir, ocultar e reportar são conduzidos pelo `LoadingProgress` que a tela vincula durante a preparação — um encontrado em um `LoadingBehavior` ou um criado por ela mesma — de modo que toda tela segura a transição da mesma forma, em vez de reimplementar isso. Uma tela que não vincula nada não segura nada.

`LoadingScreenHost` é uma cena gerenciada pelo pacote que existe durante uma transição. Adote nela tudo o que você construir, para que tenha onde viver além da cena que está sendo descarregada.

`SceneLoadingScreen` é a implementação embutida para telas baseadas em cena — é o que toda conversão implícita acima produz, e ela vincula o `LoadingBehavior` encontrado na cena carregada.

### Uma tela em prefab {/* #a-prefab-screen */}

A mesma hierarquia da cena de carregamento acima, instanciada em vez de carregada. Um `LoadingBehavior` em qualquer lugar do prefab é detectado por meio do `LoadingBehaviorRegistry` e segura a transição; sem ele, a tela não segura nada.

```cs
public class PrefabLoadingScreen : LoadingScreen
{
  readonly GameObject _prefab;
  GameObject _instance;

  public PrefabLoadingScreen(GameObject prefab) => _prefab = prefab;

  public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
  {
    _instance = Object.Instantiate(_prefab);
    host.Adopt(_instance);   // para a cena hospedeira, para que sobreviva ao descarregamento da cena de saída

    BindProgress(LoadingBehaviorRegistry.TryGet(_instance, out LoadingBehavior behavior) ? behavior.Progress : null);
    return SceneOperationPump.Completed(operation);
  }

  public override void Dispose()
  {
    if (_instance != null)
      Object.Destroy(_instance);
    base.Dispose();
  }
}

await MySceneManager.TransitionAsync("target", new PrefabLoadingScreen(prefab));
```

### Uma tela de documento UI Toolkit {/* #a-ui-toolkit-document-screen */}

Sem cena, sem prefab e sem `LoadingBehavior` em lugar nenhum. A tela cria seu próprio `LoadingProgress`, retém seus gates enquanto faz fade e retém a conclusão durante o seu tempo mínimo de exibição — tudo o que uma tela de carregamento precisa fazer se expressa por meio de um `LoadingProgress`, e um objeto C# comum pode ter um.

```cs
public class UIDocumentLoadingScreen : LoadingScreen
{
  public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
  {
    _instance = new GameObject(nameof(UIDocumentLoadingScreen));
    host.Adopt(_instance);

    UIDocument document = _instance.AddComponent<UIDocument>();
    document.panelSettings = _panelSettings;
    document.visualTreeAsset = _visualTree;

    _root  = document.rootVisualElement;
    _value = _root.Q<Label>("value");
    _fill  = _root.Q<VisualElement>("fill");

    LoadingProgress progress = new();
    progress.Progressed += OnProgressed;
    progress.LoadingCompleted += FadeOut;
    BindProgress(progress);

    progress.HoldShow(this);
    progress.HoldHide(this);

    progress.HoldCompletion(this);
    _root.schedule.Execute(() => progress.ReleaseCompletion(this)).StartingIn((long)(_minimumSeconds * 1000f));

    Fade(0, 1, () => progress.ReleaseShow(this));

    return SceneOperationPump.Completed(operation);
  }

  void FadeOut() => Fade(1, 0, () => Progress.ReleaseHide(this));

  public override void Dispose()
  {
    if (_instance != null)
      Object.Destroy(_instance);
    base.Dispose();
  }
}

await MySceneManager.TransitionAsync("target", new UIDocumentLoadingScreen(uxml, panelSettings));
```

Os fades rodam pelo próprio scheduler do UI Toolkit, então a tela não precisa de nenhum `MonoBehaviour` para rodar uma coroutine.

## Exemplo de Cena de Carregamento {/* #loading-scene-sample */}

Toda tela desta página está no exemplo [Loading Scene Examples](../samples/loading-scene-examples.md) como uma referência funcional e executável: a cena uGUI `Loading_Screen`, a cena UI Toolkit `Loading_Animated`, `PrefabLoadingScreen` e `UIDocumentLoadingScreen`.

[MonoBehaviour]: https://docs.unity3d.com/Manual/class-MonoBehaviour.html
[GameObject]: https://docs.unity3d.com/Manual/class-GameObject.html
[Canvas]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-Canvas.html
[CanvasScaler]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-CanvasScaler.html
[Image]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html
[Text]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html
[UI Legacy Text]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html
[UI Text Mesh Pro]: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.TextMeshProUGUI.html
[UI Slider]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
[Slider]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
[UI Canvas Group]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-CanvasGroup.html
[CanvasGroup]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-CanvasGroup.html
