#if ENABLE_ADDRESSABLES
/**
 * LoadSceneInfoAssetReference.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-02-01
 */

using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct LoadSceneInfoAssetReference : ILoadSceneInfo
    {
        public object Reference => _assetReference;

        readonly AssetReference _assetReference;

        public LoadSceneInfoAssetReference(AssetReference assetReference)
        {
            if (!assetReference.RuntimeKeyIsValid())
                throw new ArgumentException($"Cannot create a LoadSceneInfoAssetReference from an Asset Reference with an invalid Runtime Key: '{assetReference.RuntimeKey}'.", nameof(assetReference));
            _assetReference = assetReference;
        }

        public bool IsReferenceToScene(Scene scene)
        {
            if (!_assetReference.OperationHandle.IsValid() || !_assetReference.OperationHandle.IsDone)
                throw new Exception($"Asset Reference with key '{_assetReference.RuntimeKey}' does not reference a loaded scene.");

            var sceneInstance = _assetReference.OperationHandle.Convert<SceneInstance>().Result;
            return sceneInstance.Scene == scene;
        }
    }
}
#endif