---
sidebar_position: 3
description: Introdução básica ao uso do My Scene Manager.
---

# Guia Básico

Ao carregar cenas com este pacote, elas **sempre serão carregadas de forma aditiva**. Isso porque simplesmente não há vantagem em carregar cenas no modo **Single** quando você pretende trabalhar com várias cenas.

Você usará a classe estática `MySceneManager` para realizar as operações de cena.

## Carregando cenas {/* #loading-scenes */}

Você pode carregar cenas usando qualquer uma dessas referências:

```cs
// Nome
MySceneManager.LoadAsync("my-scene");
// Caminho (relativo à pasta Assets)
MySceneManager.LoadAsync("Scenes/my-scene");
// Índice de Build (build index)
MySceneManager.LoadAsync(1);
// Endereço Addressable
MySceneManager.LoadAsync(SceneRef.Address("my-scene-address"));
// Asset Reference
MySceneManager.LoadAsync(mySceneAssetReference);
```

:::info
Não existe uma API addressable separada. Uma simples string é procurada primeiro no seu **Build Settings** e depois no Addressables — então `LoadAsync("my-scene")` encontra sua cena onde quer que ela esteja.

`SceneRef.Address(...)` é a forma de forçar o endereço, para quando um nome existe nos dois lugares ou quando você quer pular a busca. Veja [Scene Ref](../advanced-usage/scene-ref.md#how-a-string-is-resolved).
:::

Você também pode passar um array de cenas:

```cs
// Array de índices de build
MySceneManager.LoadAsync(new int[] { 1, 2, 3 });
// Misturar tipos também funciona
MySceneManager.LoadAsync(new SceneRef[] { "scene-a", 2, SceneRef.Address("scene-c") });
```

A cena carregada pode ser marcada para se tornar a cena ativa, por meio de `SceneParameters`:

```cs
// Carrega uma cena e a habilita como a cena ativa
MySceneManager.LoadAsync(new SceneParameters("my-scene", true));

// Carrega uma lista de cenas e habilita a cena no índice 1 como a cena ativa
MySceneManager.LoadAsync(new SceneParameters(new SceneRef[] { 1, 2, 3 }, 1));
```

Toda operação retorna um handle imediatamente, e o progresso vem dele:

```cs
SceneOperation op = MySceneManager.LoadAsync("my-scene");
op.Progressed += value => progressBar.value = value;
```

## Descarregando cenas {/* #unloading-scenes */}

Você pode descarregar cenas usando qualquer referência, incluindo a própria cena.

```cs
// Nome
MySceneManager.UnloadAsync("my-scene");
// Caminho (relativo à pasta Assets)
MySceneManager.UnloadAsync("Scenes/my-scene");
// Índice de Build (build index)
MySceneManager.UnloadAsync(1);
// Endereço Addressable
MySceneManager.UnloadAsync(SceneRef.Address("my-scene-address"));
// Asset Reference
MySceneManager.UnloadAsync(mySceneAssetReference);
// Cena
MySceneManager.UnloadAsync(MySceneManager.GetActiveScene());
```

Você também pode descarregar várias cenas:

```cs
// Array de índices de build
MySceneManager.UnloadAsync(new int[] { 1, 2, 3 });
```

## Transições de Cena {/* #scene-transitions */}

Para realizar transições de cena, primeiro passe a(s) cena(s) de destino e depois a tela de carregamento (opcional).
Você pode usar as mesmas referências do método `LoadAsync`.

```cs
// Nome
MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");

// Array de AssetReference
MySceneManager.TransitionAsync(new AssetReference[] { scene1, scene2, scene3 });
```

:::info
As cenas de destino e a tela de carregamento são resolvidas de forma independente, então não precisam ser do mesmo tipo de referência — carregar uma cena por índice de build enquanto exibe uma tela de carregamento nomeada por string funciona normalmente.

A tela de carregamento nem precisa ser uma cena — veja [Telas de Carregamento](./loading-screens.md).
:::

Confira o exemplo [Loading Scene Examples](../samples/loading-scene-examples.md) para testar diferentes telas de carregamento em **Transições de Cena**.

## Recarregando Cenas {/* #scene-reloading */}

Você pode recarregar a cena ativa usando o método `ReloadActiveSceneAsync`.
Um recarregamento de cena também é uma **transição de cena** internamente.
Ela recarrega a cena ativa usando a mesma referência com que a cena foi carregada inicialmente.

Assim como nas **Transições de Cena**, você também pode passar uma tela de carregamento.

```cs
MySceneManager.ReloadActiveSceneAsync("my-loading-scene");

// Sem tela de carregamento:
MySceneManager.ReloadActiveSceneAsync();
```

## Programação Async {/* #async-programming */}

Toda operação retorna uma [`SceneOperation`](../advanced-usage/scene-operation.md) imediatamente — um handle sobre o trabalho, que você pode usar com `await` diretamente:

```cs
await MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
// Fazer algo após a transição
```

Para coroutines, use `ToCoroutine()`:

```cs
yield return MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene").ToCoroutine();
// Fazer algo após a transição
```

E se uma API de terceiros precisar de uma `Task`, `AsTask()` faz a conversão:

```cs
Task<SceneResult> task = MySceneManager.LoadAsync("my-scene").AsTask();
```

## Cancelando {/* #cancelling */}

Você cancela pelo handle, em vez de passar um token na chamada:

```cs
SceneOperation op = MySceneManager.LoadAsync("my-scene");
op.Cancel();

// Ou conecte um token que você já tem:
MySceneManager.LoadAsync("my-scene").CancelWith(destroyCancellationToken);
```

:::warning
Cancelar interrompe as notificações **desta operação**, suas fases restantes e quem estiver aguardando por ela. O carregamento interno do Unity ainda roda até o fim: uma cena que a engine já começou a carregar não pode ser abortada.
:::
