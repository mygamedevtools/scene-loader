using System.Collections;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Fades a <see cref="CanvasGroup"/> in when the loading screen appears and out when loading
    /// completes, holding the transition for the length of each fade.
    /// </summary>
    /// <remarks>
    /// The holds are what make this work with no configuration: adding this component is itself
    /// the statement that the transition should wait for the fades.
    /// </remarks>
    [AddComponentMenu("Scene Loading/Loading Fader")]
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingFader : LoadingScreenComponent
    {
        [Range(.05f, 5)]
        public float fadeTime = 1;

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
                yield return FadeRoutine(_fadeInCurve);
                Progress.ReleaseShow(this);
            }
        }

        void FadeOut()
        {
            StartCoroutine(fadeOutRoutine());
            IEnumerator fadeOutRoutine()
            {
                yield return FadeRoutine(_fadeOutCurve);
                Progress.ReleaseHide(this);
            }
        }

        IEnumerator FadeRoutine(AnimationCurve fadeCurve)
        {
            var time = 0f;
            while (time < fadeTime)
            {
                time += Time.deltaTime;
                _canvasGroup.alpha = fadeCurve.Evaluate(time / fadeTime);
                yield return null;
            }
        }
    }
}
