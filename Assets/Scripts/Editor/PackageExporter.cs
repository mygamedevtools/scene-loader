using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PackageExporter
{
    public static void ExportPackage()
    {
        string packageName = "com.mygamedevtools.scene-loader";

        string rootGuid = AssetDatabase.AssetPathToGUID("Packages/" + packageName);

        string[] collection = Array.Empty<string>();
        collection = AssetDatabase.CollectAllChildren(rootGuid, collection);

        PackageUtility.ExportPackage(collection, Path.Combine(Application.dataPath, packageName + ".unitypackage"));
        Console.WriteLine($"Exported package to: \"{Application.dataPath}/{packageName}.unitypackage\"");
    }
}
