---
sidebar_position: 2
description: Pacotes compatíveis com o My Scene Manager.
---

# Compatibilidade

## Versões do Unity {/* #unity-version */}

A versão mínima suportada do Unity é a `6000.0` (Unity 6).

:::info[Assinatura de Pacote]
Os tarballs das versões publicadas são assinados com a [assinatura de pacotes do UPM](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-signature.html), que o **Package Manager** verifica a partir da `6000.3` (Unity 6.3).
Versões anteriores do Unity simplesmente ignoram a assinatura.
Consulte o [guia de instalação](./installation.mdx) para ver quais métodos de instalação são assinados.
:::

## Pacotes {/* #packages */}

Esse pacote **não tem dependências** e é compatível com alguns pacotes.
Se você quiser usá-lo com `Addressables` ou `TextMeshPro`, garanta que estes pacotes estejam instalados:

* `com.unity.addressables` >= 1.19.0
* `com.unity.ugui` >= 2.0.0 — é ele que também inclui o `TextMeshPro`, usado pelos componentes de feedback de progresso em texto
