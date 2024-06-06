#if ENABLE_UNITASK
using Cysharp.Threading.Tasks;
using MyGameDevTools.SceneLoading.UniTaskSupport;
#endif
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    enum SceneLoaderType
    {
        Async,
#if ENABLE_UNITASK
        UniTask,
#endif
        Coroutine,
    }

    public class SceneLoaderTests : SceneTestEnvironment
    {
        const int _defaultTimeout = 3000;

        static ISceneManager[] _managers = new ISceneManager[]
        {
            new SceneManager(),
#if ENABLE_ADDRESSABLES
            new SceneManagerAddressable()
#endif
        };

        static ILoadSceneInfo[][] _targetSceneGroups = new ILoadSceneInfo[][]
        {
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[2]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            }
        };
        static ILoadSceneInfo[] _targetSceneInfos = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[2])
        };
        static ILoadSceneInfo[] _sameSceneInfos = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[1])
        };
        static ILoadSceneInfo[] _loadingSceneInfos = new ILoadSceneInfo[]
        {
            null,
            new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[0])
        };
        static ISceneLoader[] _sceneLoaders = new ISceneLoader[]
        {
            new SceneLoaderAsync(_managers[0]),
#if ENABLE_ADDRESSABLES
            new SceneLoaderAsync(_managers[1]),
#endif
#if ENABLE_UNITASK
            new SceneLoaderUniTask(_managers[0]),
#if ENABLE_ADDRESSABLES
            new SceneLoaderUniTask(_managers[1]),
#endif
#endif
            new SceneLoaderCoroutine(_managers[0]),
#if ENABLE_ADDRESSABLES
            new SceneLoaderCoroutine(_managers[1])
#endif
        };
        static Func<ISceneLoader>[] _sceneLoaderCreateFuncs = new Func<ISceneLoader>[]
        {
            () => new SceneLoaderAsync(new SceneManager()),
#if ENABLE_ADDRESSABLES
            () => new SceneLoaderAsync(new SceneManagerAddressable()),
#endif
#if ENABLE_UNITASK
            () => new SceneLoaderUniTask(new SceneManager()),
#if ENABLE_ADDRESSABLES
            () => new SceneLoaderUniTask(new SceneManagerAddressable()),
#endif
#endif
            () => new SceneLoaderCoroutine(new SceneManager()),
#if ENABLE_ADDRESSABLES
            () => new SceneLoaderCoroutine(new SceneManagerAddressable()),
#endif
        };

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var m in _managers)
                yield return SceneLoaderTestUtilities.UnloadManagerScenes(m);

            yield return SceneLoaderTestUtilities.UnloadRemainingScenes();
            Assert.AreEqual(1, UnityEngine.SceneManagement.SceneManager.loadedSceneCount);
        }

        [UnityTest]
        public IEnumerator LoadScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneGroups))] ILoadSceneInfo[] targetScenes)
        {
            int sceneCount = targetScenes.Length;
            var loadedScenes = new List<Scene>(sceneCount);

            sceneLoader.Manager.SceneLoaded += sceneLoaded;
            sceneLoader.LoadScenes(targetScenes, 0);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScenes.Count == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(sceneCount, loadedScenes.Count);
            Assert.AreEqual(getTargetActiveScene(), UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            void sceneLoaded(Scene scene)
            {
                loadedScenes.Add(scene);
            }

            Scene getTargetActiveScene()
            {
                foreach (var loadedScene in loadedScenes)
                    if (targetScenes[0].IsReferenceToScene(loadedScene))
                        return loadedScene;
                return default;
            }
        }

        [UnityTest]
        public IEnumerator LoadScene([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader)
        {
            yield return LoadFirstScene(sceneLoader);
        }

        [UnityTest]
        public IEnumerator UnloadScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneGroups))] ILoadSceneInfo[] targetScenes)
        {
            int sceneCount = targetScenes.Length;
            sceneLoader.LoadScenes(targetScenes);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => sceneLoader.Manager.LoadedSceneCount == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            Assert.AreEqual(sceneCount, sceneLoader.Manager.LoadedSceneCount);

            var unloadedScenes = new List<Scene>(sceneCount);

            sceneLoader.Manager.SceneUnloaded += sceneUnloaded;
            sceneLoader.UnloadScenes(targetScenes);

            watch.Restart();
            yield return new WaitUntil(() => unloadedScenes.Count == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneUnloaded -= sceneUnloaded;

            Assert.AreEqual(sceneCount, unloadedScenes.Count);
            Assert.AreEqual(0, sceneLoader.Manager.LoadedSceneCount);

            void sceneUnloaded(Scene scene)
            {
                unloadedScenes.Add(scene);
            }
        }

        [UnityTest]
        public IEnumerator UnloadScene([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader)
        {
            yield return LoadFirstScene(sceneLoader);

            var loadedScene = sceneLoader.Manager.GetLastLoadedScene();

            sceneLoader.Manager.SceneUnloaded += sceneUnloaded;

            Scene unloadedScene = default;
            sceneLoader.UnloadScene(new LoadSceneInfoName(SceneBuilder.SceneNames[1]));

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => unloadedScene.handle != 0 && !unloadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneUnloaded -= sceneUnloaded;

            Assert.Less(watch.ElapsedMilliseconds, _defaultTimeout);
            Assert.AreEqual(loadedScene, unloadedScene);
            Assert.IsFalse(unloadedScene.isLoaded);

            void sceneUnloaded(Scene scene)
            {
                unloadedScene = scene;
            }
        }

        [UnityTest]
        public IEnumerator TransitionToScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneGroups))] ILoadSceneInfo[] targetScenes, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return LoadFirstScene(sceneLoader);

            int sceneCount = targetScenes.Length;
            if (loadingScene != null)
                sceneCount++;

            var loadedScenes = new List<Scene>(sceneCount);

            sceneLoader.Manager.SceneLoaded += sceneLoaded;
            sceneLoader.TransitionToScenes(targetScenes, 0, loadingScene);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScenes.Count == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(sceneCount, loadedScenes.Count);
            Assert.AreEqual(getTargetActiveScene(), UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            yield return new WaitUntil(() => sceneLoader.Manager.TotalSceneCount == sceneCount);

            void sceneLoaded(Scene scene)
            {
                loadedScenes.Add(scene);
            }

            Scene getTargetActiveScene()
            {
                foreach (var loadedScene in loadedScenes)
                    if (targetScenes[0].IsReferenceToScene(loadedScene))
                        return loadedScene;
                return default;
            }
        }

        [UnityTest]
        public IEnumerator Transition([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneInfos))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return LoadFirstScene(sceneLoader);

            sceneLoader.Manager.SceneLoaded += sceneLoaded;

            Scene loadedScene = default;
            sceneLoader.TransitionToScene(targetScene, loadingScene);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScene, sceneLoader.Manager.GetActiveScene());
            Assert.AreEqual(loadedScene.name, targetScene.Reference);

            yield return new WaitUntil(() => sceneLoader.Manager.TotalSceneCount == 1);

            void sceneLoaded(Scene scene)
            {
                if (targetScene.IsReferenceToScene(scene))
                    loadedScene = scene;
            }
        }

        [UnityTest]
        public IEnumerator Transition_Stress([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneInfos))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return LoadFirstScene(sceneLoader);

            SceneLoaderType type = GetSceneLoaderType(sceneLoader);
            if (type == SceneLoaderType.Coroutine)
                yield break; // There is currently no way to correctly get the loaded/unloaded scene in the SceneLoaderCoroutine

            Scene loadedScene = default;
            var watch = new Stopwatch();

            for (int i = 0; i < 20; i++)
            {
                switch (type)
                {
                    case SceneLoaderType.Async:
                        var task = ((ISceneLoaderAsync)sceneLoader).TransitionToSceneAsync(targetScene, loadingScene).AsTask();
                        yield return new WaitTask(task);
                        loadedScene = task.Result;
                        break;
#if ENABLE_UNITASK
                    case SceneLoaderType.UniTask:
                        var unitask = ((ISceneLoaderUniTask)sceneLoader).TransitionToSceneAsync(targetScene, loadingScene).AsTask();
                        yield return new WaitTask(unitask);
                        loadedScene = unitask.Result;
                        break;
#endif
                    default: throw new NotImplementedException($"Type {type} was not implemented");
                }

                Assert.AreEqual(loadedScene.handle, sceneLoader.Manager.GetActiveScene().handle);
                Assert.AreEqual(loadedScene.name, targetScene.Reference);
            }

            yield return new WaitUntil(() => sceneLoader.Manager.TotalSceneCount == 1);
        }

        [UnityTest]
        public IEnumerator Transition_FromExternalOrigin([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneInfos))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneBuilder.SceneNames[1]);

            sceneLoader.Manager.SceneLoaded += sceneLoaded;

            Scene loadedScene = default;
            sceneLoader.TransitionToScene(targetScene, loadingScene, currentScene);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScene, sceneLoader.Manager.GetActiveScene());
            Assert.AreEqual(loadedScene.name, targetScene.Reference);

            yield return new WaitUntil(() => sceneLoader.Manager.TotalSceneCount == 1);

            void sceneLoaded(Scene scene)
            {
                if (targetScene.IsReferenceToScene(scene))
                    loadedScene = scene;
            }
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_Simple([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc)
        {
            ISceneLoader loader = loaderCreateFunc();
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringLoadScene([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc)
        {
            ISceneLoader loader = loaderCreateFunc();
            loader.LoadScene(new LoadSceneInfoName(SceneBuilder.SceneNames[1]));
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringLoadScenes([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc)
        {
            ISceneLoader loader = loaderCreateFunc();
            loader.LoadScenes(_targetSceneGroups[0]);
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringUnloadScene([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc)
        {
            ISceneLoader loader = loaderCreateFunc();
            yield return LoadFirstScene(loader);

            loader.UnloadScene(new LoadSceneInfoScene(loader.Manager.GetLastLoadedScene()));
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringUnloadScenes([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc)
        {
            ISceneLoader loader = loaderCreateFunc();
            ILoadSceneInfo[] targetScenes = _targetSceneGroups[0];
            int sceneCount = targetScenes.Length;
            loader.LoadScenes(targetScenes);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loader.Manager.LoadedSceneCount == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            loader.UnloadScenes(targetScenes);
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringTransitionToScene([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            ISceneLoader loader = loaderCreateFunc();
            yield return LoadFirstScene(loader);
            ILoadSceneInfo targetScene = _targetSceneInfos[0];

            loader.TransitionToScene(targetScene, loadingScene);
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringTransitionToScenes([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            ISceneLoader loader = loaderCreateFunc();
            yield return LoadFirstScene(loader);
            ILoadSceneInfo[] targetScenes = _targetSceneGroups[0];

            loader.TransitionToScenes(targetScenes, 0, loadingScene);
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

#if ENABLE_UNITASK
        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringLoadSceneAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            SceneLoaderType type = GetSceneLoaderType(loader);
            ILoadSceneInfo targetScene = _targetSceneInfos[0];

            UniTask<Scene> task = default;
            bool isCoroutine = false;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).LoadSceneAsync(targetScene).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).LoadSceneAsync(targetScene);
                    break;
                case SceneLoaderType.Coroutine:
                    ((ISceneLoaderCoroutine)loader).LoadSceneAsync(targetScene);
                    isCoroutine = true;
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            if (isCoroutine)
                return;

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringLoadScenesAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene[]> task = default;
            bool isCoroutine = false;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).LoadScenesAsync(_targetSceneGroups[0]).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).LoadScenesAsync(_targetSceneGroups[0]);
                    break;
                case SceneLoaderType.Coroutine:
                    ((ISceneLoaderCoroutine)loader).LoadScenesAsync(_targetSceneGroups[0]);
                    isCoroutine = true;
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            if (isCoroutine)
                return;

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringUnloadSceneAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            await LoadFirstScene(loader);

            SceneLoaderType type = GetSceneLoaderType(loader);
            ILoadSceneInfo sceneInfo = new LoadSceneInfoScene(loader.Manager.GetLastLoadedScene());

            UniTask<Scene> task = default;
            bool isCoroutine = false;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).UnloadSceneAsync(sceneInfo).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).UnloadSceneAsync(sceneInfo);
                    break;
                case SceneLoaderType.Coroutine:
                    ((ISceneLoaderCoroutine)loader).UnloadSceneAsync(sceneInfo);
                    isCoroutine = true;
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            if (isCoroutine)
                return;

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringUnloadScenesAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            ILoadSceneInfo[] targetScenes = _targetSceneGroups[0];
            int sceneCount = targetScenes.Length;
            loader.LoadScenes(targetScenes);

            await UniTask.WaitUntil(() => loader.Manager.LoadedSceneCount == sceneCount).TimeoutWithoutException(TimeSpan.FromMilliseconds(_defaultTimeout));

            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene[]> task = default;
            bool isCoroutine = false;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).UnloadScenesAsync(targetScenes).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).UnloadScenesAsync(targetScenes);
                    break;
                case SceneLoaderType.Coroutine:
                    ((ISceneLoaderCoroutine)loader).UnloadScenesAsync(targetScenes);
                    isCoroutine = true;
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            if (isCoroutine)
                return;

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringTransitionToSceneAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            await LoadFirstScene(loader);
            ILoadSceneInfo targetScene = _targetSceneInfos[0];

            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene> task = default;
            bool isCoroutine = false;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).TransitionToSceneAsync(targetScene, loadingScene).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).TransitionToSceneAsync(targetScene, loadingScene);
                    break;
                case SceneLoaderType.Coroutine:
                    ((ISceneLoaderCoroutine)loader).TransitionToSceneAsync(targetScene, loadingScene);
                    isCoroutine = true;
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            if (isCoroutine)
                return;

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringTransitionToScenesAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            await LoadFirstScene(loader);
            ILoadSceneInfo[] targetScenes = _targetSceneGroups[0];

            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene[]> task = default;
            bool isCoroutine = false;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).TransitionToScenesAsync(targetScenes, 0, loadingScene).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).TransitionToScenesAsync(targetScenes, 0, loadingScene);
                    break;
                case SceneLoaderType.Coroutine:
                    ((ISceneLoaderCoroutine)loader).TransitionToScenesAsync(targetScenes, 0, loadingScene);
                    isCoroutine = true;
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            if (isCoroutine)
                return;

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });
#endif

        /// <summary>
        /// Required to test transition scenarios, otherwise the initial (test) scene would be unloaded and stop the tests.
        /// </summary>
        IEnumerator LoadFirstScene(ISceneLoader sceneLoader)
        {
            sceneLoader.Manager.SceneLoaded += sceneLoaded;
            sceneLoader.LoadScene(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true);
            Scene loadedScene = default;

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScene.name, SceneBuilder.SceneNames[1]);
            Assert.AreEqual(loadedScene, UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            void sceneLoaded(Scene scene)
            {
                loadedScene = scene;
            }
        }

        SceneLoaderType GetSceneLoaderType(ISceneLoader loader)
        {
            if (loader is SceneLoaderAsync)
                return SceneLoaderType.Async;
#if ENABLE_UNITASK
            else if (loader is SceneLoaderUniTask)
                return SceneLoaderType.UniTask;
#endif
            else if (loader is SceneLoaderCoroutine)
                return SceneLoaderType.Coroutine;

            throw new Exception("Unexpected ISceneLoader type");
        }
    }
}