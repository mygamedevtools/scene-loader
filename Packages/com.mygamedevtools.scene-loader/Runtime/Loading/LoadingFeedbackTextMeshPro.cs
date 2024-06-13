#if ENABLE_TMP
using TMPro;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    [AddComponentMenu("Scene Loading/Loading Text")]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LoadingFeedbackTextMeshPro : MonoBehaviour
    {
        public LoadingBehavior loadingBehavior;

        TextMeshProUGUI _text;

        void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _text.SetText("0");
        }

        void Start()
        {
            loadingBehavior.Progress.Progressed += UpdateText;
        }

        void UpdateText(float progress) => _text.SetText(Mathf.CeilToInt(progress * 100).ToString());
    }
}
#endif