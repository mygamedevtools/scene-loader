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
        AssetReference _loadingSceneReference;
        [SerializeField]
        AssetReference _targetScene;
        [SerializeField]
        AssetReference _additiveScene;
        [SerializeField]
        string _additiveSceneName;

        IAddressableLoadSceneInfo _additiveSceneInfo;
        IAddressableLoadSceneInfo _targetSceneInfo;
        IAddressableSceneLoader _sceneLoader;

        void Awake()
        {
            var bootstrapper = FindObjectOfType<Bootstrapper>();
            _sceneLoader = bootstrapper.SceneLoader;
            _additiveSceneInfo = new AddressableLoadSceneInfoAsset(_additiveScene);
            _targetSceneInfo = new AddressableLoadSceneInfoAsset(_targetScene);
        }
        
        public void TransitionToScene(string runtimeKey) => _sceneLoader.TransitionToScene(new AddressableLoadSceneInfoKey(runtimeKey));

        public void TransitionToScene() => _sceneLoader.TransitionToScene(_targetSceneInfo);

        //public void SwitchToScene(string runtimeKey) => _sceneLoader.SwitchToSceneAsync(new AddressableLoadSceneInfoKey(runtimeKey));

        //public void SwitchToScene() => _sceneLoader.SwitchToSceneAsync(_targetSceneInfo);

        public void LoadScene(string runtimeKey) => _sceneLoader.LoadScene(new AddressableLoadSceneInfoKey(runtimeKey));

        public void LoadScene() => _sceneLoader.LoadScene(_additiveSceneInfo);

        public void UnloadScene()
        {
            var additiveSceneHandle = _sceneLoader.GetLoadedSceneHandle(_additiveSceneName);
            if (additiveSceneHandle.IsValid())
                _sceneLoader.UnloadScene(additiveSceneHandle);
        }
    }
}