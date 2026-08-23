---
sidebar_position: 1
---

# Introduction

**My Scene Manager** is a powerful Unity package designed to simplify scene management, improve performance, and enhance flexibility in your projects. Whether you're dealing with scene transitions, [Unity Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) scenes, or async/await workflows, this package provides an easy-to-use solution for handling all your scene management needs.

## Key Features

* **Seamless Scene Transitions**: Transition between scenes with ease, with optional loading screens for a smooth user experience.
* **Addressable and Non-Addressable Scene Support**: One API for both — a plain string finds your scene wherever it lives, with no separate addressable methods to learn.
* **A Handle For Every Operation**: Progress, phase, per-scene events and cancellation, all attached *after* the call rather than decided before it.
* **Await It Any Way You Like**: `await` directly, bridge to `Task`, or `yield return` it from a coroutine.
* **Loading Screens Beyond Scenes**: Scenes, prefabs or UI Toolkit documents, with built-in components for each.

## Installation

To get started with My Scene Manager, you can install it in various ways:

* [OpenUPM](./getting-started/installation.mdx#openupm)
* [Install from Git](./getting-started/installation.mdx#git)
* [Install from Tarball](./getting-started/installation.mdx#tarball)
* [Unity Asset Store](./getting-started/installation.mdx#asset-store)

## Quick Start

Here's how you can get started with scene transitions in just a few lines of code:

```cs
using MyGameDevTools.SceneLoading;
// [...]

// Transition to a scene with a loading screen
MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
```

That same line works whether the scenes come from your Build Settings or from Addressables.

Every operation hands back a handle you can watch, drive and cancel:

```cs
SceneOperation op = MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");

op.Progressed   += progress => bar.value = progress;
op.StateChanged += o => { if (o.State == SceneOperationState.ScreenOut) BeginIntro(); };

SceneResult result = await op;   // or op.Cancel(), or yield return op.ToCoroutine()
```

:::info
Upgrading from `4.x`? The headline call above is unchanged. See the [upgrade guide](./upgrades/from-4-to-5.md) for the rest.
:::