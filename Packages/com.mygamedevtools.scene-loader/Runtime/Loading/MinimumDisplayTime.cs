using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Keeps a loading screen up for at least a set time, however fast the load turns out to be.
    /// </summary>
    /// <remarks>
    /// A real requirement rather than a debug aid: without it, a scene that loads in two frames
    /// produces a screen that flashes on and off, which reads as a glitch.
    /// <br/><br/>
    /// It holds <see cref="LoadingProgress.HoldCompletion"/>, not the hide gate — the distinction is
    /// the whole point. Holding the hide gate delays the transition while the screen has already been
    /// told to go, so a fade runs to its end and the remaining wait plays out on an empty screen.
    /// Holding the cue keeps the screen up, and whatever plays it out starts when it should.
    /// </remarks>
    [AddComponentMenu("Scene Loading/Minimum Display Time")]
    public class MinimumDisplayTime : LoadingScreenComponent
    {
        [Tooltip("How long the screen stays up, measured from when it appeared.")]
        [Min(0)]
        [SerializeField]
        float _seconds = 2f;

        float _shownAt;

        protected override void OnBound()
        {
            _shownAt = Time.unscaledTime;
            Progress.HoldCompletion(this);
        }

        void Update()
        {
            if (Progress == null || Time.unscaledTime - _shownAt < _seconds)
                return;

            Progress.ReleaseCompletion(this);
            // Nothing left to do; releasing twice would be harmless, but polling forever is waste.
            enabled = false;
        }
    }
}
