---
sidebar_position: 7
---

# Transições de Cenas

As Transições de Cenas são uma combinação de operações de **carregamento** e **descarregamento** para transicionar entre cenas com efetividade, com ou sem uma cena intermediária. Por exemplo, geralmente, se você quiser ir da cena A para a cena B, você:

1. **Carrega** a cena B.
2. **Descarrega** a cena A.

```mermaid
flowchart LR

a{{"**Carrega** Scene B"}} --- b{{"**Descarrega** Scene A"}}
```

São só **duas** operações, mas se você quiser adicionar uma cena de carregamento, você:

1. **Carrega** a cena de carregamento.
2. **Carrega** a cena B.
4. **Descarrega** a cena A.
3. **Descarrega** a cena de carregamento.

```mermaid
flowchart LR

a{{"**Carrega** Loading Scene"}} --- b{{"**Carrega** Scene B"}} --- c{{"**Descarrega** Scene A"}} --- d{{"**Descarrega** Loading Scene"}}
```

Agora são **quatro** operações.
O método `TransitionAsync` permite que você providencie a cena (ou cenas) para quais você quer transicionar a partir da **cena ativa atualmente** e se você quer uma cena intermediária (cena de carregamento por exemplo).

## Cena de Carregamento Intermediária

Para criar uma Cena de Carregamento, você precisa usar os [Componentes de Carregamento](../getting-started/loading-screens.md#componentes-de-carregamento).
Ao realizar uma **Transição de Cena**, o `CoreSceneManager` procura por um componente `LoadingBehavior` na cena intermediária e, se existir, ele será notificado com o progresso do carregamento.

Os campos `WaitForScriptedStart` e `WaitForScriptedEnd` no `LoadingBehavior` controlam se a cena de carregamento terá uma animação para iniciar e/ou finalizar a transição.
Isso efetivamente **atrasará** o início ou o fim da operação de **Transição de Cena** para exibir um feedback visual, como um efeito de fade in/out.

Quando o método `TransitionAsync` é _awaited_, ele aguardará até que toda a transição tenha sido concluída **e** a cena de carregamento tenha sido descarregada.
Se você deseja executar uma ação exatamente quando a cena alvo for carregada, pode confiar nas chamadas `Awake()` dessa cena ou assinar o evento `SceneLoaded` do `CoreSceneManager` ou `MySceneManager`.