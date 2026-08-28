using System;
using System.Collections.Generic;
using MyGameDevTools.SceneLoading;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The examples list in SceneA and SceneB. Every entry is one line of the package's API, shown
/// next to the line that runs it.
/// </summary>
/// <remarks>
/// Nothing here reports progress or watches phases — the persistent HUD does that for every
/// operation the sample starts, through <see cref="MySceneManager.OperationStarted"/>. That is
/// the arrangement worth copying: the code that starts a transition does not have to know
/// anything about the code that displays it.
/// </remarks>
[RequireComponent(typeof(UIDocument))]
public class TransitionExamples : MonoBehaviour
{
    /// <summary>One row in the list: what it is, what it teaches, and what it runs.</summary>
    readonly struct Example
    {
        public readonly string Name;
        public readonly string Tag;
        public readonly string Description;
        public readonly string Code;
        public readonly Action Run;

        public Example(string name, string tag, string description, string code, Action run)
        {
            Name = name;
            Tag = tag;
            Description = description;
            Code = code;
            Run = run;
        }
    }

    [Header("Scenes")]
    [Tooltip("The room this scene transitions to. Resolves from the Build Settings or from Addressables.")]
    [SerializeField]
    string _targetScene;
    [SerializeField]
    string _loadingScene = "Loading_Screen";
    [SerializeField]
    string _animatedLoadingScene = "Loading_Animated";
    [Tooltip("Loaded alongside the target by the multi-scene example.")]
    [SerializeField]
    string _additiveScene = "Extra";
    [Tooltip("Loaded on demand, and never unloaded, so it can report every transition.")]
    [SerializeField]
    string _hudScene = "SceneListenerHUD";

    [Header("Loading screens that are not scenes")]
    [Tooltip("uGUI. The same screen as the loading scene, delivered as a prefab instead.")]
    [SerializeField]
    GameObject _loadingScreenPrefab;
    [SerializeField]
    VisualTreeAsset _loadingScreenDocument;
    [SerializeField]
    PanelSettings _loadingScreenPanelSettings;

    readonly List<VisualElement> _rows = new();

    VisualElement _room;
    VisualElement _setup;
    Label _setupTitle;
    Label _setupBody;
    Button _setupPrimary;
    Button _setupSecondary;

    /// <summary>
    /// Whatever this room needs to be runnable, done from the room itself so there is no second
    /// object to go missing.
    /// <br/>
    /// <b>In <c>Start</c>, not <c>Awake</c> or <c>OnEnable</c>.</b> The static manager is created
    /// by a <c>[RuntimeInitializeOnLoadMethod]</c> that runs after the first scene has finished
    /// loading — after every <c>Awake</c> and <c>OnEnable</c> in it. Touching
    /// <see cref="MySceneManager"/> any earlier throws.
    /// </summary>
    void Start()
    {
        if (!SampleSceneSetup.AreScenesRegistered())
        {
            ShowSetupGate();
            return;
        }

        SampleSceneSetup.EnsureHudLoaded(_hudScene);
    }

    /// <summary>
    /// The sample cannot reach its scenes by name yet. Ask, rather than editing the Build
    /// Settings on the user's behalf — they are project-wide state.
    /// </summary>
    void ShowSetupGate()
    {
        _setup.style.display = DisplayStyle.Flex;
        _room.style.display = DisplayStyle.None;

        _setupTitle.text = "The sample needs its scenes";
        _setupBody.text =
            $"{SampleSceneSetup.RequiredScenes.Length} scenes have to be in the Build Settings before the sample can " +
            "load them by name. Adding them changes a project-wide setting, so nothing is written until you say so.";

        _setupPrimary.text = "Add them, and exit Play Mode";
        _setupSecondary.text = "Leave without changing anything";

        _setupPrimary.clicked += AddScenes;
        _setupSecondary.clicked += SampleSceneSetup.ExitPlayMode;
    }

    /// <summary>
    /// The second state: the scenes are registered, but the Build Settings are read when Play
    /// Mode starts, so this session still cannot see them.
    /// </summary>
    void AddScenes()
    {
        int added = SampleSceneSetup.RegisterScenes();

        _setupTitle.text = added > 0 ? "Scenes added" : "Could not find the scenes";
        _setupBody.text = added > 0
            ? $"{added} scene(s) are now in the Build Settings. They are read when Play Mode starts, so this session " +
              "still cannot reach them — enter Play Mode again and the sample will run."
            : "The scenes are missing from the project. Re-import the sample from the Package Manager.";

        _setupPrimary.text = "Exit Play Mode";
        _setupPrimary.clicked -= AddScenes;
        _setupPrimary.clicked += SampleSceneSetup.ExitPlayMode;

        _setupSecondary.style.display = DisplayStyle.None;
    }

    void RemoveScenes()
    {
        SampleSceneSetup.RemoveScenes();
        SampleSceneSetup.ExitPlayMode();
    }

    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        _room           = root.Q<VisualElement>("room");
        _setup          = root.Q<VisualElement>("setup");
        _setupTitle     = root.Q<Label>("setup-title");
        _setupBody      = root.Q<Label>("setup-body");
        _setupPrimary   = root.Q<Button>("setup-primary");
        _setupSecondary = root.Q<Button>("setup-secondary");

