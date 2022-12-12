#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneTestEnvironment.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-12
 */

#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor;
using System.Linq;
#endif
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class AddressableSceneTestEnvironment : IPrebuildSetup, IPostBuildCleanup
    {
#if UNITY_EDITOR
        const string _scenePathBase = "Assets/_addressables-test";
#endif

        public void Setup()
        {
#if UNITY_EDITOR
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            SceneBuilder.BuildScenes(_scenePathBase, (i, s, p) =>
            {
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(p), settings.DefaultGroup);
                entry.SetAddress(SceneBuilder.SceneNames[i]);
            });
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var scenePaths = EditorBuildSettings.scenes.Where(scene => scene.path.StartsWith(_scenePathBase)).Select(scene => scene.path);
            foreach (var path in scenePaths)
                settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(path), false);

            AssetDatabase.DeleteAsset(_scenePathBase);
            AssetDatabase.Refresh();
#endif
        }

        [UnityTest]
        public IEnumerator EnvironmentSetup_Test()
        {
            var operation = Addressables.LoadResourceLocationsAsync(SceneBuilder.SceneNames);
            yield return operation;

            Assert.True(areLocationsValid());

            bool areLocationsValid()
            {
                foreach (var location in operation.Result)
                    if (location == null || string.IsNullOrEmpty(location.PrimaryKey))
                        return false;
                return true;
            }
        }
    }
}
#endif