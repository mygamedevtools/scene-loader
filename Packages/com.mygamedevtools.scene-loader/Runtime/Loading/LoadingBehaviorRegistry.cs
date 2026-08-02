using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Which <see cref="LoadingBehavior"/> lives in which scene.
    /// </summary>
    /// <remarks>
    /// This replaces the scan v4 ran on <b>every</b> transition:
    /// <code>
    /// var behaviors = Object.FindObjectsByType&lt;LoadingBehavior&gt;(...);
    /// var behavior  = behaviors.FirstOrDefault(l =&gt; l.gameObject.scene == loadingScene);
    /// </code>
    /// A full scene-graph walk, an allocated array and a LINQ closure, to find something that
    /// could have announced itself. Now it does, in <c>OnEnable</c>.
    /// </remarks>
    public static partial class LoadingBehaviorRegistry
    {
        static Dictionary<int, LoadingBehavior> _behaviorsBySceneHandle;

        /// <summary>
        /// Finds the behaviour registered for a scene.
        /// </summary>
        public static bool TryGet(Scene scene, out LoadingBehavior behavior)
        {
            behavior = null;
            return _behaviorsBySceneHandle != null
                && scene.IsValid()
                && _behaviorsBySceneHandle.TryGetValue(scene.handle.GetHashCode(), out behavior)
                && behavior != null;
        }

        /// <summary>
        /// Announces a behaviour. Called from <see cref="LoadingBehavior.OnEnable"/>.
        /// </summary>
        public static void Register(LoadingBehavior behavior)
        {
            if (behavior == null)
                return;

            _behaviorsBySceneHandle ??= new Dictionary<int, LoadingBehavior>(4);
            _behaviorsBySceneHandle[behavior.gameObject.scene.handle.GetHashCode()] = behavior;

            if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
                SceneManagerLog.Verbose($"Registered a {nameof(LoadingBehavior)} for scene '{behavior.gameObject.scene.name}'.");
        }

        /// <summary>
        /// Withdraws a behaviour. Called from <see cref="LoadingBehavior.OnDisable"/>.
        /// </summary>
        public static void Deregister(LoadingBehavior behavior)
        {
            if (behavior == null || _behaviorsBySceneHandle == null)
                return;

            int key = behavior.gameObject.scene.handle.GetHashCode();
            if (_behaviorsBySceneHandle.TryGetValue(key, out LoadingBehavior registered) && registered == behavior)
                _behaviorsBySceneHandle.Remove(key);
        }

        // Statics survive a disabled Domain Reload, so a previous session's entries would point
        // at destroyed behaviours in scenes that no longer exist.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        internal static void ResetStatics()
        {
            _behaviorsBySceneHandle = null;
        }
    }
}
