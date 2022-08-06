/**
 * AddressableSceneLoaderWrapper.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/28/2022 (en-US)
 */

using MyUnityTools.SceneLoader.Addressables;
using System.Threading.Tasks;
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

        AddressableSceneLoader _sceneLoader;

        void Awake()
        {
            var bootstrapper = FindObjectOfType<Bootstrapper>();
            _sceneLoader = bootstrapper.SceneLoader;
        }

        public void Setup(AddressableSceneLoader sceneLoader) => _sceneLoader = sceneLoader;

        public void TransitionToScene(string runtimeKey) => _sceneLoader.TransitionToSceneAsync(runtimeKey);

        public void TransitionToScene() => _sceneLoader.TransitionToSceneAsync(_targetScene);

        public void SwitchToScene(string runtimeKey) => _sceneLoader.SwitchToSceneAsync(runtimeKey);

        public void SwitchToScene() => _sceneLoader.SwitchToSceneAsync(_targetScene);

        public void LoadScene(string runtimeKey) => _sceneLoader.LoadSceneAsync(runtimeKey);

        public void LoadScene() => _sceneLoader.LoadSceneAsync(_additiveScene);

        public void UnloadScene()
        {
            var additiveSceneHandle = _sceneLoader.GetLoadedSceneHandle(_additiveSceneName);
            if (additiveSceneHandle.IsValid())
                _sceneLoader.UnloadSceneAsync(additiveSceneHandle);
        }
    }
}