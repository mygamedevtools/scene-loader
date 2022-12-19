#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneReferenceKey.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyGameDevTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Struct to manage addressable scene operations with the scene's runtime key.
    /// Implements <see cref="IAddressableLoadSceneReference"/>.
    /// </summary>
    public readonly struct AddressableLoadSceneReferenceKey : IAddressableLoadSceneReference
    {
        public object RuntimeKey => _runtimeKey;

        readonly string _runtimeKey;

        /// <summary>
        /// Creates a new <see cref="IAddressableLoadSceneReference"/> based on the scene's runtime key.
        /// </summary>
        /// <param name="sceneRuntimeKey">The scene's addressable runtime key.</param>
        public AddressableLoadSceneReferenceKey(string sceneRuntimeKey)
        {
            _runtimeKey = sceneRuntimeKey;
        }

        public static implicit operator AddressableLoadSceneReferenceKey(string runtimeKey) => new AddressableLoadSceneReferenceKey(runtimeKey);
    }
}
#endif