using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Picks the backend for a resolved reference kind — once per scene, at the start of an
    /// operation, rather than at every call site.
    /// <br/><br/>
    /// This replaces v4's <c>SceneDataBuilder</c> and, with it, the <c>LoadSceneInfoType</c>
    /// switches that were scattered across four files.
    /// </summary>
    public static partial class SceneBackendRegistry
    {
        static List<ISceneBackend> _backends;

        /// <summary>
        /// The backend that handles <paramref name="kind"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// No registered backend handles the kind. <see cref="SceneRefKind.Key"/> lands here by
        /// design: an unresolved key has no backend until <see cref="SceneRefResolver"/> settles
        /// it.
        /// </exception>
        public static ISceneBackend GetBackend(SceneRefKind kind)
        {
            EnsureDefaults();

            for (int i = _backends.Count - 1; i >= 0; i--)
            {
                if (_backends[i].CanHandle(kind))
                    return _backends[i];
            }

            throw new ArgumentException(
                kind == SceneRefKind.Key
                    ? $"A {nameof(SceneRefKind)}.{nameof(SceneRefKind.Key)} has no backend until it is resolved. This is an internal error: {nameof(SceneRefResolver)} should have settled it first."
                    : $"No registered {nameof(ISceneBackend)} handles a {nameof(SceneRefKind)} of '{kind}'.", nameof(kind));
        }

        /// <summary>
        /// Adds a backend, which takes precedence over anything registered before it for the
        /// kinds it claims.
        /// </summary>
        public static void Register(ISceneBackend backend)
        {
            if (backend == null)
                throw new ArgumentNullException(nameof(backend));

            EnsureDefaults();
            _backends.Add(backend);
        }

        static void EnsureDefaults()
        {
            if (_backends != null)
                return;

            _backends = new List<ISceneBackend>(2) { new StandardSceneBackend() };
#if ENABLE_ADDRESSABLES
            _backends.Add(new AddressablesSceneBackend());
#endif
        }

        // Statics survive a disabled Domain Reload, so a backend registered by a previous session
        // would otherwise still be serving requests in the next one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        internal static void ResetStatics()
        {
            _backends = null;
        }
    }
}
