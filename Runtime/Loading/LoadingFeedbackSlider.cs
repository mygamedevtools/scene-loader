using UnityEngine;
using UnityEngine.UI;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>Displays loading progress as a <see cref="Slider"/>'s value.</summary>
    [AddComponentMenu("Scene Loading/Loading Slider")]
    [RequireComponent(typeof(Slider))]
    public class LoadingFeedbackSlider : LoadingFeedback
    {
        Slider _slider;

        protected override void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.value = 0;

            base.Awake();
        }

        protected override void OnProgressed(float progress) => _slider.value = progress;
    }
}
