using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// runtime-built settings overlay — main menu only
// builds its own Canvas (sortingOrder=16), no prefab needed
public class SettingsDisplay : MonoBehaviour
{
    [Header("Context")]
    [Tooltip("hide settings that only matter on the main menu (currently: AR Mode / Mobile tab). Enable on the in-game (pause menu) instance.")]
    [SerializeField] private bool hideMobileTab = false;

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
    [Tooltip("track / handle thickness in pixels — bump up for fat fingers on mobile")]
    [SerializeField] private float sliderHeight = 20f;
    [Tooltip("vertical space the entire slider row takes (controls touch hit area). Set higher than rowHeight on mobile so taps anywhere on the track register.")]
    [SerializeField] private float sliderRowHeight = 40f;

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

    [Header("Layout Positions (live-tunable)")]
    [Tooltip("Y offset of the SETTINGS title from the panel top (negative = lower)")]
    [SerializeField] private float titleY = -10f;
    [Tooltip("Y offset of the tab bar from the panel top (negative = lower)")]
    [SerializeField] private float tabBarY = -65f;
    [Tooltip("Gap between bottom of tab bar and top of content area")]
    [SerializeField] private float contentTopGap = 15f;
    [Tooltip("Vertical space at the panel bottom reserved for Reset / Close buttons")]
    [SerializeField] private float contentBottomMargin = 65f;
    [Tooltip("Reset button anchored position (relative to panel bottom-center)")]
    [SerializeField] private Vector2 resetButtonPos = new Vector2(-100f, 12f);
    [Tooltip("Close button anchored position (relative to panel bottom-center)")]
    [SerializeField] private Vector2 closeButtonPos = new Vector2(100f, 12f);

    private GameObject root;
    private CanvasGroup canvasGroup;

    // live-tuning caches — populated in BuildUI, re-applied every frame in ApplyLiveSettings
    private Image panelImg;
    private Image borderImg;
    private RectTransform borderRect;
    private RectTransform panelRect;
    private Image backdropImg;
    private TMP_Text titleText;
    private readonly List<TMP_Text> tabTexts = new List<TMP_Text>();
    private readonly List<TMP_Text> labelTexts = new List<TMP_Text>();
    private readonly List<TMP_Text> valueTexts = new List<TMP_Text>();
    private readonly List<TMP_Text> buttonTexts = new List<TMP_Text>();
    private readonly List<Image> cycleButtonImages = new List<Image>();
    private readonly List<Image> buttonImages = new List<Image>();
    private readonly List<Image> sliderFillImages = new List<Image>();
    private readonly List<Image> sliderBgImages = new List<Image>();
    private readonly List<Image> sliderHandleImages = new List<Image>();
    // slider rects so sliderHeight + handle size live-updates
    private readonly List<RectTransform> sliderRects = new List<RectTransform>();
    private readonly List<RectTransform> sliderHandleRects = new List<RectTransform>();
    // row rects so row heights live-update and rows can be re-flowed top-to-bottom per section
    private readonly List<RectTransform> sliderRowRects = new List<RectTransform>();
    private readonly List<RectTransform> regularRowRects = new List<RectTransform>();

    // per-button auto-fit data: every frame, button is resized to text preferred + padding
    private struct StyledButton
    {
        public RectTransform rect;
        public TMP_Text text;
        public Vector2 padding;
        public bool square;        // force width = height (for < > arrows)
        public float minWidth;
        public float minHeight;
    }
    private readonly List<StyledButton> styledButtons = new List<StyledButton>();

    // cycle row layout: position of value + < depends on > button's actual width
    private struct CycleRow
    {
        public RectTransform leftBtn;
        public RectTransform rightBtn;
        public RectTransform valueRect;
        public float valueWidth;
        public float gap;
    }
    private readonly List<CycleRow> cycleRows = new List<CycleRow>();

    // layout-position caches
    private RectTransform titleRect;
    private RectTransform[] tabRects = new RectTransform[5];
    private RectTransform[] contentViewRects = new RectTransform[5];
    private RectTransform resetBtnRect;
    private RectTransform closeBtnRect;

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

    void Update()
    {
        // re-apply inspector style fields every frame so designers can tune live in play mode
        if (root == null || !isShowing) return;
        ApplyLiveSettings();
    }

