#if ENABLE_ADDRESSABLES
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct LoadSceneInfoAssetReference : ILoadSceneInfo
    {
        public readonly LoadSceneInfoType Type => LoadSceneInfoType.AssetReference;

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
            UnityEngine.Debug.LogError($"{nameof(LoadSceneInfoAssetReference)} is not supposed to validate scene references, since the {nameof(ILoadSceneOperation)} related to this type of {nameof(ILoadSceneInfo)} has direct reference to the loaded scene.");
            return false;
        }

        public override string ToString()
        {
            return $"Scene with asset reference {_assetReference}";
        }
    }
}
#endif