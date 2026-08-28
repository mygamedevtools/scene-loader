using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// Gate mechanics on their own, with no MonoBehaviour involved — which is the point: a
    /// <see cref="LoadingProgress"/> is usable by anything, not just a <see cref="LoadingBehavior"/>.
    /// </summary>
    public class LoadingProgressTests
    {
        [Test]
        public void SetState_Test()
        {
            var progress = new LoadingProgress();

            bool completed = false;
            progress.LoadingCompleted += () => completed = true;

            progress.SetLoadingCompleted();
            Assert.True(completed);
        }

        [Test]
        public void Progress_Test()
        {
            var progress = new LoadingProgress();

            float reportedValue = 0;
            progress.Progressed += value => reportedValue = value;

            progress.Report(.5f);
            Assert.AreEqual(.5f, reportedValue);

            progress.Report(1);
            Assert.AreEqual(1, reportedValue);

            progress.Report(2);
            Assert.AreEqual(1, reportedValue);
        }

        /// <summary>A gate nobody holds is open, so a screen that gates on nothing never blocks.</summary>
        [Test]
        public void Gates_AreOpen_WhenNothingHoldsThem()
        {
            var progress = new LoadingProgress();

            Assert.True(progress.IsShown);
            Assert.True(progress.IsHidden);
        }

        [Test]
        public void Gates_OpenWhenTheLastHolderReleases()
        {
            var progress = new LoadingProgress();
            object first = new();
            object second = new();

            progress.HoldShow(first);
            progress.HoldShow(second);
            Assert.False(progress.IsShown);

            progress.ReleaseShow(first);
            Assert.False(progress.IsShown, "The second holder has not released.");

            progress.ReleaseShow(second);
            Assert.True(progress.IsShown);
        }

        /// <summary>
        /// Holds are identified by owner, so a duplicate hold and a double release are both
        /// harmless — the failure mode a plain counter would have.
        /// </summary>
        [Test]
        public void Holds_AreIdempotent_InBothDirections()
        {
            var progress = new LoadingProgress();
            object holder = new();
            object other = new();

            progress.HoldHide(holder);
            progress.HoldHide(holder);
            progress.HoldHide(other);

            progress.ReleaseHide(holder);
            progress.ReleaseHide(holder);
            Assert.False(progress.IsHidden, "Releasing twice must not release someone else's hold.");

            progress.ReleaseHide(other);
            Assert.True(progress.IsHidden);
        }

        [Test]
        public void ReleasingAHoldNobodyTook_DoesNothing()
        {
            var progress = new LoadingProgress();
            object holder = new();

            progress.HoldShow(holder);
            progress.ReleaseShow(new object());

            Assert.False(progress.IsShown);
        }

        [Test]
        public void Hold_WithoutAnOwner_Throws()
        {
            var progress = new LoadingProgress();

            Assert.Throws<System.ArgumentNullException>(() => progress.HoldShow(null));
        }

        /// <summary>
        /// Nothing holding the cue means it fires the moment loading finishes — the behaviour
        /// every screen had before completion holds existed.
        /// </summary>
        [Test]
        public void LoadingCompleted_FiresImmediately_WhenNothingHoldsIt()
        {
            var progress = new LoadingProgress();

            int raised = 0;
            progress.LoadingCompleted += () => raised++;

            progress.SetLoadingCompleted();
            Assert.AreEqual(1, raised);
        }

        /// <summary>
        /// The distinction that matters. Holding the <i>cue</i> keeps the screen up; holding the
        /// hide gate would let a fade run to its end and leave the rest of the wait playing out on
        /// an empty screen — which is exactly the bug this was added to fix.
        /// </summary>
        [Test]
        public void LoadingCompleted_WaitsForTheLastCompletionHold()
        {
            var progress = new LoadingProgress();
            object first = new();
            object second = new();

            int raised = 0;
            progress.LoadingCompleted += () => raised++;

            progress.HoldCompletion(first);
            progress.HoldCompletion(second);

            progress.SetLoadingCompleted();
            Assert.AreEqual(0, raised, "Loading finished, but the cue is held.");

            progress.ReleaseCompletion(first);
            Assert.AreEqual(0, raised, "The second holder has not released.");

            progress.ReleaseCompletion(second);
            Assert.AreEqual(1, raised);
        }

        /// <summary>Releasing before loading finishes must not announce it early.</summary>
        [Test]
        public void ReleasingCompletion_BeforeLoadingFinishes_RaisesNothing()
        {
            var progress = new LoadingProgress();

            int raised = 0;
            progress.LoadingCompleted += () => raised++;

            object holder = new();
            progress.HoldCompletion(holder);
            progress.ReleaseCompletion(holder);

            Assert.AreEqual(0, raised, "Nothing has finished loading yet.");

            progress.SetLoadingCompleted();
            Assert.AreEqual(1, raised);
        }

        /// <summary>
        /// The cue is a one-shot: a screen that has already been told to hide must not be told
        /// again by a stray release.
        /// </summary>
        [Test]
        public void LoadingCompleted_IsRaisedOnce()
        {
            var progress = new LoadingProgress();
            object holder = new();

            int raised = 0;
            progress.LoadingCompleted += () => raised++;

            progress.HoldCompletion(holder);
            progress.SetLoadingCompleted();
            progress.SetLoadingCompleted();
            progress.ReleaseCompletion(holder);
            progress.ReleaseCompletion(holder);

            Assert.AreEqual(1, raised);
        }

        /// <summary>
        /// A screen torn down mid-fade should let the transition through, not hang it forever on a
        /// holder that no longer exists.
        /// </summary>
        [Test]
        public void DestroyedHolders_AreReleasedOnTheirBehalf()
        {
            var progress = new LoadingProgress();
            var holder = new GameObject(nameof(DestroyedHolders_AreReleasedOnTheirBehalf));

            progress.HoldShow(holder);
            Assert.False(progress.IsShown);

            Object.DestroyImmediate(holder);

            LogAssert.Expect(LogType.Warning, new Regex("destroyed while holding"));
            Assert.True(progress.IsShown);
        }
    }
}
