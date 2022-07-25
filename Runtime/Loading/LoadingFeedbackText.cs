/**
 * LoadingFeedbackText.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/23/2022 (en-US)
 */

using UnityEngine;
using UnityEngine.UI;

namespace MyUnityTools.SceneLoading
{
    [RequireComponent(typeof(Text))]
    public class LoadingFeedbackText : MonoBehaviour
    {
        [SerializeField]
        LoadingBehavior _loadingBehavior;

        Text _text;

        void Awake()
        {
            _text = GetComponent<Text>();
            _loadingBehavior.OnProgress += UpdateText;
            _text.text = "0";
        }

        void UpdateText(float progress) => _text.text = Mathf.FloorToInt(progress * 100).ToString();
    }
}