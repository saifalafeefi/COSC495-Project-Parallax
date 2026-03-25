using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RewardDisplay : MonoBehaviour
{
    [Header("Card Appearance")]
    [SerializeField] private Color cardBackground = new Color(0.12f, 0.12f, 0.16f, 0.95f);
    [SerializeField] private Color commonColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color uncommonColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color rareColor = new Color(0.9f, 0.7f, 0.1f);
    [SerializeField] private Color selectedGlow = new Color(1f, 0.9f, 0.4f, 1f);

    [Header("Stat Colors")]
    [SerializeField] private Color carbonColor = new Color(0.75f, 0.45f, 0.3f);
    [SerializeField] private Color economyColor = new Color(0.85f, 0.75f, 0.3f);
    [SerializeField] private Color stabilityColor = new Color(0.4f, 0.6f, 0.85f);

    [Header("Layout")]
    [SerializeField] private float cardWidth = 200f;
    [SerializeField] private float cardHeight = 280f;
    [SerializeField] private float cardSpacing = 40f;

    [Header("Tween Settings")]
    [SerializeField] private float hoverLift = 20f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float selectedScale = 1.12f;
    [SerializeField] private float tweenSpeed = 10f;
    [SerializeField] private float bounceSpeed = 3f;
    [SerializeField] private float bounceAmount = 6f;
    [SerializeField] private float selectedColorSpeed = 2.5f;
    [SerializeField] private float dealDuration = 0.4f;
    [SerializeField] private float dealStagger = 0.1f;

    private GameManager gameManager;
    private GameObject root;

    private List<GameObject> cardObjects = new List<GameObject>();
    private List<RectTransform> cardRects = new List<RectTransform>();
    private List<Image> cardBorders = new List<Image>();
    private List<Color> cardRarityColors = new List<Color>();

    private int hoveredIndex = -1;
    private int selectedIndex = -1;
    private bool showing;
    private float showTime;

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }

        if (gameManager.RewardActive && !showing)
            ShowReward();
        else if (!gameManager.RewardActive && showing)
            HideReward();

        if (showing)
            AnimateCards();
    }

    void ShowReward()
    {
        showing = true;
        showTime = Time.time;
        selectedIndex = -1;
        hoveredIndex = -1;

        // root canvas above everything
        root = new GameObject("RewardUI");
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // dark backdrop that blocks clicks to the game
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(root.transform, false);
        var bdRect = backdrop.AddComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero;
        bdRect.anchorMax = Vector2.one;
        bdRect.offsetMin = Vector2.zero;
        bdRect.offsetMax = Vector2.zero;
        var bdImg = backdrop.AddComponent<Image>();
        bdImg.color = new Color(0f, 0f, 0f, 0.7f);
        bdImg.raycastTarget = true;

        // title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(root.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 210f);
        titleRect.sizeDelta = new Vector2(600f, 50f);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Choose a Reward Card";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.92f, 0.5f);
        titleText.fontStyle = FontStyles.Bold;

        // subtitle
        var subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(root.transform, false);
        var subRect = subObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.5f);
        subRect.anchorMax = new Vector2(0.5f, 0.5f);
        subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.anchoredPosition = new Vector2(0f, 180f);
        subRect.sizeDelta = new Vector2(600f, 30f);
        var subText = subObj.AddComponent<TextMeshProUGUI>();
        subText.text = "Global carbon improved last round!";
        subText.fontSize = 15;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = new Color(0.7f, 0.8f, 0.7f);

        // card container
        var container = new GameObject("CardContainer");
        container.transform.SetParent(root.transform, false);
        var contRect = container.AddComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.5f, 0.5f);
        contRect.anchorMax = new Vector2(0.5f, 0.5f);
        contRect.pivot = new Vector2(0.5f, 0.5f);
        contRect.anchoredPosition = Vector2.zero;
        contRect.sizeDelta = new Vector2(900f, 400f);

        // build 3 reward cards
        cardObjects.Clear();
        cardRects.Clear();
        cardBorders.Clear();
        cardRarityColors.Clear();

        for (int i = 0; i < gameManager.RewardChoices.Count; i++)
        {
            var card = BuildCard(gameManager.RewardChoices[i], i);
            card.transform.SetParent(container.transform, false);

            var rect = card.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.zero;

            cardObjects.Add(card);
            cardRects.Add(rect);
        }

        // skip button
        BuildSkipButton(root.transform);
    }

    void HideReward()
    {
        showing = false;
        cardObjects.Clear();
        cardRects.Clear();
        cardBorders.Clear();
        cardRarityColors.Clear();
        hoveredIndex = -1;
        selectedIndex = -1;

        if (root != null)
        {
            Destroy(root);
            root = null;
        }
    }

    void AnimateCards()
    {
        int count = cardRects.Count;
        if (count == 0) return;

        float totalWidth = count * cardWidth + (count - 1) * cardSpacing;
        float startX = -totalWidth / 2f + cardWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            var rect = cardRects[i];
            if (rect == null) continue;

            float targetX = startX + i * (cardWidth + cardSpacing);
            float targetY = 0f;
            float targetScale = 1f;

            if (i == selectedIndex)
            {
                float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
                targetY = hoverLift + bounce;
                targetScale = selectedScale;
            }
            else if (i == hoveredIndex)
            {
                targetY = hoverLift;
                targetScale = hoverScale;
            }

            // deal-in animation: scale up from center with overshoot
            float elapsed = Time.time - (showTime + i * dealStagger);
            if (elapsed < 0f)
            {
                // hasn't started yet
                rect.localScale = Vector3.zero;
                rect.anchoredPosition = Vector2.zero;
            }
            else if (elapsed < dealDuration)
            {
                float raw = elapsed / dealDuration;
                // ease-out cubic
                float t = 1f - (1f - raw) * (1f - raw) * (1f - raw);

                float x = Mathf.Lerp(0f, targetX, t);
                float y = Mathf.Lerp(0f, targetY, t);
                rect.anchoredPosition = new Vector2(x, y);

                // scale with overshoot: goes slightly past 1.0 then settles
                float scaleT;
                if (raw < 0.7f)
                    scaleT = Mathf.Lerp(0f, targetScale * 1.08f, raw / 0.7f);
                else
                    scaleT = Mathf.Lerp(targetScale * 1.08f, targetScale, (raw - 0.7f) / 0.3f);
                rect.localScale = new Vector3(scaleT, scaleT, 1f);
            }
            else
            {
                // normal tweening after deal
                float t = Time.deltaTime * tweenSpeed;
                var pos = rect.anchoredPosition;
                pos.x = Mathf.Lerp(pos.x, targetX, t);
                pos.y = Mathf.Lerp(pos.y, targetY, t);
                rect.anchoredPosition = pos;

                float curScale = rect.localScale.x;
                float newScale = Mathf.Lerp(curScale, targetScale, t);
                rect.localScale = new Vector3(newScale, newScale, 1f);
            }

            // border glow on selected card
            if (i < cardBorders.Count && cardBorders[i] != null)
            {
                if (i == selectedIndex)
                {
                    float pulse = (Mathf.Sin(Time.time * selectedColorSpeed) + 1f) / 2f;
                    cardBorders[i].color = Color.Lerp(cardRarityColors[i], selectedGlow, pulse);
                }
                else
                {
                    float ct = Time.deltaTime * tweenSpeed;
                    cardBorders[i].color = Color.Lerp(cardBorders[i].color, cardRarityColors[i], ct);
                }
            }

            // z-ordering: hovered on top
            if (i == hoveredIndex)
                rect.SetAsLastSibling();
            else if (i == selectedIndex && hoveredIndex != selectedIndex)
                rect.transform.SetSiblingIndex(Mathf.Max(0, rect.parent.childCount - 2));
        }
    }

    public void OnCardHover(int index)
    {
        hoveredIndex = index;
    }

    public void OnCardUnhover(int index)
    {
        if (hoveredIndex == index)
            hoveredIndex = -1;
    }

    public void OnCardClick(int index)
    {
        if (selectedIndex == index)
        {
            // second click: confirm pick
            gameManager.ClaimReward(index);
        }
        else
        {
            // first click: select
            selectedIndex = index;
        }
    }

    void BuildSkipButton(Transform parent)
    {
        var btnObj = new GameObject("SkipButton");
        btnObj.transform.SetParent(parent, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0f, -200f);
        btnRect.sizeDelta = new Vector2(140f, 40f);

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() => gameManager.SkipReward());

        // hover color tint
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.45f, 0.45f, 0.5f);
        colors.pressedColor = new Color(0.25f, 0.25f, 0.3f);
        btn.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Skip";
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.8f, 0.8f, 0.8f);
    }

    // ---- card builder (mirrors HandDisplay style but shows base stats only) ----

    GameObject BuildCard(PolicyData policy, int index)
    {
        string cSign = policy.carbonDelta >= 0 ? "+" : "";
        string eSign = policy.economyDelta >= 0 ? "+" : "";
        string sSign = policy.stabilityDelta >= 0 ? "+" : "";

        string cHex = ColorUtility.ToHtmlStringRGB(carbonColor);
        string eHex = ColorUtility.ToHtmlStringRGB(economyColor);
        string sHex = ColorUtility.ToHtmlStringRGB(stabilityColor);

        string statsString = $"<color=#{cHex}>Carbon: {cSign}{policy.carbonDelta:0}</color>"
            + $"\n<color=#{eHex}>Economy: {eSign}{policy.economyDelta:0}</color>"
            + $"\n<color=#{sHex}>Stability: {sSign}{policy.stabilityDelta:0}</color>";

        // card root
        var card = new GameObject($"RewardCard_{index}");
        var cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        cardRect.pivot = new Vector2(0.5f, 0.5f);

        // border
        var borderImg = card.AddComponent<Image>();
        Color rarityCol = GetRarityColor(policy.rarity);
        borderImg.color = rarityCol;
        cardBorders.Add(borderImg);
        cardRarityColors.Add(rarityCol);

        // pointer events
        var handler = card.AddComponent<RewardCardHandler>();
        handler.cardIndex = index;
        handler.rewardDisplay = this;

        // inner background
        var inner = CreateStretchChild(card, "Inner", 2f);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = cardBackground;
        innerImg.raycastTarget = false;

        // stats at top
        var statsArea = CreateChild(inner, "Stats",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(-12f, 52f));
        var statsText = CreateText(statsArea, statsString, 11, TextAlignmentOptions.TopLeft, Color.white);
        statsText.richText = true;
        statsText.lineSpacing = -4f;
        StretchFill(statsText.gameObject, 6f);

        // title
        var titleArea = CreateChild(inner, "Title",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -62f), new Vector2(-12f, 30f));
        var titleText = CreateText(titleArea, policy.policyName, 15, TextAlignmentOptions.Center, Color.white);
        titleText.fontStyle = FontStyles.Bold;
        StretchFill(titleText.gameObject);

        // icon area
        var iconArea = CreateChild(inner, "IconArea",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(-24f, 80f));
        var iconBg = iconArea.AddComponent<Image>();
        iconBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        iconBg.raycastTarget = false;

        if (policy.icon != null)
        {
            var iconImg = CreateStretchChild(iconArea, "Icon", 8f);
            var img = iconImg.AddComponent<Image>();
            img.sprite = policy.icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        else
        {
            var placeholder = CreateText(iconArea, policy.rarity.ToString(), 13, TextAlignmentOptions.Center, rarityCol);
            placeholder.fontStyle = FontStyles.Italic;
            StretchFill(placeholder.gameObject);
        }

        // description
        var descArea = CreateChild(inner, "Desc",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0f),
            new Vector2(0f, 6f), new Vector2(-12f, -180f));
        var descText = CreateText(descArea, policy.description, 11, TextAlignmentOptions.TopLeft, new Color(0.75f, 0.75f, 0.8f));
        descText.enableWordWrapping = true;
        descText.overflowMode = TextOverflowModes.Truncate;
        StretchFill(descText.gameObject);

        return card;
    }

    Color GetRarityColor(PolicyRarity rarity)
    {
        switch (rarity)
        {
            case PolicyRarity.Common: return commonColor;
            case PolicyRarity.Uncommon: return uncommonColor;
            case PolicyRarity.Rare: return rareColor;
            default: return commonColor;
        }
    }

    // ---- UI helpers ----

    GameObject CreateChild(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return obj;
    }

    GameObject CreateStretchChild(GameObject parent, string name, float inset)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        return obj;
    }

    TMP_Text CreateText(GameObject parent, string content, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent.transform, false);
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    void StretchFill(GameObject obj, float inset = 0f)
    {
        var rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}

// pointer event handler for reward cards
public class RewardCardHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public int cardIndex;
    [HideInInspector] public RewardDisplay rewardDisplay;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rewardDisplay != null)
            rewardDisplay.OnCardHover(cardIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rewardDisplay != null)
            rewardDisplay.OnCardUnhover(cardIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (rewardDisplay == null) return;
        if (eventData.button == PointerEventData.InputButton.Left)
            rewardDisplay.OnCardClick(cardIndex);
    }
}
