using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// runtime-built settings overlay — main menu only
// builds its own Canvas (sortingOrder=16), no prefab needed
public class SettingsDisplay : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private float panelWidth = 1100f;
    [SerializeField] private float panelHeight = 600f;
    [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.96f);
    [SerializeField] private Color panelBorderColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    [SerializeField] private float panelBorderWidth = 3f;
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("Tab Bar")]
    [SerializeField] private float tabHeight = 42f;
    [SerializeField] private float tabWidth = 170f;
    [SerializeField] private float tabSpacing = 8f;
    [SerializeField] private int tabFontSize = 20;
    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    [SerializeField] private Color tabInactiveColor = new Color(0.3f, 0.3f, 0.35f, 1f);

    [Header("Title")]
    [SerializeField] private int titleFontSize = 32;

    [Header("Section Headers")]
    [SerializeField] private int headerFontSize = 26;
    [SerializeField] private Color headerColor = new Color(0.9f, 0.75f, 0.3f);

    [Header("Labels")]
    [SerializeField] private int labelFontSize = 20;
    [SerializeField] private Color labelColor = new Color(0.85f, 0.85f, 0.85f);

    [Header("Values")]
    [SerializeField] private int valueFontSize = 20;
    [SerializeField] private Color valueColor = Color.white;

    [Header("Sliders")]
    [SerializeField] private Color sliderFillColor = new Color(0.3f, 0.6f, 0.9f);
    [SerializeField] private Color sliderBgColor = new Color(0.2f, 0.2f, 0.25f);
    [SerializeField] private Color sliderHandleColor = Color.white;
    [SerializeField] private float sliderHeight = 20f;

    [Header("Buttons")]
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.2f, 0.28f);
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private int buttonFontSize = 20;
    [SerializeField] private Color cycleButtonColor = new Color(0.25f, 0.25f, 0.32f);

    [Header("Toggle")]
    [SerializeField] private Color toggleOnColor = new Color(0.3f, 0.7f, 0.3f);
    [SerializeField] private Color toggleOffColor = new Color(0.4f, 0.2f, 0.2f);

    [Header("Spacing")]
    [SerializeField] private float sectionGap = 24f;
    [SerializeField] private float rowHeight = 40f;
    [SerializeField] private float rowSpacing = 6f;
    [SerializeField] private float topPadding = 20f;
    [SerializeField] private float sidePadding = 40f;

    private GameObject root;
    private CanvasGroup canvasGroup;

    // ui references we need to update
    private TMP_Text fpsValueText;
    private TMP_Text resValueText;
    private TMP_Text fullscreenValueText;
    private TMP_Text orbitSensValueText;
    private TMP_Text zoomSensValueText;
    private TMP_Text invertYText;
    private Image invertYBg;
    private TMP_Text masterVolText;
    private TMP_Text musicVolText;
    private TMP_Text sfxVolText;
    private TMP_Text spinSpeedText;
    private TMP_Text dealSpeedText;
    private TMP_Text arModeText;
    private Image arModeBg;

    // tab views — only the active one is visible, others are SetActive(false)
    private GameObject displayView;
    private GameObject cameraView;
    private GameObject audioView;
    private GameObject gameplayView;
    private GameObject mobileView;
    private Image displayTabImg;
    private Image cameraTabImg;
    private Image audioTabImg;
    private Image gameplayTabImg;
    private Image mobileTabImg;
    private int currentTab = -1;
    private Slider orbitSensSlider;
    private Slider zoomSensSlider;
    private Slider masterVolSlider;
    private Slider musicVolSlider;
    private Slider sfxVolSlider;
    private Slider spinSpeedSlider;
    private Slider dealSpeedSlider;

    // fps/resolution cycling state
    private static readonly int[] fpsOptions = { 30, 60, 75, 90, 120, 165, 240 };
    private int fpsIndex;
    private int deviceMaxFps;
    private Resolution[] availableResolutions;
    private int resolutionIndex;
    private int fullscreenIndex;
    private static readonly string[] fullscreenLabels = { "Exclusive", "Borderless", "Maximized", "Windowed" };

    private bool isShowing;
    public bool IsShowing => isShowing;

    public System.Action OnClosed;

    void Awake()
    {
        deviceMaxFps = Mathf.Max(60, (int)Screen.currentResolution.refreshRateRatio.value);
        CacheResolutions();
    }

    void CacheResolutions()
    {
        var unique = new Dictionary<string, Resolution>();
        foreach (var r in Screen.resolutions)
        {
            string key = $"{r.width}x{r.height}";
            unique[key] = r;
        }

        var list = new List<Resolution>(unique.Values);
        list.Sort((a, b) =>
        {
            int cmp = a.width.CompareTo(b.width);
            return cmp != 0 ? cmp : a.height.CompareTo(b.height);
        });
        availableResolutions = list.ToArray();

        int curW = SettingsManager.ResolutionW;
        int curH = SettingsManager.ResolutionH;
        resolutionIndex = 0;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == curW && availableResolutions[i].height == curH)
            { resolutionIndex = i; break; }
        }
    }

    public void Show()
    {
        if (isShowing) return;
        isShowing = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.panelOpen);

        if (root == null) BuildUI();

        SyncUIToSettings();

        root.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        if (!isShowing) return;
        isShowing = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.panelClose);

        if (root != null) root.SetActive(false);

        OnClosed?.Invoke();
    }

    void BuildUI()
    {
        // canvas
        root = new GameObject("SettingsUI");
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 16;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        canvasGroup = root.AddComponent<CanvasGroup>();

        // dark backdrop — clicking it closes settings
        var backdrop = CreateStretchChild(root, "Backdrop");
        var bdImg = backdrop.AddComponent<Image>();
        bdImg.color = backdropColor;
        bdImg.raycastTarget = true;
        var bdBtn = backdrop.AddComponent<Button>();
        bdBtn.onClick.AddListener(Hide);
        var bdColors = bdBtn.colors;
        bdColors.highlightedColor = backdropColor;
        bdColors.pressedColor = backdropColor;
        bdColors.selectedColor = backdropColor;
        bdBtn.colors = bdColors;

        // panel border (codex-style frame)
        var border = CreateUIObj("Border", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(panelWidth + panelBorderWidth * 2f, panelHeight + panelBorderWidth * 2f));
        border.AddComponent<Image>().color = panelBorderColor;

        // center panel
        var panel = CreateUIObj("Panel", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(panelWidth, panelHeight));
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = panelColor;
        panelImg.raycastTarget = true;

        // --- title ---
        var titleObj = CreateUIObj("Title", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -10f), new Vector2(0f, 45f));
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "SETTINGS";
        titleTmp.fontSize = titleFontSize;
        titleTmp.color = panelBorderColor;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.raycastTarget = false;

        // --- tab bar ---
        // five tabs centered as a group below the title. always show all five even on PC, since
        // every category has at least one row applicable everywhere.
        const int tabCount = 5;
        float tabBarY = -65f;
        float totalTabsW = tabWidth * tabCount + tabSpacing * (tabCount - 1);
        float tabStartX = -totalTabsW * 0.5f + tabWidth * 0.5f;

        displayTabImg  = BuildTab(panel.transform, "DISPLAY",  tabStartX + 0 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(0));
        cameraTabImg   = BuildTab(panel.transform, "CAMERA",   tabStartX + 1 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(1));
        audioTabImg    = BuildTab(panel.transform, "AUDIO",    tabStartX + 2 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(2));
        gameplayTabImg = BuildTab(panel.transform, "GAMEPLAY", tabStartX + 3 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(3));
        mobileTabImg   = BuildTab(panel.transform, "MOBILE",   tabStartX + 4 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(4));

        // --- content area (below tab bar, above bottom buttons) ---
        float contentTopOffset = -(tabBarY - tabHeight - 15f); // distance from panel top down to content top
        float contentBottomOffset = 65f; // leaves room for Reset / Close buttons
        float contentWidth = panelWidth - sidePadding * 2f;

        // each view is a stretched rect. row builders position children top-down using y starting at 0.
        displayView  = BuildContentView(panel.transform, "DisplayView",  contentTopOffset, contentBottomOffset);
        cameraView   = BuildContentView(panel.transform, "CameraView",   contentTopOffset, contentBottomOffset);
        audioView    = BuildContentView(panel.transform, "AudioView",    contentTopOffset, contentBottomOffset);
        gameplayView = BuildContentView(panel.transform, "GameplayView", contentTopOffset, contentBottomOffset);
        mobileView   = BuildContentView(panel.transform, "MobileView",   contentTopOffset, contentBottomOffset);

        BuildDisplaySection(displayView.transform, contentWidth);
        BuildCameraSection(cameraView.transform, contentWidth);
        BuildAudioSection(audioView.transform, contentWidth);
        BuildGameplaySection(gameplayView.transform, contentWidth);
        BuildMobileSection(mobileView.transform, contentWidth);

        // --- bottom buttons ---
        float btnW = 180f;
        float btnH = 40f;
        CreateButton(panel.transform, "Reset Defaults", new Vector2(-btnW * 0.5f - 10f, 12f), btnW, btnH, OnResetDefaults);
        CreateButton(panel.transform, "Close", new Vector2(btnW * 0.5f + 10f, 12f), btnW, btnH, Hide);

        // start on Display
        SwitchToTab(0);
    }

    // --- tab builders ---

    Image BuildTab(Transform parent, string label, float x, float y, UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = CreateUIObj(label + "Tab", parent,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(x, y), new Vector2(tabWidth, tabHeight));
        var img = btnObj.AddComponent<Image>();
        img.color = tabInactiveColor;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var tmp = CreateText(btnObj.transform, label, tabFontSize, TextAlignmentOptions.Center, Color.white);
        tmp.fontStyle = FontStyles.Bold;
        StretchFill(tmp.gameObject);
        return img;
    }

    GameObject BuildContentView(Transform parent, string name, float topOffset, float bottomOffset)
    {
        var view = CreateUIObj(name, parent,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var rt = view.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(sidePadding, bottomOffset);
        rt.offsetMax = new Vector2(-sidePadding, -topOffset);
        return view;
    }

    void SwitchToTab(int index)
    {
        // only play the click on a real user-driven tab change, not the initial selection during BuildUI
        if (currentTab != -1 && index != currentTab && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        currentTab = index;

        displayView.SetActive(index == 0);
        cameraView.SetActive(index == 1);
        audioView.SetActive(index == 2);
        gameplayView.SetActive(index == 3);
        mobileView.SetActive(index == 4);

        displayTabImg.color  = index == 0 ? tabActiveColor : tabInactiveColor;
        cameraTabImg.color   = index == 1 ? tabActiveColor : tabInactiveColor;
        audioTabImg.color    = index == 2 ? tabActiveColor : tabInactiveColor;
        gameplayTabImg.color = index == 3 ? tabActiveColor : tabInactiveColor;
        mobileTabImg.color   = index == 4 ? tabActiveColor : tabInactiveColor;
    }

    // --- per-section builders ---

    void BuildDisplaySection(Transform parent, float contentWidth)
    {
        float y = 0f;
        fpsValueText = CreateCycleRow(parent, "Frame Rate", y, contentWidth, OnFpsLeft, OnFpsRight);
        y -= rowHeight + rowSpacing;

        #if !UNITY_ANDROID && !UNITY_IOS
        resValueText = CreateCycleRow(parent, "Resolution", y, contentWidth, OnResLeft, OnResRight);
        y -= rowHeight + rowSpacing;

        fullscreenValueText = CreateCycleRow(parent, "Window Mode", y, contentWidth, OnFullscreenLeft, OnFullscreenRight);
        y -= rowHeight + rowSpacing;
        #endif
    }

    void BuildCameraSection(Transform parent, float contentWidth)
    {
        float y = 0f;
        orbitSensSlider = CreateSliderRow(parent, "Orbit Sensitivity", y, contentWidth, 0.25f, 3f,
            out orbitSensValueText, OnOrbitSensChanged);
        y -= rowHeight + rowSpacing;

        zoomSensSlider = CreateSliderRow(parent, "Zoom Sensitivity", y, contentWidth, 0.25f, 3f,
            out zoomSensValueText, OnZoomSensChanged);
        y -= rowHeight + rowSpacing;

        CreateToggleRow(parent, "Invert Y-Axis", y, contentWidth, out invertYText, out invertYBg, OnInvertYToggle);
    }

    void BuildAudioSection(Transform parent, float contentWidth)
    {
        float y = 0f;
        masterVolSlider = CreateSliderRow(parent, "Master Volume", y, contentWidth, 0f, 1f,
            out masterVolText, OnMasterVolChanged);
        y -= rowHeight + rowSpacing;

        musicVolSlider = CreateSliderRow(parent, "Music Volume", y, contentWidth, 0f, 1f,
            out musicVolText, OnMusicVolChanged);
        y -= rowHeight + rowSpacing;

        sfxVolSlider = CreateSliderRow(parent, "SFX Volume", y, contentWidth, 0f, 1f,
            out sfxVolText, OnSfxVolChanged);
    }

    void BuildGameplaySection(Transform parent, float contentWidth)
    {
        float y = 0f;
        spinSpeedSlider = CreateSliderRow(parent, "Earth Spin Speed", y, contentWidth, 0f, 3f,
            out spinSpeedText, OnSpinSpeedChanged);
        y -= rowHeight + rowSpacing;

        dealSpeedSlider = CreateSliderRow(parent, "Card Deal Speed", y, contentWidth, 0.5f, 2f,
            out dealSpeedText, OnDealSpeedChanged);
    }

    void BuildMobileSection(Transform parent, float contentWidth)
    {
        // shown to PC users too — the toggle is harmless there since IsMobilePlatform() gates the
        // actual scene swap. label spells out "for mobile devices" so PC players know it's not for them.
        float y = 0f;
        CreateToggleRow(parent, "AR Mode (for mobile devices)", y, contentWidth,
            out arModeText, out arModeBg, OnArModeToggle);
    }

    void SyncUIToSettings()
    {
        int savedFps = SettingsManager.TargetFPS;
        fpsIndex = FindClosestFpsIndex(savedFps);
        UpdateFpsText();

        #if !UNITY_ANDROID && !UNITY_IOS
        CacheResolutions();
        UpdateResText();
        fullscreenIndex = SettingsManager.FullscreenMode;
        UpdateFullscreenText();
        #endif

        orbitSensSlider.SetValueWithoutNotify(SettingsManager.OrbitSensitivity);
        UpdateSliderValueText(orbitSensValueText, SettingsManager.OrbitSensitivity, "x");

        zoomSensSlider.SetValueWithoutNotify(SettingsManager.ZoomSensitivity);
        UpdateSliderValueText(zoomSensValueText, SettingsManager.ZoomSensitivity, "x");

        bool inv = SettingsManager.InvertY;
        invertYText.text = inv ? "ON" : "OFF";
        invertYBg.color = inv ? toggleOnColor : toggleOffColor;

        masterVolSlider.SetValueWithoutNotify(SettingsManager.MasterVolume);
        UpdateSliderValueText(masterVolText, SettingsManager.MasterVolume, "%", true);

        musicVolSlider.SetValueWithoutNotify(SettingsManager.MusicVolume);
        UpdateSliderValueText(musicVolText, SettingsManager.MusicVolume, "%", true);

        sfxVolSlider.SetValueWithoutNotify(SettingsManager.SFXVolume);
        UpdateSliderValueText(sfxVolText, SettingsManager.SFXVolume, "%", true);

        spinSpeedSlider.SetValueWithoutNotify(SettingsManager.SpinSpeed);
        UpdateSliderValueText(spinSpeedText, SettingsManager.SpinSpeed, "x");

        dealSpeedSlider.SetValueWithoutNotify(SettingsManager.DealSpeed);
        UpdateSliderValueText(dealSpeedText, SettingsManager.DealSpeed, "x");

        bool ar = SettingsManager.ARMode;
        arModeText.text = ar ? "ON" : "OFF";
        arModeBg.color = ar ? toggleOnColor : toggleOffColor;
    }

    // --- callbacks ---

    int GetMaxFpsIndex()
    {
        int maxIndex = 0;
        for (int i = fpsOptions.Length - 1; i >= 0; i--)
        {
            if (fpsOptions[i] <= deviceMaxFps) { maxIndex = i; break; }
        }
        return maxIndex;
    }

    void OnFpsLeft()
    {
        fpsIndex--;
        if (fpsIndex < 0) fpsIndex = GetMaxFpsIndex();
        ApplyFps();
    }

    void OnFpsRight()
    {
        fpsIndex++;
        if (fpsIndex > GetMaxFpsIndex()) fpsIndex = 0;
        ApplyFps();
    }

    void ApplyFps()
    {
        int fps = fpsOptions[fpsIndex];
        if (fps > deviceMaxFps) fps = deviceMaxFps;
        SettingsManager.TargetFPS = fps;
        UpdateFpsText();
    }

    void UpdateFpsText()
    {
        if (fpsValueText == null) return;
        int fps = fpsOptions[fpsIndex];
        if (fps > deviceMaxFps) fps = deviceMaxFps;
        fpsValueText.text = fps.ToString();
    }

    int FindClosestFpsIndex(int targetFps)
    {
        int closest = 0;
        int minDiff = Mathf.Abs(fpsOptions[0] - targetFps);
        for (int i = 1; i < fpsOptions.Length; i++)
        {
            if (fpsOptions[i] > deviceMaxFps) break;
            int diff = Mathf.Abs(fpsOptions[i] - targetFps);
            if (diff < minDiff) { minDiff = diff; closest = i; }
        }
        return closest;
    }

    void OnResLeft()
    {
        if (availableResolutions.Length == 0) return;
        resolutionIndex = Mathf.Max(0, resolutionIndex - 1);
        ApplyResolution();
    }

    void OnResRight()
    {
        if (availableResolutions.Length == 0) return;
        resolutionIndex = Mathf.Min(availableResolutions.Length - 1, resolutionIndex + 1);
        ApplyResolution();
    }

    void ApplyResolution()
    {
        if (availableResolutions.Length == 0) return;
        var res = availableResolutions[resolutionIndex];
        SettingsManager.ResolutionW = res.width;
        SettingsManager.ResolutionH = res.height;
        Screen.SetResolution(res.width, res.height, (FullScreenMode)SettingsManager.FullscreenMode);
        UpdateResText();
    }

    void UpdateResText()
    {
        if (resValueText == null || availableResolutions.Length == 0) return;
        var r = availableResolutions[resolutionIndex];
        resValueText.text = $"{r.width} x {r.height}";
    }

    void OnFullscreenLeft()
    {
        fullscreenIndex = Mathf.Max(0, fullscreenIndex - 1);
        ApplyFullscreen();
    }

    void OnFullscreenRight()
    {
        fullscreenIndex = Mathf.Min(fullscreenLabels.Length - 1, fullscreenIndex + 1);
        ApplyFullscreen();
    }

    void ApplyFullscreen()
    {
        SettingsManager.FullscreenMode = fullscreenIndex;
        Screen.SetResolution(SettingsManager.ResolutionW, SettingsManager.ResolutionH, (FullScreenMode)fullscreenIndex);
        UpdateFullscreenText();
    }

    void UpdateFullscreenText()
    {
        if (fullscreenValueText == null) return;
        fullscreenValueText.text = fullscreenLabels[fullscreenIndex];
    }

    void OnOrbitSensChanged(float val)
    {
        SettingsManager.OrbitSensitivity = val;
        UpdateSliderValueText(orbitSensValueText, val, "x");
    }

    void OnZoomSensChanged(float val)
    {
        SettingsManager.ZoomSensitivity = val;
        UpdateSliderValueText(zoomSensValueText, val, "x");
    }

    void OnInvertYToggle()
    {
        bool newVal = !SettingsManager.InvertY;
        SettingsManager.InvertY = newVal;
        invertYText.text = newVal ? "ON" : "OFF";
        invertYBg.color = newVal ? toggleOnColor : toggleOffColor;
    }

    void OnMasterVolChanged(float val)
    {
        SettingsManager.MasterVolume = val;
        UpdateSliderValueText(masterVolText, val, "%", true);
    }

    void OnMusicVolChanged(float val)
    {
        SettingsManager.MusicVolume = val;
        UpdateSliderValueText(musicVolText, val, "%", true);
    }

    void OnSfxVolChanged(float val)
    {
        SettingsManager.SFXVolume = val;
        UpdateSliderValueText(sfxVolText, val, "%", true);
    }

    void OnSpinSpeedChanged(float val)
    {
        SettingsManager.SpinSpeed = val;
        UpdateSliderValueText(spinSpeedText, val, "x");
    }

    void OnDealSpeedChanged(float val)
    {
        SettingsManager.DealSpeed = val;
        UpdateSliderValueText(dealSpeedText, val, "x");
    }

    void OnArModeToggle()
    {
        bool newVal = !SettingsManager.ARMode;
        SettingsManager.ARMode = newVal;
        arModeText.text = newVal ? "ON" : "OFF";
        arModeBg.color = newVal ? toggleOnColor : toggleOffColor;
    }

    void OnResetDefaults()
    {
        SettingsManager.ResetToDefaults();
        SyncUIToSettings();
    }

    void UpdateSliderValueText(TMP_Text text, float val, string suffix, bool asPercent = false)
    {
        if (text == null) return;
        if (asPercent)
            text.text = Mathf.RoundToInt(val * 100f) + suffix;
        else
            text.text = val.ToString("F2") + suffix;
    }

    // --- UI helpers (same patterns as DashboardDisplay) ---

    GameObject CreateUIObj(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return obj;
    }

    GameObject CreateStretchChild(GameObject parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    TMP_Text CreateText(Transform parent, string content, int fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    void StretchFill(GameObject obj)
    {
        var rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // --- row builders ---

    TMP_Text CreateCycleRow(Transform parent, string label, float y, float width,
        UnityEngine.Events.UnityAction onLeft, UnityEngine.Events.UnityAction onRight)
    {
        var row = CreateUIObj(label + "Row", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(0f, rowHeight));

        // label on left half
        var lbl = CreateText(row.transform, label, labelFontSize, TextAlignmentOptions.Left, labelColor);
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0f);
        lblRt.anchorMax = new Vector2(0.5f, 1f);
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;

        // right side: [<] value [>]
        float btnSize = 36f;

        // > button on far right
        var rightBtn = CreateUIObj(">Btn", row.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            Vector2.zero, new Vector2(btnSize, btnSize));
        var rbImg = rightBtn.AddComponent<Image>();
        rbImg.color = cycleButtonColor;
        var rbText = CreateText(rightBtn.transform, ">", buttonFontSize, TextAlignmentOptions.Center, buttonTextColor);
        StretchFill(rbText.gameObject);
        var rbBtn = rightBtn.AddComponent<Button>();
        rbBtn.targetGraphic = rbImg;
        rbBtn.onClick.AddListener(onRight);

        // value text in the middle-right
        float valWidth = 140f;
        var valObj = CreateUIObj("Value", row.transform,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(-btnSize - 4f, 0f), new Vector2(valWidth, 0f));
        var valText = CreateText(valObj.transform, "---", valueFontSize, TextAlignmentOptions.Center, valueColor);
        StretchFill(valText.gameObject);

        // < button to the left of value
        var leftBtn = CreateUIObj("<Btn", row.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-btnSize - valWidth - 8f, 0f), new Vector2(btnSize, btnSize));
        var lbImg = leftBtn.AddComponent<Image>();
        lbImg.color = cycleButtonColor;
        var lbText = CreateText(leftBtn.transform, "<", buttonFontSize, TextAlignmentOptions.Center, buttonTextColor);
        StretchFill(lbText.gameObject);
        var lbBtn = leftBtn.AddComponent<Button>();
        lbBtn.targetGraphic = lbImg;
        lbBtn.onClick.AddListener(onLeft);

        return valText;
    }

    Slider CreateSliderRow(Transform parent, string label, float y, float width,
        float min, float max, out TMP_Text valueText, UnityEngine.Events.UnityAction<float> onChange)
    {
        var row = CreateUIObj(label + "Row", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(0f, rowHeight));

        // label on left
        var lbl = CreateText(row.transform, label, labelFontSize, TextAlignmentOptions.Left, labelColor);
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0f);
        lblRt.anchorMax = new Vector2(0.45f, 1f);
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;

        // value text on far right
        var valObj = CreateUIObj("ValueBox", row.transform,
            new Vector2(0.88f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        valueText = CreateText(valObj.transform, "---", valueFontSize, TextAlignmentOptions.Right, valueColor);
        StretchFill(valueText.gameObject);

        // slider in middle area
        var sliderObj = CreateUIObj(label + "Slider", row.transform,
            new Vector2(0.46f, 0.5f), new Vector2(0.87f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(0f, sliderHeight));

        // background track
        var bgObj = CreateUIObj("Bg", sliderObj.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = sliderBgColor;

        // fill area
        var fillArea = CreateUIObj("Fill Area", sliderObj.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        var fillObj = CreateUIObj("Fill", fillArea.transform,
            Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = sliderFillColor;

        // handle area
        var handleArea = CreateUIObj("Handle Slide Area", sliderObj.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        var handleObj = CreateUIObj("Handle", handleArea.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(sliderHeight, sliderHeight * 1.4f));
        var handleImg = handleObj.AddComponent<Image>();
        handleImg.color = sliderHandleColor;

        var slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillObj.GetComponent<RectTransform>();
        slider.handleRect = handleObj.GetComponent<RectTransform>();
        slider.targetGraphic = handleImg;
        slider.minValue = min;
        slider.maxValue = max;
        slider.onValueChanged.AddListener(onChange);

        return slider;
    }

    void CreateToggleRow(Transform parent, string label, float y, float width,
        out TMP_Text valueText, out Image bgImage, UnityEngine.Events.UnityAction onClick)
    {
        var row = CreateUIObj(label + "Row", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(0f, rowHeight));

        // label
        var lbl = CreateText(row.transform, label, labelFontSize, TextAlignmentOptions.Left, labelColor);
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0f);
        lblRt.anchorMax = new Vector2(0.5f, 1f);
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;

        // toggle button on right
        float btnW = 80f;
        float btnH = 32f;
        var btnObj = CreateUIObj("ToggleBtn", row.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            Vector2.zero, new Vector2(btnW, btnH));
        bgImage = btnObj.AddComponent<Image>();
        bgImage.color = toggleOffColor;

        valueText = CreateText(btnObj.transform, "OFF", valueFontSize, TextAlignmentOptions.Center, valueColor);
        StretchFill(valueText.gameObject);
        valueText.raycastTarget = false;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bgImage;
        btn.onClick.AddListener(onClick);
    }

    void CreateHeaderText(Transform contentTransform, string text, float y, int size = -1)
    {
        if (size < 0) size = headerFontSize;

        var obj = CreateUIObj(text + "Header", contentTransform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(0f, rowHeight));

        var tmp = CreateText(obj.transform, text, size, TextAlignmentOptions.Left, headerColor);
        tmp.fontStyle = FontStyles.Bold;
        StretchFill(tmp.gameObject);
    }

    void CreateButton(Transform parent, string text, Vector2 pos, float w, float h,
        UnityEngine.Events.UnityAction onClick)
    {
        var obj = CreateUIObj(text + "Btn", parent,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            pos, new Vector2(w, h));
        var img = obj.AddComponent<Image>();
        img.color = buttonColor;

        var tmp = CreateText(obj.transform, text, buttonFontSize, TextAlignmentOptions.Center, buttonTextColor);
        StretchFill(tmp.gameObject);

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
    }
}
