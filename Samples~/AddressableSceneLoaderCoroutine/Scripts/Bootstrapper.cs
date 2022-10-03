/**
 * Bootstrapper.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/28/2022 (en-US)
 */

using MyGameDevTools.SceneLoading.AddressablesSupport;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MyGameDevTools.SceneLoading.Samples
{
    public class Bootstrapper : MonoBehaviour
    {
        public IAddressableSceneLoader SceneLoader { get; private set; }

        [SerializeField]
        AssetReference _initialSceneReference;

        void Start()
        {
            SceneLoader = new AddressableSceneLoaderCoroutine(new AddressableSceneManager());
            SceneLoader.LoadScene(new AddressableLoadSceneReferenceAsset(_initialSceneReference), true);
        }
    }
}