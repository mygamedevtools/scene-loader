/**
 * SampleAuthoring.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using UnityEditor;
using UnityEngine;
using System.IO;

namespace MyUnityTools.SceneLoading.Authoring
{
    public static class SampleAuthoring
    {
        static readonly string _packagesPath = Path.Join(Application.dataPath, "../Packages/com.myunitytools.sceneloader/Samples~");
        static readonly string _assetsPath = Path.Combine(Application.dataPath, "Samples");

        [MenuItem("Authoring/Move to Package")]
        public static void MoveSamplesToPackage()
        {
            Directory.Move(_assetsPath, _packagesPath);
            File.Delete(_assetsPath + ".meta");
            AssetDatabase.Refresh();
        }

        [MenuItem("Authoring/Move to Assets")]
        public static void MoveSamplesToAssets()
        {
            Directory.Move(_packagesPath, _assetsPath);
            AssetDatabase.Refresh();
        }

        [MenuItem("Authoring/Move to Package", true)]
        public static bool MoveSamplesToPackageValidate() => Directory.Exists(_assetsPath);

        [MenuItem("Authoring/Move to Assets", true)]
        public static bool MoveSamplesToAssetsValidate() => Directory.Exists(_packagesPath);
    }
}