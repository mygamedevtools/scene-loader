using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class PackageExporter
{
    const string PackageName = "com.mygamedevtools.scene-loader";
    const string AssetStoreToolsAssembly = "asset-store-tools-editor";

    /// <summary>
    /// Exports the package as a .unitypackage, including hidden folders such as Samples~.
    /// </summary>
    /// <remarks>
    /// Asset Store Tools' exporter walks the file system directly and scrapes GUIDs from
    /// .meta files, so it picks up hidden folders that are not in the Asset Database. That
    /// is what com.needle.upm-in-unitypackage used to patch in, and why it is no longer needed.
    ///
    /// Those exporter types are `internal` and this project vendors Asset Store Tools
    /// unmodified, so they are driven through reflection rather than by adding an
    /// InternalsVisibleTo that the next package upgrade would overwrite.
    /// </remarks>
    public static void ExportPackage()
    {
        string packagePath = $"Packages/{PackageName}";
        string outputPath = $"{PackageName}.unitypackage";

        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == AssetStoreToolsAssembly);
        if (assembly == null)
            throw new InvalidOperationException($"Could not find the '{AssetStoreToolsAssembly}' assembly. Is com.unity.asset-store-tools still in the project?");

        Type settingsType = GetType(assembly, "AssetStoreTools.Exporter.DefaultExporterSettings");
        Type exporterType = GetType(assembly, "AssetStoreTools.Exporter.DefaultPackageExporter");

        object settings = Activator.CreateInstance(settingsType, true);
        SetField(settingsType, settings, "ExportPaths", new[] { packagePath });
        SetField(settingsType, settings, "OutputFilename", outputPath);

        object exporter = Activator.CreateInstance(exporterType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { settings }, null);

        MethodInfo exportMethod = exporterType.GetMethod("Export", BindingFlags.Instance | BindingFlags.Public);
        if (exportMethod == null)
            throw new MissingMethodException(exporterType.FullName, "Export");

        // No preview generator is set, so nothing yields and the task completes synchronously.
        var task = (Task)exportMethod.Invoke(exporter, null);
        task.GetAwaiter().GetResult();

        object result = task.GetType().GetProperty("Result").GetValue(task);
        Type resultType = result.GetType();

        if (!(bool)resultType.GetField("Success").GetValue(result))
        {
            var exception = (Exception)resultType.GetField("Exception").GetValue(result);
            throw new InvalidOperationException($"Failed to export '{PackageName}'.", exception);
        }

        Debug.Log($"Exported '{resultType.GetField("ExportedPath").GetValue(result)}'.");
    }

    static Type GetType(Assembly assembly, string typeName)
    {
        Type type = assembly.GetType(typeName);
        if (type == null)
            throw new TypeLoadException($"Could not find '{typeName}' in '{assembly.GetName().Name}'. The Asset Store Tools exporter API may have changed.");
        return type;
    }

    static void SetField(Type type, object instance, string fieldName, object value)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
            throw new MissingFieldException(type.FullName, fieldName);
        field.SetValue(instance, value);
    }
}
