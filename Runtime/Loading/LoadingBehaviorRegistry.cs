using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Every live <see cref="LoadingBehavior"/>, so a loading screen can find the one that belongs
    /// to it without scanning the scene graph.
    /// </summary>
    /// <remarks>
    /// Entries are matched at query time rather than stored under a key, because a behaviour can
    /// move between scenes after it registers — a prefab loading screen is instantiated in one
    /// scene and adopted into another, which no cached key survives.
    /// </remarks>
    public static partial class LoadingBehaviorRegistry
    {
        static List<LoadingBehavior> _behaviors;

        /// <summary>Finds the behaviour that lives in <paramref name="scene"/>.</summary>
        public static bool TryGet(Scene scene, out LoadingBehavior behavior)
        {
            if (!scene.IsValid())
            {
                behavior = null;
                return false;
            }

            return TryGet(scene, null, out behavior);
        }

        /// <summary>
        /// Finds the behaviour on <paramref name="root"/> or anywhere beneath it — how a screen
        /// built from a prefab or an instantiated hierarchy finds its own.
        /// </summary>
        public static bool TryGet(GameObject root, out LoadingBehavior behavior)
        {
            if (root == null)
            {
                behavior = null;
                return false;
            }

            return TryGet(default, root.transform, out behavior);
        }

        /// <summary>Announces a behaviour, from <c>OnEnable</c>.</summary>
        public static void Register(LoadingBehavior behavior)
        {
            if (behavior == null)
                return;

            _behaviors ??= new List<LoadingBehavior>(4);
            if (_behaviors.Contains(behavior))
                return;

            _behaviors.Add(behavior);

            SceneManagerLog.Verbose($"Registered a {nameof(LoadingBehavior)} for scene '{behavior.gameObject.scene.name}'.");
        }

        /// <summary>Withdraws a behaviour, from <c>OnDisable</c>.</summary>
        public static void Deregister(LoadingBehavior behavior)
        {
            if (behavior == null || _behaviors == null)
                return;

            _behaviors.Remove(behavior);
        }

        /// <summary>
        /// The single matcher behind both lookups. <paramref name="root"/> wins when given;
        /// otherwise the behaviour's current scene is compared. Neither is cached anywhere, so a
        /// behaviour that changed scenes still resolves correctly.
        /// </summary>
        static bool TryGet(Scene scene, Transform root, out LoadingBehavior behavior)
        {
            behavior = null;
            if (_behaviors == null)
                return false;

            int matches = 0;
            // Backwards, so destroyed entries can be dropped in the same pass; the earliest
            // registered match is assigned last and therefore wins.
            for (int i = _behaviors.Count - 1; i >= 0; i--)
            {
                LoadingBehavior candidate = _behaviors[i];
                if (candidate == null)
                {
                    _behaviors.RemoveAt(i);
                    continue;
                }

                bool isMatch = root != null
                    ? candidate.transform.IsChildOf(root)
                    : candidate.gameObject.scene == scene;

                if (!isMatch)
                    continue;

                behavior = candidate;
                matches++;
            }

            if (matches > 1)
            {
                string where = root != null ? $"'{root.name}'" : $"scene '{scene.name}'";
                SceneManagerLog.Warning($"Found {matches} {nameof(LoadingBehavior)}s in {where}, and a loading screen can only be driven by one. Using '{behavior.name}'.");
            }

            return behavior != null;
        }

        // Statics survive a disabled Domain Reload, and these point at behaviours in scenes that
        // would no longer exist.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        internal static void ResetStatics()
        {
            _behaviors = null;
        }
    }
}
