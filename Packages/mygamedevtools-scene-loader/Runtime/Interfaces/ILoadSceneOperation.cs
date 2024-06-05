using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public interface ILoadSceneOperation
    {
        float Progress { get; }
        bool IsDone { get; }
        bool HasDirectReferenceToScene { get; }

        Scene GetResult();
    }
}
