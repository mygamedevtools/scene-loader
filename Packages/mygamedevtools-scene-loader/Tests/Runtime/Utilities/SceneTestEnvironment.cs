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
        public const string DisposeCategoryName = "Dispose Tests";

        public static readonly ILoadSceneInfo[][] MultipleLoadSceneInfoList = new ILoadSceneInfo[][]
        {
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[2]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
                new LoadSceneInfoIndex(1),
                new LoadSceneInfoIndex(2),
                new LoadSceneInfoIndex(3),
#if ENABLE_ADDRESSABLES
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[2]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[3]),
#endif
            },
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

#if ENABLE_ADDRESSABLES
        public static AssetReference[] SceneAssetReferences;
#endif

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
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            SceneAssetReferences = new AssetReference[sceneCount];
            SceneBuilder.TryBuildScenes(_addressableScenePathBase, (i, s, p) =>
            {
                string guid = AssetDatabase.AssetPathToGUID(p);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.SetAddress(SceneBuilder.SceneNames[i]);

                SceneAssetReferences[i] = new AssetReference(guid);
            });
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

        [OneTimeSetUp]
        public void ValidateSceneEnvironment()
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