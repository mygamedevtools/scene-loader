/**
 * LoadingFeedbackSlider.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/23/2022 (en-US)
 */

using UnityEngine;
using UnityEngine.UI;

namespace MyGameDevTools.SceneLoading
{
    [RequireComponent(typeof(Slider))]
    public class LoadingFeedbackSlider : MonoBehaviour
    {
        [SerializeField]
        public LoadingBehavior loadingBehavior;

        Slider _slider;

        void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.value = 0;
        }

        void Start()
        {
            loadingBehavior.Progress.Progressed += UpdateSlider;
        }

        private void UpdateSlider(float progress) => _slider.value = progress;
    }
}