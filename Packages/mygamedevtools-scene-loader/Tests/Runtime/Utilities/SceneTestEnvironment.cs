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
        public const int DefaultTimeout = 3000;

        public static readonly ILoadSceneInfo[][] MultipleLoadSceneInfoList = new ILoadSceneInfo[][]
        {
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[0]),
                new LoadSceneInfoIndex(1),
#if ENABLE_ADDRESSABLES
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[2]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[3]),
#endif
            },
            // This list of scenes expects two load scene infos that point to the same source scene,
            // and validates whether that causes any issues when linking to the target loaded scene.
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoIndex(1),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
#if ENABLE_ADDRESSABLES
                // Since we can't test statically with AssetReference, we should at least validate
                // that two AsyncOperations with the same addressable source do not cause issues.
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
#endif
            }
        };

        public static readonly ILoadSceneInfo[] SingleLoadSceneInfoList = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoIndex(1),
#if ENABLE_ADDRESSABLES
            new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
#endif
        };

        public static readonly ISceneManager[] SceneManagers = new ISceneManager[]
        {
            new AdvancedSceneManager(),
        };

#if UNITY_EDITOR
        const string _scenePathBase = "Assets/_test";
#if ENABLE_ADDRESSABLES
        const string _addressableScenePathBase = "Assets/_addressables-test";
        const string _sceneReferencePath = _addressableScenePathBase + "/sceneReference.asset";
#endif
#endif

        public void Setup()
        {
#if UNITY_EDITOR
            int sceneCount = SceneBuilder.SceneNames.Length;
            List<EditorBuildSettingsScene> buildScenes = new(sceneCount);

            if (!SceneBuilder.TryBuildScenes(_scenePathBase, (i, s, p) => buildScenes.Add(new EditorBuildSettingsScene(p, true))))
                return;

            Debug.Log("Adding test scenes to build settings:\n" + string.Join("\n", buildScenes.Select(scene => scene.path)));
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Union(buildScenes).ToArray();

#if ENABLE_ADDRESSABLES
            SceneReferenceData sceneReferenceData = ScriptableObject.CreateInstance<SceneReferenceData>();
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            SceneBuilder.TryBuildScenes(_addressableScenePathBase, (i, s, p) =>
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
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene => !scene.path.StartsWith(_scenePathBase)).ToArray();

            if (!Directory.Exists(_scenePathBase))
                return;

            AssetDatabase.DeleteAsset(_scenePathBase);
            AssetDatabase.Refresh();

#if ENABLE_ADDRESSABLES
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var scenePaths = EditorBuildSettings.scenes.Where(scene => scene.path.StartsWith(_addressableScenePathBase)).Select(scene => scene.path);
            foreach (var path in scenePaths)
                settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(path), false);

            settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(_sceneReferencePath), false);

            AssetDatabase.DeleteAsset(_addressableScenePathBase);
            AssetDatabase.Refresh();
#endif
#endif
        }

        public static void ValidateSceneEnvironment()
        {
#if UNITY_EDITOR
            var builtScenes = EditorBuildSettings.scenes;
            Assert.True(hasAllEnvironmentScenes());

            bool hasAllEnvironmentScenes()
            {
                foreach (var name in SceneBuilder.SceneNames)
                {
                    if (!hasBuiltSceneWithName(name))
                        return false;
                }
                return true;
            }

            bool hasBuiltSceneWithName(string name)
            {
                foreach (var builtScene in builtScenes)
                    if (builtScene.path.Contains(name))
                        return true;
                return false;
            }

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
    }
}