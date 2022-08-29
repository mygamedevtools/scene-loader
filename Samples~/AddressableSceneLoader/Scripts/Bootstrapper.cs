/**
 * Bootstrapper.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/28/2022 (en-US)
 */

using MyUnityTools.SceneLoading.AddressablesSupport;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MyUnityTools.SceneLoading.Samples
{
    public class Bootstrapper : MonoBehaviour
    {
        public IAddressableSceneLoader SceneLoader { get; private set; }

        [SerializeField]
        AssetReference _loadingSceneReference;
        [SerializeField]
        AssetReference _initialSceneReference;

        void Start()
        {
            SceneLoader = new AddressableAsyncSceneLoader(_loadingSceneReference);
            SceneLoader.LoadScene(new AddressableLoadSceneInfoAsset(_initialSceneReference), true);
        }
    }
}