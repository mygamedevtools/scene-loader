#if ENABLE_TMP
/**
 * LoadingFeedbackTextMeshPro.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/25/2022 (en-US)
 */

using TMPro;
using UnityEngine;

namespace MyUnityTools.SceneLoading
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LoadingFeedbackTextMeshPro : MonoBehaviour
    {
        [SerializeField]
        LoadingBehavior _loadingBehavior;

        TextMeshProUGUI _text;

        void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _loadingBehavior.OnProgress += UpdateText;
            _text.SetText("0");
        }

        void UpdateText(float progress) => _text.SetText(Mathf.CeilToInt(progress * 100).ToString());
    }
}
#endif