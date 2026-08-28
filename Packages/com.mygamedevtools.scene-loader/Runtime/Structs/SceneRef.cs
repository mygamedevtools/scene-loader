using System;
#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A reference to a scene, in any form this package accepts: a name, a path, an Addressables
    /// address, a build index, an <c>AssetReference</c>, or an already-loaded <see cref="Scene"/>.
    /// <br/>
    /// A bare <see cref="string"/> stays ambiguous until the operation starts — see
    /// <see cref="SceneRefResolver"/> for the precedence rules and <see cref="Address"/> for the override.
    /// </summary>
    public readonly struct SceneRef : IEquatable<SceneRef>
    {
        /// <summary>What this reference points at.</summary>
        public readonly SceneRefKind Kind => _kind;

        /// <summary>Whether this points at anything. <c>default(SceneRef)</c> does not.</summary>
        public readonly bool IsValid => _kind != SceneRefKind.None;

        /// <summary>
        /// The name, path or address this was built from, or <see langword="null"/>. A resolved
        /// reference keeps it, so a name that became a build index still matches by name.
        /// </summary>
        public readonly string Key => _key;

        /// <summary>Valid for <see cref="SceneRefKind.BuildIndex"/>.</summary>
        public readonly int BuildIndex => _buildIndex;

        /// <summary>Valid for <see cref="SceneRefKind.Scene"/>.</summary>
        public readonly Scene Scene => _scene;

#if ENABLE_ADDRESSABLES
        /// <summary>Valid for <see cref="SceneRefKind.AssetReference"/>.</summary>
        public readonly AssetReference AssetReference => _asset as AssetReference;
#endif

        readonly SceneRefKind _kind;
        readonly int _buildIndex;
        readonly Scene _scene;
        readonly string _key;
        // Typed `object` so the struct's layout does not change shape with ENABLE_ADDRESSABLES.
        // AssetReference is a class, so nothing boxes.
        readonly object _asset;

        SceneRef(SceneRefKind kind, string key, int buildIndex, Scene scene, object asset)
        {
            _kind = kind;
            _key = key;
            _buildIndex = buildIndex;
            _scene = scene;
            _asset = asset;
        }

        /// <summary>
        /// References a scene by name, path or address, settled when the operation starts.
        /// The build settings win when both match; <see cref="Address"/> forces the other way.
        /// </summary>
        public static SceneRef FromKey(string nameOrPathOrAddress)
        {
            if (string.IsNullOrWhiteSpace(nameOrPathOrAddress))
                throw new ArgumentException($"Cannot create a {nameof(SceneRef)} from an empty string.", nameof(nameOrPathOrAddress));

            return new SceneRef(SceneRefKind.Key, nameOrPathOrAddress, -1, default, null);
        }

        /// <summary>
        /// References an address directly, skipping the build-settings probe. The override for
        /// the precedence rule, and the fast path.
        /// </summary>
        public static SceneRef Address(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException($"Cannot create a {nameof(SceneRef)} from an empty address.", nameof(address));

            return new SceneRef(SceneRefKind.Address, address, -1, default, null);
        }

        /// <summary>References a scene by its build index.</summary>
        public static SceneRef FromBuildIndex(int buildIndex)
        {
            if (buildIndex < 0)
                throw new ArgumentException($"Cannot create a {nameof(SceneRef)} with a build index lower than 0.", nameof(buildIndex));

            return new SceneRef(SceneRefKind.BuildIndex, null, buildIndex, default, null);
        }

        /// <summary>References an already-loaded scene. Can only be used to unload it.</summary>
        public static SceneRef FromScene(Scene scene)
        {
            if (!scene.IsValid())
                throw new ArgumentException($"Cannot create a {nameof(SceneRef)} from an invalid scene.", nameof(scene));

            return new SceneRef(SceneRefKind.Scene, null, -1, scene, null);
        }

#if ENABLE_ADDRESSABLES
        /// <summary>References a scene by its Addressables <c>AssetReference</c>.</summary>
        public static SceneRef FromAssetReference(AssetReference assetReference)
        {
            if (assetReference == null)
                throw new ArgumentNullException(nameof(assetReference));
            if (!assetReference.RuntimeKeyIsValid())
                throw new ArgumentException($"Cannot create a {nameof(SceneRef)} from an Asset Reference with an invalid Runtime Key: '{assetReference.RuntimeKey}'.", nameof(assetReference));

            return new SceneRef(SceneRefKind.AssetReference, null, -1, default, assetReference);
        }
#endif

        /// <summary>
        /// The build-settings resolution of a key: a build index that keeps the original string,
        /// so it can still be matched against a loaded scene by name or path.
        /// </summary>
        internal static SceneRef ResolvedToBuildIndex(string key, int buildIndex)
        {
            return new SceneRef(SceneRefKind.BuildIndex, key, buildIndex, default, null);
        }

        public static implicit operator SceneRef(string nameOrPathOrAddress) => FromKey(nameOrPathOrAddress);
        public static implicit operator SceneRef(int buildIndex) => FromBuildIndex(buildIndex);
        public static implicit operator SceneRef(Scene scene) => FromScene(scene);
#if ENABLE_ADDRESSABLES
        public static implicit operator SceneRef(AssetReference assetReference) => FromAssetReference(assetReference);
#endif

        /// <summary>
        /// Whether a loaded <paramref name="scene"/> could be the one this names. The addressable
        /// kinds always answer <see langword="false"/> — an address says nothing about the
        /// resulting scene's name, so that backend hands its <see cref="Scene"/> back directly.
        /// </summary>
        public readonly bool CanBeReferenceToScene(Scene scene)
        {
            return _kind switch
            {
                SceneRefKind.Key => MatchesKey(scene),
                // A resolved key keeps matching by its original name or path. Narrowing it to
                // the build index alone would break unloading an addressable-loaded scene by
                // the same name that loaded it.
                SceneRefKind.BuildIndex => scene.buildIndex == _buildIndex || MatchesKey(scene),
                SceneRefKind.Scene => scene == _scene,
                _ => false,
            };
        }

        readonly bool MatchesKey(Scene scene) => _key != null && (scene.name == _key || scene.path == _key);

        public readonly bool Equals(SceneRef other)
        {
            if (_kind != other._kind)
                return false;

            return _kind switch
            {
                SceneRefKind.None => true,
                SceneRefKind.Key or SceneRefKind.Address => _key == other._key,
                SceneRefKind.BuildIndex => _buildIndex == other._buildIndex,
                SceneRefKind.Scene => _scene == other._scene,
                SceneRefKind.AssetReference => Equals(_asset, other._asset),
                _ => false,
            };
        }

        public override readonly bool Equals(object obj) => obj is SceneRef other && Equals(other);

        public override readonly int GetHashCode()
        {
            return _kind switch
            {
                SceneRefKind.None => 0,
                SceneRefKind.Key or SceneRefKind.Address => HashCode.Combine(_kind, _key),
                SceneRefKind.BuildIndex => HashCode.Combine(_kind, _buildIndex),
                SceneRefKind.Scene => HashCode.Combine(_kind, _scene.handle),
                SceneRefKind.AssetReference => HashCode.Combine(_kind, _asset),
                _ => 0,
            };
        }

        public static bool operator ==(SceneRef left, SceneRef right) => left.Equals(right);
        public static bool operator !=(SceneRef left, SceneRef right) => !left.Equals(right);

        public override readonly string ToString()
        {
            return _kind switch
            {
                SceneRefKind.None => "no scene",
                SceneRefKind.Key => $"scene with name/path/address '{_key}'",
                SceneRefKind.BuildIndex => _key == null
                    ? $"scene with build index '{_buildIndex}'"
                    : $"scene with name/path '{_key}' (build index {_buildIndex})",
                SceneRefKind.Scene => $"scene '{_scene.name}' ({_scene.handle})",
                SceneRefKind.AssetReference => $"scene with asset reference '{_asset}'",
                SceneRefKind.Address => $"scene with addressable address '{_key}'",
                _ => $"unknown {nameof(SceneRef)}",
            };
        }
    }
}
