using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public partial class SceneDirectorTests
    {
        readonly int[] _buildIndexes = new[] { 1, 2, 3 };

        [UnityTest]
        public IEnumerator Load_Extension_ByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(director, () => director.LoadAsync(1, true, progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_Extension_ByIndex_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(director, () => director.LoadAsync(_buildIndexes, 1, progress), progress, _buildIndexes.Length, 1);
        }

        [UnityTest]
        public IEnumerator Load_Extension_ByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(director, () => director.LoadAsync(SceneBuilder.SceneNames[1], true, progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_Extension_ByName_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(director, () => director.LoadAsync(SceneBuilder.SceneNames, 1, progress), progress, SceneBuilder.SceneNames.Length, 1);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Load_Extension_Addressable_ByAddress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(director, () => director.LoadAddressableAsync(SceneBuilder.SceneNames[1], true, progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_Extension_Addressable_ByAddress_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(director, () => director.LoadAddressableAsync(SceneBuilder.SceneNames, 1, progress), progress, SceneBuilder.SceneNames.Length, 1);
        }

        [UnityTest]
        public IEnumerator Load_Extension_Addressable_ByAssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(director, () => director.LoadAddressableAsync(_assetReferences[1], true, progress), progress, 1, 0);
        }

        [UnityTest]
        public IEnumerator Load_Extension_Addressable_ByAssetReference_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            var progress = new SimpleProgress();
            yield return Load_Template(director, () => director.LoadAddressableAsync(_assetReferences, 1, progress), progress, _assetReferences.Length, 1);
        }
#endif

        [UnityTest]
        public IEnumerator Transition_Extension_ByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Transition_Template(director, () => director.TransitionAsync(1, 1), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_ByIndex_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Transition_Template(director, () => director.TransitionAsync(_buildIndexes, 1), _buildIndexes.Length, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_ByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Transition_Template(director, () => director.TransitionAsync(SceneBuilder.SceneNames[1], SceneBuilder.SceneNames[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_ByName_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Transition_Template(director, () => director.TransitionAsync(SceneBuilder.SceneNames, SceneBuilder.ScenePaths[0]), SceneBuilder.SceneNames.Length, 0);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Transition_Extension_Addressable_ByAddress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Transition_Template(director, () => director.TransitionAddressableAsync(SceneBuilder.SceneNames[1], SceneBuilder.SceneNames[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_Addressable_ByAddress_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Transition_Template(director, () => director.TransitionAddressableAsync(SceneBuilder.SceneNames, SceneBuilder.SceneNames[0]), SceneBuilder.SceneNames.Length, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_Addressable_ByAssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Transition_Template(director, () => director.TransitionAddressableAsync(_assetReferences[1], _assetReferences[0]), 1, 0);
        }

        [UnityTest]
        public IEnumerator Transition_Extension_Addressable_ByAssetReference_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Transition_Template(director, () => director.TransitionAddressableAsync(_assetReferences, _assetReferences[0]), SceneBuilder.SceneNames.Length, 0);
        }
#endif

        [UnityTest]
        public IEnumerator Unload_Extension_ByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Unload_Template(director, () => director.LoadAsync(1, true), () => director.UnloadAsync(1), 1);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_ByIndex_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Unload_Template(director, () => director.LoadAsync(_buildIndexes, 0), () => director.UnloadAsync(_buildIndexes), _buildIndexes.Length);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_ByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Unload_Template(director, () => director.LoadAsync(SceneBuilder.SceneNames[1], true), () => director.UnloadAsync(SceneBuilder.SceneNames[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_ByName_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Unload_Template(director, () => director.LoadAsync(SceneBuilder.SceneNames, 0), () => director.UnloadAsync(SceneBuilder.SceneNames), SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_ByScene_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            Task<SceneResult> loadTask = Task.FromResult<SceneResult>(default);
            yield return Unload_Template(director, () =>
            {
                loadTask = director.LoadAsync(SceneBuilder.SceneNames, 0);
                return loadTask;
            }, () =>
            {
                SceneResult result = loadTask.GetAwaiter().GetResult();
                return director.UnloadAsync(result.GetScenes());
            }, SceneBuilder.SceneNames.Length);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Unload_Extension_Addressable_ByAddress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Unload_Template(director, () => director.LoadAddressableAsync(SceneBuilder.SceneNames[1], true), () => director.UnloadAddressableAsync(SceneBuilder.SceneNames[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_Addressable_ByAddress_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Unload_Template(director, () => director.LoadAddressableAsync(SceneBuilder.SceneNames, 0), () => director.UnloadAddressableAsync(SceneBuilder.SceneNames), SceneBuilder.SceneNames.Length);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_Addressable_ByAssetReference([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Unload_Template(director, () => director.LoadAddressableAsync(_assetReferences[1], true), () => director.UnloadAddressableAsync(_assetReferences[1]), 1);
        }

        [UnityTest]
        public IEnumerator Unload_Extension_Addressable_ByAssetReference_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director)
        {
            yield return Unload_Template(director, () => director.LoadAddressableAsync(_assetReferences, 0), () => director.UnloadAddressableAsync(_assetReferences), _assetReferences.Length);
        }
#endif
    }
}
