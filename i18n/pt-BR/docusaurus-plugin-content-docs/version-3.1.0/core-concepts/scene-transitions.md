---
sidebar_position: 4
---

# Transições de Cenas

As Transições de Cenas são uma combinação de operações de **load** e **unload** para transicionar entre cenas com efetividade, com ou sem uma cena intermediária. Por exemplo, geralmente, se você quiser ir da cena A para a cena B, você:

1. **Carrega** a cena B.
2. **Descarrega** a cena A.

São só **duas** operações, mas e se você quisesse adicionar uma cena de carregamento?
Nesse caso, você:

1. **Carrega** a cena de carregamento.
2. **Carrega** a cena B.
4. **Descarrega** a cena A.
3. **Descarrega** a cena de carregamento.

Agora são **quatro** operações.
Os métodos `TransitionToScene` e `TransitionToSceneAsync` permitem que você providencie para onde você quer ir a partir da **cena ativa** e se você quer ter uma cena intermediária (cena de carregamento por exemplo).

Além da transição a partir da cena ativa, você também pode usar as alternativas `TransitionToSceneFromScenes` e `TransitionToSceneFromAll`:

- `TransitionToSceneFromScenes` - descarrega um grupo de cenas durante a transição.
- `TransitionToSceneFromAll` - descarrega todas as cenas durante a transição.

Assim como os métodos convencionais de `Transition`, as variantes também possuem opções de cenas únicas ou múltiplas e opções _async_.