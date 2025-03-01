---
sidebar_position: 3
title: Scene Loader
---

# O Scene Loader

O **Scene Loader** é um objeto que você usará para carregar cenas no seu jogo. Ele funciona como uma camada acima do **Scene Manager**, mas adiciona as operações de **Transição de Cenas**.
Existem duas interfaces base para **Scene Loaders**: uma com uma referência ao `ISceneManager` que será utilizado, e uma interface `async` para permitir o `await` das operações.

## Interfaces `ISceneLoader`

A interface `ISceneLoader` define:

```cs
public interface ISceneLoader : IDisposable
{
  ISceneManager Manager { get; }

  void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null);

  void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

  void UnloadScenes(ILoadSceneInfo[] sceneInfos);

  void UnloadScene(ILoadSceneInfo sceneInfo);

  void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1);

  void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false);
}
```

E a `ISceneLoaderAsync`:

```cs
public interface ISceneLoaderAsync<TAsyncScene, TAsyncSceneArray> : ISceneLoader
{
  TAsyncSceneArray TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = default, CancellationToken token = default);
  
  TAsyncScene TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default, CancellationToken token = default);

  TAsyncSceneArray LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null, CancellationToken token = default);

  TAsyncScene LoadSceneAsync(ILoadSceneInfo sceneReference, bool setActive = false, IProgress<float> progress = null, CancellationToken token = default);

  TAsyncSceneArray UnloadScenesAsync(ILoadSceneInfo[] sceneReferences, CancellationToken token = default);

  TAsyncScene UnloadSceneAsync(ILoadSceneInfo sceneReference, CancellationToken token = default);
}
```
Note que a interface `ISceneLoaderAsync` herda de `ISceneLoader`.
O tipo `TAsyncScene` deve retornar uma instância de `Scene`, e pode ser qualquer coisa que você queira dar `await`, por exemplo, `Task<Scene>`, `ValueTask<Scene>` ou `UniTask<Scene>`, enquanto o `TAsyncSceneArray` deve retornar uma instância `Scene[]`, como `Task<Scene[]>`, `ValueTask<Scene[]>` ou `UniTask<Scene[]>`.

## Implementações de Scene Loader

O pacote vem com **uma** implementação base e **duas** implementações _wrapper_:
* O `SceneLoaderAsync`, que assim como a implementação do `ISceneManager`, retornará valores `ValueTask`.
* O `SceneLoaderCoroutine`, que utiliza o `SceneLoaderAsync` mas retorna uma `WaitTask` que pode ser usada em coroutines.
* O `SceneLoaderUniTask`, que utiliza o `SceneLoaderAsync` mas retorna valores `UniTask`.

Todas implementações tem interfaces para simplificar seu código:

```cs
public interface ISceneLoaderCoroutine : ISceneLoaderAsync<WaitTask<Scene>, WaitTask<Scene[]>> { }

public interface ISceneLoaderAsync : ISceneLoaderAsync<ValueTask<Scene>, ValueTask<Scene[]>> { }

public interface ISceneLoaderUniTask : ISceneLoaderAsync<UniTask<Scene>, UniTask<Scene[]>> { }
```

A propriedade `Manager` pode ser utilizada para escutar os eventos `SceneLoaded`, `SceneUnloaded`, e `ActiveSceneChanged`.
Tanto o método `LoadSceneAsync` quanto `UnloadSceneAsync` vão apenas chamar os equivalentes do `ISceneManager`, enquanto `LoadScene` e `UnloadScene` farão o mesmo mas sem o `await`.
É importante entender que `LoadScene`, `UnloadScene` e `TransitionToScene` ainda serão chamados como operações assíncronas, ao invés de bloquear a execução até sua conclusão.
Você pode usar os eventos do `ISceneManager` para reagir à conclusão desses métodos.