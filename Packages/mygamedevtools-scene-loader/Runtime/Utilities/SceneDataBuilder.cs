namespace MyGameDevTools.SceneLoading
{
    public static class SceneDataBuilder
    {
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