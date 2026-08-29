---
sidebar_position: 6
title: Scene Operation
description: Entenda o handle SceneOperation retornado por toda operação.
---

# Scene Operation

Toda operação retorna um `SceneOperation` — **de forma síncrona**, antes de o trabalho começar. Ele é um handle vivo sobre esse trabalho: em que fase está, quanto já avançou, o que produziu e como esperar por ele.

## Por que um handle e não uma Task {/* #why-a-handle-and-not-a-task */}

Uma `Task` te dá uma coisa: o resultado final. Qualquer outra coisa que você queira saber sobre um carregamento de cena — quanto já avançou, em que fase está, se ainda dá para pará-lo — precisa ser decidida *antes* da chamada, como parâmetros extras.

Já um `SceneOperation` é algo que você segura, então tudo isso é conectado depois da chamada:

```cs
SceneOperation op = MySceneManager.TransitionAsync("target", "loading");

op.Progressed   += progress => bar.value = progress;
op.StateChanged += o => { if (o.State == SceneOperationState.ScreenOut) BeginIntro(); };

SceneResult result = await op;
```

É por isso que nenhum dos quatro métodos recebe um parâmetro de progresso ou de cancelamento: há um lugar melhor para colocá-los.

## Esperando por ele {/* #waiting-for-it */}

Três formas, todas suportadas pelo mesmo handle:

```cs
SceneResult result = await op;             // direto, sem alocar Task
yield return op.ToCoroutine();             // de uma coroutine; falhas relançam a exceção
Task<SceneResult> task = op.AsTask();      // ponte para interoperar com terceiros
```

`await op` é o caminho principal. `GetAwaiter()` retorna um `SceneOperationAwaiter` sobre a própria lista de continuações da operação — sem `Task`, sem `Awaitable`. Como o pump roda no player loop, as continuações retomam na thread principal por construção, sem ida e volta pelo `SynchronizationContext`.

Ele também é **re-awaitable** — dar await duas vezes retorna o mesmo resultado, e `op.Result` continua legível após a conclusão. É justamente por isso que `Awaitable` não é usado internamente: seus objetos voltam para um pool depois de um único await.

:::info
`AsTask()` é uma conveniência, não um pilar do design. Ele custa um `TaskCompletionSource` por chamada e `await op` não, então recorra a ele apenas quando uma API de terceiros exigir uma `Task`.
:::

## O que ele reporta {/* #what-it-reports */}

| Membro | |
|---|---|
| `Kind` | Qual operação é esta — `Load`, `Unload`, `Transition`, `Reload`, `Composite` |
| `State` | A fase em que está |
| `Progress` | De 0 a 1 |
| `Result` | As cenas produzidas, vazio até a conclusão |
| `Exception` | Por que falhou, ou `null` |
| `IsDone` | Se terminou, com sucesso ou não |

E os eventos:

| Evento | |
|---|---|
| `Progressed` | Dispara quando `Progress` muda. Não é disparado para valores inalterados. |
| `StateChanged` | Dispara a cada mudança de `State` |
| `SceneLoaded` / `SceneUnloaded` | Uma vez por cena |
| `Completed` | Uma vez, ao terminar — seja sucesso, cancelamento ou falha. Inscrever-se depois da conclusão o invoca imediatamente. |

:::note
Um inscrito que lança exceção é reportado através do [`SceneManagerLog`](./logging.md) e contido. Ele não vai fazer a operação falhar, nem vai impedir os outros inscritos ou os awaiters de rodar.
:::

### Estados {/* #states */}

`Pending` → `Resolving` → `ScreenIn` → `Unloading` → `Loading` → `Activating` → `ScreenOut` → `Completed`, com `Canceled` e `Faulted` como alternativas terminais.

Quais deles você vê depende da operação — um carregamento simples nunca chega a `ScreenIn`. A ordem segue o fluxo da transição, e é por isso que `Unloading` vem antes de `Loading`: a cena de origem vai embora assim que a tela de carregamento estiver visível, antes de a cena de destino entrar.

Então "a tela de carregamento terminou o fade out, comece a cutscene" é um estado no qual você se inscreve:

```cs
op.StateChanged += o =>
{
  if (o.State == SceneOperationState.ScreenOut)
    BeginIntroCutscene();
};
```

## Cancelando {/* #cancelling */}

```cs
op.Cancel();
op.CancelWith(destroyCancellationToken);   // a ponte opcional
```

:::warning
**As operações subjacentes da Unity continuam rodando.** Uma cena que a engine já começou a carregar não pode ser abortada, então ela vai terminar. O que para é o relato desta operação, as fases restantes e os awaiters dela.
:::

## Combinando {/* #combining */}

```cs
SceneOperation both = SceneOperation.WhenAll(first, second);
SceneOperation any  = SceneOperation.WhenAny(first, second);
```

Prefira estes a um `Task.WhenAll` em cima de `AsTask()`: eles rodam sobre as listas de continuações das próprias operações, então não alocam uma `Task` por operação.

## Progresso {/* #progress */}

`Progress` é a média entre todas as cenas da operação.

:::warning
Um grupo que mistura backends avança de forma **desigual**. Os Addressables incluem o tempo de download no seu progresso e o caminho padrão não, então um grupo misto não é uma linha reta. Trate-o como uma barra de progresso, não como um relógio.
:::

:::note
`SceneOperation` deliberadamente **não é pooled**. Esta API incentiva você a manter o handle — `op.Result` após a conclusão e aguardar duas vezes são ambos suportados — então nada tem como saber quando ele está livre. É uma alocação pequena por operação, diante das dezenas de kilobytes que um carregamento de cena custa. Os buffers por operação *são* pooled.
:::
