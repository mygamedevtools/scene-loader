using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PackageExporter
{
    public static void ExportPackage()
    {
        string packageName = "com.mygamedevtools.scene-loader";
        string exportPath = Path.Combine(Application.dataPath, packageName + ".unitypackage");

        AssetDatabase.ExportPackage("Packages/" + packageName, exportPath, ExportPackageOptions.Recurse);
        Console.WriteLine($"Exported package to: \"{exportPath}\"");
    }
}
