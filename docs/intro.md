---
sidebar_position: 1
---

# Introduction

The **Advanced Scene Manager** is a powerful Unity package designed to simplify scene management, improve performance, and enhance flexibility in your projects. Whether you're dealing with scene transitions, [Unity Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) scenes, or async/await workflows, this package provides an easy-to-use solution for handling all your scene management needs.

## Key Features

* **Seamless Scene Transitions**: Transition between scenes with ease, with optional loading scenes for a smooth user experience.
* **Addressable and Non-Addressable Scene Support**: Manage both addressable and non-addressable scenes through a unified API.
* **Async/Await Support**: Fully compatible with async/await for smooth, non-blocking scene operations.
* **Loading Screens**: Easily build loading screens with built-in components.
* **Cancellation Support**: Cancel long-running scene operations to handle edge cases or user interactions.

## Instalation

To get started with the Advanced Scene Manager, you can install it in various ways:

* [OpenUPM](./getting-started/installation.mdx#openupm)
* [Install from Git](./getting-started/installation.mdx#git)
* [Install from Tarball](./getting-started/installation.mdx#tarball)

## Quick Start

Here's how you can get started with scene transitions in just a few lines of code:

```cs
using MyGameDevTools.SceneLoading;
// [...]

// Transition to a scene with a loading scene
AdvancedSceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
```