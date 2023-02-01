/**
 * LoadingFader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/23/2022 (en-US)
 */

using System.Collections;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingFader : MonoBehaviour
    {
        public float FadeTime => _fadeTime;

        public LoadingBehavior loadingBehavior;

        [SerializeField]
        AnimationCurve _fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField]
        AnimationCurve _fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Range(.05f, 5)]
        float _fadeTime = 1;

        LoadingProgress _loadingProgress;
        CanvasGroup _canvasGroup;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
        }

        void Start()
        {
            _loadingProgress = loadingBehavior.Progress;
            _loadingProgress.StateChanged += OnLoadingStateChanged;
            FadeIn();
        }

        void OnLoadingStateChanged(LoadingState loadingState)
        {
            if (loadingState == LoadingState.TargetSceneLoaded)
                FadeOut();
        }

        void FadeOut()
        {
            StartCoroutine(fadeOutRoutine());
            IEnumerator fadeOutRoutine()
            {
                yield return FadeRoutine(_fadeOutCurve);
                _loadingProgress.SetState(LoadingState.TransitionComplete);
            }
        }

        void FadeIn()
        {
            StartCoroutine(fadeInRoutine());
            IEnumerator fadeInRoutine()
            {
                yield return FadeRoutine(_fadeInCurve);
                _loadingProgress.SetState(LoadingState.Loading);
            }
        }

        IEnumerator FadeRoutine(AnimationCurve fadeCurve)
        {
            var time = 0f;
            while (time < _fadeTime)
            {
                time += Time.deltaTime;
                _canvasGroup.alpha = fadeCurve.Evaluate(time / _fadeTime);
                yield return null;
            }
        }
    }
}