    void ApplyLiveSettings()
    {
        // panel + backdrop
        if (panelImg != null) panelImg.color = panelColor;
        if (borderImg != null) borderImg.color = panelBorderColor;
        if (backdropImg != null) backdropImg.color = backdropColor;
        if (panelRect != null) panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        if (borderRect != null) borderRect.sizeDelta = new Vector2(panelWidth + panelBorderWidth * 2f, panelHeight + panelBorderWidth * 2f);

        // title (color follows the border palette)
        if (titleText != null) { titleText.fontSize = titleFontSize; titleText.color = panelBorderColor; }

        // tabs — text size + active/inactive color follow the current selection
        foreach (var t in tabTexts) if (t != null) t.fontSize = tabFontSize;
        if (displayTabImg != null)  displayTabImg.color  = currentTab == 0 ? tabActiveColor : tabInactiveColor;
        if (cameraTabImg != null)   cameraTabImg.color   = currentTab == 1 ? tabActiveColor : tabInactiveColor;
        if (audioTabImg != null)    audioTabImg.color    = currentTab == 2 ? tabActiveColor : tabInactiveColor;
        if (gameplayTabImg != null) gameplayTabImg.color = currentTab == 3 ? tabActiveColor : tabInactiveColor;
        if (mobileTabImg != null)   mobileTabImg.color   = currentTab == 4 ? tabActiveColor : tabInactiveColor;

        // text styling
        foreach (var t in labelTexts)  if (t != null) { t.fontSize = labelFontSize;  t.color = labelColor; }
        foreach (var t in valueTexts)  if (t != null) { t.fontSize = valueFontSize;  t.color = valueColor; }
        foreach (var t in buttonTexts) if (t != null) { t.fontSize = buttonFontSize; t.color = buttonTextColor; }

        // image colors
        foreach (var i in cycleButtonImages)  if (i != null) i.color = cycleButtonColor;
        foreach (var i in buttonImages)       if (i != null) i.color = buttonColor;
        foreach (var i in sliderFillImages)   if (i != null) i.color = sliderFillColor;
        foreach (var i in sliderBgImages)     if (i != null) i.color = sliderBgColor;
        foreach (var i in sliderHandleImages) if (i != null) i.color = sliderHandleColor;

        // toggle bg follows current value (so re-tinting toggleOn/Off reflects immediately)
        if (invertYBg != null) invertYBg.color = SettingsManager.InvertY ? toggleOnColor : toggleOffColor;
        if (arModeBg != null)  arModeBg.color  = SettingsManager.ARMode  ? toggleOnColor : toggleOffColor;

        // slider thickness (track + handle) — handle stays slightly taller than track
        foreach (var sr in sliderRects)
            if (sr != null) { var sd = sr.sizeDelta; sd.y = sliderHeight; sr.sizeDelta = sd; }
        foreach (var hr in sliderHandleRects)
            if (hr != null) hr.sizeDelta = new Vector2(sliderHeight, sliderHeight * 1.4f);

        // row heights (regular vs slider — slider rows can be made taller for mobile touch hit area)
        foreach (var r in regularRowRects)
            if (r != null) { var sd = r.sizeDelta; sd.y = rowHeight; r.sizeDelta = sd; }
        foreach (var r in sliderRowRects)
            if (r != null) { var sd = r.sizeDelta; sd.y = sliderRowHeight; r.sizeDelta = sd; }

        // re-flow each section's rows top-to-bottom so taller slider rows push subsequent rows down
        foreach (var view in contentViewRects)
        {
            if (view == null) continue;
            float yPos = 0f;
            for (int i = 0; i < view.childCount; i++)
            {
                var child = view.GetChild(i) as RectTransform;
                if (child == null) continue;
                child.anchoredPosition = new Vector2(child.anchoredPosition.x, yPos);
                yPos -= child.sizeDelta.y + rowSpacing;
            }
        }

        // auto-fit every styled button to its text preferred size + padding
        foreach (var sb in styledButtons)
        {
            if (sb.rect == null || sb.text == null) continue;
            sb.text.ForceMeshUpdate();
            float w = sb.text.preferredWidth + sb.padding.x * 2f;
            float h = sb.text.preferredHeight + sb.padding.y * 2f;
            if (sb.square) { float s = Mathf.Max(w, h); w = s; h = s; }
            w = Mathf.Max(w, sb.minWidth);
            h = Mathf.Max(h, sb.minHeight);
            sb.rect.sizeDelta = new Vector2(w, h);
        }

        // re-position cycle row contents so value + < button track the > button's actual width
        foreach (var cr in cycleRows)
        {
            if (cr.rightBtn == null || cr.valueRect == null || cr.leftBtn == null) continue;
            float rightW = cr.rightBtn.sizeDelta.x;
            float leftW = cr.leftBtn.sizeDelta.x;
            // value sits to the left of the > button
            cr.valueRect.anchoredPosition = new Vector2(-rightW - cr.gap, 0f);
            // < button sits to the left of the value box
            cr.leftBtn.anchoredPosition = new Vector2(-rightW - cr.valueWidth - cr.gap * 2f, 0f);
        }

        // --- layout positions ---
        if (titleRect != null)
            titleRect.anchoredPosition = new Vector2(0f, titleY);

        // tab bar Y — keep each tab's X (driven by tabWidth/tabSpacing) but use live tabBarY
        int visibleTabCount = hideMobileTab ? 4 : 5;
        float totalTabsW = tabWidth * visibleTabCount + tabSpacing * (visibleTabCount - 1);
        float tabStartX = -totalTabsW * 0.5f + tabWidth * 0.5f;
        for (int i = 0; i < tabRects.Length; i++)
        {
            if (tabRects[i] == null) continue;
            tabRects[i].anchoredPosition = new Vector2(tabStartX + i * (tabWidth + tabSpacing), tabBarY);
            tabRects[i].sizeDelta = new Vector2(tabWidth, tabHeight);
        }

        // content area top/bottom margins
        float contentTopOffset = -(tabBarY - tabHeight - contentTopGap);
        float contentBottomOffset = contentBottomMargin;
        foreach (var cv in contentViewRects)
        {
            if (cv == null) continue;
            cv.offsetMin = new Vector2(sidePadding, contentBottomOffset);
            cv.offsetMax = new Vector2(-sidePadding, -contentTopOffset);
        }

        // bottom buttons
        if (resetBtnRect != null) resetBtnRect.anchoredPosition = resetButtonPos;
        if (closeBtnRect != null) closeBtnRect.anchoredPosition = closeButtonPos;
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
        backdropImg = bdImg;
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
        borderImg = border.AddComponent<Image>();
        borderImg.color = panelBorderColor;
        borderRect = border.GetComponent<RectTransform>();

        // center panel
        var panel = CreateUIObj("Panel", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(panelWidth, panelHeight));
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = true;
        panelImg = panelImage;
        panelRect = panel.GetComponent<RectTransform>();

        // --- title ---
        var titleObj = CreateUIObj("Title", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, titleY), new Vector2(0f, 45f));
        titleRect = titleObj.GetComponent<RectTransform>();
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "SETTINGS";
        titleTmp.fontSize = titleFontSize;
        titleTmp.color = panelBorderColor;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.raycastTarget = false;
        titleText = titleTmp;

