---
sidebar_position: 2
title: My Scene Manager
---

# My Scene Manager

O `MySceneManager` é uma classe estática que engloba a classe `CoreSceneManager`, que existe para simplificar a experiência de uso das **Operações de Cena**.
Ele gerencia uma referência interna ao Core Scene Manager que é criado durante o _callback_ `RuntimeInitializeOnLoadMethod`, que é executado depois que a primeira cena é carregada e depois do primeiro ciclo de `Awake()`.
Isso significa que o `MySceneManager` não será inicializado até o primeiro ciclo de `Start()`.

```cs
[RuntimeInitializeOnLoadMethod]
internal static void Initialize()
{
  _instance = new CoreSceneManager(true);
}
```

## API Estática {/* #static-api */}

Você tem a opção de desabilitar a classe estática `MySceneManager` completamente se deseja controlar manualmente o ciclo de vida do `CoreSceneManager` e/ou estender sua funcionalidade.
Para fazer isso, apenas defina o _scripting symbol_ `DISABLE_STATIC_SCENE_MANAGER` nas suas configurações de compilação.

## Os quatro métodos {/* #the-four-methods */}

Como a instância interna de `CoreSceneManager` não é exposta, ele espelha as mesmas quatro operações de forma estática:

```cs
MySceneManager.LoadAsync(sceneParameters);
MySceneManager.UnloadAsync(sceneParameters);
MySceneManager.TransitionAsync(sceneParameters, loadingScreen);
MySceneManager.ReloadActiveSceneAsync(loadingScreen);
```

Uma assinatura por operação cobre qualquer tipo de referência, porque tanto `SceneParameters` quanto `LoadingScreen` convertem implicitamente:

```cs
MySceneManager.LoadAsync("my-scene");                     // string
MySceneManager.LoadAsync(1);                              // índice de build
MySceneManager.LoadAsync(new[] { "scene-a", "scene-b" }); // várias
MySceneManager.TransitionAsync("target", "loading");      // com uma tela de carregamento
```

## Eventos {/* #events */}

O `MySceneManager` repassa os mesmos eventos da API de instância:

| Evento | |
|---|---|
| `SceneLoaded` / `SceneUnloaded` | Uma vez por cena |
| `ActiveSceneChanged` | A cena ativa anterior e a atual |
| `OperationStarted` | Toda operação que este gerenciador inicia, **antes** de ela executar |

`OperationStarted` é o ponto de conexão para instrumentação global — ele entrega o `SceneOperation` antes da primeira mudança de estado, que é o único momento a partir do qual você consegue observar todo o ciclo de vida:

```cs
MySceneManager.OperationStarted += op =>
{
  op.StateChanged += o => Analytics.Track(o.Kind, o.State);
};
```
