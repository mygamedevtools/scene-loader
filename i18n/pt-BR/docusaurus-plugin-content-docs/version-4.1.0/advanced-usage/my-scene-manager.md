---
sidebar_position: 2
---

# My Scene Manager

O `AdvancedSceneManager` é uma classe estática que engloba a classe `CoreSceneManager`, que existe para simplificar a experiência de uso das **Operações de Cena**.
Ele gerencia uma referêncai interna ao Core Scene Manager que é criado durante o _callback_ `RuntimeInitializeOnLoadMethod`, que é executado depois que a primeira cena é carregada e depois do primeiro ciclo de `Awake()`.
Isso significa que o `AdvancedSceneManager` não será inicializado até o primeiro ciclo de `Start()`.

```cs
[RuntimeInitializeOnLoadMethod]
internal static void Initialize()
{
  _instance = new CoreSceneManager(true);
}
```

## API Estática

Você tem a opção de desabilitar a classe estática `AdvancedSceneManager` completamente se deseja controlar manualmente o ciclo de vida do `CoreSceneManager` ou modificar sua funcionalidade.
Para fazer isso, apenas defina o _scripting symbol_ `DISABLE_STATIC_SCENE_MANAGER` nas suas configurações de compilação.

## Métodos de Extensão

Como a instância interna de `CoreSceneManager` não é exposta, os métodos de extensão foram reimplementados estaticamente para que você tenha disponível exatamente a mesma usabilidade para as **Operações de Cena**.