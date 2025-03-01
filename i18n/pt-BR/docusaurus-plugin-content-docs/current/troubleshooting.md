---
sidebar_position: 6
---

# Soluções de Problemas

## Erro ao criar um `CoreSceneManager`

Ao criar um `CoreSceneManager` passando um parâmetro com valor `true` para seu construtor, como `new CoreSceneManager(true)`, ele tenta adicionar todas as cenas carregadas à sua lista interna de cenas.
Mas, se você criar durante o `Awake()`, você pode ver o erro:

```
ArgumentException: Attempted to get an {nameof(ISceneData)} through an invalid or unloaded scene.
```

Esse erro é gerado porque durante o `Awake()` a cena ainda não carregou totalmente e não pode ser adicionada à lista interna de cenas.

Mova a sua chamada para o `Start()`.