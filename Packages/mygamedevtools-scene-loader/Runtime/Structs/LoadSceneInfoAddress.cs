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

        public bool IsReferenceToScene(Scene scene)
        {
            UnityEngine.Debug.LogError($"{nameof(LoadSceneInfoAddress)} is not supposed to validate scene references, since the {nameof(IAsyncSceneOperation)} related to this type of {nameof(ILoadSceneInfo)} has direct reference to the loaded scene.");
            return false;
        }

        public bool Equals(ILoadSceneInfo other)
        {
            return Type == other.Type && Reference.Equals(other.Reference);
        }
    }
}
#endif