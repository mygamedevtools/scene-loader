---
sidebar_position: 3
description: Introdução básica ao uso do My Scene Manager.
---

# Guia Básico

Carregar cenas com esse pacote implica que as cenas **sempre serão carregadas aditivamente**. Isso porque não há vantagem em carregar cenas no modo **Single** quando você espera trabalhar com múltiplas cenas.

Você usará a classe estática `MySceneManager` para realizar as operações de cenas.

## Carregando cenas

Você pode carregar cenas usando qualquer uma dessas referências:

```cs
// Nome
MySceneManager.LoadAsync("my-scene");
// Caminho (relativo à pasta Assets)
MySceneManager.LoadAsync("Scenes/my-scene");
// Índice de Build (build index)
MySceneManager.LoadAsync(1);
// Endereço Addressable
MySceneManager.LoadAddressableAsync("my-scene-address");
// Asset Reference
MySceneManager.LoadAddressableAsync(mySceneAssetReference);
```

Além disso, você também pode fornecer um array de cenas (do mesmo tipo de referência):

```cs
// Array de índices de build
MySceneManager.LoadAsync(new int[] { 1, 2, 3});
```

A cena carregada pode ser marcada para se tornar a cena ativa:

```cs

// Carrega uma cena e a habilita como a cena ativa
MySceneManager.LoadAsync("my-scene", true);

// Carrega uma lista de cenas e habilita a cena no índice 1 como a cena ativa
MySceneManager.LoadAsync(new int[] { 1, 2, 3 }, 1);
```

Você pode ler o progresso da operação de carregamento providenciando uma implementação de `IProgress<float>`, por exemplo:

```cs
public class SimpleProgress : IProgress<float>
{
    public float Value;

    public void Report(float value) => Value = value;
}
// [...]

SimpleProgress progress = new SimpleProgress();
MySceneManager.LoadAsync("my-scene", true, progress);
```

## Descarregando cenas

Você pode descarregar cenas usando qualquer referência, incluindo a própria cena.

```cs
// Nome
MySceneManager.UnloadAsync("my-scene");
// Caminho (relativo à pasta Assets)
MySceneManager.UnloadAsync("Scenes/my-scene");
// Índice de Build (build index)
MySceneManager.UnloadAsync(1);
// Endereço Addressable
MySceneManager.UnloadAddressableAsync("my-scene-address");
// Asset Reference
MySceneManager.UnloadAddressableAsync(mySceneAssetReference);
// Cena
MySceneManager.UnloadAsync(MySceneManager.GetActiveScene());
```

Você também pode descarregar várias cenas:

```cs
// Array de índices de build
MySceneManager.UnloadAsync(new int[] { 1, 2, 3});
```

## Transições de Cena

Para realizar transições de cena, primeiro providencie a(s) cena(s) alvo e depois a cena intermediária (opcional).
Você pode usar as mesmas referências do método `LoadAsync`.

```cs
// Nome
MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");

// Array de AssetReference
MySceneManager.TransitionAddressableAsync(new AssetReference[] { scene1, scene2, scene3 });
```

:::info
O tipo de referência precisa ser o mesmo para a cena alvo e para a cena intermediária.
:::

Confira o Exemplo '[Loading Scene Examples](../samples/loading-scene-examples.md)' para testar diferentes tipos de cenas de carregamento durante **Transições de Cena**.

## Recerregando Cenas

Você pode recarregar a cena ativa usando o método `ReloadActiveSceneAsync`.
Um recarregamento de cena também é uma **transição de cena** internamente.
Isso irá recarregar a cena ativa pela mesma referência que a carregou inicialmente.

Assim como nas **Transições de Cena**, você também pode providenciar uma cena intermediária de carregamento.

```cs
MySceneManager.ReloadActiveSceneAsync("my-loading-scene");

// Sem cena de carregamento:
MySceneManager.ReloadActiveSceneAsync(intermediateSceneReference: null);
```

## Programação Async

Todas as operações de cena são _awaitable_ e podem ser usadas em coroutines também. Por exemplo:

```cs
await MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
// Fazer algo após a transição
```

Para coroutines, você precisa converter a `Task` em uma `WaitTask`, que é uma struct utilitária para suportar coroutines:

```cs
yield return MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene").ToWaitTask();
// Fazer algo após a transição
```