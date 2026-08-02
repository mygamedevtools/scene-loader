using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Component responsible for handling the loading progress report through its <see cref="Progress"/> property.
    /// Use <see cref="Progress"/> to add listener to scene loading progress events or to control loading screen transitions.
    /// <br/>
    /// It announces itself to <see cref="LoadingBehaviorRegistry"/> in <c>OnEnable</c>, which is
    /// how a transition finds it — v4 scanned every loaded object instead.
    /// </summary>
    [AddComponentMenu("Scene Loading/Loading Behavior")]
    public class LoadingBehavior : MonoBehaviour
    {
        public LoadingProgress Progress { get; private set; }

        [Tooltip("Should it wait for an animation or script to allow starting the transition?")]
        public bool waitForScriptedStart;
        [Tooltip("Should it wait for an animation or script to allow finishing the transition?")]
        public bool waitForScriptedEnd;

        void Awake()
        {
            Progress = new LoadingProgress
            {
                // Named so a gate that never opens can say which behaviour is holding it.
                OwnerDescription = $"'{name}' in scene '{gameObject.scene.name}'",
            };
            Progress.LoadingCompleted += OnLoadingCompleted;
        }

        void OnEnable()
        {
            // Registered here rather than in Awake so `Progress` is guaranteed to exist by the
            // time anything can look this up.
            LoadingBehaviorRegistry.Register(this);
        }

        void OnDisable()
        {
            LoadingBehaviorRegistry.Deregister(this);
        }

        void Start()
        {
            if (!waitForScriptedStart)
                Progress.StartTransition();
        }

        void OnLoadingCompleted()
        {
            if (!waitForScriptedEnd)
                Progress.EndTransition();
        }
    }
}