using UnityEngine;
using UnityEngine.Serialization;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Base for components that live on a loading screen and drive, or wait on, its
    /// <see cref="LoadingProgress"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="LoadingBehavior"/> is found on this object or one of its parents when it is
    /// not assigned explicitly, so the usual layout — one behaviour on the screen's root, feedback
    /// and fades below it — needs no wiring at all. Resolution is attempted in <c>Awake</c> and
    /// again in <c>Start</c>, so a reference assigned from script right after <c>AddComponent</c>
    /// still takes effect; a component that resolves nothing by then disables itself and says so,
    /// rather than throwing a <see cref="System.NullReferenceException"/> from a lifecycle method.
    /// <br/><br/>
    /// Assigning the reference outside play mode — from an editor script building a prefab, say —
    /// only records it. <see cref="OnBound"/> drives whatever the subclass cached in <c>Awake</c>,
    /// so it does not run until there is something to drive.
    /// </remarks>
    public abstract class LoadingScreenComponent : MonoBehaviour
    {
        /// <summary>
        /// The behaviour this component reads from. Optional in the Inspector; when left empty it
        /// is taken from this object or its closest parent that has one. Assigning it after the
        /// component has bound has no effect.
        /// </summary>
        public LoadingBehavior LoadingBehavior
        {
            get => _loadingBehavior;
            set
            {
                if (Progress != null)
                {
                    SceneManagerLog.Warning($"{GetType().Name} on '{name}' is already bound to a {nameof(LoadingBehavior)}. Assign it before the component starts.");
                    return;
                }

                _loadingBehavior = value;
                TryBind();
            }
        }

        /// <summary>The bound progress, or <see langword="null"/> until this component binds.</summary>
        protected LoadingProgress Progress { get; private set; }

        [Tooltip("Optional. Taken from this object or its closest parent with one when left empty.")]
        // The components that now share this base each carried their own `loadingBehavior` field,
        // so scenes authored against them keep their wiring.
        [FormerlySerializedAs("loadingBehavior")]
        [SerializeField]
        LoadingBehavior _loadingBehavior;

        protected virtual void Awake()
        {
            TryBind();
        }

        protected virtual void Start()
        {
            if (TryBind())
                return;

            SceneManagerLog.Error($"{GetType().Name} on '{name}' (scene '{gameObject.scene.name}') found no {nameof(LoadingBehavior)} on itself or its parents, and none was assigned. Disabling it.");
            enabled = false;
        }

        protected virtual void OnDestroy() { }

        /// <summary>Called once, as soon as <see cref="Progress"/> is available.</summary>
        protected abstract void OnBound();

        bool TryBind()
        {
            if (Progress != null)
                return true;

            if (_loadingBehavior == null)
                _loadingBehavior = GetComponentInParent<LoadingBehavior>(true);

            if (_loadingBehavior == null)
                return false;

            // Resolving the reference is safe at any time and worth doing — an editor script
            // that assigns it gets it serialized. Acting on it is not: OnBound drives whatever
            // the subclass cached in Awake, and outside play mode none of that has run.
            if (!Application.isPlaying)
                return false;

            Progress = _loadingBehavior.Progress;
            OnBound();
            return true;
        }
    }
}
