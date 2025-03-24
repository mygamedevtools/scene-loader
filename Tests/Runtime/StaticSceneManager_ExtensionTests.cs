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
        public IEnumerator Load_Extension_ByIndex()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(1, true, progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_Extension_ByIndex_Multiple()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(_buildIndexes, 1, progress), progress, _buildIndexes.Length, 1);
        }

        [UnityTest]
        public IEnumerator Load_Extension_ByName()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(SceneBuilder.SceneNames[1], true, progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_Extension_ByName_Multiple()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAsync(SceneBuilder.SceneNames, 1, progress), progress, SceneBuilder.SceneNames.Length, 1);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Load_Extension_Addressable_ByAddress()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAddressableAsync(SceneBuilder.SceneNames[1], true, progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_Extension_Addressable_ByAddress_Multiple()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAddressableAsync(SceneBuilder.SceneNames, 1, progress), progress, SceneBuilder.SceneNames.Length, 1);
        }

        [UnityTest]
        public IEnumerator Load_Extension_Addressable_ByAssetReference()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAddressableAsync(_assetReferences[1], true, progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_Extension_Addressable_ByAssetReference_Multiple()
        {
            var progress = new SimpleProgress();
            yield return Load_Template(() => MySceneManager.LoadAddressableAsync(_assetReferences, 1, progress), progress, _assetReferences.Length, 1);
        }
#endif

        [UnityTest]
        public IEnumerator Transition_Extension_ByIndex()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(1, 1), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_ByIndex_Multiple()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(_buildIndexes, 1), _buildIndexes.Length, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_ByName()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(SceneBuilder.SceneNames[1], SceneBuilder.SceneNames[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_ByName_Multiple()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(SceneBuilder.SceneNames, SceneBuilder.ScenePaths[0]), SceneBuilder.SceneNames.Length, 0);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Transition_Extension_Addressable_ByAddress()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAddressableAsync(SceneBuilder.SceneNames[1], SceneBuilder.SceneNames[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_Addressable_ByAddress_Multiple()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAddressableAsync(SceneBuilder.SceneNames, SceneBuilder.SceneNames[0]), SceneBuilder.SceneNames.Length, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_Addressable_ByAssetReference()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAddressableAsync(_assetReferences[1], _assetReferences[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_Addressable_ByAssetReference_Multiple()
        {
            yield return Transition_Template(() => MySceneManager.TransitionAddressableAsync(_assetReferences, _assetReferences[0]), SceneBuilder.SceneNames.Length, 0);
        }
#endif

        [UnityTest]
        public IEnumerator Unload_Extension_ByIndex()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(1, true), () => MySceneManager.UnloadAsync(1), 1);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_ByIndex_Multiple()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(_buildIndexes, 0), () => MySceneManager.UnloadAsync(_buildIndexes), _buildIndexes.Length);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_ByName()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(SceneBuilder.SceneNames[1], true), () => MySceneManager.UnloadAsync(SceneBuilder.SceneNames[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_ByName_Multiple()
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(SceneBuilder.SceneNames, 0), () => MySceneManager.UnloadAsync(SceneBuilder.SceneNames), SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_ByScene_Multiple()
        {
            Task<SceneResult> loadTask = Task.FromResult<SceneResult>(default);
            yield return Unload_Template(() =>
            {
                loadTask = MySceneManager.LoadAsync(SceneBuilder.SceneNames, 0);
                return loadTask;
            }, () =>
            {
                SceneResult result = loadTask.GetAwaiter().GetResult();
                return MySceneManager.UnloadAsync(result.GetScenes());
            }, SceneBuilder.SceneNames.Length);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Unload_Extension_Addressable_ByAddress()
        {
            yield return Unload_Template(() => MySceneManager.LoadAddressableAsync(SceneBuilder.SceneNames[1], true), () => MySceneManager.UnloadAddressableAsync(SceneBuilder.SceneNames[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_Addressable_ByAddress_Multiple()
        {
            yield return Unload_Template(() => MySceneManager.LoadAddressableAsync(SceneBuilder.SceneNames, 0), () => MySceneManager.UnloadAddressableAsync(SceneBuilder.SceneNames), SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_Addressable_ByAssetReference()
        {
            yield return Unload_Template(() => MySceneManager.LoadAddressableAsync(_assetReferences[1], true), () => MySceneManager.UnloadAddressableAsync(_assetReferences[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_Addressable_ByAssetReference_Multiple()
        {
            yield return Unload_Template(() => MySceneManager.LoadAddressableAsync(_assetReferences, 0), () => MySceneManager.UnloadAddressableAsync(_assetReferences), _assetReferences.Length);
        }
#endif
    }
}
