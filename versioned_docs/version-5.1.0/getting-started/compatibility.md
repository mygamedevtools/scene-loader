---
sidebar_position: 2
description: Packages compatible with My Scene Manager.
---

# Compatibility

## Unity Version

The minimum supported Unity Version is `6000.0` (Unity 6).

:::info[Package Signing]
Release tarballs are signed with [UPM package signing](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-signature.html), which the **Package Manager** verifies from `6000.3` (Unity 6.3) onwards.
Earlier Unity versions simply ignore the signature.
Check the [installation guide](./installation.mdx) to see which installation methods are signed.
:::

## Packages

This package has **no dependencies** and is compatible with some packages.
If you wish to use it with `Addressables` or `TextMeshPro`, make sure you install the packages:

* `com.unity.addressables` >= 1.19.0
* `com.unity.ugui` >= 2.0.0 — this is also what ships `TextMeshPro`, which the progress-text feedback components use