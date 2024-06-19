#if ENABLE_ADDRESSABLES
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage scene operations with the scene's Addressable Asset Reference. Implements <see cref="ILoadSceneInfo"/>.
    /// </summary>
    public readonly struct LoadSceneInfoAssetReference : ILoadSceneInfo
    {
        public readonly LoadSceneInfoType Type => LoadSceneInfoType.AssetReference;

        public readonly object Reference => _assetReference;

        readonly AssetReference _assetReference;

        /// <summary>
        /// Creates a new <see cref="ILoadSceneInfo"/> based on the scene's Addressable Asset Reference.
        /// The scene must be added to an Addressable group in order to be loaded.
        /// </summary>
        /// <param name="assetReference">The scene's Asset Reference.</param>
        public LoadSceneInfoAssetReference(AssetReference assetReference)
        {
            if (!assetReference.RuntimeKeyIsValid())
                throw new ArgumentException($"Cannot create a LoadSceneInfoAssetReference from an Asset Reference with an invalid Runtime Key: '{assetReference.RuntimeKey}'.", nameof(assetReference));
            _assetReference = assetReference;
        }

        public bool CanBeReferenceToScene(Scene scene)
        {
            return false;
        }

        public override string ToString()
        {
            return $"Scene with asset reference '{_assetReference}'";
        }

        public bool Equals(ILoadSceneInfo other)
        {
            return Type == other.Type && Reference.Equals(other.Reference);
        }
    }
}
#endif