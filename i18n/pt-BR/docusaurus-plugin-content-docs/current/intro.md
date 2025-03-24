---
sidebar_position: 1
---

# Introdução

**My Scene Manager** é um pacote poderoso para Unity, projetado para simplificar o gerenciamento de cenas, melhorar o desempenho e aumentar a flexibilidade nos seus projetos. Seja lidando com transições entre cenas, cenas usando [Unity Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) ou fluxos assíncronos com async/await, este pacote oferece uma solução fácil de usar para lidar com todas as suas necessidades de gerenciamento de cenas.

## Principais Funcionalidades

* **Transições de Cenas**: Troque entre cenas com facilidade, com cenas de carregamento opcionais para uma experiência de usuário fluida.
* **Suporte para Cenas Addressable e Não Addressable**: Gerencie cenas addressables e não addressables por meio de uma API unificada.
* **Suporte para Async/Await**: Totalmente compatível com async/await para operações de cena.
* **Tela de Carregamento**: Crie facilmente telas de carregamento com componentes integrados.
* **Suporte para Cancelamento**: Cancele operações longas de cenas para lidar com casos de uso específicos ou interações do usuário.

## Instalação

Para começar a usar o My Scene Manager, você pode instalá-lo de várias maneiras:

* [OpenUPM](./getting-started/installation.mdx#openupm)
* [Instalar pelo Git](./getting-started/installation.mdx#git)
* [Instalar por Tarball](./getting-started/installation.mdx#tarball)
* [Unity Asset Store](./getting-started/installation.mdx#asset-store)

## Começando Rápido

Veja como você pode começar com transições de cenas em apenas algumas linhas de código:

```cs
using MyGameDevTools.SceneLoading;
// [...]

// Transicione para uma cena com uma cena de carregamento
MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
```