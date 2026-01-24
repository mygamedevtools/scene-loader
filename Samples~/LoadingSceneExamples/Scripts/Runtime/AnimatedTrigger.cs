using MyGameDevTools.SceneLoading;
using UnityEngine;

public class AnimatedTrigger : MonoBehaviour
{
    /// <summary>
    /// Cached Animation parameter hash, for quicker access.
    /// </summary>
    static readonly int _isOpenHash = Animator.StringToHash("IsOpen");

    /// <summary>
    /// Set this component reference in the inspector to be able to read and write loading states.
    /// </summary>
    [SerializeField]
    LoadingBehavior _loadingBehavior;

    /// <summary>
    /// The <see cref="LoadingProgress"/> is used to notify when the target scenes have completed loading and to trigger the start and end of the Scene Transition.
    /// </summary>
    LoadingProgress _loadingProgress;
    // We'll use the animator to play animations.
    Animator _animator;

    /// <summary>
    /// The Awake method is invoked right after a scene has completed loading, but still didn't activate.
    /// Here we can get the references we need and trigger the "in" animation.
    /// </summary>
    void Awake()
    {
        _animator = GetComponent<Animator>();

        // Get the LoadingProgress from the LoadingBehavior
        _loadingProgress = _loadingBehavior.Progress;

        // Subscribe to the LoadingCompleted event, that is broadcast when the target scenes are completely loaded
        _loadingProgress.LoadingCompleted += PlayOutAnimation;

        // Start playing the "in" animation, to show a feedback that we're entering a loading state
        PlayInAnimation();
    }

    /// <summary>
    /// Call this method when you have finished your animation to effectively start the Scene Transition.
    /// </summary>
    public void InTransitionTrigger()
    {
        // Allow the Scene Manager to start the Scene Transition
        _loadingProgress.StartTransition();
    }

    /// <summary>
    /// Call this method when you have finished your animation to effectively end the Scene Transition.
    /// </summary>
    public void OutTransitionTrigger()
    {
        // Allow the Scene Manager to finish the Scene Transition
        _loadingProgress.EndTransition();
    }

    /// <summary>
    /// Plays the "in" transition animation for the loading screen.
    /// </summary>
    void PlayInAnimation()
    {
        _animator.SetBool(_isOpenHash, false);
    }

    /// <summary>
    /// Plays the "out" transition animation for the loading screen.
    /// </summary>
    void PlayOutAnimation()
    {
        _animator.SetBool(_isOpenHash, true);
    }
}
