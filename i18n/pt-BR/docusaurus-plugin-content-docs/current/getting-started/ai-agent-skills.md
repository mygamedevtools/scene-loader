---
sidebar_position: 4
title: Skills para Agentes de IA
description: Ensine agentes de IA a usar o My Scene Manager com as skills que vêm no pacote.
---

# Skills para Agentes de IA

O pacote inclui **AI Skills**: instruções que ensinam agentes de IA a usar o My Scene Manager corretamente, no formato `SKILL.md` que a maioria dos agentes lê.
Elas ficam em `Packages/com.mygamedevtools.scene-loader/AIAssistantSkills/`, uma pasta por skill:

| Skill | Cobre |
|---|---|
| `my-scene-manager-scene-transitions` | Trocar de cena: `TransitionAsync`, `ReloadActiveSceneAsync`, qual cena fica ativa, cenas persistentes |
| `my-scene-manager-loading-screens` | Criar e corrigir telas de carregamento em UI Toolkit ou uGUI, os componentes de carregamento, tempo mínimo de exibição e holds |
| `my-scene-manager-operation-handles` | O handle `SceneOperation`: await, progresso, estados, cancelamento, `WhenAll` / `WhenAny` |

Cada skill descreve os pedidos aos quais se aplica, então o agente só a lê quando o trabalho envolve carregamento de cenas.
Como o agente encontra as skills depende de qual você usa.

## Unity AI Assistant {/* #unity-ai-assistant */}

O **Unity AI Assistant** (um recurso pago do Unity) encontra as skills sozinho.

Skills novas começam como **Deny**. Libere-as em `Preferences > AI > Skills` (`Unity > Settings > AI > Skills` no macOS).

## Agentes de programação {/* #coding-agents */}

Agentes de programação como **Claude Code**, **Codex**, **GitHub Copilot**, **Cursor** e **Gemini CLI** leem skills de pastas na raiz do seu projeto.

Clique em `Tools > My Scene Manager > Install AI Skills for Coding Agents` para copiar as skills para `.agents/skills/` e `.claude/skills/`.

:::info[Depois de atualizar o pacote]
Clique de novo a cada atualização do pacote. Ele substitui as cópias antigas e remove qualquer skill que uma versão mais nova tenha deixado de incluir.
Ele só mexe em pastas com o nome `my-scene-manager-*`, então as outras skills dessas pastas continuam intactas.
:::

## Qualquer outro agente {/* #any-other-agent */}

Adicione esta linha ao `AGENTS.md` do seu projeto (ou ao arquivo de instruções do seu agente):

```
For scene loading, transitions and loading screens, read the skills in Packages/com.mygamedevtools.scene-loader/AIAssistantSkills/ before writing code.
```
