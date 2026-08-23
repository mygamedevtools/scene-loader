---
sidebar_position: 3
title: Cenas de Carregamento
description: Como criar cenas de carregamento com o pacote.
---

# Criando Cenas de Carregamento

Durante a transição de cenas, você tem a opção de providenciar uma cena intermediária que pode ser utilizada como uma cena de carregamento.
Isso pode ser uma splash screen animada ou uma barra de progresso, por exemplo.
Esse pacote oferece implementações para te ajudar a desenvolver suas telas de carregamento mais rápido.

## Exemplo

Considere a hierarquia da seguinte cena de tela de carregamento como exemplo:

* Canvas - ([Canvas](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-Canvas.html), [CanvasScaler](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-CanvasScaler.html), `LoadingBehavior`)
  * Group - ([CanvasGroup], `LoadingFader`)
    * Background - ([Image](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html))
    * Text_Message - ([TextMeshProUGUI])
    * Slider_Progress - ([Slider], `LoadingFeedbackSlider`)
      * Text_Progress - ([TextMeshProUGUI], `LoadingFeedbackTextMeshPro`)

Por meio dessa hierarquia na sua cena de carregamento, seria possível realizar fade in/out e mostrar tanto a barra de progresso e um feedback em texto do progresso.
Como essa cena tem o componente `LoadingFader`, lembre-se de habilitar tanto o toggle `WaitForScriptedStart` e `WaitForScriptedEnd` no componente `LoadingBehavior.

Você pode testar essa cena passando sua referência `ILoadSceneInfo` como o parâmetro `intermediateSceneInfo` em um método `ISceneLoader.TransitionToScene`.

## Componentes de Carregamento

### O Loading Behavior

O Loading Behavior é um componente [MonoBehaviour], que você pode acoplar em [GameObjects] do Unity, que recebem um valor de progresso do scene manager.
Você **precisa** adicionar um componente `LoadingBehavior` a um [GameObject] na sua cena de carregamento para poder mostrar feedback de carregamento de cenas.
Ele expõe sua instância de `LoadingProgress`, que você pode usar para escutar aos eventos de carregamento:

```cs
public class LoadingProgress : IProgress<float>
{
  public event Action<float> Progressed;
  public event Action LoadingCompleted;
}
```

O evento `LoadingCompleted` notifica que a operação de carregamento de cena completou, mas a cena de carregamento ainda está ativa.
O evento `Progressed` envia um valor `float`, de 0 a 1, para reportar o progresso da operação de carregamento de cena.

De volta ao `LoadingBehavior`, ele tem algumas opções que você pode ajustar no [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) do Unity:

* **Wait For Scripted Start**: habilite se a transição de cenas terão um efeito de **transition in**, como um fade in.
* **Wait For Scripted End**: habilite se a transição de cenas terão um efeito de **transition out**, como um fade out.

### O Feedback de Carregamento

Nesse ponto, você já deve ter sua cena de carregamento com um `LoadingBehavior` acoplado a um dos seus [GameObjects].
Agora você pode adicionar outros componentes para mostrar o feedback de carregamento.
Esse pacote vem com **três** feedbacks:

* `LoadingFeedbackSlider`: adicione a um [UI Slider] para mostrar o feedback de carregamento como uma barra de progresso.
* `LoadingFeedbackTextMeshPro`: adicione a um [UI Text Mesh Pro] para mostrar o feedback de carregamento em forma de text normalizado de 0 a 100.
* `LoadingFeedbackText` _(também conhecido como Legacy)_: adicione a um [UI Legacy Text](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html) para mostrar o feedback de carregamento em forma de text normalizado de 0 a 100.

Você pode usar uma combinação desses componentes de feedback em uma cena de carregamento.
Lembre de associar o campo `LoadingBehavior` desses componentes ao componente `LoadingBehavior` que você criou antes.

### Loading Fader

O componente `LoadingFader` realiza transições de **fade in/out**.
Adicione-o a um [GameObject] com [UI Canvas Group] para controlar o valor de alpha do grupo durante as transições visuais.
Você também pode controlar o tempo de fade e parametrizar as curvas de animação do fade in/out de acordo com a sua preferência.

Para usar efetivamente o `LoadingFader`, você deve **habilitar** os toggles `WaitForScriptedStart` e `WaitForScriptedEnd` no seu componente `LoadingBehavior`.

## Exemplo de Telas de Carregamento

Você pode testar várias telas de carregamento no Exemplo '[Loading Scene Examples](../samples/loading-scene-examples.md)'.

[MonoBehaviour]: https://docs.unity3d.com/Manual/class-MonoBehaviour.html
[MonoBehaviours]: https://docs.unity3d.com/Manual/class-MonoBehaviour.html
[ScriptableObject]: https://docs.unity3d.com/Manual/class-ScriptableObject.html
[GameObject]: https://docs.unity3d.com/Manual/class-GameObject.html
[GameObjects]: https://docs.unity3d.com/Manual/class-GameObject.html
[UI Text Mesh Pro]: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.TextMeshProUGUI.html
[TextMeshProUGUI]: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.TextMeshProUGUI.html
[UI Slider]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
[Slider]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
[UI Canvas Group]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-CanvasGroup.html
[CanvasGroup]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-CanvasGroup.html