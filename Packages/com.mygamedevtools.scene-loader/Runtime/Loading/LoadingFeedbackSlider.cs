using UnityEngine;
using UnityEngine.UI;

namespace MyGameDevTools.SceneLoading
{
    [AddComponentMenu("Scene Loading/Loading Slider")]
    [RequireComponent(typeof(Slider))]
    public class LoadingFeedbackSlider : MonoBehaviour
    {
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