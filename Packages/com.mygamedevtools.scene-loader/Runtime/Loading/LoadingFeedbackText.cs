#if ENABLE_UGUI
// Needs uGUI (legacy Text), which the package does not depend on: compiled only when com.unity.ugui is installed.
using UnityEngine;
using UnityEngine.UI;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>Displays loading progress as a percentage in a legacy UI <see cref="Text"/>.</summary>
    [AddComponentMenu("Scene Loading/Loading Text (Legacy)")]
    [RequireComponent(typeof(Text))]
    public class LoadingFeedbackText : LoadingFeedback
    {
        Text _text;

        protected override void Awake()
        {
            _text = GetComponent<Text>();
            _text.text = "0";

            base.Awake();
        }

        protected override void OnProgressed(float progress) => _text.text = Mathf.CeilToInt(progress * 100).ToString();
    }
}
#endif
