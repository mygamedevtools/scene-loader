/**
 * Bootstrapper.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/28/2022 (en-US)
 */

using MyUnityTools.SceneLoading.AddressablesSupport;
using MyUnityTools.SceneLoading.AddressablesSupport.UniTaskSupport;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MyUnityTools.SceneLoading.Samples
{
    public class Bootstrapper : MonoBehaviour
    {
        public IAddressableSceneLoader SceneLoader { get; private set; }

        [SerializeField]
        AssetReference _initialSceneReference;

        void Start()
        {
            SceneLoader = new AddressableSceneLoaderUniTask(new AddressableSceneManager());
            SceneLoader.LoadScene(new AddressableLoadSceneReferenceAsset(_initialSceneReference), true);
        }
    }
}