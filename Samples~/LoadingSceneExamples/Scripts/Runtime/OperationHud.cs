using System.Collections.Generic;
using MyGameDevTools.SceneLoading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// The sample's persistent HUD: which scene is active, and what is happening to it.
/// </summary>
/// <remarks>
/// It lives in a scene that is loaded additively at startup and never unloaded, which is the
/// only way it can report a transition from start to finish — the room scene it would
/// otherwise live in is unloaded halfway through. Nothing here uses
/// <c>DontDestroyOnLoad</c>: <see cref="MySceneManager.TransitionAsync"/> only unloads the
/// <b>active</b> scene, so an additively loaded scene simply survives.
/// </remarks>
[RequireComponent(typeof(UIDocument))]
public class OperationHud : MonoBehaviour
{
    // The UI kit's phase chips (MyGamedevToolsUI/Styles/Brand.uss).
    const string PhaseClass = "mgt-phase";
    const string PhaseDoneClass = "mgt-phase--done";
    const string PhaseCurrentClass = "mgt-phase--current";
    const string PhaseCompletedClass = "mgt-phase--completed";
    const string PhaseFailedClass = "mgt-phase--failed";

    /// <summary>
    /// Every phase a transition passes through, in order. Rendered up front and lit as the
    /// operation walks them, so the whole lifecycle is visible rather than just the current step.
    /// </summary>
    static readonly SceneOperationState[] _timeline =
    {
        SceneOperationState.Resolving,
        SceneOperationState.ScreenIn,
        SceneOperationState.Unloading,
        SceneOperationState.Loading,
        SceneOperationState.Activating,
        SceneOperationState.ScreenOut,
        SceneOperationState.Completed,
    };

    readonly List<VisualElement> _chips = new();

    Label _sceneName;
    Label _kind;
    Label _percent;
    Button _cancel;
    VisualElement _dot;
    VisualElement _phases;
    VisualElement _fill;

    SceneOperation _operation;
    ISceneManager _manager;

    /// <summary>
    /// The UI half. <see cref="UIDocument"/> rebuilds its element tree every time it is enabled,
    /// so the queries belong here rather than in <c>Awake</c>.
    /// </summary>
    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        _sceneName = root.Q<Label>("scene-name");
        _kind      = root.Q<Label>("kind");
        _percent   = root.Q<Label>("pct");
        _cancel    = root.Q<Button>("cancel");
        _dot       = root.Q<VisualElement>("dot");
        _phases    = root.Q<VisualElement>("phases");
        _fill      = root.Q<VisualElement>("fill");

        BuildTimeline();
        _cancel.clicked += CancelCurrent;

        ShowIdle();
    }

    /// <summary>
    /// The manager half, and <b>not</b> in <c>OnEnable</c>: the static manager is created by a
    /// <c>[RuntimeInitializeOnLoadMethod]</c> that runs after the first scene has loaded, so
    /// anything touching it from <c>Awake</c> or <c>OnEnable</c> throws when its scene happens to
    /// be the first one.
    /// <br/>
    /// The instance is held rather than reached for again, which is also the more honest shape:
    /// <see cref="ISceneManager"/> is the real API and <see cref="MySceneManager"/> is a
    /// convenience over one instance of it.
    /// </summary>
    void Start()
    {
        _manager = MySceneManager.Default;

        _manager.OperationStarted += OnOperationStarted;
        _manager.ActiveSceneChanged += OnActiveSceneChanged;

        _sceneName.text = _manager.GetActiveScene().name;
    }

    void OnDisable()
    {
        if (_cancel != null)
            _cancel.clicked -= CancelCurrent;

        Detach();
    }

    void OnDestroy()
    {
        if (_manager == null)
            return;

        _manager.OperationStarted -= OnOperationStarted;
        _manager.ActiveSceneChanged -= OnActiveSceneChanged;
        _manager = null;
    }

    /// <summary>
    /// The whole point of the HUD: one subscription, and every operation the sample starts
    /// reports itself here without the code that started it knowing anything about this.
    /// </summary>
    void OnOperationStarted(SceneOperation operation)
    {
        Detach();
        _operation = operation;

        operation.Progressed += OnProgressed;
        operation.StateChanged += OnStateChanged;
        operation.Completed += OnCompleted;

        _kind.text = $"{operation.Kind} operation";
        SetBusy(true);
        _cancel.style.display = DisplayStyle.Flex;

        SetProgress(0);
        OnStateChanged(operation);
    }

    void OnProgressed(float progress) => SetProgress(progress);

    void OnStateChanged(SceneOperation operation)
    {
        int current = System.Array.IndexOf(_timeline, operation.State);

        for (int i = 0; i < _chips.Count; i++)
        {
            VisualElement chip = _chips[i];
            chip.RemoveFromClassList(PhaseDoneClass);
            chip.RemoveFromClassList(PhaseCurrentClass);
            chip.RemoveFromClassList(PhaseCompletedClass);

            if (current < 0 || i > current)
                continue;

            if (i < current)
                chip.AddToClassList(PhaseDoneClass);
            else if (_timeline[i] == SceneOperationState.Completed)
                chip.AddToClassList(PhaseCompletedClass);
            else
                chip.AddToClassList(PhaseCurrentClass);
        }

        // Canceled and Faulted are not points on the timeline — they replace the rest of it.
        if (operation.State is SceneOperationState.Canceled or SceneOperationState.Faulted)
            AppendTerminal(operation.State);
    }

    void OnCompleted(SceneOperation operation)
    {
        SetBusy(false);
        _cancel.style.display = DisplayStyle.None;
        Detach();
    }

    void OnActiveSceneChanged(Scene previous, Scene current)
    {
        // Fires at Activating, one chip away from the phase that caused it.
        _sceneName.text = current.IsValid() ? current.name : "—";
    }

    void CancelCurrent() => _operation?.Cancel();

    void SetProgress(float progress)
    {
        _percent.text = $"{Mathf.RoundToInt(Mathf.Clamp01(progress) * 100)}%";
        _fill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
    }

    void ShowIdle()
    {
        _kind.text = "No operation running";
        _percent.text = string.Empty;
        _cancel.style.display = DisplayStyle.None;
        SetBusy(false);
    }

    /// <summary>
    /// The dot and the operation label read the same state: amber and bold while something runs,
    /// green and muted when the scene is settled. The text says it too, so the state never
    /// depends on color alone.
    /// </summary>
    void SetBusy(bool busy)
    {
        _dot.EnableInClassList("mgt-dot--warning", busy);
        _dot.EnableInClassList("mgt-dot--success", !busy);
        _kind.EnableInClassList("mgt-text--strong", busy);
        _kind.EnableInClassList("mgt-text--muted", !busy);
    }

    void BuildTimeline()
    {
        _phases.Clear();
        _chips.Clear();

        foreach (SceneOperationState state in _timeline)
        {
            Label chip = new(state.ToString());
            chip.AddToClassList(PhaseClass);
            _phases.Add(chip);
            _chips.Add(chip);
        }
    }

    void AppendTerminal(SceneOperationState state)
    {
        Label chip = new(state.ToString());
        chip.AddToClassList(PhaseClass);
        chip.AddToClassList(PhaseFailedClass);
        _phases.Add(chip);
        _chips.Add(chip);
    }

    void Detach()
    {
        if (_operation == null)
            return;

        _operation.Progressed -= OnProgressed;
        _operation.StateChanged -= OnStateChanged;
        _operation.Completed -= OnCompleted;
        _operation = null;
    }
}
