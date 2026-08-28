using System.Collections.Generic;
using System.IO;
using MyGameDevTools.SceneLoading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

/// <summary>
/// Builds the sample's scenes, prefab and assets from code.
/// </summary>
/// <remarks>
/// The sample's UI is authored here rather than by hand so it can be regenerated, reviewed as a
/// diff, and kept consistent between the two rooms. Run it from
/// <c>Tools/My Scene Manager/Rebuild 'Loading Scene Examples'</c>, or in batch mode with
/// <c>-executeMethod SampleBuilder.Build</c>.
/// </remarks>
public static class SampleBuilder
{
    const string _root = "Packages/com.mygamedevtools.scene-loader/Samples/LoadingSceneExamples";
    const string _scenes = _root + "/Scenes";
    const string _ui = _root + "/UI";
    const string _prefabs = _root + "/Prefabs";

    [MenuItem("Tools/My Scene Manager/Rebuild 'Loading Scene Examples'")]
    public static void Build()
    {
        // Order 0 holds the room and the UI Toolkit loading screens; the uGUI loading screen's
        // canvas sits at 50 and covers them, which is what a loading screen should do.
        PanelSettings panel = BuildPanelSettings("SamplePanelSettings", sortingOrder: 0);
        // Above that canvas, so the operation stays readable whichever screen is up.
        PanelSettings hudPanel = BuildPanelSettings("HudPanelSettings", sortingOrder: 100);

        // Anything that appears in more than one scene, or that a user would sensibly drop into
        // their own, is a prefab. The scenes below hold instances of them and nothing else.
        GameObject loadingScreen = BuildLoadingScreenPrefab();
        GameObject animatedScreen = BuildAnimatedScreenPrefab(panel);
        GameObject hud = BuildHudPrefab(hudPanel);
        GameObject roomUi = BuildRoomUiPrefab(panel, loadingScreen);

        BuildSceneFrom(loadingScreen, "Loading_Screen");
        BuildSceneFrom(animatedScreen, "Loading_Animated");
        BuildSceneFrom(hud, "SceneListenerHUD");
        BuildExtraScene();

        BuildRoom("SceneA", "SceneB", roomUi);
        BuildRoom("SceneB", "SceneA", roomUi);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SampleBuilder] Rebuilt the Loading Scene Examples sample.");
    }

    // ---------------------------------------------------------------- assets

    /// <summary>
    /// Two panels, because <see cref="UIDocument.sortingOrder"/> only orders documents
    /// <i>within</i> a panel.
    /// </summary>
    /// <remarks>
    /// A panel is sorted against uGUI canvases by <see cref="PanelSettings.sortingOrder"/>, so
    /// everything sharing one panel at order 0 lands underneath the uGUI loading screen's canvas
    /// at 50 — however high the document's own order is. The HUD has to outrank that canvas, so
    /// it gets a panel of its own.
    /// </remarks>
    static PanelSettings BuildPanelSettings(string name, int sortingOrder)
    {
        string path = $"{_ui}/{name}.asset";
        PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);

        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(settings, path);
        }

        settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(_ui + "/Theme.tss");
        settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        // The UI is laid out for a 1280x720 canvas; on a 1080p screen everything scales up by
        // 1.5. Claiming 1920 here is what made it render at two thirds the intended size.
        settings.referenceResolution = new Vector2Int(1280, 720);
        settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        settings.match = .5f;
        settings.sortingOrder = sortingOrder;

        EditorUtility.SetDirty(settings);
        return settings;
    }

    /// <summary>
    /// The uGUI loading screen, as a prefab. The loading <i>scene</i> is built from the same
    /// method, so the two are identical by construction — which is the point they exist to make.
    /// </summary>
    static GameObject BuildLoadingScreenPrefab() => SavePrefab(BuildUguiLoadingScreen(), "LoadingScreen");

    // ---------------------------------------------------------------- scenes

    /// <summary>
    /// The UI Toolkit loading scene's contents, as a prefab. Its gates live on the
    /// LoadingBehavior; AnimatedLoadingScreen finds it on the same object.
    /// </summary>
    static GameObject BuildAnimatedScreenPrefab(PanelSettings panel)
    {
        GameObject root = new("Animated Loading Screen");
        Document(root, panel, "LoadingScreenAnimated.uxml", sortingOrder: 50);

        LoadingBehavior behavior = root.AddComponent<LoadingBehavior>();
        root.AddComponent<AnimatedLoadingScreen>();
        HoldFor(root.AddComponent<MinimumDisplayTime>(), behavior);

        return SavePrefab(root, "AnimatedLoadingScreen");
    }

    /// <summary>The persistent HUD's contents, as a prefab.</summary>
    static GameObject BuildHudPrefab(PanelSettings hudPanel)
    {
        GameObject root = new("Operation HUD");
        // Alone in its panel; the ordering that matters is the panel's, not this one's.
        Document(root, hudPanel, "OperationHud.uxml", sortingOrder: 0);

        root.AddComponent<OperationHud>();

        return SavePrefab(root, "OperationHud");
    }

    /// <summary>
    /// The room UI, shared by both rooms. Only the scene it transitions to differs, and that is
    /// a per-instance override rather than a second prefab.
    /// </summary>
    static GameObject BuildRoomUiPrefab(PanelSettings panel, GameObject loadingScreenPrefab)
    {
        GameObject root = new("Examples UI");
        Document(root, panel, "RoomScreen.uxml", sortingOrder: 0);

        TransitionExamples examples = root.AddComponent<TransitionExamples>();
        SerializedObject serialized = new(examples);
        serialized.FindProperty("_loadingScreenPrefab").objectReferenceValue = loadingScreenPrefab;
        serialized.FindProperty("_loadingScreenDocument").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_ui + "/LoadingScreenMinimal.uxml");
        serialized.FindProperty("_loadingScreenPanelSettings").objectReferenceValue = panel;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, "RoomUI");
    }

    /// <summary>A scene whose entire contents are one prefab instance.</summary>
    static void BuildSceneFrom(GameObject prefab, string sceneName)
    {
        Scene scene = NewScene();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        SceneManager.MoveGameObjectToScene(instance, scene);

        SaveScene(scene, sceneName);
    }

    /// <summary>A scene with something visibly in it, for the multi-scene example.</summary>
    static void BuildExtraScene()
    {
        Scene scene = NewScene();

        GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = "Extra Prop";
        prop.transform.position = new Vector3(0, 1.5f, 2.5f);
        prop.transform.localScale = Vector3.one * .8f;
        prop.AddComponent<SpinningProp>();

        SceneManager.MoveGameObjectToScene(prop, scene);
        SaveScene(scene, "Extra");
    }

    /// <summary>
    /// Rebuilds a room's UI in place: the 3D set stays, whatever UI was there goes, and an
    /// instance of the shared room prefab takes its place — overriding only the scene it
    /// transitions to.
    /// </summary>
    static void BuildRoom(string sceneName, string targetScene, GameObject roomUiPrefab)
    {
        string path = $"{_scenes}/{sceneName}.unity";
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponent<Canvas>() != null || root.GetComponent<UIDocument>() != null || root.name == "EventSystem")
                Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(roomUiPrefab);
        SceneManager.MoveGameObjectToScene(instance, scene);

        SerializedObject serialized = new(instance.GetComponent<TransitionExamples>());
        serialized.FindProperty("_targetScene").stringValue = targetScene;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
    }

    // ---------------------------------------------------------------- uGUI screen

    /// <summary>
    /// The one uGUI screen in the sample, built with the package's own feedback components:
    /// a LoadingBehavior for the gates, a LoadingFader to hold them for the length of a fade,
    /// and slider and text feedback driven by the same LoadingProgress.
    /// </summary>
    static GameObject BuildUguiLoadingScreen()
    {
        GameObject root = new("Loading Screen", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // The same design canvas the UI Toolkit panels use, so the two systems agree on scale.
        scaler.referenceResolution = new Vector2(1280, 720);

        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0;

        LoadingBehavior behavior = root.AddComponent<LoadingBehavior>();
        LoadingFader fader = root.AddComponent<LoadingFader>();
        fader.LoadingBehavior = behavior;
        fader.fadeTime = .35f;

        // Two components holding the same hide gate, neither aware of the other: the transition
        // waits for whichever releases last.
        HoldFor(root.AddComponent<MinimumDisplayTime>(), behavior);

        GameObject backdrop = Image(root.transform, "Backdrop", new Color(.055f, .066f, .086f, 1f));
        Stretch(backdrop.GetComponent<RectTransform>());

        GameObject card = Image(root.transform, "Card", new Color(.09f, .106f, .129f, .96f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, .5f);
        cardRect.pivot = new Vector2(.5f, .5f);
        cardRect.sizeDelta = new Vector2(620, 210);
        cardRect.anchoredPosition = Vector2.zero;

        Text label = Label(card.transform, "Label", "LOADING", 20, new Color(.435f, .482f, .541f));
        Place(label.rectTransform, new Vector2(0, 1), new Vector2(34, -26), new Vector2(400, 28), TextAnchor.UpperLeft);

        // Right-aligned in a fixed box so the per-cent sign that follows always lands in the same
        // place. The box is sized to a three-digit number, so at 100% the digits start level with
        // the label above rather than floating out to the right of it.
        Text value = Label(card.transform, "Value", "0", 72, new Color(.949f, .961f, .973f));
        Place(value.rectTransform, new Vector2(0, 1), new Vector2(34, -58), new Vector2(126, 88), TextAnchor.UpperRight);
        value.gameObject.AddComponent<LoadingFeedbackText>().LoadingBehavior = behavior;

        Text symbol = Label(card.transform, "Symbol", "%", 30, new Color(.318f, .537f, .741f));
        Place(symbol.rectTransform, new Vector2(0, 1), new Vector2(166, -92), new Vector2(60, 40), TextAnchor.UpperLeft);

        Text hint = Label(card.transform, "Hint", "Scene screen · LoadingBehavior + LoadingFader", 15, new Color(.35f, .39f, .44f));
        Place(hint.rectTransform, new Vector2(0, 1), new Vector2(34, -150), new Vector2(560, 24), TextAnchor.UpperLeft);

        GameObject track = Image(card.transform, "Track", new Color(.133f, .157f, .192f));
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0, 0);
        trackRect.anchorMax = new Vector2(1, 0);
        trackRect.pivot = new Vector2(.5f, 0);
        trackRect.offsetMin = new Vector2(34, 26);
        trackRect.offsetMax = new Vector2(-34, 38);

        GameObject fill = Image(track.transform, "Fill", new Color(.318f, .537f, .741f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);

        UnityEngine.UI.Slider slider = track.AddComponent<UnityEngine.UI.Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.fillRect = fillRect;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0;
        track.AddComponent<LoadingFeedbackSlider>().LoadingBehavior = behavior;

        return root;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>
    /// How long every loading screen in the sample stays up, however fast the load turns out to
    /// be. Long enough to read what the screen is showing you, which is the point of a sample.
    /// </summary>
    const float _minimumScreenSeconds = 2f;

    static void HoldFor(MinimumDisplayTime hold, LoadingBehavior behavior)
    {
        hold.LoadingBehavior = behavior;

        SerializedObject serialized = new(hold);
        serialized.FindProperty("_seconds").floatValue = _minimumScreenSeconds;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    /// <summary>Adds a UIDocument pointing at one of the sample's UXML files.</summary>
    static UIDocument Document(GameObject go, PanelSettings panel, string uxml, float sortingOrder)
    {
        UIDocument document = go.AddComponent<UIDocument>();
        document.panelSettings = panel;
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{_ui}/{uxml}");
        document.sortingOrder = sortingOrder;
        return document;
    }

    static GameObject SavePrefab(GameObject root, string name)
    {
        Directory.CreateDirectory(_prefabs);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{_prefabs}/{name}.prefab");
        Object.DestroyImmediate(root);

        return prefab;
    }

    static void SaveScene(Scene scene, string name)
    {
        Directory.CreateDirectory(_scenes);
        Validate(scene, name);
        EditorSceneManager.SaveScene(scene, $"{_scenes}/{name}.unity");
    }

    /// <summary>
    /// Refuses to leave a scene holding a component whose script no longer exists.
    /// </summary>
    /// <remarks>
    /// Deleting a MonoBehaviour that a generated scene references leaves a "Missing (Mono
    /// Script)" entry that raises nothing and does nothing — the scene simply stops working, in
    /// silence. That is worth an error at the point it is introduced rather than a debugging
    /// session later.
    /// </remarks>
    static void Validate(Scene scene, string name)
    {
        int missing = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
                if (component == null)
                    missing++;

        if (missing > 0)
            Debug.LogError($"[SampleBuilder] '{name}' has {missing} component(s) whose script is missing. Something referenced by the scene was deleted or renamed.");
    }

    static GameObject Image(Transform parent, string name, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<UnityEngine.UI.Image>().color = color;
        return go;
    }

    static Text Label(Transform parent, string name, string text, int size, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text label = go.GetComponent<Text>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        // Arial.ttf was removed as a built-in; LegacyRuntime.ttf is the uGUI default now.
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return label;
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, TextAnchor alignment)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        if (rect.GetComponent<Text>() is Text text)
            text.alignment = alignment;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
