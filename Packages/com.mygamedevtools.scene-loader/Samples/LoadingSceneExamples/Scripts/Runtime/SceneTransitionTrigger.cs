using MyGameDevTools.SceneLoading;
using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    /// <summary>
    /// Target Scene name.
    /// Editable via the Unity Inspector.
    /// The scene can live in the Build Settings or in Addressables — the name resolves to
    /// whichever has it, with the Build Settings winning if both do.
    /// </summary>
    [SerializeField]
    string _targetScene;

    /// <summary>
    /// Triggers a Scene Transition to a scene with name provided by '<see cref="_targetScene"/>' with a loading scene with name '<paramref name="loadingScene"/>'.
    /// </summary>
    public void TransitionWithLoading(string loadingScene)
    {
        MySceneManager.TransitionAsync(_targetScene, loadingScene);
    }

    /// <summary>
    /// Triggers a Scene Transition to a scene with name provided by '<see cref="_targetScene"/>' without a loading scene.
    /// </summary>
    public void Transition()
    {
        MySceneManager.TransitionAsync(_targetScene);
    }

    /// <summary>
    /// The same transition, but watched: progress goes to the console and the operation is
    /// cancelled if this object is destroyed mid-flight.
    /// <br/>
    /// Shows the shape of the v5 handle — everything here used to have to be decided up front,
    /// as constructor arguments to the call.
    /// </summary>
    public void TransitionWatched(string loadingScene)
    {
        SceneOperation operation = MySceneManager
            .TransitionAsync(_targetScene, loadingScene)
            .CancelWith(destroyCancellationToken);

        operation.Progressed += progress => Debug.Log($"Loading {_targetScene}: {progress:P0}");
        operation.Completed += o => Debug.Log($"{_targetScene} transition finished as {o.State}.");
    }
}
