using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public interface ISceneData
    {
        ILoadSceneOperation LoadOperation { get; }
        ILoadSceneInfo LoadSceneInfo { get; }
        Scene LoadedScene { get; }

        void SetSceneReferenceManually(Scene scene);

        void UpdateSceneReference();

        ILoadSceneOperation LoadSceneAsync();
    }
}
