using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Static class to simplify the creation of <see cref="ISceneData"/> objects.
    /// </summary>
    public static class SceneDataBuilder
    {
        /// <summary>
        /// Builds an <see cref="ISceneData"/> of the appropriate type (addressable or non-addressable), depending on the <see cref="ILoadSceneInfo.Type"/> value.
        /// </summary>
        public static ISceneData BuildFromLoadSceneInfo(ILoadSceneInfo sourceLoadSceneInfo)
        {
            return sourceLoadSceneInfo.Type switch
            {
                LoadSceneInfoType.BuildIndex or LoadSceneInfoType.Name => new SceneDataStandard(sourceLoadSceneInfo),
#if ENABLE_ADDRESSABLES
                LoadSceneInfoType.AssetReference or LoadSceneInfoType.Address => new SceneDataAddressable(sourceLoadSceneInfo),
#endif
                _ => throw new System.Exception($"[{nameof(SceneDataBuilder)}] Unexpected {nameof(ILoadSceneInfo.Reference)} type."),
            };
        }

        /// <summary>
        /// Builds a non-addressable <see cref="ISceneData"/> with a loaded <see cref="Scene"/> reference.
        /// </summary>
        public static ISceneData BuildFromScene(Scene scene)
        {
            return new SceneDataStandard(scene);
        }
    }
}