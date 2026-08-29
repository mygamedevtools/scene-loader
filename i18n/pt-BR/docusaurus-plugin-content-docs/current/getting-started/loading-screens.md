---
sidebar_position: 3
title: Telas de Carregamento
description: Como criar telas de carregamento com o pacote.
---

# Criando Telas de Carregamento

Durante as transições de cena, você tem a opção de providenciar uma tela de carregamento — uma splash screen animada ou uma barra de progresso, por exemplo.

Uma tela de carregamento é um `LoadingScreen`. A mais simples é uma cena, e nomear uma cena te dá isso de graça:

```cs
MySceneManager.TransitionAsync("target", "loading");                                  // uma cena
MySceneManager.TransitionAsync("target", new PrefabLoadingScreen(prefab));            // um prefab
MySceneManager.TransitionAsync("target", new UIDocumentLoadingScreen(uxml, panel));   // um documento UI Toolkit
```

Um nome de cena, caminho, endereço, índice de build, `Scene` ou `AssetReference` são todos convertidos implicitamente em uma tela de carregamento baseada em cena, então você só escreve um `LoadingScreen` por conta própria quando quer algo que *não* é uma cena.

Seja qual for a tela, ela controla a transição da mesma forma, por meio de um `LoadingProgress`. Esta página começa com uma cena de carregamento construída a partir dos componentes do pacote, explica os portões que esses componentes usam e, em seguida, mostra como os mesmos portões conduzem um prefab ou um documento UI Toolkit.

## Uma cena de carregamento {/* #a-loading-scene */}

Considere a hierarquia da seguinte cena de carregamento como exemplo — é a cena `Loading_Screen` do exemplo [Loading Scene Examples](../samples/loading-scene-examples.md):

* Loading Screen - ([Canvas], [CanvasScaler], [CanvasGroup], `LoadingBehavior`, `LoadingFader`, `MinimumDisplayTime`)
  * Backdrop - ([Image])
  * Card - ([Image])
    * Value - ([Text], `LoadingFeedbackText`)
    * Track - ([Slider], `LoadingFeedbackSlider`)
      * Fill - ([Image])

Com essa hierarquia na sua cena de carregamento, ela faz fade in, mostra tanto uma barra de progresso quanto uma porcentagem de progresso, permanece visível por pelo menos alguns segundos e faz fade out assim que a cena de destino tiver carregado.

Nada é conectado no Inspector: todo componente abaixo do `LoadingBehavior` o encontra nos seus pais, e cada um que precisa que a transição espere — o fader, o tempo mínimo de exibição — faz sua própria retenção. A transição espera por quem liberar por último.

Você pode testar essa cena passando seu nome, caminho ou índice de build como segundo argumento de `TransitionAsync`.

