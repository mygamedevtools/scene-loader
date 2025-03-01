---
sidebar_position: 6
---

# Soluções de Problemas

## Erro ao criar um `AdvancedSceneManager`

Ao criar um `AdvancedSceneManager` passando um parâmetro com valor `true` para seu construtor, como `new AdvancedSceneManager(true)`, ele tenta adicionar todas as cenas carregadas à sua lista interna de cenas.
Mas, se você criar durante o `Awake()`, você pode ver o erro:

```
ArgumentException: Attempted to get an {nameof(ISceneData)} through an invalid or unloaded scene.
```

Esse erro é gerado porque durante o `Awake()` a cena ainda não carregou totalmente e não pode ser adicionada à lista interna de cenas.

Mova a sua chamada para o `Start()`.

## Não consigo descarregar uma cena com um `ILoadSceneInfo` diferente

Caso você tenha carregado uma cena por um tipo de `ILoadSceneInfo`, você só consegue descarregá-la usando o mesmo tipo ou explicitamente um `LoadSceneInfoScene`. Por exemplo:

```cs
ILoadSceneInfo nameInfo = new LoadSceneInfoName("MyScene");
ILoadSceneInfo indexInfo = new LoadSceneInfoIndex(3);

sceneManager.LoadSceneAsync(nameInfo);

// Você **não pode** fazer isso:
sceneManager.UnloadSceneAsync(indexInfo);

// Mas pode fazer isso:
sceneManager.UnoadSceneAsync(nameInfo);

// Ou, faça um `LoadSceneInfoScene`.
// Alternativas: GetLoadedSceneByName(name), GetLoadedSceneAt(index), GetLastLoadedScene() ou GetActiveScene()
ILoadSceneInfo sceneInfo = sceneManager.GetLoadedSceneByName("MyScene");
sceneManager.UnloadSceneAsync(sceneInfo);
```

As vezes esse problema pode ser contornado ao realizar uma **Transição de Cenas**. Se você estiver tentando descarregar a cena ativa para fazer uma transição entre cenas, você pode executar a transição pelo **Scene Manager** e deixar que ele cuide da complexidade interna. Por exemplo:

```cs
// Ao invés de descarregar a cena de origem diretamente:
sceneManager.LoadSceneAsync(targetSceneInfo)
sceneManager.UnloadSceneAsync(sourceSceneInfo);

// Faça uma transição de cenas:
sceneManager.TransitionToScene(targetSceneInfo);
```