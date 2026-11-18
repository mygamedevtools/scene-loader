using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
#if CONVERT_MATERIALS
using UnityEditor.Rendering;
using UnityEngine.Rendering;
#if CONVERT_URP
using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering.Universal;
#endif
#if CONVERT_HDRP
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition;
#endif
#endif

/// <summary>
/// Offers to upgrade the sample's Built-in materials to the project's render pipeline.
/// </summary>
/// <remarks>
/// It checks after every domain reload and again whenever the sample's materials are imported.
/// The second hook is the one that matters on a fresh import: Unity compiles the sample's scripts
/// and reloads before it imports the materials, so the reload check alone finds nothing to upgrade.
/// </remarks>
[InitializeOnLoad]
public static class MaterialPipelineConverter
{
    const string IgnoredKey = "MyGameDevTools.SceneLoading.Samples.MaterialUpgradeIgnored";

    static bool _scheduled;
    static string _materialsFolder;

    static MaterialPipelineConverter()
    {
        Schedule();
    }

    /// <summary>Runs <see cref="ConvertMaterials"/> once, after the current import or reload settles.</summary>
    internal static void Schedule()
    {
        if (_scheduled)
            return;

        _scheduled = true;
        EditorApplication.delayCall += () =>
        {
            _scheduled = false;
            ConvertMaterials();
        };
    }

    /// <summary>
    /// The sample's Materials folder, found from this script (<c>&lt;sample&gt;/Scripts/Editor</c>)
    /// rather than by name: the Package Manager names an imported sample's folder after its
    /// display name and version. Null while the sample is read from inside the package, where
    /// upgrading would rewrite the package's own files.
    /// </summary>
    internal static string MaterialsFolder()
    {
        // Cached until the next domain reload: the postprocessor asks on every import.
        if (!string.IsNullOrEmpty(_materialsFolder))
            return _materialsFolder;

        foreach (string guid in AssetDatabase.FindAssets($"{nameof(MaterialPipelineConverter)} t:MonoScript"))
        {
            string script = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(script) != nameof(MaterialPipelineConverter))
                continue;
            if (!script.StartsWith("Assets/"))
                continue;

            string sample = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(script)));
            return _materialsFolder = System.IO.Path.Combine(sample ?? string.Empty, "Materials").Replace('\\', '/');
        }

        return null;
    }

    public static void ConvertMaterials()
    {
#if CONVERT_MATERIALS
        RenderPipelineAsset renderPipeline = GraphicsSettings.currentRenderPipeline;
        if (!renderPipeline)
            return;

        // Asked once per session: "Ignore" means not now, not on every reload.
        if (SessionState.GetBool(IgnoredKey, false))
            return;

        string folder = MaterialsFolder();
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            return;

        string[] materialPaths = AssetDatabase.FindAssets("t:Material", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        if (materialPaths.Length == 0)
            return;

        Material[] materials = materialPaths
            .Select(path => AssetDatabase.LoadAssetAtPath<Material>(path))
            .Where(material => material != null && material.shader.name == "Standard")
            .ToArray();

        if (materials.Length == 0)
            return;

        List<MaterialUpgrader> upgraders = GetMaterialUpgraders(renderPipeline);
        if (upgraders.Count == 0)
            return;

        if (EditorUtility.DisplayDialog("Sample Material Upgrader", "The sample materials have to be upgraded to match your render pipeline. Would you like to upgrade them now?", "Upgrade", "Ignore"))
        {
            foreach (Material material in materials)
            {
                MaterialUpgrader.Upgrade(material, upgraders, MaterialUpgrader.UpgradeFlags.None);
            }
            AssetDatabase.Refresh();
        }
        else
        {
            SessionState.SetBool(IgnoredKey, true);
        }
#endif
    }

#if CONVERT_MATERIALS
    static List<MaterialUpgrader> GetMaterialUpgraders(RenderPipelineAsset renderPipeline)
    {
        List<MaterialUpgrader> upgraders = new();
#if CONVERT_URP
        if (renderPipeline is UniversalRenderPipelineAsset)
            upgraders.Add(new StandardUpgrader("Standard"));
#endif
#if CONVERT_HDRP
        if (renderPipeline is HDRenderPipelineAsset)
            upgraders.Add(new StandardsToHDLitMaterialUpgrader("Standard", "HDRP/Lit"));
#endif
        return upgraders;
    }
#endif
}

/// <summary>
/// Schedules the upgrade check when the sample's materials are imported, which on a fresh import
/// happens after the domain reload that runs the converter's static constructor.
/// </summary>
class MaterialPipelineConverterPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        string folder = MaterialPipelineConverter.MaterialsFolder();
        if (string.IsNullOrEmpty(folder))
            return;

        string prefix = folder + "/";
        if (importedAssets.Any(path => path.StartsWith(prefix) && path.EndsWith(".mat")))
            MaterialPipelineConverter.Schedule();
    }
}

#if CONVERT_MATERIALS
#if CONVERT_HDRP
// Modified original from `Packages/com.unity.render-pipelines.high-definition/Editor/Material/Lit/StandardsToHDLitMaterialUpgrader.cs`
internal class StandardsToHDLitMaterialUpgrader : MaterialUpgrader
{
    internal StandardsToHDLitMaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer = null)
    {
        RenameShader(sourceShaderName, destShaderName, finalizer);

        RenameTexture("_MainTex", "_BaseColorMap");
        RenameColor("_Color", "_BaseColor");
        RenameFloat("_Glossiness", "_Smoothness");
        RenameTexture("_BumpMap", "_NormalMap");
        RenameFloat("_BumpScale", "_NormalScale");
        RenameTexture("_ParallaxMap", "_HeightMap");
        RenameTexture("_EmissionMap", "_EmissiveColorMap");
        RenameTexture("_DetailAlbedoMap", "_DetailMap");
        RenameFloat("_UVSec", "_UVDetail");
        SetFloat("_LinkDetailsWithBase", 0);
        RenameFloat("_DetailNormalMapScale", "_DetailNormalScale");
        RenameFloat("_Cutoff", "_AlphaCutoff");
        RenameKeywordToFloat("_ALPHATEST_ON", "_AlphaCutoffEnable", 1f, 0f);

        SetFloat("_MaterialID", 1f);
    }

    public override void Convert(Material srcMaterial, Material dstMaterial)
    {
        dstMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;

        base.Convert(srcMaterial, dstMaterial);

        float metallicValue = Mathf.Pow(srcMaterial.GetFloat("_Metallic"), 2.2f);
        dstMaterial.SetFloat("_Metallic", metallicValue);
        dstMaterial.SetFloat("_AORemapMin", 1f - srcMaterial.GetFloat("_OcclusionStrength"));
        dstMaterial.SetFloat("_SmoothnessRemapMax", srcMaterial.GetFloat("_Glossiness"));
        dstMaterial.SetFloat("_SurfaceType", 0);
        dstMaterial.SetFloat("_BlendMode", 0);
        dstMaterial.SetFloat("_AlphaCutoffEnable", 0);
        dstMaterial.SetFloat("_EnableBlendModePreserveSpecularLighting", 1);

        Color hdrEmission = srcMaterial.GetColor("_EmissionColor");

        if (!srcMaterial.IsKeywordEnabled("_EMISSION"))
            hdrEmission = Color.black;

        dstMaterial.SetColor("_EmissiveColor", hdrEmission);

        HDShaderUtils.ResetMaterialKeywords(dstMaterial);
    }
}
#endif
#endif