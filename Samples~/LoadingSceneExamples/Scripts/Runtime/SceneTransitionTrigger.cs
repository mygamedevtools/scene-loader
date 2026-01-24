using MyGameDevTools.SceneLoading;
using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    /// <summary>
    /// Target Scene name.
    /// Editable via the Unity Inspector.
    /// Make sure to provide a scene that has been added to the Build Settings.
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
}
