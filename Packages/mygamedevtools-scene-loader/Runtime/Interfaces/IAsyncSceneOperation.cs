using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public interface IAsyncSceneOperation
    {
        float Progress { get; }
        bool IsDone { get; }
        bool HasDirectReferenceToScene { get; }

        Scene GetResult();
    }
}
