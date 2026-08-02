#if ENABLE_ADDRESSABLES
using System.Collections;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    // Note: AssetReference scene refs cannot be created statically, since the scenes are
    // generated in IPrebuildSetup and don't have deterministic guids between Unity Editor
    // sessions. So, we must build them "manually".
    public partial class SceneManagerTests
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
                SceneRef.FromAssetReference(sceneReferenceData.sceneReferences[3]),
                SceneRef.FromAssetReference(sceneReferenceData.sceneReferences[1]),
            };

            Addressables.Release(operationHandle);
        }

        [UnityTest]
        public IEnumerator Load_AssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(nameof(_setIndexActiveParameterValues))] int setIndexActive)
        {
            yield return Load(manager, new SceneParameters(_assetReferenceScenes, setIndexActive));
        }

        [UnityTest]
        public IEnumerator Reload_AssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(nameof(LoadingScenes))] SceneRef loadingScene)
        {
            yield return Reload(manager, _assetReferenceScenes[0], loadingScene);
        }

        [UnityTest]
        public IEnumerator Unload_AssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return Unload(manager, new SceneParameters(_assetReferenceScenes));
        }
    }
}
#endif