using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Which <see cref="LoadingBehavior"/> lives in which scene. Replaces the full scene-graph
    /// scan v4 ran on every transition, to find something that could just announce itself.
    /// </summary>
    public static partial class LoadingBehaviorRegistry
    {
        static Dictionary<int, LoadingBehavior> _behaviorsBySceneHandle;

        /// <summary>Finds the behaviour registered for a scene.</summary>
        public static bool TryGet(Scene scene, out LoadingBehavior behavior)
        {
            behavior = null;
            return _behaviorsBySceneHandle != null
                && scene.IsValid()
                && _behaviorsBySceneHandle.TryGetValue(scene.handle.GetHashCode(), out behavior)
                && behavior != null;
        }

        /// <summary>Announces a behaviour, from <c>OnEnable</c>.</summary>
        public static void Register(LoadingBehavior behavior)
        {
            if (behavior == null)
                return;

            _behaviorsBySceneHandle ??= new Dictionary<int, LoadingBehavior>(4);
            _behaviorsBySceneHandle[behavior.gameObject.scene.handle.GetHashCode()] = behavior;

            if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
                SceneManagerLog.Verbose($"Registered a {nameof(LoadingBehavior)} for scene '{behavior.gameObject.scene.name}'.");
        }

        /// <summary>Withdraws a behaviour, from <c>OnDisable</c>.</summary>
        public static void Deregister(LoadingBehavior behavior)
        {
            if (behavior == null || _behaviorsBySceneHandle == null)
                return;

            int key = behavior.gameObject.scene.handle.GetHashCode();
            if (_behaviorsBySceneHandle.TryGetValue(key, out LoadingBehavior registered) && registered == behavior)
                _behaviorsBySceneHandle.Remove(key);
        }

        // Statics survive a disabled Domain Reload, and these point at behaviours in scenes that
        // would no longer exist.
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
