using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
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

public static class MaterialPipelineConverter
{
    const string _partialPath = "LoadingSceneExamples/Materials";

    public static void ConvertMaterials()
    {
#if CONVERT_MATERIALS
        string fullPath = FindFolderByPartialPath(_partialPath);
        if (string.IsNullOrEmpty(fullPath))
            return;

        List<MaterialUpgrader> upgraders = GetMaterialUpgraders();
        if (upgraders.Count > 0)
        {
            MaterialUpgrader.UpgradeProjectFolder(GetMaterialUpgraders(), fullPath);
            AssetDatabase.Refresh();
        }
#endif
    }

#if CONVERT_MATERIALS
    static List<MaterialUpgrader> GetMaterialUpgraders()
    {
        RenderPipelineAsset renderPipeline = GraphicsSettings.currentRenderPipeline;
        List<MaterialUpgrader> upgraders = new();
#if CONVERT_HDRP
        if (renderPipeline is HDRenderPipelineAsset)
            upgraders.Add(new StandardsToHDLitMaterialUpgrader("Standard", "HDRP/Lit"));
#endif
#if CONVERT_URP
        if (renderPipeline is UniversalRenderPipelineAsset)
            upgraders.Add(new StandardUpgrader("Standard"));
#endif
        return upgraders;
    }
#endif

    static string FindFolderByPartialPath(string partialPath)
    {
        string[] allFolders = AssetDatabase.GetAllAssetPaths();
        foreach (string folder in allFolders)
        {
            if (folder.EndsWith(partialPath))
                return folder;
        }
        return null;
    }
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