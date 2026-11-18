---
sidebar_position: 8
description: Route and filter the package's diagnostics with SceneManagerLog.
---

# Logging

`SceneManagerLog` is the single sink for every diagnostic the package emits. It exists so the package has one place to report from, and you have one place to control.

```cs
SceneManagerLog.Level = SceneLogLevel.Verbose;   // Off | Error | Warning | Info | Verbose
SceneManagerLog.Handler = myHandler;             // in-game console, analytics, test capture
```

## Levels

| Level | |
|---|---|
| `Off` | Emits nothing. |
| `Error` | An operation failed, or state the manager depends on is inconsistent. |
| `Warning` | Something recoverable, or an API used in a way that will not do what the caller expects. |
| `Info` | Coarse progress through an operation. |
| `Verbose` | Step-by-step detail, for diagnosing a specific failure. |

`Level` defaults to **`Warning` in development builds** and **`Error` in release** — that is `Debug.isDebugBuild`, so it follows the editor and development player automatically.

Filtering is **entirely runtime**. There is no compile-time switch, deliberately: being able to raise logging inside a build you have already shipped is the situation this is worth having for.

```cs
// Ship this behind a debug menu and you can diagnose a player's install.
SceneManagerLog.Level = SceneLogLevel.Verbose;
```

## Routing

`Handler` is a `UnityEngine.ILogHandler` — the same interface Unity's own console implements, so anything that already accepts one works here.

```cs
SceneManagerLog.Handler = new MyInGameConsole();
```

:::note
Assigning `null` **restores the Unity console** rather than silencing. Use `SceneLogLevel.Off` to silence.

Those are different intents, and conflating them would make an accidental null look like a working kill switch.
:::

:::info
A handler that throws is contained and reported to the Unity console instead. A broken analytics sink will not take down the scene load that was trying to report through it.
:::

## What costs what

Because filtering is runtime, the message is built at the call site whether or not it is emitted. The package guards the handful of sites that run per operation or per scene, and leaves the rest unguarded — a message that fires once or never is not worth an `if`.

For your own code, the same rule applies: check the level before building anything expensive on a path that runs often.

```cs
if (SceneManagerLog.Level >= SceneLogLevel.Verbose)
    SceneManagerLog.Verbose($"...{something.Expensive()}...");
```

## Where the messages come from

Most of what you will see is covered on the **Troubleshooting** page, which lists the common ones and what to do about them:

- double-match resolution warnings, when a name is in both the build settings and Addressables
- unresolvable scene keys
- a transition waiting on a loading-screen gate that was never opened
- an operation faulting, with the exception that caused it
