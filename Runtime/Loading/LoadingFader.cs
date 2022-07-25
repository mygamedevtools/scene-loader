/**
 * LoadingFader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/23/2022 (en-US)
 */

using System.Collections;
using UnityEngine;

namespace MyUnityTools.SceneLoading
{
    public class LoadingFader : MonoBehaviour
    {
        [SerializeField]
        LoadingBehavior _loadingBehavior;
        [SerializeField]
        AnimationCurve _fadeOutCurve;
        [SerializeField]
        AnimationCurve _fadeInCurve;
        [SerializeField, Range(.05f, 5)]
        float _fadeTime;

        CanvasGroup _canvasGroup;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _loadingBehavior.OnLoadingComplete += FadeOut;
            FadeIn();
        }

        void FadeOut()
        {
            StartCoroutine(fadeOutRoutine());
            IEnumerator fadeOutRoutine()
            {
                yield return FadeRoutine(_fadeOutCurve);
                _loadingBehavior.SetLoadingActive(false);
            }
        }

        void FadeIn()
        {
            StartCoroutine(fadeInRoutine());
            IEnumerator fadeInRoutine()
            {
                yield return FadeRoutine(_fadeInCurve);
                _loadingBehavior.SetLoadingActive(true);
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