        // --- tab bar ---
        // tabs centered as a group below the title. mobile tab is skipped on the in-game instance.
        int visibleTabCount = hideMobileTab ? 4 : 5;
        float totalTabsW = tabWidth * visibleTabCount + tabSpacing * (visibleTabCount - 1);
        float tabStartX = -totalTabsW * 0.5f + tabWidth * 0.5f;

        displayTabImg  = BuildTab(panel.transform, "DISPLAY",  tabStartX + 0 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(0));
        cameraTabImg   = BuildTab(panel.transform, "CAMERA",   tabStartX + 1 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(1));
        audioTabImg    = BuildTab(panel.transform, "AUDIO",    tabStartX + 2 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(2));
        gameplayTabImg = BuildTab(panel.transform, "GAMEPLAY", tabStartX + 3 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(3));
        if (!hideMobileTab)
            mobileTabImg = BuildTab(panel.transform, "MOBILE",   tabStartX + 4 * (tabWidth + tabSpacing), tabBarY, () => SwitchToTab(4));

        tabRects[0] = displayTabImg.rectTransform;
        tabRects[1] = cameraTabImg.rectTransform;
        tabRects[2] = audioTabImg.rectTransform;
        tabRects[3] = gameplayTabImg.rectTransform;
        tabRects[4] = mobileTabImg != null ? mobileTabImg.rectTransform : null;

        // --- content area (below tab bar, above bottom buttons) ---
        float contentTopOffset = -(tabBarY - tabHeight - contentTopGap); // distance from panel top down to content top
        float contentBottomOffset = contentBottomMargin; // leaves room for Reset / Close buttons
        float contentWidth = panelWidth - sidePadding * 2f;

