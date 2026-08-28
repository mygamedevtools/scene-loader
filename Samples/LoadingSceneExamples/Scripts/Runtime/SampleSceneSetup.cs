using MyGameDevTools.SceneLoading;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
#endif

/// <summary>
/// The sample needs its scenes in the Build Settings to reach them by name. This is how it asks,
/// and how it puts them back.
/// </summary>
/// <remarks>
/// <b>Nothing here runs on its own.</b> The Build Settings are project-wide state that the rest
/// of a project — its own scenes, its own tests — depends on, so a sample has no business
/// editing them because you happened to press Play. Every method here is called from a button.
/// </remarks>
public static class SampleSceneSetup
{
    /// <summary>Every scene the sample needs to reach by name.</summary>
    public static readonly string[] RequiredScenes =
    {
        "SceneA",
        "SceneB",
        "SceneListenerHUD",
        "Extra",
        "Loading_Screen",
        "Loading_Animated",
    };

    /// <summary>Whether the sample can run: all of its scenes are registered.</summary>
    public static bool AreScenesRegistered()
    {
#if UNITY_EDITOR
        return Missing().Length == 0;
#else
        // A built player can only contain what was registered when it was built.
        return true;
#endif
    }

    /// <summary>
    /// Loads the HUD if it is not already there, so it does not matter which room you started in.
    /// </summary>
    public static void EnsureHudLoaded(string hudScene)
    {
        // Walk the scenes actually in play rather than asking GetSceneByName, which reports a
        // valid Scene for one the Build Settings merely know about. sceneCount covers loaded and
        // still-loading alike, so this cannot start a second load for one already in flight.
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == hudScene)
                return;

        MySceneManager.LoadAsync(hudScene);
    }

#if UNITY_EDITOR
    /// <summary>Adds whatever is missing. Returns how many were added.</summary>
    public static int RegisterScenes()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        int added = 0;

        foreach (string sceneName in Missing())
        {
            string path = FindScenePath(sceneName);
            if (string.IsNullOrEmpty(path))
                continue;

            scenes.Add(new EditorBuildSettingsScene(path, true));
            added++;
        }

        if (added > 0)
            EditorBuildSettings.scenes = scenes.ToArray();

        return added;
    }

    /// <summary>Takes them back out, leaving everything else alone. Returns how many were removed.</summary>
    public static int RemoveScenes()
    {
        EditorBuildSettingsScene[] before = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] after = before
            .Where(scene => !RequiredScenes.Contains(Path.GetFileNameWithoutExtension(scene.path)))
            .ToArray();

        EditorBuildSettings.scenes = after;
        return before.Length - after.Length;
    }

    /// <summary>
    /// The Build Settings are read when Play Mode starts, so any change to them only takes effect
    /// on the next one. Every button that changes them ends the session for that reason.
    /// </summary>
    public static void ExitPlayMode() => EditorApplication.isPlaying = false;

    static string[] Missing() => RequiredScenes.Where(scene => !IsRegistered(scene)).ToArray();

    static bool IsRegistered(string sceneName)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (Path.GetFileNameWithoutExtension(scene.path) == sceneName)
                return true;

        return false;
    }

    /// <summary>
    /// Finds the scene wherever the sample ended up: read from the package during development,
    /// or copied under <c>Assets/Samples</c> once imported.
    /// </summary>
    static string FindScenePath(string sceneName)
    {
        return AssetDatabase.FindAssets($"{sceneName} t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path) == sceneName &&
                path.Contains("LoadingSceneExamples"));
    }
#else
    public static int RegisterScenes() => 0;
    public static int RemoveScenes() => 0;
    public static void ExitPlayMode() => Application.Quit();
#endif
}
