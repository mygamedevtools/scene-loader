#if ENABLE_ADDRESSABLES
/**
 * IAddressableLoadSceneReference.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Interface to standardize addressable scene load reference.
    /// Can be created with either the target scene's <see cref="UnityEngine.AddressableAssets.AssetReference"/> (<see cref="AddressableLoadSceneReferenceAsset"/>) or the scene's runtime key (<see cref="AddressableLoadSceneReferenceKey"/>).
    /// </summary>
    public interface IAddressableLoadSceneReference
    {
        object RuntimeKey { get; }
    }
}
#endif