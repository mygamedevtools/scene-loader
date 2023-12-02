using UnityEngine;
using UnityEngine.UI;

namespace MyGameDevTools.SceneLoading
{
    [AddComponentMenu("Scene Loading/Loading Text (Legacy)")]
    [RequireComponent(typeof(Text))]
    public class LoadingFeedbackText : MonoBehaviour
    {
        public LoadingBehavior loadingBehavior;

        Text _text;

        void Awake()
        {
            _text = GetComponent<Text>();
            _text.text = "0";
        }

        void Start()
        {
            loadingBehavior.Progress.Progressed += UpdateText;
        }

        void UpdateText(float progress) => _text.text = Mathf.CeilToInt(progress * 100).ToString();
    }
}