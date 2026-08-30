#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
#endif
using UnityEngine.TestTools;
using NUnit.Framework;
#if ENABLE_ADDRESSABLES
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
#endif
using UnityEngine.AddressableAssets;
#endif

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneTestEnvironment : IPrebuildSetup, IPostBuildCleanup
    {
        public const string ScenePathBase = "Assets/_test";
        public const int DefaultTimeout = 3000;

        // ⚠️ Every scene name below exists TWICE: once under `Assets/_test`, which is in the
        // build settings, and once under `Assets/_addressables-test`, registered as an
        // Addressables entry under the very same address. So every bare name is a double match,
        // and the build settings win — which means a case that must actually exercise
        // Addressables has to say `SceneRef.Address(...)`. Translating one of those to a plain
        // string would silently move it onto the standard backend and keep passing.
        static readonly SceneRef[][] _multipleSceneRefList = new SceneRef[][]
        {
            new SceneRef[]
            {
                SceneBuilder.SceneNames[0],
                1,
#if ENABLE_ADDRESSABLES
                SceneRef.Address(SceneBuilder.SceneNames[2]),
                SceneRef.Address(SceneBuilder.SceneNames[3]),
#endif
                SceneBuilder.ScenePaths[3]
            },
            // This list of scenes expects two scene refs that point to the same source scene,
            // and validates whether that causes any issues when linking to the target loaded scene.
            new SceneRef[]
            {
                1,
                SceneBuilder.SceneNames[1],
                SceneBuilder.ScenePaths[1],
#if ENABLE_ADDRESSABLES
                // Since we can't test statically with AssetReference, we should at least validate
                // that two AsyncOperations with the same addressable source do not cause issues.
                SceneRef.Address(SceneBuilder.SceneNames[1]),
                SceneRef.Address(SceneBuilder.SceneNames[1]),
#endif
            }
        };

        public static readonly SceneRef[] SingleSceneRefList = new SceneRef[]
        {
            SceneBuilder.SceneNames[1],
            SceneBuilder.ScenePaths[1],
            1,
#if ENABLE_ADDRESSABLES
            SceneRef.Address(SceneBuilder.SceneNames[1]),
#endif
        };

        public static readonly SceneRef[] SingleSceneRefList_NoAddressable = new SceneRef[]
        {
            SceneBuilder.SceneNames[1],
            SceneBuilder.ScenePaths[1],
        };

        public static readonly SceneParameters[] SceneParametersList = new SceneParameters[]
        {
            new(SingleSceneRefList[0], false),
            new(SingleSceneRefList[0], true),
            new(SingleSceneRefList[1], false),
            new(SingleSceneRefList[1], true),
            new(SingleSceneRefList[2], false),
            new(SingleSceneRefList[2], true),
#if ENABLE_ADDRESSABLES
            new(SingleSceneRefList[3], false),
            new(SingleSceneRefList[3], true),
#endif
            new(_multipleSceneRefList[0], -1),
            new(_multipleSceneRefList[0], 1),
            new(_multipleSceneRefList[1], -1),
            new(_multipleSceneRefList[1], 1),
        };
        public static readonly SceneParameters[] TransitionSceneParametersList = new SceneParameters[]
        {
            new(SingleSceneRefList[0], true),
            new(SingleSceneRefList[1], true),
            new(SingleSceneRefList[2], true),
#if ENABLE_ADDRESSABLES
            new(SingleSceneRefList[3], true),
#endif
            new(_multipleSceneRefList[0], 1),
            new(_multipleSceneRefList[1], 1),
        };

        public static readonly ISceneManager[] SceneManagers = new ISceneManager[]
        {
            new CoreSceneManager(),
        };

#if UNITY_EDITOR
#if ENABLE_ADDRESSABLES
        public const string AddressableScenePathBase = "Assets/_addressables-test";
        const string _sceneReferencePath = AddressableScenePathBase + "/sceneReference.asset";
#endif
#endif

        public void Setup()
        {
#if UNITY_EDITOR
            if (IsSceneEnvironmentSetup())
                return;

            int sceneCount = SceneBuilder.SceneNames.Length;
            List<EditorBuildSettingsScene> buildScenes = new(sceneCount);

            if (!SceneBuilder.TryBuildScenes(ScenePathBase, (i, s, p) => buildScenes.Add(new EditorBuildSettingsScene(p, true))))
                return;

            Debug.Log("Adding test scenes to build settings:\n" + string.Join("\n", buildScenes.Select(scene => scene.path)));
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Union(buildScenes).ToArray();

#if ENABLE_ADDRESSABLES
            SceneReferenceData sceneReferenceData = ScriptableObject.CreateInstance<SceneReferenceData>();
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            SceneBuilder.TryBuildScenes(AddressableScenePathBase, (i, s, p) =>
            {
                string guid = AssetDatabase.AssetPathToGUID(p);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.SetAddress(SceneBuilder.SceneNames[i]);

                sceneReferenceData.sceneReferences.Add(new AssetReference(guid));
            });

            AssetDatabase.CreateAsset(sceneReferenceData, _sceneReferencePath);
            var guid = AssetDatabase.AssetPathToGUID(_sceneReferencePath);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.SetAddress(nameof(SceneReferenceData));
#endif
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            if (!IsSceneEnvironmentSetup())
                return;

            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene => !scene.path.StartsWith(ScenePathBase)).ToArray();

            if (!Directory.Exists(ScenePathBase))
                return;

            AssetDatabase.DeleteAsset(ScenePathBase);
            AssetDatabase.Refresh();

#if ENABLE_ADDRESSABLES
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var scenePaths = EditorBuildSettings.scenes.Where(scene => scene.path.StartsWith(AddressableScenePathBase)).Select(scene => scene.path);
            foreach (var path in scenePaths)
                settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(path), false);

            settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(_sceneReferencePath), false);

            AssetDatabase.DeleteAsset(AddressableScenePathBase);
            AssetDatabase.Refresh();
#endif
#endif
        }

        public static void ValidateSceneEnvironment()
        {
#if UNITY_EDITOR
            Assert.True(IsSceneEnvironmentSetup());

#if ENABLE_ADDRESSABLES
            var operation = Addressables.LoadResourceLocationsAsync(SceneBuilder.SceneNames);
            operation.WaitForCompletion();

            Assert.True(areLocationsValid());

            bool areLocationsValid()
            {
                foreach (var location in operation.Result)
                    if (location == null || string.IsNullOrEmpty(location.PrimaryKey))
                        return false;
                return true;
            }
#endif
#endif
        }

        public static bool IsSceneEnvironmentSetup()
        {
#if UNITY_EDITOR
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            foreach (string name in SceneBuilder.SceneNames)
            {
                if (!hasBuiltSceneWithName(name, buildScenes))
                    return false;
            }
            return true;

            static bool hasBuiltSceneWithName(string name, EditorBuildSettingsScene[] buildScenes)
            {
                foreach (EditorBuildSettingsScene buildScene in buildScenes)
                    if (buildScene.path.Contains(name))
                        return true;
                return false;
            }
#else
            return false;
#endif
        }
    }
}
