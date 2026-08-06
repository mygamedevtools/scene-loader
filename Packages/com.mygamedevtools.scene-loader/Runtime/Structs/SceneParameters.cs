using System;
#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// One or more <see cref="SceneRef"/>s, plus which of them should become the active scene.
    /// <br/>
    /// The conversions below are what collapsed v4's 64 async methods into four. They exist one
    /// per source type rather than chaining through <see cref="SceneRef"/> because <b>C#
    /// user-defined conversions do not chain</b>.
    /// </summary>
    public readonly struct SceneParameters
    {
        /// <summary>How many scenes this refers to.</summary>
        public readonly int Length => _sceneRefs.Length;

        readonly SceneRef[] _sceneRefs;
        readonly int _setIndexActive;

        /// <summary>Refers to one or more scenes, activating none of them.</summary>
        public SceneParameters(params SceneRef[] sceneRefs)
        {
            _sceneRefs = Validate(sceneRefs);
            _setIndexActive = -1;
        }

        /// <summary>Refers to one or more scenes, activating the one at <paramref name="setIndexActive"/>, or none if negative.</summary>
        public SceneParameters(SceneRef[] sceneRefs, int setIndexActive)
        {
            _sceneRefs = Validate(sceneRefs);
            if (setIndexActive >= _sceneRefs.Length)
                throw new ArgumentOutOfRangeException(nameof(setIndexActive), setIndexActive, $"The index to activate is beyond the {_sceneRefs.Length} provided scenes.");

            _setIndexActive = setIndexActive;
        }

        /// <summary>Refers to a single scene, optionally activating it.</summary>
        public SceneParameters(SceneRef sceneRef, bool setActive)
        {
            _sceneRefs = new[] { sceneRef };
            _setIndexActive = setActive ? 0 : -1;
        }

        // The array conversions cannot carry an index to activate. These four keep "load these
        // and make the second one active" a one-liner, per array type rather than per operation.

        /// <inheritdoc cref="SceneParameters(SceneRef[], int)"/>
        public SceneParameters(string[] namesOrPathsOrAddresses, int setIndexActive) : this(Convert(namesOrPathsOrAddresses, SceneRef.FromKey), setIndexActive) { }

        /// <inheritdoc cref="SceneParameters(SceneRef[], int)"/>
        public SceneParameters(int[] buildIndices, int setIndexActive) : this(Convert(buildIndices, SceneRef.FromBuildIndex), setIndexActive) { }

        /// <inheritdoc cref="SceneParameters(SceneRef[], int)"/>
        public SceneParameters(Scene[] scenes, int setIndexActive) : this(Convert(scenes, SceneRef.FromScene), setIndexActive) { }

#if ENABLE_ADDRESSABLES
        /// <inheritdoc cref="SceneParameters(SceneRef[], int)"/>
        public SceneParameters(AssetReference[] assetReferences, int setIndexActive) : this(Convert(assetReferences, SceneRef.FromAssetReference), setIndexActive) { }
#endif

        /// <summary>The first referenced scene.</summary>
        public readonly SceneRef GetSceneRef() => _sceneRefs[0];

        /// <summary>Every referenced scene.</summary>
        public readonly SceneRef[] GetSceneRefs() => _sceneRefs;

        /// <summary>Whether any scene should be activated once loaded.</summary>
        public readonly bool ShouldSetActive() => _setIndexActive >= 0;

        /// <summary>Index of the scene to activate, or negative if none should be.</summary>
        public readonly int GetIndexToActivate() => _setIndexActive;

        static SceneRef[] Validate(SceneRef[] sceneRefs)
        {
            if (sceneRefs == null || sceneRefs.Length == 0)
                throw new ArgumentException($"Cannot create a {nameof(SceneParameters)} from a null or empty {nameof(SceneRef)} array.", nameof(sceneRefs));

            return sceneRefs;
        }

        public static implicit operator SceneParameters(SceneRef sceneRef) => new(sceneRef, false);
        public static implicit operator SceneParameters(string nameOrPathOrAddress) => new(SceneRef.FromKey(nameOrPathOrAddress), false);
        public static implicit operator SceneParameters(int buildIndex) => new(SceneRef.FromBuildIndex(buildIndex), false);
        public static implicit operator SceneParameters(Scene scene) => new(SceneRef.FromScene(scene), false);

        public static implicit operator SceneParameters(SceneRef[] sceneRefs) => new(sceneRefs);
        public static implicit operator SceneParameters(string[] namesOrPathsOrAddresses) => new(Convert(namesOrPathsOrAddresses, SceneRef.FromKey));
        public static implicit operator SceneParameters(int[] buildIndices) => new(Convert(buildIndices, SceneRef.FromBuildIndex));
        public static implicit operator SceneParameters(Scene[] scenes) => new(Convert(scenes, SceneRef.FromScene));

#if ENABLE_ADDRESSABLES
        public static implicit operator SceneParameters(AssetReference assetReference) => new(SceneRef.FromAssetReference(assetReference), false);
        public static implicit operator SceneParameters(AssetReference[] assetReferences) => new(Convert(assetReferences, SceneRef.FromAssetReference));
#endif

        static SceneRef[] Convert<T>(T[] source, Func<T, SceneRef> selector)
        {
            if (source == null)
                throw new ArgumentException($"Cannot create a {nameof(SceneParameters)} from a null array.", nameof(source));

            SceneRef[] sceneRefs = new SceneRef[source.Length];
            for (int i = 0; i < source.Length; i++)
                sceneRefs[i] = selector(source[i]);

            return sceneRefs;
        }
    }
}
