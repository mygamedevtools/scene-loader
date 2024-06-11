#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
#endif
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using UnityEditor.AddressableAssets.Settings;

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
        protected const string _disposeCategoryName = "Dispose Tests";

#if ENABLE_ADDRESSABLES
        /// <summary>
        /// Note: AssetReference guids are deterministic based on the <see cref="AssetDatabase.AssetPathToGUID"/>.
        /// If the addressable scene generation changes, remember to update the addressable scene guids.
        /// </summary>
        protected static readonly AssetReference[] _sceneAssetReferences = new AssetReference[]
        {
            new AssetReference("baa7036acde30204aac9b51385e12c3f"),
            new AssetReference("466279f958a18964083e418c273a9595"),
            new AssetReference("547f8ae62e408b3419e168a8f326d17a"),
            new AssetReference("bdce3e0b7d1e86447846538f4433e712"),
        };
#endif

        protected static readonly ILoadSceneInfo[][] _multipleLoadSceneInfoList = new ILoadSceneInfo[][]
        {
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[2]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            },
            new ILoadSceneInfo[]
            {
#if UNITY_EDITOR
                new LoadSceneInfoIndex(0),
#endif
                new LoadSceneInfoIndex(1),
                new LoadSceneInfoIndex(2),
#if !UNITY_EDITOR
                new LoadSceneInfoIndex(3),
#endif
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoIndex(1),
                new LoadSceneInfoIndex(1),
                new LoadSceneInfoIndex(1),
            },
#if ENABLE_ADDRESSABLES
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[2]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[3]),
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoAssetReference(_sceneAssetReferences[1]),
                new LoadSceneInfoAssetReference(_sceneAssetReferences[2]),
                new LoadSceneInfoAssetReference(_sceneAssetReferences[3]),
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoAssetReference(_sceneAssetReferences[1]),
                new LoadSceneInfoAssetReference(_sceneAssetReferences[1]),
                new LoadSceneInfoAssetReference(_sceneAssetReferences[1]),
            },
#endif
        };

        protected static readonly ILoadSceneInfo[] _singleLoadSceneInfoList = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoIndex(1),
#if ENABLE_ADDRESSABLES
            new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoAssetReference(new AssetReference("466279f958a18964083e418c273a9595")),
#endif
        };

        protected static readonly ISceneManager[] _sceneManagers = new ISceneManager[]
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
                string guid = AssetDatabase.AssetPathToGUID(p);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.SetAddress(SceneBuilder.SceneNames[i]);

                AssetReference assetReference = new AssetReference(guid);
                sceneReferenceData.sceneReferences.Add(assetReference);
                Debug.Log($"Generated AssetReference for scene '{SceneBuilder.SceneNames[i]}' with guid: '{assetReference.RuntimeKey}'");
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