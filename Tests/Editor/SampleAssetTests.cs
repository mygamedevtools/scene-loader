using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// Guards the sample's committed scenes and prefabs against holding a component whose script
    /// no longer exists.
    /// </summary>
    /// <remarks>
    /// Deleting or renaming a MonoBehaviour the sample references leaves a "Missing (Mono Script)"
    /// entry that raises nothing and does nothing — the scene simply stops working, in silence.
    /// It has happened twice: the sample's HUD stopped loading because the component asking for it
    /// had been deleted, and neither compilation nor the play-mode suite noticed, because neither
    /// looks at what a scene actually holds.
    /// <br/><br/>
    /// Assets are discovered by path rather than from a list, so a scene or prefab added to the
    /// sample is covered without anyone remembering to add it here.
    /// </remarks>
    public class SampleAssetTests
    {
        const string _sampleRoot = "Packages/com.mygamedevtools.scene-loader/Samples/LoadingSceneExamples";

        static string[] ScenePaths => Find("t:Scene");
        static string[] PrefabPaths => Find("t:Prefab");

        [Test]
        public void TheSampleHasScenesAndPrefabsToCheck()
        {
            // Guards the guard: a path typo would otherwise leave every test below passing
            // vacuously over an empty set.
            Assert.IsNotEmpty(ScenePaths, $"No scenes found under '{_sampleRoot}'.");
            Assert.IsNotEmpty(PrefabPaths, $"No prefabs found under '{_sampleRoot}'.");
        }

        [Test]
        public void Scenes_HoldNoMissingScripts([ValueSource(nameof(ScenePaths))] string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            List<string> broken = new();
            foreach (GameObject root in scene.GetRootGameObjects())
                Collect(root, broken);

            AssertNone(broken, path);
        }

        [Test]
        public void Prefabs_HoldNoMissingScripts([ValueSource(nameof(PrefabPaths))] string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, $"'{path}' could not be loaded as a prefab.");

            List<string> broken = new();
            Collect(prefab, broken);

            AssertNone(broken, path);
        }

        static string[] Find(string filter) =>
            System.Array.ConvertAll(AssetDatabase.FindAssets(filter, new[] { _sampleRoot }), AssetDatabase.GUIDToAssetPath);

        /// <summary>
        /// Counts components whose script is gone, on every object in the hierarchy.
        /// </summary>
        /// <remarks>
        /// Through <see cref="GameObjectUtility"/>, the API built for the question, rather than
        /// scanning for <see langword="null"/> entries from <c>GetComponentsInChildren</c>.
        /// <br/>
        /// Verified the only way worth trusting: by pointing a prefab's script reference at a GUID
        /// that does not exist and watching this fail — on the prefab and on both scenes that
        /// instance it — then restoring it.
        /// </remarks>
        static void Collect(GameObject root, List<string> broken)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (missing > 0)
                    broken.Add($"{transform.name} ({missing})");
            }
        }

        static void AssertNone(List<string> broken, string path)
        {
            Assert.IsEmpty(broken,
                $"'{path}' holds {broken.Count} component(s) whose script is missing, on: {string.Join(", ", broken)}. " +
                "Something the asset references was deleted or renamed.");
        }
    }
}