        // each view is a stretched rect. row builders position children top-down using y starting at 0.
        displayView  = BuildContentView(panel.transform, "DisplayView",  contentTopOffset, contentBottomOffset);
        cameraView   = BuildContentView(panel.transform, "CameraView",   contentTopOffset, contentBottomOffset);
        audioView    = BuildContentView(panel.transform, "AudioView",    contentTopOffset, contentBottomOffset);
        gameplayView = BuildContentView(panel.transform, "GameplayView", contentTopOffset, contentBottomOffset);
        if (!hideMobileTab)
            mobileView = BuildContentView(panel.transform, "MobileView",   contentTopOffset, contentBottomOffset);

        contentViewRects[0] = displayView.GetComponent<RectTransform>();
        contentViewRects[1] = cameraView.GetComponent<RectTransform>();
        contentViewRects[2] = audioView.GetComponent<RectTransform>();
        contentViewRects[3] = gameplayView.GetComponent<RectTransform>();
        contentViewRects[4] = mobileView != null ? mobileView.GetComponent<RectTransform>() : null;

        BuildDisplaySection(displayView.transform, contentWidth);
        BuildCameraSection(cameraView.transform, contentWidth);
        BuildAudioSection(audioView.transform, contentWidth);
        BuildGameplaySection(gameplayView.transform, contentWidth);
        if (!hideMobileTab)
            BuildMobileSection(mobileView.transform, contentWidth);

        // --- bottom buttons ---
        float btnW = 180f;
        float btnH = 40f;
        var resetBtnObj  = CreateButton(panel.transform, "Reset Defaults", resetButtonPos, btnW, btnH, OnResetDefaults);
        var closeBtnObj  = CreateButton(panel.transform, "Close",          closeButtonPos, btnW, btnH, Hide);
        resetBtnRect = resetBtnObj.GetComponent<RectTransform>();
        closeBtnRect = closeBtnObj.GetComponent<RectTransform>();

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
        tabTexts.Add(tmp);
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
        if (mobileView != null) mobileView.SetActive(index == 4);

        displayTabImg.color  = index == 0 ? tabActiveColor : tabInactiveColor;
        cameraTabImg.color   = index == 1 ? tabActiveColor : tabInactiveColor;
        audioTabImg.color    = index == 2 ? tabActiveColor : tabInactiveColor;
        gameplayTabImg.color = index == 3 ? tabActiveColor : tabInactiveColor;
        if (mobileTabImg != null) mobileTabImg.color = index == 4 ? tabActiveColor : tabInactiveColor;
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

        if (arModeText != null && arModeBg != null)
        {
            bool ar = SettingsManager.ARMode;
            arModeText.text = ar ? "ON" : "OFF";
            arModeBg.color = ar ? toggleOnColor : toggleOffColor;
        }
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
        // Overflow (not Ellipsis) so live font-size tweaks never collapse text into "..." / empty
        text.overflowMode = TextOverflowModes.Overflow;
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
        regularRowRects.Add(row.GetComponent<RectTransform>());

        // label on left half
        var lbl = CreateText(row.transform, label, labelFontSize, TextAlignmentOptions.Left, labelColor);
        labelTexts.Add(lbl);
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
        cycleButtonImages.Add(rbImg);
        var rbText = CreateText(rightBtn.transform, ">", buttonFontSize, TextAlignmentOptions.Center, buttonTextColor);
        StretchFill(rbText.gameObject);
        buttonTexts.Add(rbText);
        var rbBtn = rightBtn.AddComponent<Button>();
        rbBtn.targetGraphic = rbImg;
        rbBtn.onClick.AddListener(onRight);
        var rbRect = rightBtn.GetComponent<RectTransform>();
        styledButtons.Add(new StyledButton { rect = rbRect, text = rbText, padding = new Vector2(8f, 4f), square = true, minWidth = 28f, minHeight = 28f });

        // value text in the middle-right
        float valWidth = 140f;
        var valObj = CreateUIObj("Value", row.transform,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(-btnSize - 4f, 0f), new Vector2(valWidth, 0f));
        var valText = CreateText(valObj.transform, "---", valueFontSize, TextAlignmentOptions.Center, valueColor);
        StretchFill(valText.gameObject);
        valueTexts.Add(valText);
        var valRect = valObj.GetComponent<RectTransform>();

