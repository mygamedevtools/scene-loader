/**
 * LoadingState.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-01-31
 */

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// States of the loading scene, during a scene transition operation.
    /// </summary>
    public enum LoadingState
    {
        /// <summary>
        /// Default state. Holds the loading of the target scene until the <see cref="Loading"/> state is set.
        /// Can be used to display loading screen transition effects, such as a fade in.
        /// </summary>
        WaitingToStart,
        /// <summary>
        /// The target scene is being loaded during this state.
        /// Can be set automatically by the <see cref="LoadingBehavior"/> if <see cref="LoadingBehavior.waitForScriptedStart"/> is disabled.
        /// <br/>
        /// Otherwise, it should be set via <see cref="LoadingProgress.SetState(LoadingState)"/> after a loading screen transition effect, such as a fade in.
        /// </summary>
        Loading,
        /// <summary>
        /// The target scene has been successfully loaded at this state.
        /// It is set by the <see cref="ISceneLoader"/>. Do not set this state manually.
        /// <br/>
        /// Holds the conclusion of the scene transition until the <see cref="TransitionComplete"/> state is set.
        /// </summary>
        TargetSceneLoaded,
        /// <summary>
        /// The scene transition is complete and the loading scene can be unloaded.
        /// Can be set automatically by the <see cref="LoadingBehavior"/> if <see cref="LoadingBehavior.waitForScriptedEnd"/> is disabled.
        /// <br/>
        /// Otherwise, it should be set via <see cref="LoadingProgress.SetState(LoadingState)"/> after a loading screen transition effect, such as a fade out.
        /// </summary>
        TransitionComplete
    }
}