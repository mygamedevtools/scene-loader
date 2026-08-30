using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A <see cref="LoadingProgress"/> you can author in a scene and reference from the Inspector.
    /// Put one on a loading screen and everything else — feedback, fades, animations — hangs off
    /// its <see cref="Progress"/>.
    /// </summary>
    /// <remarks>
    /// It holds both of its progress' gates from <c>Awake</c> and releases them on the default
    /// schedule: the show gate in <c>Start</c>, and the hide gate as soon as loading completes.
    /// Anything that needs the transition to wait longer than that takes a hold of its own — see
    /// <see cref="LoadingProgress.HoldShow"/> — and the transition waits for whoever releases last.
    /// <br/><br/>
    /// A transition finds this through <see cref="LoadingBehaviorRegistry"/>, which it announces
    /// itself to in <c>OnEnable</c>.
    /// </remarks>
    [AddComponentMenu("Scene Loading/Loading Behavior")]
    public class LoadingBehavior : MonoBehaviour
    {
        /// <summary>
        /// The progress and gates this behaviour anchors. Created on first access rather than in
        /// <c>Awake</c>, so reading it from another component's <c>Awake</c> never depends on
        /// script execution order.
        /// </summary>
        public LoadingProgress Progress => _progress ??= CreateProgress();

        LoadingProgress _progress;

        void Awake()
        {
            // Before anything else can run, so a hold is never taken against a gate the transition
            // has already read.
            Progress.HoldShow(this);
            Progress.HoldHide(this);
            Progress.LoadingCompleted += OnLoadingCompleted;
        }

        void OnEnable()
        {
            LoadingBehaviorRegistry.Register(this);
        }

        void OnDisable()
        {
            LoadingBehaviorRegistry.Deregister(this);
        }

        void Start()
        {
            // In Start rather than Awake, so every object in the loading screen has awakened —
            // and taken its own holds — before this one stops holding the transition back.
            Progress.ReleaseShow(this);
        }

        void OnDestroy()
        {
            if (_progress != null)
                _progress.LoadingCompleted -= OnLoadingCompleted;
        }

        void OnLoadingCompleted()
        {
            Progress.ReleaseHide(this);
        }

        LoadingProgress CreateProgress()
        {
            return new LoadingProgress
            {
                // Named so a gate that never opens can say which behaviour is holding it.
                OwnerDescription = $"'{name}' in scene '{gameObject.scene.name}'",
            };
        }
    }
}
