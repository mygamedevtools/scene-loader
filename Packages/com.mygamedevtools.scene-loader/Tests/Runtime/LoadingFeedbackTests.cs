using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if ENABLE_TMP
using TMPro;
#endif

namespace MyGameDevTools.SceneLoading.Tests
{
    public class LoadingFeedbackTests
    {
        LoadingBehavior _loadingBehavior;
        LoadingProgress _progress;

        [SetUp]
        public void Setup()
        {
            _loadingBehavior = new GameObject().AddComponent<LoadingBehavior>();
            _progress = _loadingBehavior.Progress;
        }

        [TearDown]
        public void Teardown()
        {
            _progress = null;
            Object.DestroyImmediate(_loadingBehavior.gameObject);
        }

        [UnityTest]
        public IEnumerator SliderFeedback()
        {
            var feedbackSlider = new GameObject("Slider", typeof(Slider)).AddComponent<LoadingFeedbackSlider>();
            feedbackSlider.loadingBehavior = _loadingBehavior;

            var slider = feedbackSlider.GetComponent<Slider>();
            Assert.AreEqual(0, slider.value);

            yield return null;

            _progress.Report(.5f);

            Assert.AreEqual(.5f, slider.value);
        }

        [UnityTest]
        public IEnumerator TextFeedback()
        {
            var feedbackText = new GameObject("Text", typeof(Text)).AddComponent<LoadingFeedbackText>();
            feedbackText.loadingBehavior = _loadingBehavior;

            var text = feedbackText.GetComponent<Text>();
            Assert.AreEqual("0", text.text);

            yield return null;

            _progress.Report(.5f);

            Assert.AreEqual(Mathf.CeilToInt(.5f * 100).ToString(), text.text);
        }

#if ENABLE_TMP
        [UnityTest]
        public IEnumerator TextMeshFeedback()
        {
            var feedbackText = new GameObject("TextMesh", typeof(TextMeshProUGUI)).AddComponent<LoadingFeedbackTextMeshPro>();
            feedbackText.loadingBehavior = _loadingBehavior;

            var text = feedbackText.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual("0", text.text);

            yield return null;

            _progress.Report(.5f);

            Assert.AreEqual(Mathf.CeilToInt(.5f * 100).ToString(), text.text);

        }
#endif
    }
}