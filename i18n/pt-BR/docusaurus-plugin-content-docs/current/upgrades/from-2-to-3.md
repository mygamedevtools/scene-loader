---
sidebar_position: 1
title: De 2.x para 3.x
description: Atualize da versão 2.x para 3.x
---

# Atualizando da 2.x para 3.x

A atualização `3.x` **unificou** os scene managers addressable e não addressable em uma **implementação única**. Isso introduziu algumas mudanças com quebra de compatibilidade nas interfaces `ISceneLoader`, `ISceneManager` e `ILoadSceneInfo`.
Também foi atualizada a forma como as implementaçòes do scene loader funcionam, especialmente o `SceneLoaderCoroutine` que tem um tipo diferente de retorno.

## Principais Mudanças

* Unificação do `SceneManager` e `SceneManagerAddressable` no `AdvancedSceneManager`.
* Mudança de tipos de retorno do `ISceneLoaderCoroutine` e `SceneLoaderCoroutine` para operações de cenas.
* Mudança das implementações de scene loader para serem `readonly struct` imutáveis.
* Adição das interfaces `ISceneData` e `IAsyncSceneOperation` para tratar a complexidade entre as operações de cenas addressable e não addressable.
* Adição de testes para assegurar operaçòes de cena que utilizam `AssetReference`.
* Correção do problema de não poder transicionar diretamente a uma cena se não houver cena de carregamento.

## Mudanças no Scene Manager

### Interface `ISceneManager`

A antiga propriedade `SceneCount` foi separada em duas propriedades: `LoadedSceneCount` (contagem de cenas carregadas) e `TotalSceneCount` (contagem de cenas carregadas + descarregando)

```diff
-    int SceneCount { get; }
+    int LoadedSceneCount { get; }
+    int TotalSceneCount { get; }
```

### Advanced Scene Manager

O `AdvancedSceneManager` combina as antigas implementações de `SceneManager` e `SceneManagerAddressable` com o uso de `ISceneData` para tratar a complexidade entre as operações de cenas addressable e não addressable internamente.

```diff
-ISceneManager sceneManager = new SceneManager();
-ISceneManager sceneManagerAddressable = new SceneManagerAddressable();
+ISceneManager sceneManager = new AdvancedSceneManager();
```

#### Construtores

Você tem mais opções ao criar um `AdvancedSceneManager`. Você pode escolher incluir todas as cenas carregadas na sua inicialização, ou apenas um grupo de cenas que você queira que ele gerencie.

```cs
// Padrão, construtor vazio
ISceneManager emptyManager = new AdvancedSceneManager();

// Inicialize com todas as cenas carregadas
ISceneManager initializedManager = new AdvancedSceneManager(addLoadedScenes: true);

// Inicialize com as cenas que você quer incluir
ISceneManager customSceneManager = new AdvancedSceneManager(initializationScenes: mySceneArray);
```

### Interface `ISceneManagerReporter`

Essa interface era utilizada internamente pela assembly de teste e foi **removida**. A estrutura geral dos testes foi atualizada e não requer mais essa interface para executar.

## Mudanças no Scene Loader

### Interface `ISceneLoader`

Com a adição das cenas carregadas no construtor do `AdvancedSceneManager` e correção das transições diretas de cena, o parâmetro `externalOriginScene` foi removido.

```diff
-void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default);
+void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null);

-void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default);
+void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);
```

### Interface `ISceneLoaderAsync`

Também foi removido o parâmetro `externalOriginScene` dos métodos de transição.

```diff
-TAsyncSceneArray TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = default, Scene externalOriginScene = default);
+TAsyncSceneArray TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = default);

-TAsyncScene TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default, Scene externalOriginScene = default);
+TAsyncScene TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default);
```

### Interface `ISceneLoaderCoroutine`

Mudou o tipo de retorno de `Coroutine` para `WaitTas<Scene>` e `WaitTask<Scene[]>`.
O tipo `WaitTask` é mais flexível porque pode ser utilizado dentro de coroutines com `yield return`, pode retornar valores e causar exceptions.
Isso removeu a necessidade do `RoutineBehavior`, que também foi removido.

```diff
-public interface ISceneLoaderCoroutine : ISceneLoaderAsync<Coroutine, Coroutine> { }
+public interface ISceneLoaderCoroutine : ISceneLoaderAsync<WaitTask<Scene>, WaitTask<Scene[]>> { }
```

Para esperar operações de coroutine, você pode apenas usar `yield return` dentro de uma coroutine:

```cs
public Coroutine TransitionToSceneAndExecute()
{
    return StartCoroutine(transitionToSceneAndExecuteRoutine());

    IEnumerator transitionToSceneAndExecuteRoutine()
    {
        yield return sceneLoaderCoroutine.TransitionToSceneAsync(targetSceneInfo, loadingSceneInfo);
        // Execute lógica customizada após a transição
    }
}
```

### Implementações de Scene Loader

