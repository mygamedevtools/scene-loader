using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The fader holds both gates for the length of its fades, with nothing to configure — adding
    /// the component is the whole statement that the transition should wait for it.
    /// </summary>
    public class LoadingFaderTests
    {
        LoadingBehavior _loadingBehavior;
        LoadingFader _loadingFader;

        [TearDown]
        public void Teardown()
        {
            if (_loadingFader != null)
                Object.DestroyImmediate(_loadingFader.gameObject);
            if (_loadingBehavior != null)
                Object.DestroyImmediate(_loadingBehavior.gameObject);
        }

        [UnityTest]
        public IEnumerator FadeInOut()
        {
            _loadingBehavior = new GameObject(nameof(LoadingBehavior)).AddComponent<LoadingBehavior>();
            LoadingProgress progress = _loadingBehavior.Progress;

            _loadingFader = new GameObject("Fader", typeof(CanvasGroup)).AddComponent<LoadingFader>();
            CanvasGroup canvasGroup = _loadingFader.GetComponent<CanvasGroup>();

            _loadingFader.fadeTime = .2f;

            Assert.AreEqual(0, canvasGroup.alpha, "The screen starts fully transparent.");

            // Binding is what starts the fade, so everything below happens after it.
            _loadingFader.LoadingBehavior = _loadingBehavior;
            Assert.False(progress.IsShown, "The fader holds the show gate as soon as it binds.");

            yield return new WaitForSeconds(_loadingFader.fadeTime * 2);

            Assert.True(progress.IsShown, "The fade in finished, so nothing holds the show gate.");
            Assert.AreEqual(1, canvasGroup.alpha);

            progress.SetLoadingCompleted();
            Assert.False(progress.IsHidden, "The fader holds the hide gate while it fades out.");

            yield return new WaitForSeconds(_loadingFader.fadeTime * 2);

            Assert.True(progress.IsHidden);
            Assert.AreEqual(0, canvasGroup.alpha);
        }

        /// <summary>
        /// The regression this replaces: with the old opt-in flag left at its default, the gate
        /// opened on the first frame and the transition ran straight through the fade.
        /// </summary>
        [UnityTest]
        public IEnumerator FadeIn_HoldsTheShowGate_PastTheFirstFrame()
        {
            _loadingBehavior = new GameObject(nameof(LoadingBehavior)).AddComponent<LoadingBehavior>();
            LoadingProgress progress = _loadingBehavior.Progress;

            _loadingFader = new GameObject("Fader", typeof(CanvasGroup)).AddComponent<LoadingFader>();
            _loadingFader.fadeTime = 1;
            _loadingFader.LoadingBehavior = _loadingBehavior;

            yield return null;
            yield return null;

            Assert.False(progress.IsShown, "The behaviour released on the first frame, but the fader has not.");
        }
    }
}
