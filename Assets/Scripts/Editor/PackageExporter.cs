using System.Linq;
using Needle.HybridPackages;
using UnityEditor;

public static class PackageExporter
{
    public static void ExportPackage()
    {
        string packageName = "com.mygamedevtools.scene-loader";
        string[] guids = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Packages/" + packageName + "/")).Select(p => AssetDatabase.AssetPathToGUID(p)).ToArray();
        AssetStoreToolsPatchProvider.PackagerExportPatch.ExportPackage(guids, packageName + ".unitypackage");
    }
}
