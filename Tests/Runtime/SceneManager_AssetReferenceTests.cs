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

        [UnityTest]
        public IEnumerator Transition_AssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(nameof(LoadingScenes))] SceneRef loadingScene)
        {
            yield return Transition(manager, new SceneParameters(_assetReferenceScenes, 0), loadingScene);
        }

        /// <summary>
        /// The conversion shapes for <see cref="AssetReference"/>, which
        /// <c>SceneRefConversionTests</c> covers for every other source type. They live here
        /// because an <see cref="AssetReference"/> cannot be built statically.
        /// </summary>
        [Test]
        public void SceneParameters_ConvertsFromAssetReference()
        {
            SceneParameters single = _assetReferences[1];
            Assert.AreEqual(1, single.Length);
            Assert.AreEqual(SceneRefKind.AssetReference, single.GetSceneRef().Kind);
            Assert.False(single.ShouldSetActive(), "A bare conversion must not silently activate the scene.");

            SceneParameters array = _assetReferences;
            Assert.AreEqual(_assetReferences.Length, array.Length);
            Assert.AreEqual(SceneRefKind.AssetReference, array.GetSceneRefs()[0].Kind);
            Assert.False(array.ShouldSetActive());
        }
    }
}
#endif