Como todos os scene loaders são basicamente implementações de `ISceneLoaderAsync` com diferentes tipos de retorno, eles foram simplificados para contarem com uma única implementação.
O `SceneLoaderCoroutine` e `SceneLoaderUniTask` agora usam uma instância interna de um `SceneLoaderAsync`.
Isso _provavelmente_ será atualizado em uma futura versão para unificar melhor os tipos de retorno.

#### `readonly struct`

Como os scene loaders não contém nenhum estado e apenas operam a sua referência `readonly ISceneManager`, eles foram convertidos para `readonly struct`.
That does not change how you use the scene loaders.

:::info[Importante]
É recomendado que você salve uma referência à interface `ISceneLoader` ou `ISceneLoaderAsync` nos seus sistemas ao invés de um tipo implementado como `SceneLoaderAsync`.
:::

## Mudanças em Load Scene Info

Para unificar os fluxos addressable e não addressable, o `ISceneInfo` teve algumas mudanças para refletir o comportamento esperado.
O método `IsReferenceToScene` foi refatorado para `CanBeReferenceToScene` porque:

1. Apenas o `LoadSceneInfoScene` é uma referência direta a uma cena carregada.
2. O `LoadSceneInfoName` e `LoadSceneInfoIndex` podem referencias uma cena carregada, mas não podem ser uma referência direta se houverem várias cenas carregadas com o mesmo nome ou índice de build.
3. Os tipos `ILoadSceneInfo` addressable podem ser conectados às suas cenas carregadas através de seu `AsyncOperationHandle`, não acessível pela interface `ILoadSceneInfo`.

### Enum `LoadSceneInfoType`

O enum `LoadSceneInfoType` foi criado para simplificar a interpretação de um `ILoadSceneInfo`, permitindo a troca de várias conversões no pior caso para apenas uma conversão do valor de `Reference`.
Se você precisa trabalhar com uma implementação customizada do `ISceneManager` e `ILoadSceneInfo`, você pose usar o valor `LoadSceneInfoType.Other`.

### `LoadSceneInfoAddress`

A implementação do `LoadSceneInfoAddress` foi adicionada para operações de cena addressable.
Na versão anterior, você poderia passar um `LoadSceneInfoName` com o addressable address de uma cena para o `SceneManagerAddressable`.
Isso **não é mais suportado**.
Você deve usar o `LoadSceneInfoAddress` para carregar uma cena pelo seu addressable address no `AdvancedSceneManager`.
O `LoadSceneInfoName` **só pode** ser utilizado para carregar uma cena pelo seu nome/caminho que foi adicionada ao build settings.

## Novidades

A unificação dos fluxos addressable e não addressable permitiram a adição das estruturas `ISceneData` e `IAsyncSceneOperation`, que ajudam a tratar a complexidade desses dois fluxos.
Essas novas adições são usadas internamente pelo `AdvancedSceneManager` e não requerem a sua interação, a menos que você tenha que criar uma implementação customizada do `ISceneManager`.

### `IAsyncSceneOperation`

Essa interface é utilizada para guardar uma referência de [AsyncOperation](https://docs.unity3d.com/ScriptReference/AsyncOperation.html) (non-addressable) ou [AsyncOperationHandle](https://docs.unity3d.com/Packages/com.unity.addressables@2.1/manual/AddressableAssetsAsyncOperationHandle.html) (addressable).

```cs
public interface IAsyncSceneOperation
{
    float Progress { get; }

    bool IsDone { get; }

    bool HasDirectReferenceToScene { get; }

    Scene GetResult();
}
```

A propriedade `HasDirectReferenceToScene` determina se essa `IAsyncSceneOperation` pode ser utilizado para conectar a uma cena carregada por meio de um `ILoadSceneInfo`.
Retorna o valor `true` para operaçòes addressable e `false` caso contrário.
Isso também sugere que o método `GetResult()` pode retornar uma `Scene` válida.

### `ISceneData`

Essa interface é utilizada para guardar uma referência de um `ILoadSceneInfo`, sua `IAsyncSceneOperation`, e sua `Scene` carregada.
Ela pode disparar o carregamento e descarregamento de cenas.
O `AdvancedSceneManager` depende em grande medida na `ISceneData` para controlar as cenas carregadas e como as operaçòes de carregamento e descarregamento modificam o estado geral das cenas.

```cs
public interface ISceneData
{
    IAsyncSceneOperation AsyncOperation { get; }

    ILoadSceneInfo LoadSceneInfo { get; }

    Scene SceneReference { get; }

    void SetSceneReferenceManually(Scene scene);

    void UpdateSceneReference();

    IAsyncSceneOperation LoadSceneAsync();

    IAsyncSceneOperation UnloadSceneAsync();
}
```

## Conclusão

Na transição da versão `1.x` para `2.x` houve a unificação das implementações addressable e não addressable de `ILoadSceneInfo`.
Agora também unificamos as implementações de `ISceneManager`.
É muito provável esperar que a implementação atual de `ISceneLoaderAsync` também mude no futuro próximo, para melhorar ainda mais a experiência de usuário.
Esperamos que essas mudanças melhorem a confiabilidade do pacote e o torne mais amigável para novos usuários.