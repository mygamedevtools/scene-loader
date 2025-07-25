# Changelog

# [4.1.0](https://github.com/mygamedevtools/scene-loader/compare/4.0.0...4.1.0) (2025-07-08)


### Features

* add reload scene methods and tests ([#53](https://github.com/mygamedevtools/scene-loader/issues/53)) ([8b5ecb9](https://github.com/mygamedevtools/scene-loader/commit/8b5ecb988abbcf0ea06f469f7c957255ed9d8df2))

# [4.0.0](https://github.com/mygamedevtools/scene-loader/compare/3.1.0...4.0.0) (2025-03-24)


### Features

* consolidate simplification ([#50](https://github.com/mygamedevtools/scene-loader/issues/50)) ([edeba3c](https://github.com/mygamedevtools/scene-loader/commit/edeba3cc7776b4244ecdf1a0ffedbaa03af841b9))


### BREAKING CHANGES

* The 4.x update consolidates the changes in the 3.x and simplifies a few more steps of the usage experience. It completely removes the ISceneLoader implementations and adds a static MySceneManager class so you don't have to manually control its lifecycle. Refer to the upgrade guide at https://scene-loader.mygamedevtools.com/docs/upgrades/from-3-to-4 for more details.

# [3.1.0](https://github.com/mygamedevtools/scene-loader/compare/3.0.2...3.1.0) (2024-08-27)


### Features

* add new scene transition methods ([#46](https://github.com/mygamedevtools/scene-loader/issues/46)) ([de9ccb7](https://github.com/mygamedevtools/scene-loader/commit/de9ccb777ae0919391b446e9264d48766b65ef2d))

## [3.0.2](https://github.com/mygamedevtools/scene-loader/compare/3.0.1...3.0.2) (2024-08-16)


### Bug Fixes

* add warning and documentation regarding Awake() ([#43](https://github.com/mygamedevtools/scene-loader/issues/43)) ([e24423d](https://github.com/mygamedevtools/scene-loader/commit/e24423ddd568e3cce856f9ce6b14c49ec3061d48))

## [3.0.1](https://github.com/mygamedevtools/scene-loader/compare/3.0.0...3.0.1) (2024-07-31)


### Bug Fixes

* scene linking for scenes loaded by path ([#41](https://github.com/mygamedevtools/scene-loader/issues/41)) ([7dc5641](https://github.com/mygamedevtools/scene-loader/commit/7dc5641dfbb2cfae70ba7fe9268f4052183c0d73))

# [3.0.0](https://github.com/mygamedevtools/scene-loader/compare/2.3.2...3.0.0) (2024-06-19)


### Features

* unify addressable and non-addressable workflows ([#37](https://github.com/mygamedevtools/scene-loader/issues/37)) ([68e93f3](https://github.com/mygamedevtools/scene-loader/commit/68e93f3e36efdf5cc3e12adb92490cbc3e75f77f))


### BREAKING CHANGES

* The ISceneManager, ISceneLoader and ILoadSceneInfo interfaces have been changed to accomodate the unification of addressable and non-addressable workflows. Refer to the migration guide at https://github.com/mygamedevtools/scene-loader/wiki/Migration-Guide-3.x for more details

# [3.0.0-pre.8](https://github.com/mygamedevtools/scene-loader/compare/3.0.0-pre.7...3.0.0-pre.8) (2024-06-15)


### Bug Fixes

* add addressable preprocessor to scenedatabuilder ([7720e1e](https://github.com/mygamedevtools/scene-loader/commit/7720e1e3f3c97c2f93ab9cf83f314f79b77f0e9f))
* test reusable workflow ([575af28](https://github.com/mygamedevtools/scene-loader/commit/575af284a41ae4a654a7a0d591661911d0a5e1bb))

## [2.3.2](https://github.com/mygamedevtools/scene-loader/compare/2.3.1...2.3.2) (2024-03-04)


### Bug Fixes

* remove unitask dependency ([#31](https://github.com/mygamedevtools/scene-loader/issues/31)) ([4bf74d0](https://github.com/mygamedevtools/scene-loader/commit/4bf74d0d7e246d1ec9de42e73ea70aba7d260f1d))

## [2.3.1](https://github.com/mygamedevtools/scene-loader/compare/2.3.0...2.3.1) (2024-01-30)


### Bug Fixes

* improve dispose/cancellation error handling ([#28](https://github.com/mygamedevtools/scene-loader/issues/28)) ([2bfbcb9](https://github.com/mygamedevtools/scene-loader/commit/2bfbcb95487aa4a81bc4e0d7547908c00597715c))

# [2.3.0](https://github.com/mygamedevtools/scene-loader/compare/2.2.3...2.3.0) (2024-01-30)


### Features

* implement IDisposable on ISceneManager and ISceneLoader ([#27](https://github.com/mygamedevtools/scene-loader/issues/27)) ([fae1cf4](https://github.com/mygamedevtools/scene-loader/commit/fae1cf471e930a2b773ead6d99b3b1418cf022b9))

## [2.2.3](https://github.com/mygamedevtools/scene-loader/compare/2.2.2...2.2.3) (2023-12-20)


### Bug Fixes

* replace WaitForCompletion with async logic ([#24](https://github.com/mygamedevtools/scene-loader/issues/24)) ([3f87bc4](https://github.com/mygamedevtools/scene-loader/commit/3f87bc4345808cee7e80435286a23ee2268deb73))

## [2.2.2](https://github.com/mygamedevtools/scene-loader/compare/2.2.1...2.2.2) (2023-12-02)


### Bug Fixes

* upgrade to unity 2023.2 ([#22](https://github.com/mygamedevtools/scene-loader/issues/22)) ([3aac886](https://github.com/mygamedevtools/scene-loader/commit/3aac8868f770696bcbb86595da77718afc147c9b))

## [2.2.1](https://github.com/mygamedevtools/scene-loader/compare/2.2.0...2.2.1) (2023-10-19)


### Bug Fixes

* add preprocessor directives to filter out addressables code ([#21](https://github.com/mygamedevtools/scene-loader/issues/21)) ([82b0616](https://github.com/mygamedevtools/scene-loader/commit/82b06169bc2eedadbadcc24d3899e85fa095b683))

# [2.2.0](https://github.com/mygamedevtools/scene-loader/compare/2.1.1...2.2.0) (2023-06-28)


### Features

* add support for multiple scene operations ([#17](https://github.com/mygamedevtools/scene-loader/issues/17)) ([87117e0](https://github.com/mygamedevtools/scene-loader/commit/87117e087dff8dec8dbf688da38a3aa172c8019d))

## [2.1.1](https://github.com/mygamedevtools/scene-loader/compare/2.1.0...2.1.1) (2023-03-08)


### Bug Fixes

* correct scene assignment on load/unload ops ([5953402](https://github.com/mygamedevtools/scene-loader/commit/5953402dadcec15fd493298611fe86d13cfb8cf0))

# [2.1.0](https://github.com/mygamedevtools/scene-loader/compare/2.0.2...2.1.0) (2023-02-12)


### Features

* add transitions from external scenes ([64205fb](https://github.com/mygamedevtools/scene-loader/commit/64205fb336f443834c16898fb590dceeaa672a29))

## [2.0.2](https://github.com/mygamedevtools/scene-loader/compare/2.0.1...2.0.2) (2023-02-08)


### Bug Fixes

* Point intermediate ci checkout to new release ([39ad3ee](https://github.com/mygamedevtools/scene-loader/commit/39ad3eef4e1093f2970b2ff4cbb822ee2280909e))

## [2.0.1](https://github.com/mygamedevtools/scene-loader/compare/2.0.0...2.0.1) (2023-02-08)


### Bug Fixes

* Update upm branch generation ([6dbab20](https://github.com/mygamedevtools/scene-loader/commit/6dbab20225b1f9df9fd7e1b9255c528d6877c204))

# [2.0.0](https://github.com/mygamedevtools/scene-loader/compare/1.4.1...2.0.0) (2023-02-08)


### Code Refactoring

* Merge addressable and standard interfaces ([ed9a523](https://github.com/mygamedevtools/scene-loader/commit/ed9a523c92b381c39fd946ab514d838990627da9))


### Features

* Add new SceneManager for addressable ops ([06ca0bd](https://github.com/mygamedevtools/scene-loader/commit/06ca0bd51fd4af4e2e8324d5e5feecc0e38618bc))
* Add Scene Info linked by Asset Reference ([db996d7](https://github.com/mygamedevtools/scene-loader/commit/db996d78b853a7125512e6b34c36451b2b01748d))
* Update internal addressable scene manager ([6217814](https://github.com/mygamedevtools/scene-loader/commit/62178144cc92cf55577db03095f5b92a0376757f))


### BREAKING CHANGES

* Removed IAddressable interfaces.

The interfaces have been standardized, so the usage of specific ones for Addressable workflow is no longer required.

## [1.4.1](https://github.com/mygamedevtools/scene-loader/compare/1.4.0...1.4.1) (2022-12-08)


### Bug Fixes

* Unload scene before loading in transitions ([f6b8c7a](https://github.com/mygamedevtools/scene-loader/commit/f6b8c7a7c06d980be709687dfb3de5cf2761adb0))

# [1.4.0](https://github.com/mygamedevtools/scene-loader/compare/1.3.1...1.4.0) (2022-10-25)


### Features

* Add CI configuration ([9bb2de8](https://github.com/mygamedevtools/scene-loader/commit/9bb2de8fa695337f08d6f70a5a3aeb4e772cac0f))

## [1.3.1] - 2022-10-24
- Added: OpenUPM documentation.

## [1.3.0] - 2022-10-03
- Fixed: Updated assembly definition and namespaces names to reflect the organization name changes.

## [1.2.0] - 2022-10-03
- Changed: Updated organization name to comply with [Unity Package Guidelines](https://unity.com/legal/terms-of-service/software/package-guidelines).

## [1.1.0] - 2022-09-13
- Changed: Moved repository to My Unity Tools organization.
- Changed: Updated package name and author.

## [1.0.0] - 2022-09-05
- Initial Release.

[1.3.1]: https://github.com/mygamedevtools/scene-loader/compare/1.3.0...1.3.1
[1.3.0]: https://github.com/mygamedevtools/scene-loader/compare/1.2.0...1.3.0
[1.2.0]: https://github.com/mygamedevtools/scene-loader/compare/1.1.0...1.2.0
[1.1.0]: https://github.com/mygamedevtools/scene-loader/compare/1.0.0...1.1.0
[1.0.0]: https://github.com/mygamedevtools/scene-loader/compare/f2b6582...1.0.0
