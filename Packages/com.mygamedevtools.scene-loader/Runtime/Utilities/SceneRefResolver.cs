using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_ADDRESSABLES
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.AddressableAssets.ResourceLocators;
#endif

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Decides what a bare string means, which is what lets
    /// <c>TransitionAsync("target", "loading")</c> work for addressable and non-addressable
    /// scenes alike.
    /// <br/><br/>
    /// <b>The build settings win.</b> <see cref="SceneRef.Address"/> is the override.
    /// <br/><br/>
    /// Resolution is observable behaviour: adding a scene to the build settings later can flip a
    /// string from one backend to the other with no code change. Every first resolution logs at
    /// <see cref="SceneLogLevel.Verbose"/> and a double match at <see cref="SceneLogLevel.Warning"/>,
    /// so that is diagnosable rather than mysterious. The first addressable-by-string resolution
    /// also pays catalog-initialisation latency; later ones hit the cache.
    /// </summary>
    public static partial class SceneRefResolver
    {
        // Name and path to build index, built once per session.
        static Dictionary<string, int> _buildSettingsMap;
        // Key to its settled reference, so a key is probed at most once.
        static Dictionary<string, SceneRef> _resolutionCache;
        // The size the map was built from, so we notice when the build settings change.
        static int _mappedSceneCount = -1;

        /// <summary>
        /// Settles every reference, returning a new array. Only a never-seen key the build
        /// settings do not have needs the catalog, and only that case suspends.
        /// </summary>
        public static Task<SceneRef[]> ResolveAllAsync(SceneRef[] sceneRefs)
        {
            if (sceneRefs == null || sceneRefs.Length == 0)
                throw new System.ArgumentException($"Cannot resolve a null or empty {nameof(SceneRef)} array.", nameof(sceneRefs));

            int length = sceneRefs.Length;
            SceneRef[] resolved = new SceneRef[length];

            for (int i = 0; i < length; i++)
            {
                if (!TryResolveImmediate(sceneRefs[i], out resolved[i]))
                    return ResolveAllSlowAsync(sceneRefs, resolved, i);
            }

            return Task.FromResult(resolved);
        }

        /// <summary>Settles a single reference. See <see cref="ResolveAllAsync"/> for the cost model.</summary>
        public static async Task<SceneRef> ResolveAsync(SceneRef sceneRef)
        {
            if (TryResolveImmediate(sceneRef, out SceneRef resolved))
                return resolved;

            return await ResolveByProbeAsync(sceneRef);
        }

        /// <summary>
        /// Settles a reference without touching the catalog.
        /// </summary>
        /// <returns><see langword="false"/> when the answer needs the catalog, which makes it async.</returns>
        public static bool TryResolveImmediate(SceneRef sceneRef, out SceneRef resolved)
        {
            resolved = sceneRef;

            if (sceneRef.Kind != SceneRefKind.Key)
                return true;

            // Before the cache, not after: this is what drops previously cached answers when
            // the build settings have changed underneath them.
            EnsureBuildSettingsMap();

            if (_resolutionCache != null && _resolutionCache.TryGetValue(sceneRef.Key, out SceneRef cached))
            {
                resolved = cached;
                return true;
            }

            if (TryResolveFromBuildSettings(sceneRef.Key, out resolved))
            {
                WarnIfAlsoAddressable(sceneRef.Key);
                Cache(sceneRef.Key, resolved);
                return true;
            }

#if ENABLE_ADDRESSABLES
            resolved = sceneRef;
            return false;
#else
            throw NotFound(sceneRef.Key);
#endif
        }

        /// <summary>
        /// Drops the build-settings map and every cached resolution. Automatic when the
        /// build-settings count changes; exposed so tests can force a re-probe.
        /// </summary>
        public static void Invalidate()
        {
            _buildSettingsMap = null;
            _resolutionCache = null;
            _mappedSceneCount = -1;
        }

        static async Task<SceneRef[]> ResolveAllSlowAsync(SceneRef[] sceneRefs, SceneRef[] resolved, int startIndex)
        {
            int length = sceneRefs.Length;
            for (int i = startIndex; i < length; i++)
            {
                resolved[i] = TryResolveImmediate(sceneRefs[i], out SceneRef immediate)
                    ? immediate
                    : await ResolveByProbeAsync(sceneRefs[i]);
            }
            return resolved;
        }

        static bool TryResolveFromBuildSettings(string key, out SceneRef resolved)
        {
            if (_buildSettingsMap.TryGetValue(key, out int buildIndex))
            {
                resolved = SceneRef.ResolvedToBuildIndex(key, buildIndex);
                return true;
            }

            resolved = default;
            return false;
        }

        static void EnsureBuildSettingsMap()
        {
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            if (_buildSettingsMap != null && _mappedSceneCount == sceneCount)
                return;

            // The count changed, so any key that previously missed the build settings and fell
            // through to Addressables may now resolve differently. Drop those answers too.
            _resolutionCache = null;
            _buildSettingsMap = new Dictionary<string, int>(sceneCount * 2);
            _mappedSceneCount = sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path))
                    continue;

                _buildSettingsMap[path] = i;

                // Both spellings the Unity Scene Manager itself accepts.
                int slash = path.LastIndexOf('/');
                int dot = path.LastIndexOf('.');
                if (dot > slash)
                    _buildSettingsMap[path.Substring(slash + 1, dot - slash - 1)] = i;
            }
        }

        static void Cache(string key, SceneRef resolved)
        {
            _resolutionCache ??= new Dictionary<string, SceneRef>();
            _resolutionCache[key] = resolved;

            if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
                SceneManagerLog.Verbose($"Resolved '{key}' to {resolved}.");
        }

        static System.Exception NotFound(string key)
        {
#if ENABLE_ADDRESSABLES
            const string lookedIn = "the build settings or the Addressables catalog";
#else
            const string lookedIn = "the build settings (Addressables is not installed, so no address lookup was attempted)";
#endif
            return new System.ArgumentException(
                $"Could not resolve the scene '{key}'. It was not found in {lookedIn}. " +
                $"Add it to the build settings, register it as an Addressables entry, or pass an explicit reference.", nameof(key));
        }

        // Statics survive a disabled Domain Reload, so the previous session's build-settings map
        // and resolution answers would otherwise carry into the next one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        internal static void ResetStatics()
        {
            Invalidate();
        }