:::tip
A cena de carregamento não precisa ser uGUI. A cena `Loading_Animated` do exemplo é um `UIDocument` de UI Toolkit com o mesmo `LoadingBehavior` — veja [Componentes personalizados](#custom-components) abaixo.
:::

## Componentes de Carregamento {/* #loading-components */}

### O Loading Behavior {/* #the-loading-behavior */}

O `LoadingBehavior` é um componente [MonoBehaviour] que ancora o `LoadingProgress` da tela. Coloque um na raiz da sua tela de carregamento e todo o resto — feedbacks, fades, animações — se pendura no seu `Progress`:

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
O evento `LoadingCompleted` notifica quando a operação de carregamento de cena foi concluída, mas a tela de carregamento ainda está ativa — é o sinal para a tela começar a se esconder.

:::info[Como ele é encontrado]
Um `LoadingBehavior` se registra quando é **habilitado**, sob a cena em que vive — ou, para uma tela em prefab, sob a hierarquia em que foi instanciado. Duas consequências que vale a pena conhecer:

* Um `LoadingBehavior` em um GameObject **desabilitado** nunca é encontrado, e a transição roda sem feedback e sem espera, em vez de reportar um problema.
* **Um por tela de carregamento.** Se uma cena contiver dois, a transição registra um aviso e conduz o primeiro que se registrou.
:::

:::note
Um `LoadingBehavior` é **opcional**. Uma cena de carregamento sem um ainda funciona como tela de carregamento — você simplesmente não recebe feedback de progresso, e a tela aparece exatamente pelo tempo que o carregamento levar.
:::

### Portões e retenções {/* #gates-and-holds */}

A transição espera em dois **portões**: o portão de *exibição*, antes de descarregar a cena de onde você veio, e o portão de *ocultação*, antes de considerar que a tela de carregamento se foi. Ambos estão **abertos, a menos que algo os esteja retendo fechados**.

Qualquer coisa que precise que a transição espere — um fade, uma animação, um script — chama `HoldShow` ou `HoldHide` passando a si mesma como dona, e libera quando termina. O portão abre quando o último detentor solta, e é isso que permite que vários componentes controlem a mesma transição sem que nenhum deles saiba dos outros.

```cs
void Awake()
{
    // Faça as retenções antes que a transição possa ler os portões.
    _loadingBehavior.Progress.HoldShow(this);
    _loadingBehavior.Progress.HoldHide(this);
    _loadingBehavior.Progress.LoadingCompleted += PlayOut;

    PlayIn();
}

void OnPlayInFinished()  => _loadingBehavior.Progress.ReleaseShow(this);
void OnPlayOutFinished() => _loadingBehavior.Progress.ReleaseHide(this);
```

As retenções são identificadas pelo seu dono, então reter duas vezes e liberar duas vezes são ambos inofensivos. Faça as retenções em `Awake` ou `OnEnable`: uma feita mais tarde pode chegar depois que a transição já tiver lido o portão.

Existe uma terceira retenção, `HoldCompletion`, que atrasa o **sinal** `LoadingCompleted` em vez de um portão. Reter o portão de ocultação atrasa a *transição* enquanto a tela já foi avisada para ir embora, então um fade out roda até o fim e o resto da espera acontece em uma tela vazia. Reter a conclusão mantém a tela visível, e o que quer que a leve embora começa quando deveria. É isso que um [tempo mínimo de exibição](#minimum-display-time) quer.

:::note
Para esperar pelos portões por conta própria, use `WaitForShowAsync()` e `WaitForHideAsync()`, ou leia as propriedades `IsShown` / `IsHidden`.
:::

:::warning
Se você fizer uma retenção e nunca liberá-la, a transição espera. Ela não vai falhar silenciosamente: depois de 10 segundos, um development build nomeia o detentor e continua esperando. Um detentor que é destruído sem liberar é descartado, em vez de ficar bloqueando para sempre.
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
Você também pode definir o tempo de fade e personalizar as curvas de animação do fade in/out de acordo com a sua preferência.

Ele retém ambos os portões pela duração de cada fade, então adicionar o componente é, por si só, a declaração de que a transição deve esperar pelos fades — não há nada para habilitar no `LoadingBehavior`.

### Componentes personalizados {/* #custom-components */}

Os feedbacks e o fader estendem `LoadingScreenComponent`, a base para qualquer coisa que vive em uma tela de carregamento e conduz, ou espera por, o seu `LoadingProgress`. Ele resolve o `LoadingBehavior` para você e chama `OnBound` assim que o `Progress` estiver disponível — que é onde você se inscreve nos eventos e faz suas retenções.

Um feedback são poucas linhas:

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

Uma animação pela qual a transição precisa esperar tem o mesmo formato, mais as retenções. Esta é a cena `Loading_Animated` do exemplo — um documento UI Toolkit cujos painéis deslizam até se encontrarem, e deslizam de volta assim que o carregamento termina:

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

Cada portão é liberado quando o seu deslize **terminou**, não quando começa, então a cena de saída nunca é descarregada atrás de uma cortina que ainda está abrindo.

### Tempo mínimo de exibição {/* #minimum-display-time */}

Uma cena que carrega em dois frames produz uma tela de carregamento que pisca, o que parece um bug. O componente `MinimumDisplayTime` do exemplo mantém uma tela visível por pelo menos um tempo definido, e é o motivo pelo qual `HoldCompletion` existe:

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

Coloque-o ao lado de um `LoadingBehavior` e o sinal `LoadingCompleted` espera por ele — o fader, ou o que quer que leve a tela embora, começa assim que tanto o carregamento quanto o temporizador tiverem terminado.

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

`PrepareAsync` é o único membro que uma tela precisa escrever, mais `Dispose` se ela tiver construído alguma coisa. Exibir, ocultar e reportar são conduzidos pelo `LoadingProgress` que a tela vincula enquanto se prepara — um encontrado em um `LoadingBehavior`, ou um que ela cria para si mesma — para que toda tela controle a transição da mesma forma, em vez de reimplementá-la. Uma tela que não vincula nada não controla nada.

`LoadingScreenHost` é uma cena de propriedade do pacote que existe pela duração de uma transição. Adote nele o que quer que você construa, para que tenha um lugar para viver que não seja a cena sendo descarregada.

`SceneLoadingScreen` é a implementação embutida para telas baseadas em cena — é o que toda conversão implícita acima produz, e ele vincula o `LoadingBehavior` encontrado na cena carregada.

### Uma tela em prefab {/* #a-prefab-screen */}

A mesma hierarquia da cena de carregamento acima, instanciada em vez de carregada. Um `LoadingBehavior` em qualquer lugar do prefab é detectado por meio do `LoadingBehaviorRegistry` e controla a transição; sem um, a tela não segura nada.

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

Sem cena, sem prefab e sem `LoadingBehavior` em lugar nenhum. A tela cria seu próprio `LoadingProgress`, retém seus portões enquanto faz fade e retém a conclusão pelo seu tempo mínimo de exibição — tudo o que uma tela de carregamento precisa fazer é expresso por meio de `LoadingProgress`, e um objeto C# comum pode ter um.

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

## Exemplo de Cenas de Carregamento {/* #loading-scene-sample */}

Toda tela desta página está no exemplo [Loading Scene Examples](../samples/loading-scene-examples.md) como uma referência funcional e executável: a cena uGUI `Loading_Screen`, a cena UI Toolkit `Loading_Animated`, `PrefabLoadingScreen`, `UIDocumentLoadingScreen` e `MinimumDisplayTime`.

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
