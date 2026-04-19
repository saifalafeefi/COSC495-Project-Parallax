using System.Collections.Generic;
using UnityEngine;
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
    BuyCard,
    PickReward,
    EndRoundNaturally
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
    [SerializeField] private float messageFontSize = 24f;
    [SerializeField] private float nextButtonFontSize = 20f;
    [SerializeField] private Vector2 nextButtonSize = new Vector2(140f, 40f);
    [SerializeField] private string nextButtonLabel = "NEXT";

    [Header("Steps")]
    [Tooltip("the full ordered list of tutorial steps — edit in Inspector")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

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

    float mascotSlideT;
    int currentStep;
    bool ending;
    float endTime;

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
    }

    void Update()
    {
        // reapply inspector values every frame so tuning works live in play mode
        ApplyLiveSettings();

        UpdateMascot();

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

        // textbox panel
        if (textboxRect != null)
        {
            textboxRect.anchoredPosition = textboxPosition;
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

        // slide in from the left with ease-out cubic
        if (mascotSlideT < 1f)
        {
            mascotSlideT += Time.unscaledDeltaTime * mascotSlideSpeed;
            if (mascotSlideT > 1f) mascotSlideT = 1f;
        }
        float eased = 1f - (1f - mascotSlideT) * (1f - mascotSlideT) * (1f - mascotSlideT);
        float slideX = Mathf.Lerp(mascotOffsetX - 600f, mascotOffsetX, eased);

        // fade opacity in step with the slide
        if (mascotImage != null)
        {
            Color c = mascotImage.color;
            c.a = eased;
            mascotImage.color = c;
        }

        // gentle bob up and down
        float bob = Mathf.Sin(Time.unscaledTime * mascotBobSpeed) * mascotBobAmount;
        // sway tilt with a slightly different frequency so it doesn't sync with the bob
        float sway = Mathf.Sin(Time.unscaledTime * mascotSwaySpeed * 0.7f) * mascotSwayAngle;

        mascotRect.anchoredPosition = new Vector2(slideX, mascotOffsetY + bob);
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

    bool CurrentStepAllows(TutorialAction action)
    {
        if (ending) return false;
        if (steps == null || currentStep < 0 || currentStep >= steps.Count) return false;

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

        // action matched — advance to the next step, or end if we're at the last one
        int next = currentStep + 1;
        if (next >= steps.Count)
            EndTutorial();
        else
            ShowStep(next);
    }

    void EndTutorial()
    {
        if (messageText != null)
            messageText.text = farewellMessage;

        // hide the Next button during the farewell
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        ending = true;
        endTime = Time.unscaledTime;
    }
}
