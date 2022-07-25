/**
 * LoadingFeedbackSlider.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/23/2022 (en-US)
 */

using UnityEngine;
using UnityEngine.UI;

namespace MyUnityTools.SceneLoading
{
    [RequireComponent(typeof(Slider))]
    public class LoadingFeedbackSlider : MonoBehaviour
    {
        [SerializeField]
        LoadingBehavior _loadingBehavior;

        Slider _slider;

        void Awake()
        {
            _slider = GetComponent<Slider>();
            _loadingBehavior.OnProgress += UpdateSlider;
            _slider.value = 0;
        }

        private void UpdateSlider(float progress) => _slider.value = progress;
    }
}