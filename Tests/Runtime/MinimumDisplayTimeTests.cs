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
    /// <remarks>
    /// These tests read the same clock the component does, <see cref="Time.unscaledTime"/>, which
    /// only advances between frames. That makes every assertion exact: in a frame where less than
    /// the minimum has passed the cue is held, in the first frame where it has passed the cue is
    /// raised, however long any single frame took. Waiting a fixed real time instead and asserting
    /// what state the component should be in by then assumes a frame is shorter than the minimum,
    /// which a loaded CI runner does not guarantee.
    /// </remarks>
    public class MinimumDisplayTimeTests
    {
        LoadingBehavior _loadingBehavior;
        MinimumDisplayTime _minimumDisplayTime;
        float _shownAt;

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

            // Checked in the frame it bound as well, where no time has passed at all: that is
            // what tells a hold apart from a cue that was never delayed in the first place.
            while (!MinimumHasPassed)
            {
                Assert.False(completed, "Loading finished, but the screen has not been up long enough to be told.");
                yield return null;
            }

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

            yield return WaitForTheMinimum();

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

            yield return WaitForTheMinimum();

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

            yield return WaitForTheMinimum();

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

            // The clock does not move within a frame, so this is the very value the component
            // recorded when it bound, and the tests can predict its decisions exactly.
            _shownAt = Time.unscaledTime;

            return _loadingBehavior.Progress;
        }

        /// <summary>
        /// Whether the component, in this frame, considers its minimum served. Its Update runs
        /// before a test coroutine resumes, so by the time this is read it has already acted on it.
        /// </summary>
        bool MinimumHasPassed => Time.unscaledTime - _shownAt >= _minimumDisplayTime.seconds;

        /// <summary>
        /// Resumes in the first frame the component has had the chance to let go, no matter how
        /// many frames, or how few, it took to get there.
        /// </summary>
        IEnumerator WaitForTheMinimum()
        {
            while (!MinimumHasPassed)
                yield return null;
        }
    }
}
