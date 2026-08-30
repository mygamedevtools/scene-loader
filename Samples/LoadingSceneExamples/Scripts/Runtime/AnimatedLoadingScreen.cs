using MyGameDevTools.SceneLoading;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the animated loading scene: two panels slide in to meet, and slide back out once
/// loading finishes.
/// </summary>
/// <remarks>
/// A loading screen that is a <b>scene</b> does not have to be uGUI — this one is UI Toolkit,
/// and the transition neither knows nor cares. What it does care about is the gates: both are
/// held from <c>Awake</c> through <see cref="LoadingScreenComponent"/>, and each is released
/// only when its slide has finished, so the outgoing scene is never unloaded behind a curtain
/// that is still opening.
/// </remarks>
[RequireComponent(typeof(UIDocument))]
public class AnimatedLoadingScreen : LoadingScreenComponent
{
    const string ClosedClass = "animated__panel--closed";
    const string VisibleClass = "animated__content--visible";

    [Tooltip("Must match the transition-duration on .animated__panel in the stylesheet.")]
    [SerializeField]
    float _slideSeconds = .42f;

    VisualElement _left;
    VisualElement _right;
    VisualElement _content;
    VisualElement _mark;
    VisualElement _fill;
    Label _value;

    float _spin;

    protected override void Awake()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        _left    = root.Q<VisualElement>("panel-left");
        _right   = root.Q<VisualElement>("panel-right");
        _content = root.Q<VisualElement>("content");
        _mark    = root.Q<VisualElement>("mark");
        _fill    = root.Q<VisualElement>("fill");
        _value   = root.Q<Label>("value");

        // USS has no @keyframes, so the ring is stepped from the panel's own scheduler.
        _mark.schedule.Execute(SpinMark).Every(16);

        base.Awake();
    }

    /// <summary>
    /// Both gates, before the transition can read either: the panels have to finish closing
    /// before anything unloads, and finish opening before the screen counts as gone.
    /// </summary>
    protected override void OnBound()
    {
        Progress.HoldShow(this);
        Progress.HoldHide(this);

        Progress.Progressed += OnProgressed;
        Progress.LoadingCompleted += SlideOut;

        SlideIn();
    }

    protected override void OnDestroy()
    {
        if (Progress != null)
        {
            Progress.Progressed -= OnProgressed;
            Progress.LoadingCompleted -= SlideOut;
        }

        base.OnDestroy();
    }

    /// <summary>
    /// Brings the panels in from the sides — the exact inverse of <see cref="SlideOut"/>.
    /// </summary>
    /// <remarks>
    /// <b>Applied a tick late, deliberately.</b> This runs from <c>Awake</c>, in the same frame the
    /// document is built, and a USS transition needs its starting value committed to a frame before
    /// the target one arrives. Setting both at once gives it nothing to animate between, so the
    /// panels would simply appear shut. One scheduled tick is enough for the off-screen translate
    /// to land, and the slide then runs.
    /// </remarks>
    void SlideIn()
    {
        // Waits for the panel's first layout. A scheduled tick is not enough — it can still run
        // inside the frame the document was built, leaving the transition with nothing to animate
        // from. GeometryChangedEvent fires once the off-screen translate has actually been laid
        // out, which is exactly the guarantee needed.
        void OnFirstLayout(GeometryChangedEvent _)
        {
            _left.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

            _left.AddToClassList(ClosedClass);
            _right.AddToClassList(ClosedClass);
            _content.AddToClassList(VisibleClass);

            // The gate opens when the slide finishes, not when it starts.
            _left.schedule.Execute(() => Progress.ReleaseShow(this)).StartingIn(Milliseconds());
        }

        _left.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }

    void SlideOut()
    {
        _content.RemoveFromClassList(VisibleClass);
        _left.RemoveFromClassList(ClosedClass);
        _right.RemoveFromClassList(ClosedClass);

        _left.schedule.Execute(() => Progress.ReleaseHide(this)).StartingIn(Milliseconds());
    }

    void OnProgressed(float progress)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100);

        _value.text = $"{percent}%";
        _fill.style.width = Length.Percent(percent);
    }

    void SpinMark()
    {
        _spin = (_spin + 4f) % 360f;
        _mark.style.rotate = new StyleRotate(new Rotate(new Angle(_spin, AngleUnit.Degree)));
    }

    long Milliseconds() => (long)(Mathf.Max(0, _slideSeconds) * 1000f);
}
