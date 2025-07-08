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

[InitializeOnLoad]
public static class MaterialPipelineConverter
{
    const string _partialPath = "Loading Scene Examples/Materials";

    static MaterialPipelineConverter()
    {
        EditorApplication.delayCall += ConvertMaterials;
    }

    public static void ConvertMaterials()
    {
        RenderPipelineAsset renderPipeline = GraphicsSettings.currentRenderPipeline;
        if (!renderPipeline)
            return;

#if CONVERT_MATERIALS
        string[] materialPaths = AssetDatabase.FindAssets("t:Material")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.Contains(_partialPath))
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
        if (upgraders.Count > 0 && EditorUtility.DisplayDialog("Sample Material Upgrader", "The sample materials have to be upgraded to match your render pipeline. Would you like to upgrade them now?", "Upgrade", "Ignore"))
        {
            foreach (Material material in materials)
            {
                MaterialUpgrader.Upgrade(material, upgraders, MaterialUpgrader.UpgradeFlags.None);
            }
            AssetDatabase.Refresh();
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