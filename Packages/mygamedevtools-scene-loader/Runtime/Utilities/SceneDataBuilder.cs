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
                LoadSceneInfoType.AssetReference or LoadSceneInfoType.Address => new SceneDataAddressable(sourceLoadSceneInfo),
                _ => throw new System.Exception($"[{nameof(SceneDataBuilder)}] Unexpected {nameof(ILoadSceneInfo.Reference)} type."),
            };
        }
    }
}