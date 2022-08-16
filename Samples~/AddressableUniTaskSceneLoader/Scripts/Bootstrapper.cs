/**
 * Bootstrapper.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/28/2022 (en-US)
 */

using MyUnityTools.SceneLoading.Addressables.UniTaskSupport;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MyUnityTools.SceneLoading.Samples
{
    public class Bootstrapper : MonoBehaviour
    {
        public AddressableUniTaskSceneLoader SceneLoader { get; private set; }

        [SerializeField]
        AssetReference _loadingSceneReference;
        [SerializeField]
        AssetReference _initialSceneReference;

        void Start()
        {
            SceneLoader = new AddressableUniTaskSceneLoader(_loadingSceneReference);
            _ = SceneLoader.LoadSceneAsync(_initialSceneReference, true);
        }
    }
}