using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public partial class SceneManagerTests : SceneTestBase
    {
        // `default` is how "no loading scene" is spelled now that the parameter is a value type.
        public static readonly SceneRef[] LoadingScenes = new SceneRef[]
        {
            default,
            SceneBuilder.SceneNames[3],
            SceneBuilder.SceneNames[0],
        };

        static readonly bool[] _setActiveParameterValues = new[] { false, true };
        static readonly int[] _setIndexActiveParameterValues = new[] { -1, 1 };

        int _scenesActivated;
        int _scenesUnloaded;
        int _scenesLoaded;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            SceneTestEnvironment.ValidateSceneEnvironment();

            ISceneManager[] sceneManagers = SceneTestEnvironment.SceneManagers;
            for (int i = 0; i < sceneManagers.Length; i++)
            {
                var manager = sceneManagers[i];
                manager.ActiveSceneChanged += ReportSceneActivation;
                manager.SceneUnloaded += ReportSceneUnloaded;
                manager.SceneLoaded += ReportSceneLoaded;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            ISceneManager[] sceneManagers = SceneTestEnvironment.SceneManagers;
            for (int i = 0; i < sceneManagers.Length; i++)
            {
                var manager = sceneManagers[i];
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

        [UnityTest]
        public IEnumerator Constructor_AddLoadedScenes()
        {
            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);

            ISceneManager sceneManager = new CoreSceneManager(true);

            Assert.AreEqual(2, sceneManager.LoadedSceneCount);
            Assert.AreEqual(sceneManager.TotalSceneCount, sceneManager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Constructor_InitializationScenes()
        {
            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            ISceneManager sceneManager = new CoreSceneManager(new Scene[] { loadedScene });

            Assert.AreEqual(1, sceneManager.LoadedSceneCount);
            Assert.AreEqual(sceneManager.TotalSceneCount, sceneManager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator InitializationScene_Unload()
        {
            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            ISceneManager sceneManager = new CoreSceneManager(new Scene[] { loadedScene });

            SceneOperation unloadOperation = null;
            Assert.DoesNotThrow(() => unloadOperation = sceneManager.UnloadAsync(new SceneParameters(SceneRef.FromScene(loadedScene))));

            yield return unloadOperation.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator SetActive_NotThroughmanager([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Scene loadedScene = default;
            SceneManager.sceneLoaded += assignLoadedScene;

            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadedScene.IsValid());

            Assert.Throws<InvalidOperationException>(() => manager.SetActiveScene(loadedScene));

            yield return SceneManager.UnloadSceneAsync(loadedScene);

            void assignLoadedScene(Scene scene, LoadSceneMode loadSceneMode)
            {
                SceneManager.sceneLoaded -= assignLoadedScene;
                loadedScene = scene;
            }
        }

        [Test]
        public void GetActiveScene_Empty([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Assert.False(manager.GetActiveScene().IsValid());
        }

        [UnityTest]
        public IEnumerator GetActiveScene_Valid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            SceneOperation loadOperation = manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true));

            yield return loadOperation.ToCoroutine();

            Scene loadedScene = loadOperation.Result;
            var managerActiveScene = manager.GetActiveScene();

            Assert.True(loadedScene.IsValid());
            Assert.True(managerActiveScene.IsValid());
            Assert.AreEqual(loadedScene, managerActiveScene);
        }

        [Test]
        public void GetLoadedSceneByName_Invalid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Assert.Throws<ArgumentException>(() => manager.GetLoadedSceneByName("not-a-real-scene"));
        }

        [UnityTest]
        public IEnumerator GetLoadedSceneByName_Valid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1])).ToCoroutine();

            Assert.True(manager.GetLoadedSceneByName(SceneBuilder.SceneNames[1]).IsValid());
        }

        [Test]
        public void EmptyState([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Assert.False(manager.GetLastLoadedScene().IsValid());
            Assert.False(manager.GetActiveScene().IsValid());
        }

        [Test]
        public void GetLoadedSceneAt_IndexError([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetLoadedSceneAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetLoadedSceneAt(1));
        }

        [UnityTest]
        public IEnumerator Load([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            return Load_Template(manager, () => manager.LoadAsync(sceneParameters), sceneParameters.Length, sceneParameters.GetIndexToActivate());
        }

        [UnityTest]
        public IEnumerator Load_Progress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            SceneOperation operation = manager.LoadAsync(sceneParameters);

            var progress = new SimpleProgress();
            operation.Progressed += progress.Report;
            Assert.AreEqual(0, progress.Value);

            yield return operation.ToCoroutine();
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator Load_Stress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            SceneRef[] sceneRefs = sceneParameters.GetSceneRefs();
            int length = sceneRefs.Length;
            bool setActive = sceneParameters.GetIndexToActivate() == 1;

            var loadedScenes = new Scene[length];

            for (int i = 0; i < length; i++)
            {
                SceneOperation loadOperation = manager.LoadAsync(new SceneParameters(sceneRefs[i], setActive));
                yield return loadOperation.ToCoroutine();
                loadedScenes[i] = loadOperation.Result;
            }

            Assert.AreEqual(length, manager.LoadedSceneCount);
            Assert.AreEqual(loadedScenes[^1], manager.GetLastLoadedScene());

            for (int i = 0; i < length; i++)
                Assert.AreEqual(loadedScenes[i], manager.GetLoadedSceneAt(i));

            Assert.That(setActive ? loadedScenes[^1] == manager.GetActiveScene() : loadedScenes[^1] != manager.GetActiveScene());
            Assert.AreEqual(length, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setActive ? length : 0, _scenesActivated);
        }

        // Resolution fails before Unity is asked to load, so there is no engine error to expect
        // — and deciding a key is not addressable needs the catalog, so this now waits.
        [UnityTest]
        public IEnumerator Load_NotInBuild([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            LogAssert.Expect(LogType.Error, new Regex("faulted during Resolving"));

            SceneOperation operation = manager.LoadAsync("not-a-real-scene");
            yield return new WaitUntil(() => operation.IsDone);

            Assert.AreEqual(SceneOperationState.Faulted, operation.State);
            Assert.That(operation.Exception.Message, Does.Contain("build settings"));
        }

        [UnityTest]
        public IEnumerator Unload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(sceneParameters), () => manager.UnloadAsync(sceneParameters), sceneParameters.Length);
        }

        [UnityTest]
        public IEnumerator Unload_NotLoaded([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            // Only the fault is reported now; the linker throws without logging first.
            LogAssert.Expect(LogType.Error, new Regex("faulted during"));

            SceneOperation operation = manager.UnloadAsync("not-a-real-scene");
            yield return new WaitUntil(() => operation.IsDone);

            Assert.AreEqual(SceneOperationState.Faulted, operation.State);
        }

        [UnityTest]
        public IEnumerator Reload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleSceneRefList))] SceneRef sceneRef, [ValueSource(nameof(LoadingScenes))] SceneRef loadingScene)
        {
            yield return Reload_Template(manager, sceneRef, () => manager.ReloadActiveSceneAsync(loadingScene));
        }

        [UnityTest]
        public IEnumerator Transition([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.
        TransitionSceneParametersList))] SceneParameters sceneParameters, [ValueSource(nameof(LoadingScenes))] SceneRef loadingScene)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(sceneParameters, loadingScene), sceneParameters.Length, sceneParameters.GetIndexToActivate());
        }

        [UnityTest]
        public IEnumerator Transition_NoSourceScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleSceneRefList))] SceneRef targetScene, [ValueSource(nameof(LoadingScenes))] SceneRef loadingScene)
        {
            int expectedLoadedScenes = loadingScene.IsValid ? 2 : 1;
            // If there's no loading scene, the scene manager will create a temporary scene
            // for the transition, and will unload it after the transition is complete.
            int expectedUnloadedScenes = 1;

            int unloadedScenesCount = 0;

            // The temporary scene unload does not go through the ISceneManager
            SceneManager.sceneUnloaded += sceneUnloaded;

            SceneOperation operation = manager.TransitionAsync(new SceneParameters(targetScene, true), loadingScene);

            yield return operation.ToCoroutine();

            Scene loadedScene = operation.Result;

            SceneManager.sceneUnloaded -= sceneUnloaded;

            Assert.AreEqual(loadedScene, manager.GetActiveScene());
            Assert.AreEqual(expectedLoadedScenes, _scenesLoaded);
            Assert.AreEqual(expectedUnloadedScenes, unloadedScenesCount);

            yield return WaitForTotalSceneCount(manager, 1);

            void sceneUnloaded(Scene scene)
            {
                unloadedScenesCount++;
            }
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleSceneRefList))] SceneRef sceneRef)
        {
            SceneOperation operation = manager.LoadAsync(new SceneParameters(sceneRef));

            yield return operation.ToCoroutine();

            Scene scene = operation.Result;

            yield return manager.UnloadAsync(scene).ToCoroutine();

            Assert.Zero(manager.LoadedSceneCount);
        }

        /// <summary>
        /// Unloading by the <see cref="Scene"/> array a load handed back. The only end-to-end
        /// cover for the <c>Scene[]</c> conversion — every other source type is asserted at the
        /// shape level in <c>SceneRefConversionTests</c>, but a <see cref="Scene"/> has to be
        /// loaded before it can be converted.
        /// </summary>
        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadBySceneArray([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            SceneOperation loadOperation = null;
            yield return Unload_Template(manager, () =>
            {
                loadOperation = manager.LoadAsync(new SceneParameters(SceneBuilder.SceneNames, 0));
                return loadOperation;
            }, () =>
            {
                SceneResult result = loadOperation.Result;
                return manager.UnloadAsync(result.GetScenes());
            }, SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleSceneRefList_NoAddressable))] SceneRef sceneRef)
        {
            yield return manager.LoadAsync(new SceneParameters(sceneRef)).ToCoroutine();

            yield return manager.UnloadAsync(SceneBuilder.SceneNames[1]).ToCoroutine();

            Assert.Zero(manager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByPath([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleSceneRefList_NoAddressable))] SceneRef sceneRef)
        {
            yield return manager.LoadAsync(new SceneParameters(sceneRef)).ToCoroutine();

            yield return manager.UnloadAsync(SceneBuilder.ScenePaths[1]).ToCoroutine();

            Assert.Zero(manager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleSceneRefList_NoAddressable))] SceneRef sceneRef)
        {
            yield return manager.LoadAsync(new SceneParameters(sceneRef)).ToCoroutine();

            // The index the scene actually landed on, rather than the one it happens to occupy
            // in this project. A hardcoded index breaks as soon as the Build Settings hold
            // anything else — a sample, or the consuming project's own scenes.
            int buildIndex = manager.GetLastLoadedScene().buildIndex;
            yield return manager.UnloadAsync(buildIndex).ToCoroutine();

            Assert.Zero(manager.LoadedSceneCount);
        }

        public IEnumerator Load_Template(ISceneManager manager, Func<SceneOperation> loadOperation, int sceneCount, int setIndexActive)
        {
            var reportedScenes = new List<Scene>(sceneCount);
            manager.SceneLoaded += reportSceneLoaded;

            SceneOperation operation = loadOperation();

            var progress = new SimpleProgress();
            operation.Progressed += progress.Report;
            Assert.AreEqual(0, progress.Value);
            Assert.AreEqual(0, operation.Progress);

            yield return operation.ToCoroutine();

            manager.SceneLoaded -= reportSceneLoaded;
            Scene[] loadedScenes = operation.Result;

            Assert.AreEqual(SceneOperationState.Completed, operation.State);
            Assert.AreEqual(1, progress.Value);
            Assert.AreEqual(1, operation.Progress);
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(sceneCount, manager.LoadedSceneCount);
            if (setIndexActive >= 0)
                Assert.AreEqual(manager.GetActiveScene(), loadedScenes[setIndexActive]);
            Assert.AreEqual(sceneCount, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setIndexActive >= 0 ? 1 : 0, _scenesActivated);

            void reportSceneLoaded(Scene loadedScene) => reportedScenes.Add(loadedScene);
        }

        public IEnumerator Reload_Template(ISceneManager manager, SceneRef sceneRef, Func<SceneOperation> reloadOperation)
        {
            yield return manager.LoadAsync(new SceneParameters(sceneRef, true)).ToCoroutine();
            string activeScene = manager.GetActiveScene().name;

            SceneOperation operation = reloadOperation();
            yield return operation.ToCoroutine();

            Scene loadedScene = operation.Result;
            Assert.AreEqual(manager.GetActiveScene(), loadedScene);
            Assert.AreEqual(activeScene, loadedScene.name);

            yield return WaitForTotalSceneCount(manager, 1);
        }

        public IEnumerator Transition_Template(ISceneManager manager, Func<SceneOperation> transitionOperation, int sceneCount, int setIndexActive)
        {
            yield return LoadFirstScene(manager);

            SceneOperation operation = transitionOperation();
            yield return operation.ToCoroutine();

            Scene[] loadedScenes = operation.Result;
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(loadedScenes[setIndexActive], manager.GetActiveScene());

            yield return WaitForTotalSceneCount(manager, sceneCount);
        }

        public IEnumerator Unload_Template(ISceneManager manager, Func<SceneOperation> loadOperation, Func<SceneOperation> unloadOperation, int sceneCount)
        {
            var load = loadOperation();
            yield return load.ToCoroutine();
            var loadedScenes = load.Result.GetScenes();

            var reportedScenes = new List<Scene>(sceneCount);
            manager.SceneUnloaded += reportSceneUnloaded;

            var unload = unloadOperation();
            yield return unload.ToCoroutine();

            manager.SceneUnloaded -= reportSceneUnloaded;
            Scene[] unloadedScenes = unload.Result;

            Assert.AreEqual(sceneCount, unloadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(0, manager.LoadedSceneCount);
            Assert.AreEqual(sceneCount, _scenesLoaded);
            Assert.AreEqual(sceneCount, _scenesUnloaded);

            for (int i = 0; i < sceneCount; i++)
                Assert.True(hasReference(loadedScenes[i], reportedScenes));

            void reportSceneUnloaded(Scene loadedScene) => reportedScenes.Add(loadedScene);

            bool hasReference(Scene expectedScene, List<Scene> scenes)
            {
                foreach (var scene in scenes)
                    if (scene.handle == expectedScene.handle)
                    {
                        scenes.Remove(scene);
                        return true;
                    }
                return false;
            }
        }

        /// <summary>
        /// Required to test some transition scenarios.
        /// </summary>
        public static IEnumerator LoadFirstScene(ISceneManager sceneManager) => sceneManager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

        /// <summary>
        /// Waits for the manager's bookkeeping to settle, and says what it settled on if it
        /// never does. A bare WaitUntil here turns a bookkeeping bug into a hung test run,
        /// which is a much worse way to find out.
        /// </summary>
        public static IEnumerator WaitForTotalSceneCount(ISceneManager manager, int expected)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (manager.TotalSceneCount != expected)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    List<string> loaded = new();
                    for (int i = 0; i < manager.LoadedSceneCount; i++)
                        loaded.Add(manager.GetLoadedSceneAt(i).name);

                    Assert.Fail($"Timed out waiting for TotalSceneCount to reach {expected}. It is {manager.TotalSceneCount}, with these loaded: {string.Join(", ", loaded)}.");
                }

                yield return null;
            }
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