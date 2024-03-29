using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Component responsible for handling the loading progress report through its <see cref="Progress"/> property.
    /// Use <see cref="Progress"/> to add listener to scene loading progress events or to control loading screen transitions.
    /// <br/>
    /// This component is located by the <see cref="ISceneLoader"/> during a scene transition.
    /// </summary>
    [AddComponentMenu("Scene Loading/Loading Behavior")]
    public class LoadingBehavior : MonoBehaviour
    {
        public LoadingProgress Progress { get; private set; }

        [Tooltip("Should it wait for an animation or script to allow starting the transition?")]
        public bool waitForScriptedStart;
        [Tooltip("Should it wait for an animation or script to allow finishing the transition?")]
        public bool waitForScriptedEnd;

        [SerializeField, Tooltip("Common scene operations stop at 90%, but addressable scene operations go all the way up to 100%. Enabling this value reduces the ratio to 90% instead of 100%")]
        bool _reduceLoadRatio;

        void Awake()
        {
            Progress = new LoadingProgress(_reduceLoadRatio);
            Progress.StateChanged += OnLoadingStateChange;
        }

        void Start()
        {
            if (!waitForScriptedStart)
                Progress.SetState(LoadingState.Loading);
        }

        void OnLoadingStateChange(LoadingState loadingState)
        {
            if (loadingState == LoadingState.TargetSceneLoaded && !waitForScriptedEnd)
                Progress.SetState(LoadingState.TransitionComplete);
        }
    }
}