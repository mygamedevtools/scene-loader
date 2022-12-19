#if ENABLE_ADDRESSABLES
/**
 * IAddressableUnloadSceneInfo.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

namespace MyGameDevTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Interface to standardize addressable scene unload info.
    /// Can be created with either the target scene's <see cref="UnityEngine.ResourceManagement.ResourceProviders.SceneInstance"/>
    /// (<see cref="AddressableLoadSceneInfoInstance"/>) or its name (<see cref="AddressableLoadSceneInfoName"/>).
    /// </summary>
    public interface IAddressableLoadSceneInfo
    {
        object Info { get; }
    }
}
#endif