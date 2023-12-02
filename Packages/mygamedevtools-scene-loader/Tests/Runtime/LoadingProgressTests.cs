using NUnit.Framework;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class LoadingProgressTests
    {
        [Test]
        public void SetState_Test()
        {
            var progress = new LoadingProgress();
            Assert.AreEqual(LoadingState.WaitingToStart, progress.State);

            LoadingState cachedState = default;
            progress.StateChanged += state => cachedState = state;

            progress.SetState(LoadingState.Loading);
            Assert.AreEqual(LoadingState.Loading, progress.State);
            Assert.AreEqual(progress.State, cachedState);
        }

        [Test]
        public void Progress_Test()
        {
            var progress = new LoadingProgress();

            float reportedValue = 0;
            progress.Progressed += value => reportedValue = value;

            progress.Report(.5f);
            Assert.AreEqual(.5f, reportedValue);

            progress = new LoadingProgress(true);

            progress.Progressed += value => reportedValue = value;

            progress.Report(.9f);
            Assert.AreEqual(1, reportedValue);
        }
    }
}