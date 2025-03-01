---
sidebar_position: 3
description: Introdução básica ao uso do Advanced Scene Manager.
---

# Guia Básico

Carregar cenas com esse pacote implica que as cenas **sempre serão carregadas aditivamente**. Isso porque não há vantagem em carregar cenas no modo **Single** quando você espera trabalhar com múltiplas cenas.

Você usará a classe estática `AdvancedSceneManager` para realizar as operações de cenas.

## Carregando cenas

Você pode carregar cenas usando qualquer uma dessas referências:

```cs
// Nome
AdvancedSceneManager.LoadAsync("my-scene");
// Caminho (relativo à pasta Assets)
AdvancedSceneManager.LoadAsync("Scenes/my-scene");
// Índice de Build (build index)
AdvancedSceneManager.LoadAsync(1);
// Endereço Addressable
AdvancedSceneManager.LoadAddressableAsync("my-scene-address");
// Asset Reference
AdvancedSceneManager.LoadAddressableAsync(mySceneAssetReference);
```

Além disso, você também pode fornecer um array de cenas (do mesmo tipo de referência):

```cs
// Array de índices de build
AdvancedSceneManager.LoadAsync(new int[] { 1, 2, 3});
```

A cena carregada pode ser marcada para se tornar a cena ativa:

```cs

// Carrega uma cena e a habilita como a cena ativa
AdvancedSceneManager.LoadAsync("my-scene", true);

// Carrega uma lista de cenas e habilita a cena no índice 1 como a cena ativa
AdvancedSceneManager.LoadAsync(new int[] { 1, 2, 3 }, 1);
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
AdvancedSceneManager.LoadAsync("my-scene", true, progress);
```

## Descarregando cenas

Você pode descarregar cenas usando qualquer referência, incluindo a própria cena.

```cs
// Nome
AdvancedSceneManager.UnloadAsync("my-scene");
// Caminho (relativo à pasta Assets)
AdvancedSceneManager.UnloadAsync("Scenes/my-scene");
// Índice de Build (build index)
AdvancedSceneManager.UnloadAsync(1);
// Endereço Addressable
AdvancedSceneManager.UnloadAddressableAsync("my-scene-address");
// Asset Reference
AdvancedSceneManager.UnloadAddressableAsync(mySceneAssetReference);
// Cena
AdvancedSceneManager.UnloadAsync(AdvancedSceneManager.GetActiveScene());
```

Você também pode descarregar várias cenas:

```cs
// Array de índices de build
AdvancedSceneManager.UnloadAsync(new int[] { 1, 2, 3});
```

## Transições de Cena

Para realizar transições de cena, primeiro providencie a(s) cena(s) alvo e depois a cena intermediária (opcional).
Você pode usar as mesmas referências do método `LoadAsync`.

```cs
// Nome
AdvancedSceneManager.TransitionAsync("my-target-scene", "my-loading-scene");

// Array de AssetReference
AdvancedSceneManager.TransitionAddressableAsync(new AssetReference[] { scene1, scene2, scene3 });
```

:::info
O tipo de referência precisa ser o mesmo para a cena alvo e para a cena intermediária.
:::

## Programação Async

Todas as operações de cena são _awaitable_ e podem ser usadas em coroutines também. Por exemplo:

```cs
await AdvancedSceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
// Fazer algo após a transição
```

Para coroutines, você precisa converter a `Task` em uma `WaitTask`, que é uma struct utilitária para suportar coroutines:

```cs
yield return AdvancedSceneManager.TransitionAsync("my-target-scene", "my-loading-scene").ToWaitTask();
// Fazer algo após a transição
```