using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneManagerTests : SceneTestEnvironment
    {
        static readonly ILoadSceneInfo[][] _loadSceneInfos_multiple = new ILoadSceneInfo[][]
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
        static readonly ILoadSceneInfo[] _loadSceneInfos_single = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1])
        };
        static readonly bool[] _setActiveValues = new bool[] { true, false };
        static readonly int[] _setIndexActiveValues = new int[] { -1, 1 };

        static readonly ISceneManager[] _sceneManagers = new ISceneManager[]
        {
            new SceneManager(),
#if ENABLE_ADDRESSABLES
            new SceneManagerAddressable()
#endif
        };

        static readonly Func<ISceneManager>[] _sceneManagerCreateFuncs = new Func<ISceneManager>[]
        {
            () => new SceneManager(),
#if ENABLE_ADDRESSABLES
            () => new SceneManagerAddressable()
#endif
        };

        int _scenesActivated;
        int _scenesUnloaded;
        int _scenesLoaded;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            for (int i = 0; i < _sceneManagers.Length; i++)
            {
                var manager = _sceneManagers[i];
                manager.ActiveSceneChanged += ReportSceneActivation;
                manager.SceneUnloaded += ReportSceneUnloaded;
                manager.SceneLoaded += ReportSceneLoaded;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            for (int i = 0; i < _sceneManagers.Length; i++)
            {
                var manager = _sceneManagers[i];
                manager.ActiveSceneChanged -= ReportSceneActivation;
                manager.SceneUnloaded -= ReportSceneUnloaded;
                manager.SceneLoaded -= ReportSceneLoaded;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _scenesActivated = 0;
            _scenesUnloaded = 0;
            _scenesLoaded = 0;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = 0; i < _sceneManagers.Length; i++)
                yield return SceneLoaderTestUtilities.UnloadManagerScenes(_sceneManagers[i]);

            yield return SceneLoaderTestUtilities.UnloadRemainingScenes();
            Assert.AreEqual(1, UnityEngine.SceneManagement.SceneManager.sceneCount);
        }

        [UnityTest]
        public IEnumerator SetActive_NotThroughManager([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            Scene loadedScene = default;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += assignLoadedScene;

            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadedScene.IsValid());

            Assert.Throws<InvalidOperationException>(() => manager.SetActiveScene(loadedScene));

            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneBuilder.SceneNames[1]);

            void assignLoadedScene(Scene scene, LoadSceneMode loadSceneMode)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= assignLoadedScene;
                loadedScene = scene;
            }
        }

        [Test]
        public void GetActiveScene_Empty([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            Assert.False(manager.GetActiveScene().IsValid());
        }

        [UnityTest]
        public IEnumerator GetActiveScene_Valid([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            var loadTask = manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true).AsTask();

            yield return new WaitTask(loadTask);

            var loadedScene = loadTask.Result;
            var managerActiveScene = manager.GetActiveScene();

            Assert.True(loadedScene.IsValid());
            Assert.True(managerActiveScene.IsValid());
            Assert.AreEqual(loadedScene, managerActiveScene);
        }

        [Test]
        public void GetLoadedSceneByName_Invalid([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            Assert.Throws<ArgumentException>(() => manager.GetLoadedSceneByName("not-a-real-scene"));
        }

        [UnityTest]
        public IEnumerator GetLoadedSceneByName_Valid([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            yield return new WaitTask(manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1])).AsTask());

            Assert.True(manager.GetLoadedSceneByName(SceneBuilder.SceneNames[1]).IsValid());
        }

        [Test]
        public void EmptyState([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            Assert.False(manager.GetLastLoadedScene().IsValid());
            Assert.False(manager.GetActiveScene().IsValid());
        }

        [Test]
        public void GetLoadedSceneAt_IndexError([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetLoadedSceneAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetLoadedSceneAt(1));
        }

        [UnityTest]
        public IEnumerator LoadScene([ValueSource(nameof(_sceneManagers))] ISceneManager manager, [ValueSource(nameof(_loadSceneInfos_single))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveValues))] bool setActive)
        {
            var loadTask = manager.LoadSceneAsync(sceneInfo, setActive).AsTask();

            Scene eventScene = default;
            manager.SceneLoaded += setEventScene;

            yield return new WaitTask(loadTask);

            manager.SceneLoaded -= setEventScene;
            var loadedScene = loadTask.Result;

            Assert.AreEqual(1, manager.SceneCount);
            Assert.That(setActive ? loadedScene == manager.GetActiveScene() : loadedScene != manager.GetActiveScene());
            Assert.AreEqual(loadedScene, manager.GetLastLoadedScene());
            Assert.AreEqual(loadedScene, manager.GetLoadedSceneByName(loadedScene.name));
            Assert.AreEqual(loadedScene, eventScene);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setActive ? 1 : 0, _scenesActivated);

            void setEventScene(Scene scene) => eventScene = scene;
        }

        [UnityTest]
        public IEnumerator LoadScenes([ValueSource(nameof(_sceneManagers))] ISceneManager manager, [ValueSource(nameof(_loadSceneInfos_multiple))] ILoadSceneInfo[] sceneInfos, [ValueSource(nameof(_setIndexActiveValues))] int setIndexActive)
        {
            int scenesToLoad = sceneInfos.Length;
            var reportedScenes = new List<Scene>(scenesToLoad);
            manager.SceneLoaded += reportSceneLoaded;

            var progress = new SimpleProgress();
            var loadTask = manager.LoadScenesAsync(sceneInfos, setIndexActive, progress).AsTask();

            Assert.AreEqual(0, progress.Value);

            yield return new WaitTask(loadTask);

            manager.SceneLoaded -= reportSceneLoaded;
            var loadedScenes = loadTask.Result;

            Assert.AreEqual(1, progress.Value);
            Assert.AreEqual(scenesToLoad, loadedScenes.Length);
            Assert.AreEqual(scenesToLoad, reportedScenes.Count);
            Assert.AreEqual(scenesToLoad, manager.SceneCount);
            if (setIndexActive >= 0)
                Assert.AreEqual(manager.GetActiveScene(), loadedScenes[setIndexActive]);
            Assert.AreEqual(scenesToLoad, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setIndexActive >= 0 ? 1 : 0, _scenesActivated);

            for (int i = 0; i < scenesToLoad; i++)
                Assert.True(hasReference(sceneInfos[i], reportedScenes));

            void reportSceneLoaded(Scene loadedScene) => reportedScenes.Add(loadedScene);

            bool hasReference(ILoadSceneInfo info, List<Scene> scenes)
            {
                foreach (var scene in scenes)
                    if (info.IsReferenceToScene(scene))
                    {
                        scenes.Remove(scene);
                        return true;
                    }
                return false;
            }
        }

        [UnityTest]
        public IEnumerator LoadScene_Progress([ValueSource(nameof(_sceneManagers))] ISceneManager manager, [ValueSource(nameof(_loadSceneInfos_single))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveValues))] bool setActive)
        {
            var progress = new SimpleProgress();
            Assert.AreEqual(0, progress.Value);
            yield return new WaitTask(manager.LoadSceneAsync(sceneInfo, setActive, progress).AsTask());
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator LoadScene_Multiple([ValueSource(nameof(_sceneManagers))] ISceneManager manager, [ValueSource(nameof(_loadSceneInfos_multiple))] ILoadSceneInfo[] sceneInfos, [ValueSource(nameof(_setActiveValues))] bool setActive)
        {
            var length = sceneInfos.Length;
            var loadedScenes = new Scene[length];

            for (int i = 0; i < length; i++)
            {
                var loadTask = manager.LoadSceneAsync(sceneInfos[i], setActive).AsTask();
                yield return new WaitTask(loadTask);
                loadedScenes[i] = loadTask.Result;
            }

            Assert.AreEqual(length, manager.SceneCount);
            Assert.AreEqual(loadedScenes[^1], manager.GetLastLoadedScene());

            for (int i = 0; i < length; i++)
                Assert.AreEqual(loadedScenes[i], manager.GetLoadedSceneAt(i));

            Assert.That(setActive ? loadedScenes[^1] == manager.GetActiveScene() : loadedScenes[^1] != manager.GetActiveScene());
            Assert.AreEqual(3, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setActive ? 3 : 0, _scenesActivated);
        }

        [Test]
        public void LoadScene_NotInBuild([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            var sceneName = "not-a-real-scene";
            if (manager is SceneManager)
                LogAssert.Expect(LogType.Error, new Regex("'not-a-real-scene' couldn't be loaded"));
            var wait = new WaitTask(manager.LoadSceneAsync(new LoadSceneInfoName(sceneName), false).AsTask());
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator UnloadScene([ValueSource(nameof(_sceneManagers))] ISceneManager manager, [ValueSource(nameof(_loadSceneInfos_single))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveValues))] bool setActive)
        {
            yield return new WaitTask(manager.LoadSceneAsync(sceneInfo, setActive).AsTask());

            Scene eventScene = default;
            manager.SceneUnloaded += setEventScene;

            var workingScene = manager.GetLastLoadedScene();

            var task = manager.UnloadSceneAsync(new LoadSceneInfoScene(workingScene)).AsTask();
            yield return new WaitTask(task);

            manager.SceneUnloaded -= setEventScene;
            var unloadedScene = task.Result;

            Assert.AreEqual(workingScene, unloadedScene);
            Assert.AreEqual(workingScene, eventScene);
            Assert.IsFalse(workingScene.isLoaded);
            Assert.IsFalse(manager.GetActiveScene().IsValid());
            Assert.AreEqual(0, manager.SceneCount);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(1, _scenesUnloaded);
            Assert.AreEqual(setActive ? 2 : 0, _scenesActivated, "Activated scenes did not match expectation");

            void setEventScene(Scene scene) => eventScene = scene;
        }

        [UnityTest]
        public IEnumerator UnloadScenes([ValueSource(nameof(_sceneManagers))] ISceneManager manager, [ValueSource(nameof(_loadSceneInfos_multiple))] ILoadSceneInfo[] sceneInfos, [ValueSource(nameof(_setIndexActiveValues))] int setIndexActive)
        {
            var loadTask = manager.LoadScenesAsync(sceneInfos, setIndexActive).AsTask();
            yield return new WaitTask(loadTask);
            var loadedSceneHandles = loadTask.Result.Select(s => s.handle).ToArray();

            int scenesToUnload = sceneInfos.Length;
            var reportedScenes = new List<Scene>(scenesToUnload);
            manager.SceneUnloaded += reportSceneUnloaded;

            var task = manager.UnloadScenesAsync(sceneInfos).AsTask();
            yield return new WaitTask(task);

            manager.SceneUnloaded -= reportSceneUnloaded;
            var unloadedScenes = task.Result;

            Assert.AreEqual(scenesToUnload, unloadedScenes.Length);
            Assert.AreEqual(scenesToUnload, reportedScenes.Count);
            Assert.AreEqual(0, manager.SceneCount);
            Assert.AreEqual(scenesToUnload, _scenesLoaded);
            Assert.AreEqual(scenesToUnload, _scenesUnloaded);
            Assert.AreEqual(setIndexActive >= 0 ? 2 : 0, _scenesActivated, "Activated scenes did not match expectation");

            for (int i = 0; i < scenesToUnload; i++)
                Assert.True(hasReference(loadedSceneHandles[i], reportedScenes));

            void reportSceneUnloaded(Scene loadedScene) => reportedScenes.Add(loadedScene);

            bool hasReference(int handle, List<Scene> scenes)
            {
                foreach (var scene in scenes)
                    if (scene.handle == handle)
                    {
                        scenes.Remove(scene);
                        return true;
                    }
                return false;
            }
        }

        [Test]
        public void UnloadScene_NotLoaded([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            var sceneName = "not-a-real-scene";
            LogAssert.Expect(LogType.Warning, new Regex("Some of the scenes could not be found loaded"));
            var wait = new WaitTask(manager.UnloadSceneAsync(new LoadSceneInfoName(sceneName)).AsTask());
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator LoadAndUnload_BuildIndex()
        {
#if UNITY_EDITOR
            var firstScene = new LoadSceneInfoIndex(0);
#else
            var firstScene = new LoadSceneInfoIndex(1);
#endif

            var task = _sceneManagers[0].LoadSceneAsync(firstScene).AsTask();
            yield return new WaitTask(task);

            Assert.True(firstScene.IsReferenceToScene(task.Result));
            var handle = task.Result.handle;

            task = _sceneManagers[0].UnloadSceneAsync(firstScene).AsTask();
            yield return new WaitTask(task);

            Assert.AreEqual(handle, task.Result.handle);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator LoadAndUnload_AssetReference()
        {
            var sceneReferenceDataOperation = Addressables.LoadAssetAsync<SceneReferenceData>(nameof(SceneReferenceData));
            sceneReferenceDataOperation.WaitForCompletion();

            var targetScene = new LoadSceneInfoAssetReference(sceneReferenceDataOperation.Result.sceneReferences[1]);

            var task = _sceneManagers[1].LoadSceneAsync(targetScene).AsTask();
            yield return new WaitTask(task);

            Assert.True(targetScene.IsReferenceToScene(task.Result));

            task = _sceneManagers[1].UnloadSceneAsync(targetScene).AsTask();
            yield return new WaitTask(task);

            Assert.Zero(_sceneManagers[1].SceneCount);
        }
#endif

        [UnityTest]
        public IEnumerator LoadByName_UnloadByScene([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            var task = manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1])).AsTask();

            yield return new WaitTask(task);

            var scene = task.Result;

            task = manager.UnloadSceneAsync(new LoadSceneInfoScene(scene)).AsTask();

            yield return new WaitTask(task);

            Assert.Zero(_sceneManagers[1].SceneCount);
        }

        [Test]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public void Dispose_Simple([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc)
        {
            ISceneManager manager = managerCreateFunc();
            Assert.DoesNotThrow(manager.Dispose);
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringLoadScene([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc)
        {
            ISceneManager manager = managerCreateFunc();
            Task task = manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1])).AsTask();
            manager.Dispose();
            yield return new WaitTask(task);
            Assert.That(task.IsCompleted);
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dispose_DuringLoadScenes([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc)
        {
            ISceneManager manager = managerCreateFunc();
            Task task = manager.LoadScenesAsync(_loadSceneInfos_multiple[0]).AsTask();
            manager.Dispose();
            yield return new WaitTask(task);
            Assert.True(task.IsCanceled);
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dipose_DuringUnloadScene([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc)
        {
            ISceneManager manager = managerCreateFunc();
            ILoadSceneInfo sceneInfo = new LoadSceneInfoName(SceneBuilder.SceneNames[1]);
            Task task = manager.LoadSceneAsync(sceneInfo).AsTask();
            yield return new WaitTask(task);

            task = manager.UnloadSceneAsync(sceneInfo).AsTask();
            manager.Dispose();
            yield return new WaitTask(task);
            Assert.True(task.IsCanceled);
        }

        [UnityTest]
        [Category(SceneLoaderTestUtilities.DisposeCategoryName)]
        public IEnumerator Dipose_DuringUnloadScenes([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc)
        {
            ISceneManager manager = managerCreateFunc();
            Task task = manager.LoadScenesAsync(_loadSceneInfos_multiple[0]).AsTask();
            yield return new WaitTask(task);

            task = manager.UnloadScenesAsync(_loadSceneInfos_multiple[0]).AsTask();
            manager.Dispose();
            yield return new WaitTask(task);
            Assert.True(task.IsCanceled);
        }

        [UnityTest]
        public IEnumerator Cancellation_DuringLoadScene([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            CancellationTokenSource tokenSource = new();
            Task task = manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), token: tokenSource.Token).AsTask();
            tokenSource.Cancel();
            yield return new WaitTask(task);
            Assert.That(task.IsCompleted);
            tokenSource.Dispose();
        }

        [UnityTest]
        public IEnumerator Cancellation_DuringLoadScenes([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            CancellationTokenSource tokenSource = new();
            Task task = manager.LoadScenesAsync(_loadSceneInfos_multiple[0], token: tokenSource.Token).AsTask();
            tokenSource.Cancel();
            yield return new WaitTask(task);
            Assert.True(task.IsCanceled);
            tokenSource.Dispose();
        }

        [UnityTest]
        public IEnumerator Cancellation_DuringUnloadScene([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            CancellationTokenSource tokenSource = new();
            ILoadSceneInfo sceneInfo = new LoadSceneInfoName(SceneBuilder.SceneNames[1]);
            Task task = manager.LoadSceneAsync(sceneInfo).AsTask();
            yield return new WaitTask(task);

            task = manager.UnloadSceneAsync(sceneInfo, token: tokenSource.Token).AsTask();
            tokenSource.Cancel();
            yield return new WaitTask(task);
            Assert.True(task.IsCanceled);
            tokenSource.Dispose();
        }

        [UnityTest]
        public IEnumerator Cancellation_DuringUnloadScenes([ValueSource(nameof(_sceneManagers))] ISceneManager manager)
        {
            CancellationTokenSource tokenSource = new();
            Task task = manager.LoadScenesAsync(_loadSceneInfos_multiple[0]).AsTask();
            yield return new WaitTask(task);

            task = manager.UnloadScenesAsync(_loadSceneInfos_multiple[0], token: tokenSource.Token).AsTask();
            tokenSource.Cancel();
            yield return new WaitTask(task);
            Assert.True(task.IsCanceled);
            tokenSource.Dispose();
        }

        void ReportSceneActivation(Scene previousScene, Scene newScene)
        {
            _scenesActivated++;
        }

        void ReportSceneUnloaded(Scene unloadedScene)
        {
            _scenesUnloaded++;
        }

        void ReportSceneLoaded(Scene loadedScene)
        {
            _scenesLoaded++;
        }
    }

    public class SimpleProgress : IProgress<float>
    {
        public float Value;

        public void Report(float value) => Value = value;
    }
}