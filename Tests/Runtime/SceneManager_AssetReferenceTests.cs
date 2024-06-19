#if ENABLE_ADDRESSABLES
using System.Collections;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    // Note: AssetReference load scene infos cannot be created statically, since the
    // scenes are generated in IPrebuildSetup and don't have deterministic guids between
    // Unity Editor sessions. So, we must test AssetReference load scene infos "manually".
    public partial class SceneManagerTests
    {
        ILoadSceneInfo[] _assetReferenceLoadSceneInfos;

        [OneTimeSetUp]
        public void AssetReferenceSetup()
        {
            AsyncOperationHandle<SceneReferenceData> operationHandle = Addressables.LoadAssetAsync<SceneReferenceData>(nameof(SceneReferenceData));
            operationHandle.WaitForCompletion();

            SceneReferenceData sceneReferenceData = operationHandle.Result;

            _assetReferenceLoadSceneInfos = new ILoadSceneInfo[]
            {
                new LoadSceneInfoAssetReference(sceneReferenceData.sceneReferences[1]),
                new LoadSceneInfoAssetReference(sceneReferenceData.sceneReferences[2]),
                new LoadSceneInfoAssetReference(sceneReferenceData.sceneReferences[3]),
                new LoadSceneInfoAssetReference(sceneReferenceData.sceneReferences[1]),
            };

            Addressables.Release(operationHandle);
        }

        [UnityTest]
        public IEnumerator LoadScene_AssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(nameof(_setActiveParameterValues))] bool setActive)
        {
            yield return LoadScene(manager, _assetReferenceLoadSceneInfos[1], setActive);
        }

        [UnityTest]
        public IEnumerator LoadScenes_AssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(nameof(_setIndexActiveParameterValues))] int setIndexActive)
        {
            yield return LoadScenes(manager, _assetReferenceLoadSceneInfos, setIndexActive);
        }

        [UnityTest]
        public IEnumerator UnloadScene_AssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(nameof(_setActiveParameterValues))] bool setActive)
        {
            yield return UnloadScene(manager, _assetReferenceLoadSceneInfos[1], setActive);
        }

        [UnityTest]
        public IEnumerator UnloadScenes_AssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(nameof(_setIndexActiveParameterValues))] int setIndexActive)
        {
            yield return UnloadScenes(manager, _assetReferenceLoadSceneInfos, setIndexActive);
        }
    }
}
#endif