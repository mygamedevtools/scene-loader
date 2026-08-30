using System;
using MyGameDevTools.SceneLoading;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A loading screen built from a UI Toolkit document — no scene, no prefab, and no
/// <see cref="LoadingBehavior"/> anywhere. It creates its own <see cref="LoadingProgress"/> and
/// gates the transition on that.
/// </summary>
/// <remarks>
/// This is the case that shows the gating contract is not tied to the component set: everything
/// a loading screen has to do is expressed through <see cref="LoadingProgress"/>, and a plain C#
/// object can hold one.
/// <code>
/// await MySceneManager.TransitionAsync("target", new UIDocumentLoadingScreen(uxml, panelSettings));
/// </code>
/// </remarks>
public class UIDocumentLoadingScreen : LoadingScreen
{
    readonly VisualTreeAsset _visualTree;
    readonly PanelSettings _panelSettings;
    readonly float _fadeSeconds;
    readonly float _minimumSeconds;

    GameObject _instance;
    VisualElement _root;
    Label _value;
    VisualElement _fill;

    /// <param name="minimumSeconds">
    /// How long the screen stays up even when the load finishes sooner. A scene that loads in
    /// two frames would otherwise produce a screen that flashes on and off.
    /// </param>
    public UIDocumentLoadingScreen(VisualTreeAsset visualTree, PanelSettings panelSettings, float fadeSeconds = .25f, float minimumSeconds = 2f)
    {
        _visualTree = visualTree != null ? visualTree : throw new ArgumentNullException(nameof(visualTree));
        _panelSettings = panelSettings != null ? panelSettings : throw new ArgumentNullException(nameof(panelSettings));
        _fadeSeconds = Mathf.Max(0, fadeSeconds);
        _minimumSeconds = Mathf.Max(0, minimumSeconds);
    }

    public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
    {
        _instance = new GameObject(nameof(UIDocumentLoadingScreen));
        // Into the holder scene, so it survives the outgoing scene being unloaded.
        host.Adopt(_instance);

        UIDocument document = _instance.AddComponent<UIDocument>();
        document.panelSettings = _panelSettings;
        document.visualTreeAsset = _visualTree;
        // Above the room's document in the same panel, so the screen covers what it replaces.
        document.sortingOrder = 50;

        _root  = document.rootVisualElement;
        _value = _root?.Q<Label>("value");
        _fill  = _root?.Q<VisualElement>("fill");

        LoadingProgress progress = new();
        progress.Progressed += OnProgressed;
        progress.LoadingCompleted += FadeOut;
        BindProgress(progress);

        // Held before the transition can read the gates, and released when each fade ends.
        progress.HoldShow(this);
        progress.HoldHide(this);

        // Delays the cue rather than the gate, so the screen stays up for its minimum instead
        // of fading out early and leaving the rest of the wait on an empty screen.
        if (_minimumSeconds > 0)
        {
            progress.HoldCompletion(this);
            _root?.schedule.Execute(() => progress.ReleaseCompletion(this))
                  .StartingIn((long)(_minimumSeconds * 1000f));
        }

        Fade(0, 1, () => progress.ReleaseShow(this));

        return SceneOperationPump.Completed(operation);
    }

    public override void Dispose()
    {
        if (Progress != null)
        {
            Progress.Progressed -= OnProgressed;
            Progress.LoadingCompleted -= FadeOut;
        }

        if (_instance != null)
            UnityEngine.Object.Destroy(_instance);

        _instance = null;
        _root = null;
        _value = null;
        _fill = null;

        base.Dispose();
    }

    void OnProgressed(float progress)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100);

        if (_value != null)
            _value.text = $"{percent}%";

        if (_fill != null)
            _fill.style.width = Length.Percent(percent);
    }

    /// <summary>
    /// The cue already waited for the minimum, so this just plays the screen out.
    /// </summary>
    void FadeOut()
    {
        LoadingProgress progress = Progress;
        Fade(1, 0, () => progress.ReleaseHide(this));
    }

    /// <summary>
    /// Drives the root's opacity through UI Toolkit's own scheduler, so the screen needs no
    /// MonoBehaviour of its own to run a coroutine.
    /// </summary>
    void Fade(float from, float to, Action onComplete)
    {
        if (_root == null || _fadeSeconds <= 0)
        {
            onComplete();
            return;
        }

        _root.style.opacity = from;

        // Wall-clock, not accumulated frame deltas. The scheduler fires on its own interval
        // rather than once per frame, so adding Time.unscaledDeltaTime per call under-counts
        // whenever the game runs faster than the interval — at 200fps a 0.25s fade took closer
        // to a second, and the faster the machine the slower it got.
        float startedAt = Time.unscaledTime;

        IVisualElementScheduledItem item = null;
        item = _root.schedule.Execute(() =>
        {
            float t = Mathf.Clamp01((Time.unscaledTime - startedAt) / _fadeSeconds);
            _root.style.opacity = Mathf.Lerp(from, to, t);

            if (t < 1)
                return;

            item.Pause();
            onComplete();
        }).Every(16);
    }
}
