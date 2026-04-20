using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// the set of gameplay actions the tutorial can gate on
// None = no required action; the step advances when the player clicks Next
public enum TutorialAction
{
    None,
    OrbitEarth,
    ZoomEarth,
    SelectRegion,
    SelectCard,
    PlayCard,
    SkipRound,
    OpenShop,
    OpenDashboard,
    CloseDashboard,
    BuyCard,
    PickReward,
    EndRoundNaturally
}

// a configurable rectangle drawn on top of the UI during a tutorial step,
// used to point at buttons or UI elements the tutorial can't auto-highlight.
// color / pulse / border thickness all come from TutorialManager's Global Highlight Style
[System.Serializable]
public class TutorialHighlightBox
{
    [Tooltip("anchored position relative to the screen center (1920x1080 reference)")]
    public Vector2 position = Vector2.zero;
    [Tooltip("size of the highlight box in pixels")]
    public Vector2 size = new Vector2(120f, 60f);
}

// runtime-built scripted tutorial overlay
// wakes up only when TutorialSettings.IsTutorial is true on scene load
// shows a mascot (shopkeeper sprite) with a textbox that walks the player through each game concept
// this skeleton just advances via a Next button — action gating comes in a later step
public class TutorialManager : MonoBehaviour
{
    // other scripts read this to know the tutorial is running
    public static bool IsActive { get; private set; }

