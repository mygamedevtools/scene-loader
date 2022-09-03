/**
 * ILoading.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/3/2022 (en-US)
 */

using System;
using UnityEngine;

namespace MyUnityTools.SceneLoading
{
    /// <summary>
    /// Base class to standardize the loading screen behavior.
    /// Also implements <see cref="IProgress{T}"/>.
    /// </summary>
    public abstract class LoadingBase : MonoBehaviour, IProgress<float>
    {
        /// <summary>
        /// Event that reports the scene load progression in percentage.
        /// </summary>
        public event SceneLoadProgressDelegate OnProgress;
        /// <summary>
        /// Event that reports when the loading has been completed.
        /// </summary>
        public event Action OnLoadingComplete;

        /// <summary>
        /// Controls if the loading screen is in an active state, in other words, if it's currently in focus.
        /// This is required for the <see cref="ISceneLoader.TransitionToScene(ILoadSceneInfo, ILoadSceneInfo)"/> to know when it should perform its internal operations.
        /// Before the loading screen can be actually seen, <see cref="Active"/> should be set to false.
        /// For example, if the loading screen is being faded in, or if an animation is transitioning to it.
        /// Then, once it's completely visible, <see cref="Active"/> should be set to true. The loading process starts.
        /// Once the target scene has finished loading, then the loading screen gets faded out or transitions out.
        /// After the transition has been completed and the loading screen is no longer visible, <see cref="Active"/>
        /// should be set to false again.
        /// </summary>
        public bool Active { get; protected set; }

        [SerializeField, Tooltip("Common scene operations stop at 90%, but addressable scene operations go all the way up to 100%. Enabling this value reduces the ratio to 90% instead of 100%")]
        protected bool _reduceLoadRatio;

        /// <summary>
        /// Default ratio to consider when displaying the loading progress bar.
        /// The <see cref="UnityEngine.SceneManagement.SceneManager"/> operations stop at 90%, so you might want to set that to 0.9f.
        /// The addressable scene operations go all the way up to 100%.
        /// </summary>
        protected float _ratio = 1f;

        protected virtual void Awake() => _ratio = _reduceLoadRatio ? .9f : 1f;

        /// <summary>
        /// Reports that the loading of the target scene has been completed, and the loading view can transition out of the screen.
        /// </summary>
        public virtual void CompleteLoading() => OnLoadingComplete?.Invoke();

        /// <summary>
        /// Reports the current scene load progression in percentage.
        /// </summary>
        /// <param name="progress">The percentage of the scene load progression.</param>
        public virtual void Report(float progress) => OnProgress?.Invoke(progress / _ratio);
    }

    /// <summary>
    /// Delegate to propagate the scene load progress in percentage.
    /// </summary>
    /// <param name="progress">The percentage value of the load progression from 0 to 1.</param>
    public delegate void SceneLoadProgressDelegate(float progress);
}