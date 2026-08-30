---
sidebar_position: 2
title: My Scene Manager
---

# My Scene Manager

O `MySceneManager` é um wrapper estático da classe `CoreSceneManager`, e existe para simplificar o uso das **Operações de Cena**.
Ele mantém uma referência interna a um Core Scene Manager, criado durante o _callback_ `RuntimeInitializeOnLoadMethod`, que é executado depois que a primeira cena foi carregada e depois do primeiro ciclo de `Awake()`.
Isso significa que o `MySceneManager` só estará inicializado a partir do primeiro ciclo de `Start()`.

```cs
[RuntimeInitializeOnLoadMethod]
internal static void Initialize()
{
  _instance = new CoreSceneManager(true);
}
```

## API Estática {/* #static-api */}

Se quiser, você pode desabilitar a classe estática `MySceneManager` por completo, caso prefira controlar manualmente o ciclo de vida do `CoreSceneManager` e/ou estender alguma funcionalidade.
Para isso, basta definir o _scripting symbol_ `DISABLE_STATIC_SCENE_MANAGER` nas configurações de compilação do seu projeto.

## Os quatro métodos {/* #the-four-methods */}

Ele não expõe a instância interna de `CoreSceneManager`, então espelha as mesmas quatro operações de forma estática:

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
| `OperationStarted` | Toda operação que este gerenciador inicia, **antes** de ela rodar |

`OperationStarted` é o ponto de encaixe para instrumentação global — ele te entrega o `SceneOperation` antes da primeira mudança de estado, o único momento a partir do qual dá para observar o ciclo de vida inteiro:

```cs
MySceneManager.OperationStarted += op =>
{
  op.StateChanged += o => Analytics.Track(o.Kind, o.State);
};
```
