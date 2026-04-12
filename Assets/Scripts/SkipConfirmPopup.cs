using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SkipConfirmPopup : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color panelColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
    [SerializeField] private Color panelBorderColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    [SerializeField] private Color warningColor = new Color(0.9f, 0.4f, 0.4f);
    [SerializeField] private Color safeColor = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color confirmColor = new Color(0.8f, 0.3f, 0.3f);
    [SerializeField] private Color cancelColor = new Color(0.4f, 0.5f, 0.6f);

    [Header("Font Sizes")]
    [SerializeField] private float titleFontSize = 28f;
    [SerializeField] private float bodyFontSize = 20f;
    [SerializeField] private float buttonFontSize = 22f;

    [Header("Panel Size")]
    [SerializeField] private float panelWidth = 500f;
    [SerializeField] private float panelHeight = 300f;
    [SerializeField] private float borderWidth = 3f;
    [SerializeField] private float buttonWidth = 160f;
    [SerializeField] private float buttonHeight = 50f;
    [SerializeField] private float buttonSpacing = 30f;

    private GameManager gameManager;
    private GameObject root;
    private TMP_Text titleText;
    private TMP_Text bodyText;
    private bool showing;

    public bool IsShowing => showing;

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }

        // close on ESC
        if (showing && UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Hide();
        }
    }

    public void Show(bool isWasteful, int cardsLeft, float penaltyAmount, float stabPenalty, int streak)
    {
        if (showing) return;
        showing = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.panelOpen);

        BuildUI(isWasteful, cardsLeft, penaltyAmount, stabPenalty, streak);
    }

    public void Hide()
    {
        if (!showing) return;
        showing = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.panelClose);

        if (root != null) Destroy(root);
        root = null;
    }

    void BuildUI(bool isWasteful, int cardsLeft, float penaltyAmount, float stabPenalty, int streak)
    {
        if (root != null) Destroy(root);

        // canvas
        root = new GameObject("SkipConfirmCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // dark backdrop
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(root.transform, false);
        var bdRect = backdrop.AddComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero;
        bdRect.anchorMax = Vector2.one;
        bdRect.sizeDelta = Vector2.zero;
        var bdImg = backdrop.AddComponent<Image>();
        bdImg.color = backdropColor;
        bdImg.raycastTarget = true;

        // panel border (slightly larger panel behind)
        var border = new GameObject("Border");
        border.transform.SetParent(root.transform, false);
        var borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(panelWidth + borderWidth * 2, panelHeight + borderWidth * 2);
        var borderImg = border.AddComponent<Image>();
        borderImg.color = panelBorderColor;

        // main panel
        var panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = panelColor;

        // title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(-40f, 40f);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = isWasteful ? "WASTEFUL SKIP" : "SKIP ROUND";
        titleText.fontSize = titleFontSize;
        titleText.color = isWasteful ? warningColor : Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        // body
        var bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(panel.transform, false);
        var bodyRect = bodyObj.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(30f, 80f);
        bodyRect.offsetMax = new Vector2(-30f, -70f);
        bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = bodyFontSize;
        bodyText.color = Color.white;
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.enableWordWrapping = true;

        if (isWasteful)
        {
            string streakNote = streak > 0 ? $"\n<color=#{ColorUtility.ToHtmlStringRGB(warningColor)}>Consecutive skip streak: {streak + 1}x</color>" : "";
            bodyText.text = $"You still have {cardsLeft} playable card{(cardsLeft > 1 ? "s" : "")}!\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(warningColor)}>+{penaltyAmount:F0} carbon, -{stabPenalty:F0} stability to ALL regions</color>\n" +
                $"Trade-off: random bonus capital next round{streakNote}";
        }
        else
        {
            bodyText.text = $"No playable cards remaining.\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(safeColor)}>No penalty — safe to skip.</color>";
        }

        // buttons
        float totalWidth = buttonWidth * 2 + buttonSpacing;
        float startX = -totalWidth / 2f + buttonWidth / 2f;

        // cancel button (go back)
        CreateButton(panel.transform, "Cancel", "Go Back", cancelColor, startX,
            () => Hide());

        // confirm button (skip)
        CreateButton(panel.transform, "Confirm", isWasteful ? "Skip Anyway" : "Skip Round", confirmColor, startX + buttonWidth + buttonSpacing,
            () => { Hide(); if (gameManager != null) gameManager.DoSkipRound(); });
    }

    void CreateButton(Transform parent, string name, string label, Color color, float xPos, System.Action onClick)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(xPos, 15f);
        btnRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = color;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() => onClick());

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = buttonFontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }
}
