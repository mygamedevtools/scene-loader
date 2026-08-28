using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The default gate schedule: hold both from <c>Awake</c>, release show on the first frame and
    /// hide when loading completes — and get out of the way of anything else that holds them.
    /// </summary>
    public class LoadingBehaviorTests
    {
        readonly List<GameObject> _created = new();

        [TearDown]
        public void Teardown()
        {
            foreach (GameObject gameObject in _created)
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);

            _created.Clear();
        }

        /// <summary>
        /// The gates exist before <c>Awake</c> does, so reading <see cref="LoadingBehavior.Progress"/>
        /// from another component's <c>Awake</c> cannot depend on script execution order.
        /// </summary>
        [Test]
        public void Progress_ExistsWithoutAwakeHavingRun()
        {
            GameObject gameObject = Create(active: false);
            LoadingBehavior behavior = gameObject.AddComponent<LoadingBehavior>();

            Assert.NotNull(behavior.Progress);
            Assert.AreSame(behavior.Progress, behavior.Progress, "Progress should be created once and cached.");
            Assert.True(behavior.Progress.IsShown, "Nothing took a hold, because Awake never ran.");
        }

        [UnityTest]
        public IEnumerator DefaultSchedule_ReleasesShowOnTheFirstFrame_AndHideOnCompletion()
        {
            LoadingBehavior behavior = Create().AddComponent<LoadingBehavior>();
            LoadingProgress progress = behavior.Progress;

            Assert.False(progress.IsShown, "Awake should hold the show gate.");
            Assert.False(progress.IsHidden, "Awake should hold the hide gate.");

            yield return null;

            Assert.True(progress.IsShown, "Start should release the show gate.");
            Assert.False(progress.IsHidden, "Loading has not completed yet.");

            progress.Report(1);
            progress.SetLoadingCompleted();

            Assert.True(progress.IsHidden);
        }

        /// <summary>
        /// The whole point of holds: a second participant gates the same transition without the
        /// behaviour knowing anything about it, and no flag has to be set anywhere.
        /// </summary>
        [UnityTest]
        public IEnumerator AnotherHolder_KeepsBothGatesClosed_UntilItReleases()
        {
            LoadingBehavior behavior = Create().AddComponent<LoadingBehavior>();
            LoadingProgress progress = behavior.Progress;

            object holder = new();
            progress.HoldShow(holder);
            progress.HoldHide(holder);

            yield return null;

            Assert.False(progress.IsShown, "The behaviour released, but the other holder has not.");

            progress.ReleaseShow(holder);
            Assert.True(progress.IsShown);

            progress.SetLoadingCompleted();
            Assert.False(progress.IsHidden, "The behaviour released, but the other holder has not.");

            progress.ReleaseHide(holder);
            Assert.True(progress.IsHidden);
        }

        GameObject Create(bool active = true)
        {
            GameObject gameObject = new(nameof(LoadingBehavior));
            if (!active)
                gameObject.SetActive(false);

            _created.Add(gameObject);
            return gameObject;
        }
    }
}
