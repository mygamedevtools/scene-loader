using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The <c>SceneManagerExtensions</c> tests, kept rather than deleted: the extensions are
    /// gone, but the call sites they covered still have to work. Each now reaches the same
    /// operation through the conversion that replaced its extension method.
    /// </summary>
    public partial class SceneManagerTests
    {
        readonly int[] _buildIndexes = new[] { 1, 2, 3 };

        [UnityTest]
        public IEnumerator Load_ByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(manager, () => manager.LoadAsync(new SceneParameters((SceneRef)1, true), progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_ByIndex_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(manager, () => manager.LoadAsync(new SceneParameters(_buildIndexes, 1), progress), progress, _buildIndexes.Length, 1);
        }

        [UnityTest]
        public IEnumerator Load_ByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(manager, () => manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true), progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_ByName_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(manager, () => manager.LoadAsync(new SceneParameters(SceneBuilder.SceneNames, 1), progress), progress, SceneBuilder.SceneNames.Length, 1);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Load_ByAddress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(manager, () => manager.LoadAsync(new SceneParameters(SceneRef.Address(SceneBuilder.SceneNames[1]), true), progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_ByAddress_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(manager, () => manager.LoadAsync(new SceneParameters(SceneTestEnvironment.Addresses(SceneBuilder.SceneNames), 1), progress), progress, SceneBuilder.SceneNames.Length, 1);
        }

        [UnityTest]
        public IEnumerator Load_ByAssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(manager, () => manager.LoadAsync(new SceneParameters((SceneRef)_assetReferences[1], true), progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_ByAssetReference_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(manager, () => manager.LoadAsync(new SceneParameters(_assetReferences, 1), progress), progress, _assetReferences.Length, 1);
        }
#endif

        [UnityTest]
        public IEnumerator Transition_ByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(1, 1), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByIndex_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(_buildIndexes, 1), _buildIndexes.Length, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(SceneBuilder.SceneNames[1], SceneBuilder.SceneNames[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByName_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(SceneBuilder.SceneNames, SceneBuilder.ScenePaths[0]), SceneBuilder.SceneNames.Length, 0);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Transition_ByAddress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(SceneRef.Address(SceneBuilder.SceneNames[1]), SceneRef.Address(SceneBuilder.SceneNames[0])), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByAddress_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(SceneTestEnvironment.Addresses(SceneBuilder.SceneNames), SceneRef.Address(SceneBuilder.SceneNames[0])), SceneBuilder.SceneNames.Length, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByAssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(_assetReferences[1], _assetReferences[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByAssetReference_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(_assetReferences, _assetReferences[0]), _assetReferences.Length, 0);
        }
#endif

        [UnityTest]
        public IEnumerator Reload_ByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Reload_Template(manager, SceneBuilder.SceneNames[1], () => manager.ReloadActiveSceneAsync(SceneBuilder.SceneNames[1]));
        }

        [UnityTest]
        public IEnumerator Reload_ByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Reload_Template(manager, 1, () => manager.ReloadActiveSceneAsync(1));
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Reload_ByAddress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Reload_Template(manager, SceneRef.Address(SceneBuilder.SceneNames[1]), () => manager.ReloadActiveSceneAsync(SceneRef.Address(SceneBuilder.SceneNames[1])));
        }

        [UnityTest]
        public IEnumerator Reload_ByAssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Reload_Template(manager, _assetReferences[1], () => manager.ReloadActiveSceneAsync(_assetReferences[1]));
        }
#endif

        [UnityTest]
        public IEnumerator Unload_ByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(new SceneParameters((SceneRef)1, true)), () => manager.UnloadAsync(1), 1);
        }

        [UnityTest]
        public IEnumerator Unload_ByIndex_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(new SceneParameters(_buildIndexes, 0)), () => manager.UnloadAsync(_buildIndexes), _buildIndexes.Length);
        }

        [UnityTest]
        public IEnumerator Unload_ByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)), () => manager.UnloadAsync(SceneBuilder.SceneNames[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_ByName_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(new SceneParameters(SceneBuilder.SceneNames, 0)), () => manager.UnloadAsync(SceneBuilder.SceneNames), SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Unload_ByScene_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Task<SceneResult> loadTask = Task.FromResult<SceneResult>(default);
            yield return Unload_Template(manager, () =>
            {
                loadTask = manager.LoadAsync(new SceneParameters(SceneBuilder.SceneNames, 0));
                return loadTask;
            }, () =>
            {
                SceneResult result = loadTask.GetAwaiter().GetResult();
                return manager.UnloadAsync(result.GetScenes());
            }, SceneBuilder.SceneNames.Length);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Unload_ByAddress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(new SceneParameters(SceneRef.Address(SceneBuilder.SceneNames[1]), true)), () => manager.UnloadAsync(SceneRef.Address(SceneBuilder.SceneNames[1])), 1);
        }

        [UnityTest]
        public IEnumerator Unload_ByAddress_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(new SceneParameters(SceneTestEnvironment.Addresses(SceneBuilder.SceneNames), 0)), () => manager.UnloadAsync(SceneTestEnvironment.Addresses(SceneBuilder.SceneNames)), SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Unload_ByAssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(new SceneParameters((SceneRef)_assetReferences[1], true)), () => manager.UnloadAsync(_assetReferences[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_ByAssetReference_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(new SceneParameters(_assetReferences, 0)), () => manager.UnloadAsync(_assetReferences), _assetReferences.Length);
        }
#endif
    }
}