        // Hidden until something asks for it; Start decides.
        _setup.style.display = DisplayStyle.None;
        root.Q<Button>("remove-scenes").clicked += RemoveScenes;

        ScrollView list = root.Q<ScrollView>("list");
        Label count = root.Q<Label>("count");

        Example[] examples = BuildExamples();

        list.Clear();
        _rows.Clear();

        for (int i = 0; i < examples.Length; i++)
            list.Add(BuildRow(examples[i], i));

        count.text = $"{examples.Length} examples";
    }

    Example[] BuildExamples()
    {
        return new[]
        {
            new Example(
                "Direct", "NONE",
                "Straight swap, no loading screen.",
                $"TransitionAsync(\"{_targetScene}\")",
                () => MySceneManager.TransitionAsync(_targetScene)),

            new Example(
                "Loading scene", "SCENE",
                "A scene with a LoadingBehavior and a LoadingFader, built with uGUI.",
                $"TransitionAsync(\"{_targetScene}\", \"{_loadingScene}\")",
                () => MySceneManager.TransitionAsync(_targetScene, _loadingScene)),

            new Example(
                "Prefab screen", "PREFAB",
                "The same screen as a prefab: no extra scene, no Build Settings entry.",
                "TransitionAsync(target, new PrefabLoadingScreen(prefab))",
                () => MySceneManager.TransitionAsync(_targetScene, new PrefabLoadingScreen(_loadingScreenPrefab))),

            new Example(
                "UI Toolkit screen", "UITK",
                "A UXML document that owns its LoadingProgress, with no LoadingBehavior anywhere.",
                "TransitionAsync(target, new UIDocumentLoadingScreen(uxml, panel))",
                () => MySceneManager.TransitionAsync(
                    _targetScene,
                    new UIDocumentLoadingScreen(_loadingScreenDocument, _loadingScreenPanelSettings))),

            new Example(
                "Animated screen", "SCENE",
                "A UI Toolkit loading scene whose gates are held until each slide finishes.",
                $"TransitionAsync(\"{_targetScene}\", \"{_animatedLoadingScene}\")",
                () => MySceneManager.TransitionAsync(_targetScene, _animatedLoadingScene)),

            new Example(
                "Reload this scene", "HANDLE",
                "Reloads whatever is active, so the same button works in both rooms.",
                "ReloadActiveSceneAsync(loadingScreen)",
                () => MySceneManager.ReloadActiveSceneAsync(_loadingScene)),

            new Example(
                "Two scenes at once", "HANDLE",
                "One operation, two scenes, the first of them made active.",
                "TransitionAsync(new[] { target, extra }, loadingScreen)",
                TransitionToBoth),

            new Example(
                "Await the handle", "HANDLE",
                "The operation is awaitable; the result carries the scenes it produced.",
                "SceneResult result = await TransitionAsync(target, loadingScreen)",
                AwaitTransition),
        };
    }

    /// <summary>
    /// Two scenes in one operation. The array converts to <see cref="SceneParameters"/> on its
    /// own, and a transition always activates something — the first scene, unless the parameters
    /// name another.
    /// </summary>
    void TransitionToBoth()
    {
        SceneParameters parameters = new[] { _targetScene, _additiveScene };
        MySceneManager.TransitionAsync(parameters, _loadingScene);
    }

    /// <summary>
    /// <c>async void</c> because a UI callback has nothing to await it — the same shape a button
    /// handler in your own project would have. The operation is cancelled if this object goes
    /// away mid-flight, which is what <see cref="SceneOperation.CancelWith"/> is for.
    /// </summary>
    async void AwaitTransition()
    {
        SceneResult result = await MySceneManager
            .TransitionAsync(_targetScene, _loadingScene)
            .CancelWith(destroyCancellationToken);

        Debug.Log($"Transition finished with {result.GetScenes().Length} scene(s) loaded.");
    }

    VisualElement BuildRow(Example example, int index)
    {
        VisualElement row = new();
        row.AddToClassList("example");

        VisualElement head = new();
        head.AddToClassList("example__head");

        Label name = new(example.Name);
        name.AddToClassList("example__name");

        Label tag = new(example.Tag);
        tag.AddToClassList("example__tag");
        tag.AddToClassList($"example__tag--{example.Tag.ToLowerInvariant()}");

        head.Add(name);
        head.Add(tag);

        Label description = new(example.Description);
        description.AddToClassList("example__desc");

        Label code = new(example.Code);
        code.AddToClassList("example__code");

        row.Add(head);
        row.Add(description);
        row.Add(code);

        row.RegisterCallback<ClickEvent>(_ =>
        {
            Highlight(index);
            example.Run();
        });

        _rows.Add(row);
        return row;
    }

    void Highlight(int index)
    {
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].EnableInClassList("example--active", i == index);
    }
}