    // held so the static accessors can reach the current instance
    static TutorialManager instance;

    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(3, 8)]
        [Tooltip("the message the mascot shows for this step")]
        public string mascotText = "";

        [Tooltip("action the player must perform to advance. None = player clicks the Next button instead")]
        public TutorialAction requiredAction = TutorialAction.None;

        [Tooltip("when true, the player can interact with everything during this step. when false, only requiredAction is allowed (None steps block all gameplay input)")]
        public bool allowFreeInteraction = false;

        [Tooltip("show the Next button even when this step has a required action, so the player can skip ahead without performing it. None steps always show the Next button regardless")]
        public bool showNextButton = false;

        [Header("Per-Step Position Override (optional)")]
        [Tooltip("when true, mascotPosition below overrides the default mascot position for this step")]
        public bool overrideMascotPosition = false;
        [Tooltip("mascot offset from screen center (only applied when overrideMascotPosition is true)")]
        public Vector2 mascotPosition = new Vector2(-640f, -60f);

        [Tooltip("when true, textboxPosition below overrides the default textbox position for this step")]
        public bool overrideTextboxPosition = false;
        [Tooltip("textbox offset from screen center (only applied when overrideTextboxPosition is true)")]
        public Vector2 textboxPositionStep = new Vector2(-150f, -80f);

        [Header("Custom Highlight Boxes (optional)")]
        [Tooltip("when true, the highlight boxes below are drawn on top of the UI for this step. works even for steps with no required action (e.g. just pointing at UI the player should look at)")]
        public bool showCustomHighlights = false;
        [Tooltip("one or more pulsing boxes pointing at buttons / UI elements the tutorial text describes")]
        public List<TutorialHighlightBox> customHighlights = new List<TutorialHighlightBox>();
    }


    [Header("Mascot Sprite")]
    [Tooltip("drag the same shopkeeper idle sprite used by ShopDisplay")]
    [SerializeField] private Sprite mascotSprite;
    [Tooltip("square bounding box size in pixels — the sprite keeps its aspect ratio inside this box")]
    [SerializeField] private float mascotSize = 360f;
    [Tooltip("final horizontal position relative to screen center (negative = left)")]
    [SerializeField] private float mascotOffsetX = -640f;
    [SerializeField] private float mascotOffsetY = -60f;
    [SerializeField] private float mascotSlideSpeed = 4f;
    [SerializeField] private float mascotBobSpeed = 1.5f;
    [SerializeField] private float mascotBobAmount = 8f;
    [SerializeField] private float mascotSwaySpeed = 1f;
    [SerializeField] private float mascotSwayAngle = 3f;

    [Header("Textbox")]
    [SerializeField] private Color textboxColor = new Color(0.08f, 0.1f, 0.14f, 0.95f);
    [SerializeField] private Color textboxBorderColor = new Color(0.9f, 0.85f, 0.5f, 1f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color nextButtonColor = new Color(0.2f, 0.5f, 0.85f);
    [SerializeField] private Color nextButtonTextColor = Color.white;
    [SerializeField] private float textboxWidth = 680f;
    [SerializeField] private float textboxHeight = 220f;
    [Tooltip("textbox anchored position relative to screen center")]
    [SerializeField] private Vector2 textboxPosition = new Vector2(-150f, -80f);
    [Tooltip("how fast the textbox glides between per-step positions. higher = snappier")]
    [SerializeField] private float textboxSlideSpeed = 6f;
    [SerializeField] private float messageFontSize = 24f;
    [SerializeField] private float nextButtonFontSize = 20f;
    [SerializeField] private Vector2 nextButtonSize = new Vector2(140f, 40f);
    [SerializeField] private string nextButtonLabel = "NEXT";

    [Header("Steps")]
    [Tooltip("the full ordered list of tutorial steps — edit in Inspector")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Highlight")]
    [Tooltip("trait of the region to pulse whenever the current step is waiting on SelectRegion. first region with this trait is the target")]
    [SerializeField] private RegionTrait regionHighlightTrait = RegionTrait.Temperate;

    [Header("Scripted Hand")]
    [Tooltip("drag in the exact policy cards (in order) the player should see at the start of the tutorial. index 0 is the leftmost card. leave empty for a normal random draw")]
    [SerializeField] private List<PolicyData> scriptedStartingHand = new List<PolicyData>();

    [Header("Scripted Reward")]
    [Tooltip("when true, the reward popup opens automatically after the tutorial's skip-round step regardless of whether the round was carbon-positive")]
    [SerializeField] private bool forceRewardAfterSkipRound = true;
    [Tooltip("exact reward cards to show when the reward popup opens during the tutorial. leave empty for a normal random reward draw")]
    [SerializeField] private List<PolicyData> scriptedRewardChoices = new List<PolicyData>();
    [Tooltip("which reward card index to glow during a PickReward step. -1 = no highlight. matches scriptedRewardChoices ordering")]
    [SerializeField] private int scriptedRewardHighlightIndex = 0;

    [Header("Global Highlight Style")]
    [Tooltip("ONE place to tune every tutorial highlight — drives the hand card glow, reward card glow, Skip button glow, custom highlight boxes, and the region tutorial border rings. per-box and per-ring size stays local to each script, but color / alpha pulse / pulse speed / padding / border thickness all read from here")]
    [SerializeField] private Color globalHighlightColor = new Color(1f, 0.95f, 0.3f, 1f);
    [Tooltip("minimum alpha during the highlight pulse")]
    [SerializeField, Range(0f, 1f)] private float globalHighlightMinAlpha = 0.3f;
    [Tooltip("maximum alpha during the highlight pulse")]
    [SerializeField, Range(0f, 1f)] private float globalHighlightMaxAlpha = 1f;
    [Tooltip("how fast the highlight breathes (higher = faster pulse)")]
    [SerializeField] private float globalHighlightPulseSpeed = 4f;
    [Tooltip("padding in pixels that card / button glows extend past the element's edges")]
    [SerializeField] private float globalHighlightPadding = 14f;
    [Tooltip("border thickness in pixels for custom highlight boxes")]
    [SerializeField] private float globalHighlightBorderThickness = 4f;

    [Header("Input Thresholds")]
    [Tooltip("how far the mouse must move in a single frame (squared pixels) before an orbit counts as a real drag. higher = more forgiving, ignores clicks and tiny jitter. 1600 = 40px")]
    [SerializeField] private float orbitDragThresholdSqr = 1600f;

    [Header("End of Tutorial")]
    [TextArea(2, 4)]
    [SerializeField] private string farewellMessage = "Good luck!";
    [Tooltip("how long the farewell stays on screen before returning to main menu")]
    [SerializeField] private float farewellDuration = 2.5f;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    GameObject root;
    RectTransform mascotRect;
    Image mascotImage;
    RectTransform textboxRect;
    Image textboxBg;
    Outline textboxOutline;
    TMP_Text messageText;
    Button nextButton;
    RectTransform nextButtonRect;
    Image nextButtonImage;
    TMP_Text nextButtonLabelText;

    GameObject customHighlightContainer;
    List<RectTransform> customHighlightBoxRects = new List<RectTransform>();
    // 4 edges per box: [0]=top, [1]=bottom, [2]=left, [3]=right
    List<Image[]> customHighlightBoxEdges = new List<Image[]>();

    // glow attached as a child of a specific side-tab button (currently the Skip tab during the SkipRound step)
    GameObject sideTabGlow;
    Image sideTabGlowImage;

    float mascotSlideT;
    float mascotFadeT;
    Vector2 mascotSlideFrom;
    Vector2 mascotSlideTo;
    Vector2 textboxSlideFrom;
    Vector2 textboxSlideTo;
    float textboxSlideT;
    bool mascotSlideInitialized;
    bool textboxSlideInitialized;
    int currentStep;
    bool ending;
    float endTime;
    // set when the current step is waiting on the skip flow to finish (popup -> event banner -> next round)
    // during this window the mascot + textbox are hidden and the step advance is deferred until
    // GameManager calls NotifyRoundStarted
    bool waitingForRoundStart;

    void Start()
    {
        // stay dormant during normal play
        if (!TutorialSettings.IsTutorial)
        {
            enabled = false;
            return;
        }

        instance = this;
        IsActive = true;
        BuildUI();
        ShowStep(0);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        IsActive = false;
        HighlightedCardIndex = -1;
        HighlightedRewardIndex = -1;

        // make sure the region pulse doesn't hang around into the main menu
        var rm = FindFirstObjectByType<RegionManager>();
        if (rm != null) rm.ClearTutorialTargetRegion();

        // the side-tab glow is parented under a button outside the tutorial canvas —
        // it won't get destroyed with root, so tear it down explicitly
        DetachSideTabGlow();
    }

    void Update()
    {
        // reapply inspector values every frame so tuning works live in play mode
        ApplyLiveSettings();

        UpdateMascot();
        UpdateCustomHighlights();
        UpdateSideTabGlow();

        // spacebar shortcut for Next — only when the Next button is actually showing, so it
        // can't be used to bypass action-gated steps that hide Next
        if (!ending && !PauseMenu.IsPaused && nextButton != null && nextButton.gameObject.activeSelf
            && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnNext();
        }

        // after the farewell has been on screen long enough, go back to main menu
        if (ending && Time.unscaledTime - endTime >= farewellDuration)
        {
            ending = false;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // pushes every inspector-exposed field back onto the live UI each frame
    // lets you tweak sprites, sizes, colors, font sizes, and positions while the game is running
    void ApplyLiveSettings()
    {
        // mascot
        if (mascotRect != null)
            mascotRect.sizeDelta = new Vector2(mascotSize, mascotSize);
        if (mascotImage != null && mascotImage.sprite != mascotSprite)
            mascotImage.sprite = mascotSprite;

        // textbox panel — same slide pattern as the mascot so live inspector tweaks snap instantly
        // once the transition finishes (EaseOutCubic at t=1 returns the target exactly)
        if (textboxRect != null)
        {
            textboxSlideTo = ResolveTextboxTarget(CurrentStepData());
            if (textboxSlideT < 1f)
            {
                textboxSlideT += Time.unscaledDeltaTime * textboxSlideSpeed;
                if (textboxSlideT > 1f) textboxSlideT = 1f;
            }
            textboxRect.anchoredPosition = Vector2.Lerp(textboxSlideFrom, textboxSlideTo, EaseOutCubic(textboxSlideT));
            textboxRect.sizeDelta = new Vector2(textboxWidth, textboxHeight);
        }
        if (textboxBg != null)
            textboxBg.color = textboxColor;
        if (textboxOutline != null)
            textboxOutline.effectColor = textboxBorderColor;

        // message text
        if (messageText != null)
        {
            messageText.fontSize = messageFontSize;
            messageText.color = textColor;
        }

        // next button
        if (nextButtonRect != null)
            nextButtonRect.sizeDelta = nextButtonSize;
        if (nextButtonImage != null)
            nextButtonImage.color = nextButtonColor;
        if (nextButtonLabelText != null)
        {
            nextButtonLabelText.fontSize = nextButtonFontSize;
            nextButtonLabelText.color = nextButtonTextColor;
            // only reassign when the label actually changed, otherwise TMP keeps rebuilding the mesh every frame
            if (nextButtonLabelText.text != nextButtonLabel)
                nextButtonLabelText.text = nextButtonLabel;
        }
    }

    void BuildUI()
    {
        root = new GameObject("TutorialUI");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // above banners (25) and rewards (20) so the tutorial always wins the stack
        canvas.sortingOrder = 30;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        BuildMascot(root.transform);
        BuildTextbox(root.transform);
    }

    void BuildMascot(Transform parent)
    {
        // start offscreen to the left, slide in
        float startX = mascotOffsetX - 600f;

        var obj = new GameObject("Mascot");
        obj.transform.SetParent(parent, false);
        mascotRect = obj.AddComponent<RectTransform>();
        mascotRect.anchorMin = new Vector2(0.5f, 0.5f);
        mascotRect.anchorMax = new Vector2(0.5f, 0.5f);
        mascotRect.pivot = new Vector2(0.5f, 0.5f);
        mascotRect.anchoredPosition = new Vector2(startX, mascotOffsetY);
        mascotRect.sizeDelta = new Vector2(mascotSize, mascotSize);

        mascotImage = obj.AddComponent<Image>();
        mascotImage.preserveAspect = true;
        mascotImage.raycastTarget = false;
        if (mascotSprite != null)
            mascotImage.sprite = mascotSprite;
        // start fully transparent, fade in with the slide
        mascotImage.color = new Color(1f, 1f, 1f, 0f);

        mascotSlideT = 0f;
    }

    void UpdateMascot()
    {
        if (mascotRect == null) return;

        // re-read the step's target every frame so inspector tuning and per-step overrides both apply live
        mascotSlideTo = ResolveMascotTarget(CurrentStepData());

        // slide from the current "from" toward the latest target with ease-out cubic
        if (mascotSlideT < 1f)
        {
            mascotSlideT += Time.unscaledDeltaTime * mascotSlideSpeed;
            if (mascotSlideT > 1f) mascotSlideT = 1f;
        }
        Vector2 settled = Vector2.Lerp(mascotSlideFrom, mascotSlideTo, EaseOutCubic(mascotSlideT));

        // fade runs on its own clock so step transitions don't re-trigger it
        if (mascotFadeT < 1f)
        {
            mascotFadeT += Time.unscaledDeltaTime * mascotSlideSpeed;
            if (mascotFadeT > 1f) mascotFadeT = 1f;
        }
        if (mascotImage != null)
        {
            Color c = mascotImage.color;
            c.a = EaseOutCubic(mascotFadeT);
            mascotImage.color = c;
        }

        // gentle bob up and down
        float bob = Mathf.Sin(Time.unscaledTime * mascotBobSpeed) * mascotBobAmount;
        // sway tilt with a slightly different frequency so it doesn't sync with the bob
        float sway = Mathf.Sin(Time.unscaledTime * mascotSwaySpeed * 0.7f) * mascotSwayAngle;

        mascotRect.anchoredPosition = new Vector2(settled.x, settled.y + bob);
        mascotRect.localRotation = Quaternion.Euler(0f, 0f, sway);
    }

    void BuildTextbox(Transform parent)
    {
        var panel = new GameObject("Textbox");
        panel.transform.SetParent(parent, false);
        textboxRect = panel.AddComponent<RectTransform>();
        textboxRect.anchorMin = new Vector2(0.5f, 0.5f);
        textboxRect.anchorMax = new Vector2(0.5f, 0.5f);
        textboxRect.pivot = new Vector2(0.5f, 0.5f);
        textboxRect.anchoredPosition = textboxPosition;
        textboxRect.sizeDelta = new Vector2(textboxWidth, textboxHeight);

        textboxBg = panel.AddComponent<Image>();
        textboxBg.color = textboxColor;
        textboxBg.raycastTarget = true;

        // simple outline so the textbox reads clearly against the game
        textboxOutline = panel.AddComponent<Outline>();
        textboxOutline.effectColor = textboxBorderColor;
        textboxOutline.effectDistance = new Vector2(2f, -2f);

        // message text fills the panel with padding, leaving room for the Next button at the bottom
        var msgObj = new GameObject("Message");
        msgObj.transform.SetParent(panel.transform, false);
        var msgRect = msgObj.AddComponent<RectTransform>();
        msgRect.anchorMin = Vector2.zero;
        msgRect.anchorMax = Vector2.one;
        msgRect.offsetMin = new Vector2(24f, 64f);
        msgRect.offsetMax = new Vector2(-24f, -20f);
        messageText = msgObj.AddComponent<TextMeshProUGUI>();
        messageText.fontSize = messageFontSize;
        messageText.color = textColor;
        messageText.alignment = TextAlignmentOptions.TopLeft;
        messageText.enableWordWrapping = true;

        // next button pinned to bottom-right of the textbox
        var btnObj = new GameObject("NextButton");
        btnObj.transform.SetParent(panel.transform, false);
        nextButtonRect = btnObj.AddComponent<RectTransform>();
        nextButtonRect.anchorMin = new Vector2(1f, 0f);
        nextButtonRect.anchorMax = new Vector2(1f, 0f);
        nextButtonRect.pivot = new Vector2(1f, 0f);
        nextButtonRect.anchoredPosition = new Vector2(-16f, 14f);
        nextButtonRect.sizeDelta = nextButtonSize;

        nextButtonImage = btnObj.AddComponent<Image>();
        nextButtonImage.color = nextButtonColor;

        nextButton = btnObj.AddComponent<Button>();
        nextButton.targetGraphic = nextButtonImage;
        nextButton.onClick.AddListener(OnNext);

        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        nextButtonLabelText = btnTextObj.AddComponent<TextMeshProUGUI>();
        nextButtonLabelText.text = nextButtonLabel;
        nextButtonLabelText.fontSize = nextButtonFontSize;
        nextButtonLabelText.alignment = TextAlignmentOptions.Center;
        nextButtonLabelText.color = nextButtonTextColor;
    }

    void ShowStep(int index)
    {
        // empty step list means nothing to show — jump straight to farewell
        if (steps == null || steps.Count == 0)
        {
            EndTutorial();
            return;
        }

        if (index < 0) index = 0;
        if (index >= steps.Count)
        {
            EndTutorial();
            return;
        }

        currentStep = index;
        var step = steps[index];
        if (messageText != null)
            messageText.text = step.mascotText;

        // show the Next button when there's no action to wait on, or when the step opts in
        // via showNextButton so the player can advance past open-ended prompts (like orbit)
        if (nextButton != null)
            nextButton.gameObject.SetActive(step.requiredAction == TutorialAction.None || step.showNextButton);

        // glide mascot and textbox to this step's target positions
        Vector2 mascotTarget = ResolveMascotTarget(step);
        if (!mascotSlideInitialized)
        {
            // first step — start offscreen so the mascot slides in with the fade
            mascotSlideFrom = new Vector2(mascotTarget.x - 600f, mascotTarget.y);
            mascotSlideInitialized = true;
        }
        else
        {
            // snapshot wherever the mascot currently sits so the new slide starts from there
            mascotSlideFrom = Vector2.Lerp(mascotSlideFrom, mascotSlideTo, EaseOutCubic(mascotSlideT));
        }
        mascotSlideTo = mascotTarget;
        mascotSlideT = 0f;

        Vector2 textboxTarget = ResolveTextboxTarget(step);
        if (!textboxSlideInitialized)
        {
            // first step — textbox simply appears at the target, no slide-in
            textboxSlideFrom = textboxTarget;
            textboxSlideInitialized = true;
        }
        else
        {
            // snapshot current textbox position so the new slide starts from there
            textboxSlideFrom = Vector2.Lerp(textboxSlideFrom, textboxSlideTo, EaseOutCubic(textboxSlideT));
        }
        textboxSlideTo = textboxTarget;
        textboxSlideT = 0f;

        ApplyScriptedHighlight(index);
        RebuildCustomHighlights(step);
    }

    // tears down any existing highlight boxes and rebuilds container + 4 edges per box.
    // position / size / color / thickness are reapplied live every frame by UpdateCustomHighlights,
    // so this only needs to run when the step's highlight list count changes (or on step change)
    void RebuildCustomHighlights(TutorialStep step)
    {
        if (customHighlightContainer != null)
        {
            Destroy(customHighlightContainer);
            customHighlightContainer = null;
        }
        customHighlightBoxRects.Clear();
        customHighlightBoxEdges.Clear();

        if (step == null || !step.showCustomHighlights || step.customHighlights == null || step.customHighlights.Count == 0) return;
        if (root == null) return;

        customHighlightContainer = new GameObject("CustomHighlights");
        customHighlightContainer.transform.SetParent(root.transform, false);
        var contRect = customHighlightContainer.AddComponent<RectTransform>();
        contRect.anchorMin = Vector2.zero;
        contRect.anchorMax = Vector2.one;
        contRect.offsetMin = Vector2.zero;
        contRect.offsetMax = Vector2.zero;

        for (int i = 0; i < step.customHighlights.Count; i++)
        {
            var boxObj = new GameObject("HighlightBox");
            boxObj.transform.SetParent(customHighlightContainer.transform, false);
            var boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            customHighlightBoxRects.Add(boxRect);

            // four hollow-border edges — just build the objects; offsets/colors get set in UpdateCustomHighlights
            var edges = new Image[4];
            edges[0] = BuildHighlightEdge(boxObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f)); // top
            edges[1] = BuildHighlightEdge(boxObj.transform, new Vector2(0f, 0f), new Vector2(1f, 0f)); // bottom
            edges[2] = BuildHighlightEdge(boxObj.transform, new Vector2(0f, 0f), new Vector2(0f, 1f)); // left
            edges[3] = BuildHighlightEdge(boxObj.transform, new Vector2(1f, 0f), new Vector2(1f, 1f)); // right
            customHighlightBoxEdges.Add(edges);
        }
    }

    Image BuildHighlightEdge(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject("Edge");
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        var img = obj.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    // runs every frame — reads the current step's highlight boxes and pushes position, size,
    // border thickness, color, and pulsing alpha onto the existing edge images. rebuilds only
    // when the list count changes (e.g. inspector add/remove). this is what makes tuning live
    void UpdateCustomHighlights()
    {
        var step = CurrentStepData();
        bool wantActive = step != null && step.showCustomHighlights && step.customHighlights != null && step.customHighlights.Count > 0;

        if (!wantActive)
        {
            if (customHighlightContainer != null)
            {
                Destroy(customHighlightContainer);
                customHighlightContainer = null;
                customHighlightBoxRects.Clear();
                customHighlightBoxEdges.Clear();
            }
            return;
        }

        // list count changed in the inspector — rebuild the edge objects once, then fall through
        if (customHighlightBoxRects.Count != step.customHighlights.Count)
        {
            RebuildCustomHighlights(step);
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * globalHighlightPulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(globalHighlightMinAlpha, globalHighlightMaxAlpha, pulse);
        Color col = new Color(globalHighlightColor.r, globalHighlightColor.g, globalHighlightColor.b, alpha);
        float t = globalHighlightBorderThickness;

        Vector2 hOffMin = new Vector2(0f, -t);
        Vector2 hOffMax = new Vector2(0f, t);
        Vector2 vOffMin = new Vector2(-t, 0f);
        Vector2 vOffMax = new Vector2(t, 0f);

        for (int i = 0; i < step.customHighlights.Count && i < customHighlightBoxRects.Count; i++)
        {
            var h = step.customHighlights[i];
            var boxRect = customHighlightBoxRects[i];
            if (h == null || boxRect == null) continue;

            // live position + size from the inspector
            boxRect.anchoredPosition = h.position;
            boxRect.sizeDelta = h.size;

            var edges = customHighlightBoxEdges[i];
            if (edges == null) continue;

            // top + bottom use horizontal anchors, left + right use vertical anchors
            ApplyEdge(edges[0], hOffMin, hOffMax, col);
            ApplyEdge(edges[1], hOffMin, hOffMax, col);
            ApplyEdge(edges[2], vOffMin, vOffMax, col);
            ApplyEdge(edges[3], vOffMin, vOffMax, col);
        }
    }

    void ApplyEdge(Image edge, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        if (edge == null) return;
        edge.rectTransform.offsetMin = offsetMin;
        edge.rectTransform.offsetMax = offsetMax;
        edge.color = color;
    }

    // highlights are driven by the current step's requiredAction:
    //   SelectRegion -> pulse the region matching regionHighlightTrait
    //   SelectCard / PlayCard -> glow the first card in hand (the textbox always describes that card)
    // anything else clears both highlights
    void ApplyScriptedHighlight(int stepIdx)
    {
        if (steps == null || stepIdx < 0 || stepIdx >= steps.Count) return;
        var action = steps[stepIdx].requiredAction;

        var rm = FindFirstObjectByType<RegionManager>();
        if (rm != null)
        {
            if (action == TutorialAction.SelectRegion)
                rm.SetTutorialTargetRegion(rm.FindFirstRegionByTrait(regionHighlightTrait));
            else
                rm.ClearTutorialTargetRegion();
        }

        HighlightedCardIndex = (action == TutorialAction.SelectCard || action == TutorialAction.PlayCard) ? 0 : -1;
        HighlightedRewardIndex = (action == TutorialAction.PickReward) ? scriptedRewardHighlightIndex : -1;

        // auto-glow the matching side-tab button for step actions that happen via the side tabs
        TabAction tabTarget = SideTabForAction(action);
        if (tabTarget != TabAction.None)
            AttachSideTabGlow(tabTarget);
        else
            DetachSideTabGlow();
    }

    // maps a tutorial action to the side-tab button that triggers it, so the tutorial can
    // auto-highlight the right tab without each step needing a manual custom highlight box
    static TabAction SideTabForAction(TutorialAction action)
    {
        switch (action)
        {
            case TutorialAction.SkipRound: return TabAction.SkipRound;
            // extend this with Shop / Dashboard if we want those tabs auto-highlighted too
            default: return TabAction.None;
        }
    }

    void AttachSideTabGlow(TabAction target)
    {
        // already attached to the right tab? nothing to do — the update loop keeps it live
        if (sideTabGlow != null && sideTabGlowImage != null && sideTabGlow.transform.parent != null)
        {
            var existing = sideTabGlow.transform.parent.GetComponent<SideTabButton>();
            if (existing != null && existing.Action == target) return;
        }

        DetachSideTabGlow();

        SideTabButton match = null;
        var tabs = FindObjectsByType<SideTabButton>(FindObjectsSortMode.None);
        foreach (var tab in tabs)
        {
            if (tab != null && tab.Action == target) { match = tab; break; }
        }
        if (match == null) return;

        sideTabGlow = new GameObject("TutorialGlow");
        sideTabGlow.transform.SetParent(match.transform, false);
        sideTabGlow.transform.SetAsFirstSibling();
        var rect = sideTabGlow.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        sideTabGlowImage = sideTabGlow.AddComponent<Image>();
        sideTabGlowImage.raycastTarget = false;
    }

    void DetachSideTabGlow()
    {
        if (sideTabGlow != null) Destroy(sideTabGlow);
        sideTabGlow = null;
        sideTabGlowImage = null;
    }

    // keeps the side-tab glow in sync with Global Highlight Style every frame (padding, color, pulse)
    void UpdateSideTabGlow()
    {
        if (sideTabGlowImage == null) return;

        float pad = globalHighlightPadding;
        sideTabGlowImage.rectTransform.offsetMin = new Vector2(-pad, -pad);
        sideTabGlowImage.rectTransform.offsetMax = new Vector2(pad, pad);

        float pulse = (Mathf.Sin(Time.unscaledTime * globalHighlightPulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(globalHighlightMinAlpha, globalHighlightMaxAlpha, pulse);
        sideTabGlowImage.color = new Color(globalHighlightColor.r, globalHighlightColor.g, globalHighlightColor.b, alpha);
    }

    void OnNext()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);

        int next = currentStep + 1;
        if (next >= steps.Count)
            EndTutorial();
        else
            ShowStep(next);
    }

    // gameplay scripts call this before accepting an input during the tutorial
    // returns true when the action matches what the current step is asking for, or when the step allows free interaction
    // also returns true when the tutorial isn't active, so normal play is unaffected
    public static bool CanPerformAction(TutorialAction action)
    {
        if (!IsActive || instance == null) return true;
        return instance.CurrentStepAllows(action);
    }

    // gameplay scripts read this to know how hard the player must drag before it counts as an orbit
    // returns a safe default when the tutorial isn't loaded, so normal play is unaffected
    public static float OrbitDragThresholdSqr
    {
        get
        {
            if (instance == null) return 1600f;
            return instance.orbitDragThresholdSqr;
        }
    }

    // HandDisplay reads this every frame to know which card slot in the hand to glow
    // -1 = no card highlighted
    public static int HighlightedCardIndex { get; private set; } = -1;

    // RewardDisplay reads this to glow the reward card the tutorial is pointing at. -1 = none
    public static int HighlightedRewardIndex { get; private set; } = -1;

    // single source of truth for every tutorial highlight style — read by HandDisplay, RewardDisplay,
    // SkipConfirmPopup, RegionManager and the custom highlight boxes. defaults kick in when the
    // tutorial isn't loaded (normal play), so the consuming scripts still have safe values to use
    public static Color GlobalHighlightColor => instance != null ? instance.globalHighlightColor : new Color(1f, 0.95f, 0.3f, 1f);
    public static float GlobalHighlightMinAlpha => instance != null ? instance.globalHighlightMinAlpha : 0.3f;
    public static float GlobalHighlightMaxAlpha => instance != null ? instance.globalHighlightMaxAlpha : 1f;
    public static float GlobalHighlightPulseSpeed => instance != null ? instance.globalHighlightPulseSpeed : 4f;
    public static float GlobalHighlightPadding => instance != null ? instance.globalHighlightPadding : 14f;
    public static float GlobalHighlightBorderThickness => instance != null ? instance.globalHighlightBorderThickness : 4f;

    // GameManager reads this to decide whether to force a reward popup after the skip-round step
    public static bool ForceRewardAfterSkipRound
    {
        get
        {
            if (!IsActive || instance == null) return false;
            return instance.forceRewardAfterSkipRound;
        }
    }

    // GameManager calls this when building the reward popup. returns null when the tutorial isn't active or no cards were configured
    public static List<PolicyData> GetScriptedRewardChoices()
    {
        if (!IsActive || instance == null) return null;
        if (instance.scriptedRewardChoices == null || instance.scriptedRewardChoices.Count == 0) return null;
        return instance.scriptedRewardChoices;
    }

    // gameplay scripts read this to branch behavior by the current step's required action
    // (e.g. skipping a round during the SkipRound tutorial step shouldn't trigger the wasteful penalty)
    public static TutorialAction CurrentStepAction
    {
        get
        {
            if (!IsActive || instance == null) return TutorialAction.None;
            var step = instance.CurrentStepData();
            return step != null ? step.requiredAction : TutorialAction.None;
        }
    }

    // GameManager calls this when building round 1's hand. returns null when the tutorial isn't active or no hand was configured
    public static List<PolicyData> GetScriptedStartingHand()
    {
        if (!IsActive || instance == null) return null;
        if (instance.scriptedStartingHand == null || instance.scriptedStartingHand.Count == 0) return null;
        return instance.scriptedStartingHand;
    }

    bool CurrentStepAllows(TutorialAction action)
    {
        if (ending) return false;
        if (steps == null || currentStep < 0 || currentStep >= steps.Count) return false;

        // camera framing is always allowed — orbit and zoom don't affect gameplay decisions,
        // so the player can freely reposition the view during any action-gated step
        if (action == TutorialAction.OrbitEarth || action == TutorialAction.ZoomEarth) return true;

        var step = steps[currentStep];
        if (step.allowFreeInteraction) return true;
        return step.requiredAction == action;
    }

    // gameplay scripts call this after a successful action so the tutorial can advance
    // no-op when the tutorial isn't active, when the action doesn't match, or when the tutorial has already ended
    public static void NotifyAction(TutorialAction action)
    {
        if (!IsActive || instance == null) return;
        instance.HandleAction(action);
    }

    void HandleAction(TutorialAction action)
    {
        if (ending) return;
        if (steps == null || currentStep < 0 || currentStep >= steps.Count) return;

        var step = steps[currentStep];
        if (step.requiredAction != action) return;

        // when the step exposes a Next button, the player advances by clicking Next, not by performing the action
        // this prevents accidental skips on open-ended prompts like "orbit the earth"
        if (step.showNextButton) return;

        // SkipRound triggers a multi-stage sequence (confirm popup -> event banner -> EndRound -> StartRound).
        // hide the tutorial UI and wait for GameManager to tell us the next round has actually started,
        // so the mascot doesn't float on top of the event banner or show the next step's text prematurely
        if (action == TutorialAction.SkipRound)
        {
            waitingForRoundStart = true;
            if (root != null) root.SetActive(false);
            // the side-tab glow is parented under the Skip button (outside root), so hiding root
            // doesn't hide it. drop the glow the moment the player clicks so it doesn't linger through the popup
            DetachSideTabGlow();
            return;
        }

        // action matched — advance to the next step, or end if we're at the last one
        int next = currentStep + 1;
        if (next >= steps.Count)
            EndTutorial();
        else
            ShowStep(next);
    }

    // GameManager calls this at the end of StartRound. if the tutorial is waiting after a scripted
    // SkipRound, this is the signal to unhide the mascot and advance to the next step
    public static void NotifyRoundStarted()
    {
        if (!IsActive || instance == null) return;
        if (!instance.waitingForRoundStart) return;
        instance.waitingForRoundStart = false;
        if (instance.root != null) instance.root.SetActive(true);
        int next = instance.currentStep + 1;
        if (next >= instance.steps.Count)
            instance.EndTutorial();
        else
            instance.ShowStep(next);
    }

    TutorialStep CurrentStepData()
    {
        if (steps == null || currentStep < 0 || currentStep >= steps.Count) return null;
        return steps[currentStep];
    }

    Vector2 ResolveMascotTarget(TutorialStep step)
    {
        if (step != null && step.overrideMascotPosition) return step.mascotPosition;
        return new Vector2(mascotOffsetX, mascotOffsetY);
    }

    Vector2 ResolveTextboxTarget(TutorialStep step)
    {
        if (step != null && step.overrideTextboxPosition) return step.textboxPositionStep;
        return textboxPosition;
    }

    static float EaseOutCubic(float t)
    {
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    void EndTutorial()
    {
        if (messageText != null)
            messageText.text = farewellMessage;

        // hide the Next button during the farewell
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        // drop any active highlight so the farewell moment reads cleanly
        var rm = FindFirstObjectByType<RegionManager>();
        if (rm != null) rm.ClearTutorialTargetRegion();
        HighlightedCardIndex = -1;
        HighlightedRewardIndex = -1;
        DetachSideTabGlow();

        ending = true;
        endTime = Time.unscaledTime;
    }
}
