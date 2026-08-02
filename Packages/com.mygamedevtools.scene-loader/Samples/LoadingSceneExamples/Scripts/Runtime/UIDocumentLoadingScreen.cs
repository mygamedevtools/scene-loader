using MyGameDevTools.SceneLoading;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A loading screen built from a UI Toolkit document, with no scene and no prefab.
/// <br/><br/>
/// Another reference implementation — copy it and change the element names, the fade, or the
/// gating to suit your project.
/// <code>
/// await MySceneManager.TransitionAsync("target", new UIDocumentLoadingScreen(uxml, panelSettings));
/// </code>
/// </summary>
public class UIDocumentLoadingScreen : LoadingScreen
{
    /// <summary>
    /// The element name this screen drives with progress. Any
    /// <see cref="UnityEngine.UIElements.ProgressBar"/> or <see cref="Slider"/> by this name is
    /// bound automatically.
    /// </summary>
    public const string ProgressElementName = "progress";

    readonly VisualTreeAsset _visualTree;
    readonly PanelSettings _panelSettings;

    GameObject _instance;
    ProgressBar _progressBar;

    public UIDocumentLoadingScreen(VisualTreeAsset visualTree, PanelSettings panelSettings)
    {
        _visualTree = visualTree != null ? visualTree : throw new System.ArgumentNullException(nameof(visualTree));
        _panelSettings = panelSettings != null ? panelSettings : throw new System.ArgumentNullException(nameof(panelSettings));
    }

    public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
    {
        _instance = new GameObject(nameof(UIDocumentLoadingScreen));
        // Into the holder scene, so it survives the outgoing scene being unloaded.
        host.Adopt(_instance);

        UIDocument document = _instance.AddComponent<UIDocument>();
        document.panelSettings = _panelSettings;
        document.visualTreeAsset = _visualTree;

        _progressBar = document.rootVisualElement?.Q<ProgressBar>(ProgressElementName);

        return SceneOperationPump.Completed(operation);
    }

    /// <summary>
    /// Shows immediately. Give this a fade by returning a gate that opens when the animation
    /// finishes — <see cref="LoadingProgress.WaitForShowAsync"/> is what the scene-based screen
    /// uses for exactly that.
    /// </summary>
    public override SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation) => SceneOperationPump.Completed(operation);

    public override void ReportProgress(float progress)
    {
        if (_progressBar != null)
            _progressBar.value = Mathf.Clamp01(progress) * 100f;
    }

    public override SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation) => SceneOperationPump.Completed(operation);

    public override void Dispose()
    {
        if (_instance != null)
            Object.Destroy(_instance);

        _instance = null;
        _progressBar = null;
    }
}
