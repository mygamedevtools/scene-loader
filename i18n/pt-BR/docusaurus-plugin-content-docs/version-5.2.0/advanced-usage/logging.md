---
sidebar_position: 8
title: Logging
description: Direcione e filtre os diagnósticos do pacote com SceneManagerLog.
---

# Logging

`SceneManagerLog` é o destino único de todo diagnóstico que o pacote emite. Ele existe para que o pacote tenha um só lugar de onde reportar, e você, um só lugar de onde controlar.

```cs
SceneManagerLog.Level = SceneLogLevel.Verbose;   // Off | Error | Warning | Info | Verbose
SceneManagerLog.Handler = myHandler;             // console in-game, analytics, captura em testes
```

## Níveis {/* #levels */}

| Nível | |
|---|---|
| `Off` | Não emite nada. |
| `Error` | Uma operação falhou, ou um estado do qual o gerenciador depende está inconsistente. |
| `Warning` | Algo recuperável, ou uma API usada de um jeito que não vai fazer o que quem chamou espera. |
| `Info` | Progresso em linhas gerais ao longo de uma operação. |
| `Verbose` | Detalhe passo a passo, para diagnosticar uma falha específica. |

`Level` tem como padrão **`Warning` em builds de desenvolvimento** e **`Error` em release** — isto é, segue `Debug.isDebugBuild`, então acompanha automaticamente o editor e o player de desenvolvimento.

A filtragem é **inteiramente em runtime**. Não existe chave de compilação, e isso é proposital: poder aumentar o nível de log em uma build que você já lançou é justamente a situação que justifica esse custo.

```cs
// Coloque isto atrás de um menu de debug e você consegue diagnosticar a instalação de um jogador.
SceneManagerLog.Level = SceneLogLevel.Verbose;
```

## Direcionamento {/* #routing */}

`Handler` é um `UnityEngine.ILogHandler` — a mesma interface que o console da própria Unity implementa, então qualquer coisa que já aceite um funciona aqui.

```cs
SceneManagerLog.Handler = new MyInGameConsole();
```

:::note
Atribuir `null` **restaura o console da Unity** em vez de silenciar. Use `SceneLogLevel.Off` para silenciar.

São intenções diferentes, e confundir as duas faria um `null` acidental parecer um botão de desligar que funciona.
:::

:::info
Um handler que lança exceção é contido, e o erro é reportado ao console da Unity no lugar dele. Um sink de analytics quebrado não vai derrubar o carregamento de cena que estava tentando reportar por ele.
:::

## O que custa o quê {/* #what-costs-what */}

Como a filtragem é em runtime, a mensagem é construída no ponto de chamada, seja ela emitida ou não. O pacote protege os poucos pontos que rodam por operação ou por cena, e deixa o resto desprotegido — uma mensagem que dispara uma vez ou nunca não vale um `if`.

Para o seu próprio código, vale a mesma regra: verifique o nível antes de construir qualquer coisa cara em um caminho que roda com frequência.

```cs
if (SceneManagerLog.Level >= SceneLogLevel.Verbose)
    SceneManagerLog.Verbose($"...{something.Expensive()}...");
```

## De onde vêm as mensagens {/* #where-the-messages-come-from */}

A maior parte do que você vai ver está coberta na página de **Troubleshooting**, que lista as mensagens mais comuns e o que fazer a respeito:

- avisos de resolução com correspondência dupla, quando um nome está tanto nas Build Settings quanto nos Addressables
- chaves de cena que não podem ser resolvidas
- uma transição esperando por um gate de tela de carregamento que nunca foi aberto
- uma operação falhando, com a exceção que causou a falha
