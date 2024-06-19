#if ENABLE_ADDRESSABLES
using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage scene operations with the scene's Addressable address. Implements <see cref="ILoadSceneInfo"/>.
    /// </summary>
    public readonly struct LoadSceneInfoAddress : ILoadSceneInfo
    {
        public readonly LoadSceneInfoType Type => LoadSceneInfoType.Address;

        public readonly object Reference => _address;

        readonly string _address;

        /// <summary>
        /// Creates a new <see cref="ILoadSceneInfo"/> based on the scene's Addressable Address.
        /// The scene must be added to an Addressable group in order to be loaded.
        /// </summary>
        /// <param name="address">The scene's Addressable address.</param>
        public LoadSceneInfoAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException($"Cannot create a {nameof(LoadSceneInfoAddress)} from an empty string.", nameof(address));
            _address = address;
        }

        public bool CanBeReferenceToScene(Scene scene)
        {
            return false;
        }

        public override string ToString()
        {
            return $"Scene with addressable address '{_address}'";
        }

        public bool Equals(ILoadSceneInfo other)
        {
            return Type == other.Type && Reference.Equals(other.Reference);
        }
    }
}
#endif