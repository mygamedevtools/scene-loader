using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public interface ISceneData
    {
        IAsyncSceneOperation AsyncOperation { get; }
        ILoadSceneInfo LoadSceneInfo { get; }
        Scene SceneReference { get; }

        void SetSceneReferenceManually(Scene scene);

        void UpdateSceneReference();

        IAsyncSceneOperation LoadSceneAsync();

        IAsyncSceneOperation UnloadSceneAsync();
    }
}
