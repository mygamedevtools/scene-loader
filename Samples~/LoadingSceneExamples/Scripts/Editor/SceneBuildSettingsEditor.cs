using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

[InitializeOnLoad]
public static class SceneBuildSettingsEditor
{
    const string _sampleName = "Loading Scene Examples";
    const string _sampleSlug = "asm_lse";
    const string _dontShowAgainKey = _sampleSlug + "::dontShowAddScenesPrompt";

    static readonly string[] _requiredSceneNames =
    {
        "SceneA",
        "SceneB",
        "Loading_Fade",
        "Loading_Animated",
        "Loading_Custom"
    };

    static SceneBuildSettingsEditor()
    {
        EditorApplication.delayCall += CheckScenesInBuildSettings;
    }

    static void CheckScenesInBuildSettings()
    {
        if (EditorPrefs.GetBool(_dontShowAgainKey, false))
            return;

        string[] buildScenes = EditorBuildSettings.scenes
            .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
            .ToArray();

        string[] allScenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        string[] missingScenes = _requiredSceneNames
            .Where(name => !buildScenes.Contains(name))
            .Select(name => allScenePaths.FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == name))
            .Where(path => !string.IsNullOrEmpty(path) && !path.Contains("Packages/com.mygamedevtools.scene-loader"))
            .ToArray();

        if (missingScenes.Length > 0)
        {
            bool addScenes = EditorUtility.DisplayDialogComplex(
                _sampleName,
                "To run the " + _sampleName + ", you need to add the sample scenes to the Build Settings. Would you like to add them now?\nYou can remove them later in the menu: \"Tools/My Scene Manager/Remove '" + _sampleName + "' from Build Settings\".",
                "Add Scenes",
                "Ignore",
                "Don't Show Again"
            ) switch
            {
                0 => true,
                2 => SetDontShowAgain(),
                _ => false
            };

            if (addScenes)
            {
                AddScenesToBuildSettings(missingScenes);
            }
        }
    }

    static bool SetDontShowAgain()
    {
        EditorPrefs.SetBool(_dontShowAgainKey, true);
        return false;
    }

    static void AddScenesToBuildSettings(string[] scenes)
    {
        List<EditorBuildSettingsScene> currentScenes = EditorBuildSettings.scenes.ToList();

        foreach (string scene in scenes)
        {
            currentScenes.Add(new EditorBuildSettingsScene(scene, true));
        }

        EditorBuildSettings.scenes = currentScenes.ToArray();
    }

    [MenuItem("Tools/My Scene Manager/Remove '" + _sampleName + "' from Build Settings")]
    static void RemoveSampleScenesFromBuildSettings()
    {
        List<EditorBuildSettingsScene> currentScenes = EditorBuildSettings.scenes.ToList();
        currentScenes.RemoveAll(scene => _requiredSceneNames.Contains(Path.GetFileNameWithoutExtension(scene.path)));

        EditorBuildSettings.scenes = currentScenes.ToArray();
    }

    [MenuItem("Tools/My Scene Manager/Reset '" + _sampleName + "' Add Scenes Prompt")]
    private static void ResetDontShowAgain()
    {
        EditorPrefs.DeleteKey(_dontShowAgainKey);
    }
}
