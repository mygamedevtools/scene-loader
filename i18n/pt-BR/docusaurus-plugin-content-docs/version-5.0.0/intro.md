---
sidebar_position: 1
---

# Introdução

**My Scene Manager** é um pacote poderoso para Unity, projetado para simplificar o gerenciamento de cenas, melhorar o desempenho e aumentar a flexibilidade nos seus projetos. Seja para transições de cena, cenas do [Unity Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) ou fluxos com async/await, este pacote oferece uma solução fácil de usar para todas as suas necessidades de gerenciamento de cenas.

## Principais Funcionalidades {/* #key-features */}

* **Transições de Cena Fluidas**: Faça transições entre cenas com facilidade, com telas de carregamento opcionais para uma experiência de usuário suave.
* **Suporte para Cenas Addressable e Não Addressable**: Uma única API para ambas — uma simples string encontra sua cena onde quer que ela esteja, sem métodos separados para addressables que você precise aprender.
* **Um Handle Para Cada Operação**: Progresso, fase, eventos por cena e cancelamento, tudo conectado *depois* da chamada, em vez de decidido antes dela.
* **Aguarde Do Jeito Que Preferir**: Use `await` diretamente, converta para `Task` ou faça `yield return` a partir de uma coroutine.
* **Telas de Carregamento Além de Cenas**: Cenas, prefabs ou documentos do UI Toolkit, com componentes integrados para cada um.

## Instalação {/* #installation */}

Para começar a usar o My Scene Manager, você pode instalá-lo de várias maneiras:

* [OpenUPM](./getting-started/installation.mdx#openupm)
* [Instalar pelo Git](./getting-started/installation.mdx#git)
* [Instalar por Tarball](./getting-started/installation.mdx#tarball)
* [Unity Asset Store](./getting-started/installation.mdx#asset-store)

## Início Rápido {/* #quick-start */}

Veja como começar a fazer transições de cena com apenas algumas linhas de código:

```cs
using MyGameDevTools.SceneLoading;
// [...]

// Transicione para uma cena com uma tela de carregamento
MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
```

Essa mesma linha funciona independentemente de as cenas virem do seu Build Settings ou do Addressables.

Toda operação devolve um handle que você pode observar, controlar e cancelar:

```cs
SceneOperation op = MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");

op.Progressed   += progress => bar.value = progress;
op.StateChanged += o => { if (o.State == SceneOperationState.ScreenOut) BeginIntro(); };

SceneResult result = await op;   // ou op.Cancel(), ou yield return op.ToCoroutine()
```

:::info
Atualizando a partir da `4.x`? A chamada principal acima não mudou. Veja o [guia de atualização](./upgrades/from-4-to-5.md) para o restante.
:::
