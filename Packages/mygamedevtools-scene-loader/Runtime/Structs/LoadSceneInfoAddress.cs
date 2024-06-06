#if ENABLE_ADDRESSABLES
using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct LoadSceneInfoAddress : ILoadSceneInfo
    {
        public readonly LoadSceneInfoType Type => LoadSceneInfoType.Address;

        public readonly object Reference => _address;

        readonly string _address;

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