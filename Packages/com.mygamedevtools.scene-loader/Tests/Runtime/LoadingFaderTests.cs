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
            // Restored here rather than at the end of the test that sets it, so a failed
            // assertion cannot leave the rest of the suite running at a stopped clock.
            Time.timeScale = 1;

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

            _loadingFader.fadeInTime = .2f;
            _loadingFader.fadeOutTime = .2f;
            // Long enough to never bind here: the clamp has its own test, and leaving it at its
            // default would make this one's timing depend on how long the runner's frames are.
            _loadingFader.maxFrameStep = 1;

            Assert.AreEqual(0, canvasGroup.alpha, "The screen starts fully transparent.");

            // Binding is what starts the fade, so everything below happens after it.
            _loadingFader.LoadingBehavior = _loadingBehavior;
            Assert.False(progress.IsShown, "The fader holds the show gate as soon as it binds.");

            yield return new WaitForSecondsRealtime(_loadingFader.fadeInTime * 2);

            Assert.True(progress.IsShown, "The fade in finished, so nothing holds the show gate.");
            Assert.AreEqual(1, canvasGroup.alpha);

            progress.SetLoadingCompleted();
            Assert.False(progress.IsHidden, "The fader holds the hide gate while it fades out.");

            yield return new WaitForSecondsRealtime(_loadingFader.fadeOutTime * 2);

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
            _loadingFader.fadeInTime = 1;
            _loadingFader.LoadingBehavior = _loadingBehavior;

            yield return null;
            yield return null;

            Assert.False(progress.IsShown, "The behaviour released on the first frame, but the fader has not.");
        }

        /// <summary>
        /// The regression: the fade used to advance on <c>Time.deltaTime</c>, so a transition
        /// entered from a paused game never advanced it at all — and the show gate it holds
        /// never opened, leaving the transition stalled with no error to explain it.
        /// </summary>
        [UnityTest]
        public IEnumerator FadeIn_OpensTheGate_WhileTheGameIsPaused()
        {
            _loadingBehavior = new GameObject(nameof(LoadingBehavior)).AddComponent<LoadingBehavior>();
            LoadingProgress progress = _loadingBehavior.Progress;

            _loadingFader = new GameObject("Fader", typeof(CanvasGroup)).AddComponent<LoadingFader>();
            _loadingFader.fadeInTime = .2f;
            _loadingFader.maxFrameStep = 1;

            Time.timeScale = 0;
            _loadingFader.LoadingBehavior = _loadingBehavior;

            yield return new WaitForSecondsRealtime(_loadingFader.fadeInTime * 2);

            Assert.True(progress.IsShown, "The fade finished on a stopped clock, so nothing holds the show gate.");
            Assert.AreEqual(1, _loadingFader.GetComponent<CanvasGroup>().alpha);
        }

        /// <summary>
        /// The regression: one long frame — the frame a scene activates on is routinely over a
        /// second — used to advance the fade by its whole length, so the first frame the player
        /// saw was already the last one.
        /// </summary>
        [UnityTest]
        public IEnumerator Fade_SurvivesASingleLongFrame()
        {
            _loadingBehavior = new GameObject(nameof(LoadingBehavior)).AddComponent<LoadingBehavior>();
            LoadingProgress progress = _loadingBehavior.Progress;

            _loadingFader = new GameObject("Fader", typeof(CanvasGroup)).AddComponent<LoadingFader>();
            CanvasGroup canvasGroup = _loadingFader.GetComponent<CanvasGroup>();

            _loadingFader.fadeInTime = .2f;
            _loadingFader.maxFrameStep = .02f;
            _loadingFader.LoadingBehavior = _loadingBehavior;

            yield return null;

            // The hitch itself. Blocking the main thread is what the loading frame does to it,
            // and the next frame's unscaledDeltaTime reports the whole of it.
            System.Threading.Thread.Sleep(500);
            yield return null;

            Assert.Less(canvasGroup.alpha, 1, "A frame far longer than the fade advanced it by one step, not all of it.");
            Assert.False(progress.IsShown, "So the fade is still running, and still holding the show gate.");
        }
    }
}
