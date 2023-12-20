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
using UnityEditor.AddressableAssets;
#endif
using UnityEngine.AddressableAssets;
#endif

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneTestEnvironment : IPrebuildSetup, IPostBuildCleanup
    {
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
            var buildScenes = new List<EditorBuildSettingsScene>(SceneBuilder.SceneNames.Length);

            if (!SceneBuilder.TryBuildScenes(_scenePathBase, (i, s, p) => buildScenes.Add(new EditorBuildSettingsScene(p, true))))
                return;

            Debug.Log("Adding test scenes to build settings:\n" + string.Join("\n", buildScenes.Select(scene => scene.path)));
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Union(buildScenes).ToArray();

#if ENABLE_ADDRESSABLES
            var sceneReferenceData = ScriptableObject.CreateInstance<SceneReferenceData>();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            SceneBuilder.TryBuildScenes(_addressableScenePathBase, (i, s, p) =>
            {
                var guid = AssetDatabase.AssetPathToGUID(p);
                var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
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