        // < button to the left of value
        var leftBtn = CreateUIObj("<Btn", row.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-btnSize - valWidth - 8f, 0f), new Vector2(btnSize, btnSize));
        var lbImg = leftBtn.AddComponent<Image>();
        lbImg.color = cycleButtonColor;
        cycleButtonImages.Add(lbImg);
        var lbText = CreateText(leftBtn.transform, "<", buttonFontSize, TextAlignmentOptions.Center, buttonTextColor);
        StretchFill(lbText.gameObject);
        buttonTexts.Add(lbText);
        var lbBtn = leftBtn.AddComponent<Button>();
        lbBtn.targetGraphic = lbImg;
        lbBtn.onClick.AddListener(onLeft);
        var lbRect = leftBtn.GetComponent<RectTransform>();
        styledButtons.Add(new StyledButton { rect = lbRect, text = lbText, padding = new Vector2(8f, 4f), square = true, minWidth = 28f, minHeight = 28f });

        // remember the row so ApplyLiveSettings can re-position value + left button when right button resizes
        cycleRows.Add(new CycleRow { leftBtn = lbRect, rightBtn = rbRect, valueRect = valRect, valueWidth = valWidth, gap = 4f });

        return valText;
    }

    Slider CreateSliderRow(Transform parent, string label, float y, float width,
        float min, float max, out TMP_Text valueText, UnityEngine.Events.UnityAction<float> onChange)
    {
        var row = CreateUIObj(label + "Row", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(0f, sliderRowHeight));
        sliderRowRects.Add(row.GetComponent<RectTransform>());

        // label on left
        var lbl = CreateText(row.transform, label, labelFontSize, TextAlignmentOptions.Left, labelColor);
        labelTexts.Add(lbl);
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
        valueTexts.Add(valueText);

        // slider in middle area
        var sliderObj = CreateUIObj(label + "Slider", row.transform,
            new Vector2(0.46f, 0.5f), new Vector2(0.87f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(0f, sliderHeight));
        sliderRects.Add(sliderObj.GetComponent<RectTransform>());

        // background track
        var bgObj = CreateUIObj("Bg", sliderObj.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = sliderBgColor;
        bgImg.raycastTarget = true; // make the track itself a tap target so click-anywhere-to-jump works
        sliderBgImages.Add(bgImg);

        // fill area
        var fillArea = CreateUIObj("Fill Area", sliderObj.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        var fillObj = CreateUIObj("Fill", fillArea.transform,
            Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = sliderFillColor;
        sliderFillImages.Add(fillImg);

        // handle area
        var handleArea = CreateUIObj("Handle Slide Area", sliderObj.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        var handleObj = CreateUIObj("Handle", handleArea.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(sliderHeight, sliderHeight * 1.4f));
        var handleImg = handleObj.AddComponent<Image>();
        handleImg.color = sliderHandleColor;
        sliderHandleImages.Add(handleImg);
        sliderHandleRects.Add(handleObj.GetComponent<RectTransform>());

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
        regularRowRects.Add(row.GetComponent<RectTransform>());

        // label
        var lbl = CreateText(row.transform, label, labelFontSize, TextAlignmentOptions.Left, labelColor);
        labelTexts.Add(lbl);
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
        valueTexts.Add(valueText);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bgImage;
        btn.onClick.AddListener(onClick);

        // auto-fit toggle to its label (ON/OFF) + padding
        styledButtons.Add(new StyledButton {
            rect = btnObj.GetComponent<RectTransform>(),
            text = valueText,
            padding = new Vector2(20f, 10f),
            square = false,
            minWidth = 70f,
            minHeight = 28f,
        });
    }

GameObject CreateButton(Transform parent, string text, Vector2 pos, float w, float h,
        UnityEngine.Events.UnityAction onClick)
    {
        var obj = CreateUIObj(text + "Btn", parent,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            pos, new Vector2(w, h));
        var img = obj.AddComponent<Image>();
        img.color = buttonColor;
        buttonImages.Add(img);

        var tmp = CreateText(obj.transform, text, buttonFontSize, TextAlignmentOptions.Center, buttonTextColor);
        StretchFill(tmp.gameObject);
        buttonTexts.Add(tmp);

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        // auto-fit bottom buttons to their text + padding
        styledButtons.Add(new StyledButton {
            rect = obj.GetComponent<RectTransform>(),
            text = tmp,
            padding = new Vector2(28f, 12f),
            square = false,
            minWidth = w,
            minHeight = h,
        });
        return obj;
    }
}
