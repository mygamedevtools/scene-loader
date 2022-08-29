/**
 * AddressableSceneLoaderWrapper.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/28/2022 (en-US)
 */

using MyUnityTools.SceneLoading.AddressablesSupport;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MyUnityTools.SceneLoading.Samples
{
    public class AddressableSceneLoaderWrapper : MonoBehaviour
    {
        [SerializeField]
        AssetReference _loadingScene;
        [SerializeField]
        AssetReference _targetScene;
        [SerializeField]
        AssetReference _additiveScene;
        [SerializeField]
        string _additiveSceneName;

        IAddressableLoadSceneReference _additiveSceneReference;
        IAddressableLoadSceneReference _loadingSceneReference;
        IAddressableLoadSceneReference _targetSceneReference;
        IAddressableSceneLoader _sceneLoader;

        void Awake()
        {
            var bootstrapper = FindObjectOfType<Bootstrapper>();
            _sceneLoader = bootstrapper.SceneLoader;
            _additiveSceneReference = new AddressableLoadSceneReferenceAsset(_additiveScene);
            _loadingSceneReference = new AddressableLoadSceneReferenceAsset(_loadingScene);
            _targetSceneReference = new AddressableLoadSceneReferenceAsset(_targetScene);
        }

        public void TransitionToScene_Loading(string runtimeKey) => _sceneLoader.TransitionToScene(new AddressableLoadSceneReferenceKey(runtimeKey), _loadingSceneReference);

        public void TransitionToScene_Loading() => _sceneLoader.TransitionToScene(_targetSceneReference, _loadingSceneReference);

        public void TransitionToScene_Simple(string runtimeKey) => _sceneLoader.TransitionToScene(new AddressableLoadSceneReferenceKey(runtimeKey));

        public void TransitionToScene_Simple() => _sceneLoader.TransitionToScene(_targetSceneReference);

        public void LoadScene(string runtimeKey) => _sceneLoader.LoadScene(new AddressableLoadSceneReferenceKey(runtimeKey));

        public void LoadScene() => _sceneLoader.LoadScene(_additiveSceneReference);

        public void UnloadScene() => _sceneLoader.UnloadScene(new AddressableLoadSceneInfoName(_additiveSceneName));
    }
}