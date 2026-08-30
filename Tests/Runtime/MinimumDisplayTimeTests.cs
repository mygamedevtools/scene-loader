using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The component delays the <i>cue</i> rather than the transition, which is the distinction
    /// the whole thing exists for: the screen stays up for its minimum, and whatever plays it out
    /// starts when it should instead of running to its end against an empty screen.
    /// </summary>
    public class MinimumDisplayTimeTests
    {
        LoadingBehavior _loadingBehavior;
        MinimumDisplayTime _minimumDisplayTime;

        [TearDown]
        public void Teardown()
        {
            // Restored here rather than at the end of the test that sets it, so a failed
            // assertion cannot leave the rest of the suite running at a stopped clock.
            Time.timeScale = 1;

            if (_minimumDisplayTime != null)
                Object.DestroyImmediate(_minimumDisplayTime.gameObject);
            if (_loadingBehavior != null)
                Object.DestroyImmediate(_loadingBehavior.gameObject);
        }

        /// <summary>
        /// The requirement: a scene that loads in two frames would otherwise flash a screen on
        /// and off, which reads as a glitch rather than as a load.
        /// </summary>
        [UnityTest]
        public IEnumerator HoldsTheCue_WhenTheLoadFinishesFirst()
        {
            LoadingProgress progress = Bind(.3f);

            bool completed = false;
            progress.LoadingCompleted += () => completed = true;

            // The load a real screen would be waiting on, finishing immediately.
            progress.SetLoadingCompleted();

            yield return null;
            Assert.False(completed, "Loading finished, but the screen has not been up long enough to be told.");

            yield return new WaitForSecondsRealtime(.4f);

            Assert.True(completed, "Its time is up, so the cue it was holding is raised.");
        }

        /// <summary>
        /// The other half: a load slower than the minimum must not have the minimum added to it.
        /// </summary>
        [UnityTest]
        public IEnumerator DoesNotHoldTheCue_WhenTheLoadTakesLonger()
        {
            LoadingProgress progress = Bind(.1f);

            bool completed = false;
            progress.LoadingCompleted += () => completed = true;

            yield return new WaitForSecondsRealtime(.2f);

            Assert.False(completed, "Nothing has finished loading yet, so there is no cue to raise.");

            progress.SetLoadingCompleted();

            Assert.True(completed, "The screen had already served its minimum, so the cue is raised at once.");
        }

        /// <summary>
        /// Measured on the unscaled clock, so a screen shown over a paused game still counts down.
        /// A scaled one would hold the cue forever and strand the transition behind it.
        /// </summary>
        [UnityTest]
        public IEnumerator CountsDown_WhileTheGameIsPaused()
        {
            Time.timeScale = 0;

            LoadingProgress progress = Bind(.2f);

            bool completed = false;
            progress.LoadingCompleted += () => completed = true;

            progress.SetLoadingCompleted();

            yield return new WaitForSecondsRealtime(.4f);

            Assert.True(completed, "The clock is stopped, but the minimum is not measured against it.");
        }

        /// <summary>
        /// It has one job and it is done; polling for the rest of the screen's life is waste.
        /// </summary>
        [UnityTest]
        public IEnumerator StopsPolling_OnceItsTimeIsUp()
        {
            Bind(.1f);

            Assert.True(_minimumDisplayTime.enabled, "It polls while it is still holding the cue.");

            yield return new WaitForSecondsRealtime(.2f);

            Assert.False(_minimumDisplayTime.enabled);
        }

        /// <summary>
        /// Binding is what starts the clock, so every test does it the same way and after
        /// everything it wants to observe is in place.
        /// </summary>
        LoadingProgress Bind(float seconds)
        {
            _loadingBehavior = new GameObject(nameof(LoadingBehavior)).AddComponent<LoadingBehavior>();

            _minimumDisplayTime = new GameObject(nameof(MinimumDisplayTime)).AddComponent<MinimumDisplayTime>();
            _minimumDisplayTime.seconds = seconds;
            _minimumDisplayTime.LoadingBehavior = _loadingBehavior;

            return _loadingBehavior.Progress;
        }
    }
}
