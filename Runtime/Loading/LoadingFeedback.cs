namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Base for components that display loading progress. Subclasses only have to say what to do
    /// with a value between 0 and 1.
    /// </summary>
    public abstract class LoadingFeedback : LoadingScreenComponent
    {
        protected override void OnBound()
        {
            Progress.Progressed += OnProgressed;
        }

        protected override void OnDestroy()
        {
            if (Progress != null)
                Progress.Progressed -= OnProgressed;

            base.OnDestroy();
        }

        /// <summary>Displays the loading progress, from 0 to 1.</summary>
        protected abstract void OnProgressed(float progress);
    }
}
