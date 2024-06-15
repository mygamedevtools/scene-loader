using System;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Responsible for reporting the scene loading progress.
    /// </summary>
    public class LoadingProgress : IProgress<float>
    {
        /// <summary>
        /// Reports when the <see cref="State"/> has changed.
        /// </summary>
        public event Action<LoadingState> StateChanged;
        /// <summary>
        /// Reports when the scene loading progress increases. Values range from 0 to 1.
        /// </summary>
        public event Action<float> Progressed;

        /// <summary>
        /// Current state of the scene loading progress.
        /// </summary>
        public LoadingState State { get; private set; }

        /// <summary>
        /// Creates a new instance of a <see cref="LoadingProgress"/>, optionally adjusting its loading ratio.
        /// <br/>
        /// </summary>
        public LoadingProgress()
        {
            State = LoadingState.WaitingToStart;
        }

        /// <summary>
        /// Sets the <see cref="State"/> value.
        /// You can use this method to apply loading screen transition effects, such as fade in/out.
        /// <br/>
        /// The target scene will only start to be loaded on the <see cref="LoadingState.Loading"/> state, and the entire transition will only finish in the <see cref="LoadingState.TransitionComplete"/> state.
        /// The <see cref="ISceneLoader"/> only sets the <see cref="LoadingState.TargetSceneLoaded"/> state, once after the target scene has finished loading in the background.
        /// </summary>
        /// <param name="targetState">The <see cref="LoadingState"/> to be set.</param>
        public void SetState(LoadingState targetState)
        {
            State = targetState;
            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// <see cref="IProgress{T}"/> implementation. Reports the scene loading progress value, ranging from 0 to 1.
        /// </summary>
        /// <param name="value">Scene loading progress value, ranging from 0 to 1.</param>
        public void Report(float value)
        {
            Progressed?.Invoke(Mathf.Clamp01(value));
        }
    }
}