---
sidebar_position: 6
---

# Soluções de Problemas

## Nada é rastreado ao criar um `CoreSceneManager` {/* #nothing-is-tracked-when-creating-a-corescenemanager */}

Ao criar um `CoreSceneManager` passando um valor `true` para seu construtor, como `new CoreSceneManager(true)`, ele tenta adicionar todas as cenas carregadas à sua lista de cenas rastreadas.
Porém, se você fizer essa chamada durante o `Awake()`, a cena ainda não está totalmente carregada e não há nada para adicionar, então você verá:

```
[MySceneManager] Tried to create a Scene Manager with all loaded scenes, but encoutered none.
Did you create the Scene Manager on `Awake()`? If so, try moving the call to `Start()` instead.
```

Mova a sua chamada para o `Start()`.

## Uma cena é resolvida para o backend errado {/* #a-scene-resolves-to-the-wrong-backend */}

Uma string simples é resolvida consultando **primeiro as Build Settings** e depois o Addressables. Se uma cena existe em ambos, as Build Settings vencem e você verá:

```
[MySceneManager] The scene 'my-scene' matches both the build settings and an Addressables entry.
The build settings take precedence. Use SceneRef.Address("my-scene") to load the addressable one.
```

Use `SceneRef.Address("my-scene")` para forçar a versão addressable. Veja [Scene Ref](./advanced-usage/scene-ref.md#how-a-string-is-resolved).

## Uma cena não é encontrada de jeito nenhum {/* #a-scene-cannot-be-found-at-all */}

```
Could not resolve the scene 'my-scene'. It was not found in the build settings or the Addressables catalog.
```

Adicione-a às Build Settings, registre-a como uma entrada do Addressables ou passe uma referência explícita. Se o Addressables não estiver instalado, a mensagem informa isso — apenas as Build Settings foram consultadas.

## Uma operação parece travar {/* #an-operation-appears-to-hang */}

Depois de 10 segundos esperando pela mesma operação da engine, um build de desenvolvimento reporta o que está esperando e continua esperando:

```
[MySceneManager] A Transition operation has been waiting 10 seconds on ...
```

Isso normalmente significa que um portão da tela de carregamento nunca foi liberado — um componente que chamou `HoldShow` ou `HoldHide` no seu `LoadingProgress` e nunca chamou o `ReleaseShow` / `ReleaseHide` correspondente. O aviso nomeia quem está retendo. Um detentor que é destruído sem liberar é descartado automaticamente, então o culpado é um que ainda está vivo.

## Aumentando os diagnósticos {/* #turning-the-diagnostics-up */}

Tudo acima é emitido através do `SceneManagerLog`, que por padrão usa `Warning` em builds de desenvolvimento e `Error` em release. Aumente o nível em tempo de execução — inclusive dentro de um build publicado, que é quando isso realmente vale a pena:

```cs
SceneManagerLog.Level = SceneLogLevel.Verbose;
SceneManagerLog.Handler = myHandler;   // redirecione para um console dentro do jogo ou para analytics
```

`SceneLogLevel.Off` silencia o log. Atribuir `null` ao `Handler` restaura o console da Unity em vez de silenciar.

Veja [Logging](./advanced-usage/logging.md) para os níveis, o roteamento e o custo de cada um.
