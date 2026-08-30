using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Fades a <see cref="CanvasGroup"/> in when the loading screen appears and out when loading
    /// completes, holding the transition for the length of each fade.
    /// </summary>
    /// <remarks>
    /// The holds are what make this work with no configuration: adding this component is itself
    /// the statement that the transition should wait for the fades.
    /// <br/><br/>
    /// Both fades run on <b>unscaled, clamped</b> time. Unscaled because a transition started
    /// from a paused game would otherwise never advance a fade, and never open the gate that fade
    /// is holding. Clamped because the frame a scene activates on is routinely long enough to
    /// spend an entire fade before it is ever drawn — see <see cref="maxFrameStep"/>.
    /// </remarks>
    [AddComponentMenu("Scene Loading/Loading Fader")]
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingFader : LoadingScreenComponent
    {
        [Tooltip("How long the screen takes to fade in, in unscaled seconds.")]
        [Range(.05f, 5)]
        // The single `fadeTime` this replaces, so screens authored against it keep their timing
        // on the fade they were tuned for. The fade out falls back to this field's default.
        [FormerlySerializedAs("fadeTime")]
        public float fadeInTime = 1;

        [Tooltip("How long the screen takes to fade out, in unscaled seconds.")]
        [Range(.05f, 5)]
        public float fadeOutTime = 1;

        [Tooltip("The longest a single frame may advance a fade, in seconds. Keeps one long frame — the scene it is fading away from activating, say — from spending the fade before it is drawn.")]
        [Range(1 / 120f, 1)]
        public float maxFrameStep = 1 / 30f;

        [SerializeField]
        AnimationCurve _fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField]
        AnimationCurve _fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        CanvasGroup _canvasGroup;

        protected override void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            base.Awake();
        }

        protected override void OnBound()
        {
            // Both gates at once: the transition waits for the fade in before it unloads anything,
            // and for the fade out before it considers the screen gone.
            Progress.HoldShow(this);
            Progress.HoldHide(this);
            Progress.LoadingCompleted += FadeOut;

            FadeIn();
        }

        protected override void OnDestroy()
        {
            if (Progress != null)
                Progress.LoadingCompleted -= FadeOut;

            base.OnDestroy();
        }

        void FadeIn()
        {
            StartCoroutine(fadeInRoutine());
            IEnumerator fadeInRoutine()
            {
                yield return FadeRoutine(_fadeInCurve, fadeInTime);
                Progress.ReleaseShow(this);
            }
        }

        void FadeOut()
        {
            StartCoroutine(fadeOutRoutine());
            IEnumerator fadeOutRoutine()
            {
                yield return FadeRoutine(_fadeOutCurve, fadeOutTime);
                Progress.ReleaseHide(this);
            }
        }

        IEnumerator FadeRoutine(AnimationCurve fadeCurve, float duration)
        {
            if (duration > 0)
            {
                var time = 0f;
                while (time < duration)
                {
                    // Unscaled and clamped, and the gate this holds is why both matter rather
                    // than being a matter of taste: at `timeScale` 0 a scaled step never
                    // advances, so the fade never ends and the transition never resumes; and a
                    // step the length of a hitch leaves the fade finished on a frame nobody saw.
                    time += Mathf.Min(Time.unscaledDeltaTime, maxFrameStep);
                    _canvasGroup.alpha = fadeCurve.Evaluate(time / duration);
                    yield return null;
                }
            }

            // Exactly, rather than wherever the last step happened to land.
            _canvasGroup.alpha = fadeCurve.Evaluate(1);
        }
    }
}