#if ENABLE_ADDRESSABLES
        static async Task<SceneRef> ResolveByProbeAsync(SceneRef sceneRef)
        {
            // Another reference in the same group may have probed this key while we awaited.
            if (TryResolveImmediate(sceneRef, out SceneRef immediate))
                return immediate;

            string key = sceneRef.Key;
            SceneRef resolved = await IsAddressableAsync(key)
                ? SceneRef.Address(key)
                : throw NotFound(key);

            Cache(key, resolved);
            return resolved;
        }

        static async Task<bool> IsAddressableAsync(string key)
        {
            AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(key);
            try
            {
                IList<IResourceLocation> locations = await handle.Task;
                return handle.Status == AsyncOperationStatus.Succeeded && locations != null && locations.Count > 0;
            }
            finally
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Reports a key the build settings answered that Addressables could also have. Walks
        /// already-loaded locators rather than starting a catalog load, so the fast path stays
        /// synchronous — the trade is a missed warning when the catalog is not up yet, never a
        /// wrong resolution.
        /// </summary>
        static void WarnIfAlsoAddressable(string key)
        {
            if (!SceneManagerLog.IsEnabled(SceneLogLevel.Warning))
                return;

            foreach (IResourceLocator locator in Addressables.ResourceLocators)
            {
                if (!locator.Locate(key, typeof(object), out IList<IResourceLocation> locations) || locations == null || locations.Count == 0)
                    continue;

                SceneManagerLog.Warning(
                    $"The scene '{key}' matches both the build settings and an Addressables entry. " +
                    $"The build settings take precedence. Use {nameof(SceneRef)}.{nameof(SceneRef.Address)}(\"{key}\") to load the addressable one.");
                return;
            }
        }
#else
        static Task<SceneRef> ResolveByProbeAsync(SceneRef sceneRef)
        {
            throw NotFound(sceneRef.Key);
        }

        static void WarnIfAlsoAddressable(string key) { }
#endif
    }
}
