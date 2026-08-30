#if ENABLE_ADDRESSABLES
using System.Collections;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The static facade against <see cref="AssetReference"/>, the one kind
    /// <see cref="SceneTestEnvironment.SingleSceneRefList"/> cannot carry — asset references have
    /// no deterministic guid across editor sessions, so they must be built at runtime.
    /// <br/><br/>
    /// Deliberately thinner than the instance coverage: every <c>MySceneManager</c> operation is a
    /// one-line delegation to <c>Default</c>, and the scene reference's kind plays no part in that
    /// delegation. These pin that the addressable kind survives the hop; the per-kind behaviour
    /// itself is proven once, on the instance API.
    /// </summary>
    public partial class StaticSceneManager_Tests
    {
        AssetReference[] _assetReferences;
        SceneRef[] _assetReferenceScenes;

        [OneTimeSetUp]
        public void AssetReferenceSetup()
        {
            AsyncOperationHandle<SceneReferenceData> operationHandle = Addressables.LoadAssetAsync<SceneReferenceData>(nameof(SceneReferenceData));
            operationHandle.WaitForCompletion();

            SceneReferenceData sceneReferenceData = operationHandle.Result;
            _assetReferences = sceneReferenceData.sceneReferences.ToArray();

            _assetReferenceScenes = new SceneRef[]
            {
                SceneRef.FromAssetReference(sceneReferenceData.sceneReferences[1]),
                SceneRef.FromAssetReference(sceneReferenceData.sceneReferences[2]),
            };

            Addressables.Release(operationHandle);
        }

        [UnityTest]
        public IEnumerator Load_AssetReference()
        {
            yield return Load(new SceneParameters(_assetReferenceScenes, 0));
        }

        [UnityTest]
        public IEnumerator Transition_AssetReference()
        {
            yield return Transition(new SceneParameters(_assetReferenceScenes, 0));
        }
    }
}
#endif
