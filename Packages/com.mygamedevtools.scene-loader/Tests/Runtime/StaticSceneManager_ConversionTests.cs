using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>The static mirror of <see cref="SceneManagerTests"/>' conversion coverage.</summary>
    public partial class StaticSceneManager_Tests
    {
        int[] _buildIndexes = new[] { 1, 2, 3 };

#if ENABLE_ADDRESSABLES
        AssetReference[] _assetReferences;

        [OneTimeSetUp]
        public void AssetReferenceSetup()
        {
            AsyncOperationHandle<SceneReferenceData> operationHandle = Addressables.LoadAssetAsync<SceneReferenceData>(nameof(SceneReferenceData));
            operationHandle.WaitForCompletion();

            SceneReferenceData sceneReferenceData = operationHandle.Result;
            _assetReferences = sceneReferenceData.sceneReferences.ToArray();

            Addressables.Release(operationHandle);
        }
#endif

        [UnityTest]
        public IEnumerator Load_ByIndex()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(new SceneParameters((SceneRef)1, true), progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_ByIndex_Multiple()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(new SceneParameters(_buildIndexes, 1), progress), progress, _buildIndexes.Length, 1);
        }

        [UnityTest]
        public IEnumerator Load_ByName()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true), progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_ByName_Multiple()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(new SceneParameters(SceneBuilder.SceneNames, 1), progress), progress, SceneBuilder.SceneNames.Length, 1);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Load_ByAddress()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(new SceneParameters(SceneRef.Address(SceneBuilder.SceneNames[1]), true), progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_ByAddress_Multiple()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(new SceneParameters(SceneTestEnvironment.Addresses(SceneBuilder.SceneNames), 1), progress), progress, SceneBuilder.SceneNames.Length, 1);
        }

        [UnityTest]
        public IEnumerator Load_ByAssetReference()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(new SceneParameters((SceneRef)_assetReferences[1], true), progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_ByAssetReference_Multiple()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(new SceneParameters(_assetReferences, 1), progress), progress, _assetReferences.Length, 1);
        }
#endif

        [UnityTest]
        public IEnumerator Transition_ByIndex()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(1, 1), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByIndex_Multiple()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(_buildIndexes, 1), _buildIndexes.Length, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByName()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(SceneBuilder.SceneNames[1], SceneBuilder.SceneNames[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByName_Multiple()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(SceneBuilder.SceneNames, SceneBuilder.ScenePaths[0]), SceneBuilder.SceneNames.Length, 0);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Transition_ByAddress()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(SceneRef.Address(SceneBuilder.SceneNames[1]), SceneRef.Address(SceneBuilder.SceneNames[0])), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByAddress_Multiple()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(SceneTestEnvironment.Addresses(SceneBuilder.SceneNames), SceneRef.Address(SceneBuilder.SceneNames[0])), SceneBuilder.SceneNames.Length, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByAssetReference()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(_assetReferences[1], _assetReferences[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_ByAssetReference_Multiple()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(_assetReferences, _assetReferences[0]), SceneBuilder.SceneNames.Length, 0);
        }
#endif

        [UnityTest]
        public IEnumerator Reload_ByName()
        {
            yield return Reload_Template((SceneRef)SceneBuilder.SceneNames[1], () => MySceneManager.ReloadActiveSceneAsync(SceneBuilder.SceneNames[1]));
        }

        [UnityTest]
        public IEnumerator Reload_ByIndex()
        {
            yield return Reload_Template((SceneRef)1, () => MySceneManager.ReloadActiveSceneAsync(1));
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Reload_ByAddress()
        {
            yield return Reload_Template(SceneRef.Address(SceneBuilder.SceneNames[1]), () => MySceneManager.ReloadActiveSceneAsync(SceneRef.Address(SceneBuilder.SceneNames[1])));
        }

        [UnityTest]
        public IEnumerator Reload_ByAssetReference()
        {
            yield return Reload_Template(SceneRef.FromAssetReference(_assetReferences[1]), () => MySceneManager.ReloadActiveSceneAsync(_assetReferences[1]));
        }
#endif

        [UnityTest]
        public IEnumerator Unload_ByIndex()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(new SceneParameters((SceneRef)1, true)), () => MySceneManager.UnloadAsync(1), 1);
        }

        [UnityTest]
        public IEnumerator Unload_ByIndex_Multiple()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(new SceneParameters(_buildIndexes, 0)), () => MySceneManager.UnloadAsync(_buildIndexes), _buildIndexes.Length);
        }

        [UnityTest]
        public IEnumerator Unload_ByName()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)), () => MySceneManager.UnloadAsync(SceneBuilder.SceneNames[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_ByName_Multiple()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(new SceneParameters(SceneBuilder.SceneNames, 0)), () => MySceneManager.UnloadAsync(SceneBuilder.SceneNames), SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Unload_ByScene_Multiple()
        {
            Task<SceneResult> loadTask = Task.FromResult<SceneResult>(default);
            yield return Unload_Template(() =>
            {
                loadTask = MySceneManager.LoadAsync(new SceneParameters(SceneBuilder.SceneNames, 0));
                return loadTask;
            }, () =>
            {
                SceneResult result = loadTask.GetAwaiter().GetResult();
                return MySceneManager.UnloadAsync(result.GetScenes());
            }, SceneBuilder.SceneNames.Length);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Unload_ByAddress()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(new SceneParameters(SceneRef.Address(SceneBuilder.SceneNames[1]), true)), () => MySceneManager.UnloadAsync(SceneRef.Address(SceneBuilder.SceneNames[1])), 1);
        }

        [UnityTest]
        public IEnumerator Unload_ByAddress_Multiple()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(new SceneParameters(SceneTestEnvironment.Addresses(SceneBuilder.SceneNames), 0)), () => MySceneManager.UnloadAsync(SceneTestEnvironment.Addresses(SceneBuilder.SceneNames)), SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Unload_ByAssetReference()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(new SceneParameters((SceneRef)_assetReferences[1], true)), () => MySceneManager.UnloadAsync(_assetReferences[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_ByAssetReference_Multiple()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(new SceneParameters(_assetReferences, 0)), () => MySceneManager.UnloadAsync(_assetReferences), _assetReferences.Length);
        }
#endif
    }
}
