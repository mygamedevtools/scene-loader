namespace MyGameDevTools.SceneLoading
{
    public static class SceneDataBuilder
    {
        public static ISceneData BuildFromLoadSceneInfo(ILoadSceneInfo sourceLoadSceneInfo)
        {
            return sourceLoadSceneInfo.Type switch
            {
                LoadSceneInfoType.BuildIndex or LoadSceneInfoType.Name => new SceneDataStandard(sourceLoadSceneInfo),
                LoadSceneInfoType.AssetReference => new SceneDataAddressable(sourceLoadSceneInfo),
                _ => throw new System.Exception($"[{nameof(SceneDataBuilder)}] Unexpected {nameof(ILoadSceneInfo.Reference)} type."),
            };
        }
    }
}