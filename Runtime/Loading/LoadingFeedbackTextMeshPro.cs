#if ENABLE_TMP
using TMPro;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>Displays loading progress as a percentage in a <see cref="TextMeshProUGUI"/>.</summary>
    [AddComponentMenu("Scene Loading/Loading Text")]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LoadingFeedbackTextMeshPro : LoadingFeedback
    {
        TextMeshProUGUI _text;

        protected override void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _text.SetText("0");

            base.Awake();
        }

        protected override void OnProgressed(float progress) => _text.SetText(Mathf.CeilToInt(progress * 100).ToString());
    }
}
#endif
