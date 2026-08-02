using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Static class to simplify the creation of <see cref="ISceneData"/> objects.
    /// </summary>
    public static class SceneDataBuilder
    {
        /// <summary>
        /// Builds an <see cref="ISceneData"/> of the appropriate type for a <b>resolved</b>
        /// <see cref="SceneRef"/> — see <see cref="SceneRefResolver"/>, which settles a bare
        /// string into one of these kinds before it ever gets here.
        /// </summary>
        public static ISceneData BuildFromSceneRef(SceneRef sceneRef)
        {
            return sceneRef.Kind switch
            {
                SceneRefKind.BuildIndex => new SceneDataStandard(sceneRef),
#if ENABLE_ADDRESSABLES
                SceneRefKind.AssetReference or SceneRefKind.Address => new SceneDataAddressable(sceneRef),
#endif
                _ => throw new System.ArgumentException($"[{nameof(SceneDataBuilder)}] Cannot load {sceneRef}. A {nameof(SceneRefKind)} of '{sceneRef.Kind}' cannot start a load operation.", nameof(sceneRef)),
            };
        }

        /// <summary>
        /// Builds a non-addressable <see cref="ISceneData"/> with a loaded <see cref="Scene"/> reference.
        /// </summary>
        public static ISceneData BuildFromScene(Scene scene)
        {
            return new SceneDataStandard(scene);
        }
    }
}
