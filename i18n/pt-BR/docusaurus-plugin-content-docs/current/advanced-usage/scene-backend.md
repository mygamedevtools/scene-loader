---
sidebar_position: 5
title: Scene Backend
description: Entenda como os backends despacham operações de cena.
---

# Scene Backend

Um **backend** é o que de fato carrega uma cena. Há dois inclusos no pacote — o Unity Scene Manager e os Addressables — e `ISceneBackend` é o que transforma essa escolha em uma consulta em vez de um desvio condicional.

Você não vai mexer nisso para carregar uma cena. Isso importa quando você quer saber *como* uma cena é carregada, ou quando quer adicionar um jeito próprio.

## A interface {/* #the-interface */}

```cs
public interface ISceneBackend
{
  bool CanHandle(SceneRefKind kind);

  SceneBackendHandle Load(SceneRef sceneRef);
  SceneBackendHandle Unload(SceneBackendHandle handle);

  float GetProgress(SceneBackendHandle handle);
  bool IsDone(SceneBackendHandle handle);

  bool TryResolveScene(SceneBackendHandle handle, out Scene scene);
}
```

A decisão addressable-ou-não acontece **uma vez** por operação, quando o `SceneRefKind` resolvido é entregue ao registry:

```mermaid
flowchart LR
  sr([SceneRef])
  res(SceneRefResolver)
  reg(SceneBackendRegistry)
  std(StandardSceneBackend)
  add(AddressablesSceneBackend)
  h(SceneBackendHandle)

  sr --> res
  res -->|tipo resolvido| reg
  reg --> std
  reg --> add
  std --> h
  add --> h
```

| Tipo | Backend |
|---|---|
| `BuildIndex`, `Scene` | `StandardSceneBackend` |
| `Address`, `AssetReference` | `AddressablesSceneBackend` |
| `Key`, `None` | Rejeitado — chegar à seleção com uma chave não resolvida significa que o resolver foi pulado |

## A única diferença honesta entre eles {/* #the-one-honest-difference-between-them */}

`TryResolveScene` é o único método cuja resposta realmente difere por backend, e difere **retornando `false`** em vez de avisar e devolver um valor padrão:

- `AddressablesSceneBackend` recebe um `SceneInstance` de volta dos Addressables, então consegue nomear sua própria cena diretamente.
- `StandardSceneBackend` não consegue. O Unity Scene Manager não tem uma API que diga "este `AsyncOperation` produziu aquela `Scene`", então a resposta honesta é "não", e a cena é correspondida depois pelo linker.

Retornar `false` em vez de uma `Scene` padrão é o que mantém essa etapa de correspondência explícita em vez de silenciosa.

## Handles {/* #handles */}

`SceneBackendHandle` é uma **readonly struct** — um valor, não um objeto — carregando o backend que a possui, o `SceneRef` de onde veio, a `Scene` assim que conhecida, e a operação subjacente da Unity.

Os handles são atualizados pelo `SceneOperationPump`, uma única passagem do player loop sobre cada operação ativa. É isso que dispara `Progressed` — e somente quando o valor de fato avançou além de um pequeno epsilon, para que uma barra vinculada a ele não oscile a cada frame.

## Escrevendo seu próprio backend {/* #writing-your-own-backend */}

`SceneBackendRegistry.Register` coloca o seu na frente dos padrões — a ordem de registro decide a precedência, e o último registrado vence:

```cs
public class MyBackend : ISceneBackend
{
  public bool CanHandle(SceneRefKind kind) => kind == SceneRefKind.Address;
  // ...
}

SceneBackendRegistry.Register(new MyBackend());
```

:::info
Este é o ponto de extensão para um sistema de entrega de assets diferente — um pipeline de bundles customizado, ou um SDK de loja que entrega cenas para você. Implemente seis métodos, registre o backend, e todos os pontos de chamada existentes continuam funcionando.
:::

:::warning
O registry é estático, então um registro feito no editor sobrevive a um Domain Reload desabilitado. Registre a partir de um `[RuntimeInitializeOnLoadMethod]` em vez de código arbitrário, e tenha em mente que testes que registram um backend precisam redefini-lo depois.
:::
