using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HandDisplay : MonoBehaviour
{
    [Header("Card Appearance")]
    [SerializeField] private Color cardBackground = new Color(0.12f, 0.12f, 0.16f, 0.95f);
    [SerializeField] private Color cardBorder = new Color(0.3f, 0.3f, 0.4f, 1f);
    [SerializeField] private Color commonColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color uncommonColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color rareColor = new Color(0.9f, 0.7f, 0.1f);

    [Header("Stat Colors")]
    [SerializeField] private Color carbonColor = new Color(0.75f, 0.45f, 0.3f);
    [SerializeField] private Color economyColor = new Color(0.85f, 0.75f, 0.3f);
    [SerializeField] private Color stabilityColor = new Color(0.4f, 0.6f, 0.85f);

    private GameManager gameManager;
    private RegionManager regionManager;
    private Canvas canvas;
    private RectTransform cardContainer;
    private List<GameObject> cardObjects = new List<GameObject>();

    // tracks what we last rendered so we only rebuild when hand changes
    private int lastHandCount = -1;
    private int lastRound = -1;

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }

        if (regionManager == null)
            regionManager = FindFirstObjectByType<RegionManager>();

        if (canvas == null)
            BuildCanvas();

        // rebuild cards when hand changes
        var hand = gameManager.CurrentHand;
        int handCount = hand != null ? hand.Count : 0;
        int round = gameManager.CurrentRound;

        if (handCount != lastHandCount || round != lastRound)
        {
            lastHandCount = handCount;
            lastRound = round;
            RebuildCards();
        }

        // highlight the card matching hovered number key
        UpdateCardHighlights();
    }

    void BuildCanvas()
    {
        // create a screen-space overlay canvas for the cards
        var canvasObj = new GameObject("HandCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // container anchored to bottom center
        var containerObj = new GameObject("CardContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        cardContainer = containerObj.AddComponent<RectTransform>();
        cardContainer.anchorMin = new Vector2(0.5f, 0f);
        cardContainer.anchorMax = new Vector2(0.5f, 0f);
        cardContainer.pivot = new Vector2(0.5f, 0f);
        cardContainer.anchoredPosition = new Vector2(0f, 20f);
        cardContainer.sizeDelta = new Vector2(660f, 280f);

        var layout = containerObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(10, 10, 0, 0);
    }

    void RebuildCards()
    {
        // clear old cards
        foreach (var obj in cardObjects)
        {
            if (obj != null) Destroy(obj);
        }
        cardObjects.Clear();

        if (gameManager.CurrentHand == null) return;

        for (int i = 0; i < gameManager.CurrentHand.Count; i++)
        {
            var card = gameManager.CurrentHand[i];
            var cardObj = BuildCard(card, i);
            cardObj.transform.SetParent(cardContainer, false);
            cardObjects.Add(cardObj);
        }
    }

    GameObject BuildCard(PolicyData policy, int index)
    {
        // card root with background
        var card = new GameObject($"Card_{index}");
        var cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(190f, 260f);

        var layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 190f;
        layoutElement.preferredHeight = 260f;

        // border background
        var borderImg = card.AddComponent<Image>();
        Color rarityCol = GetRarityColor(policy.rarity);
        borderImg.color = rarityCol;

        // inner background panel (slightly inset to show border)
        var inner = CreateChild(card, "Inner", new Vector2(0, 0), new Vector2(-4f, -4f));
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = cardBackground;

        // -- stats section at top --
        string carbonHex = ColorUtility.ToHtmlStringRGB(carbonColor);
        string economyHex = ColorUtility.ToHtmlStringRGB(economyColor);
        string stabilityHex = ColorUtility.ToHtmlStringRGB(stabilityColor);

        string carbonSign = policy.carbonDelta >= 0 ? "+" : "";
        string econSign = policy.economyDelta >= 0 ? "+" : "";
        string stabSign = policy.stabilityDelta >= 0 ? "+" : "";

        string statsString = $"<color=#{carbonHex}>Carbon: {carbonSign}{policy.carbonDelta:0}</color>\n"
            + $"<color=#{economyHex}>Economy: {econSign}{policy.economyDelta:0}</color>\n"
            + $"<color=#{stabilityHex}>Stability: {stabSign}{policy.stabilityDelta:0}</color>";

        var statsArea = CreateChild(inner, "Stats",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(-12f, 52f));
        var statsText = CreateText(statsArea, statsString, 12, TextAlignmentOptions.TopLeft, Color.white);
        statsText.richText = true;
        statsText.lineSpacing = -4f;
        StretchFill(statsText.gameObject, 6f);

        // -- key hint (top-right corner) --
        var keyHint = CreateChild(inner, "KeyHint",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-4f, -4f), new Vector2(28f, 24f));
        var keyBg = keyHint.AddComponent<Image>();
        keyBg.color = new Color(0f, 0f, 0f, 0.5f);
        var keyText = CreateText(keyHint, $"{index + 1}", 14, TextAlignmentOptions.Center, Color.white);
        StretchFill(keyText);

        // -- title --
        var title = CreateChild(inner, "Title",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -62f), new Vector2(-12f, 30f));
        var titleText = CreateText(title, policy.policyName, 16, TextAlignmentOptions.Center, Color.white);
        titleText.fontStyle = FontStyles.Bold;
        StretchFill(titleText.gameObject);

        // -- icon area --
        var iconArea = CreateChild(inner, "IconArea",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(-24f, 80f));
        var iconBg = iconArea.AddComponent<Image>();
        iconBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

        if (policy.icon != null)
        {
            var iconImg = CreateChild(iconArea, "Icon", Vector2.zero, Vector2.zero);
            var img = iconImg.AddComponent<Image>();
            img.sprite = policy.icon;
            img.preserveAspect = true;
            StretchFill(iconImg, 8f);
        }
        else
        {
            // placeholder rarity label when no icon
            var placeholder = CreateText(iconArea, policy.rarity.ToString(), 13, TextAlignmentOptions.Center, rarityCol);
            placeholder.fontStyle = FontStyles.Italic;
            StretchFill(placeholder.gameObject);
        }

        // -- description --
        var descArea = CreateChild(inner, "Desc",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0f),
            new Vector2(0f, 6f), new Vector2(-12f, -180f));
        var descText = CreateText(descArea, policy.description, 11, TextAlignmentOptions.TopLeft, new Color(0.75f, 0.75f, 0.8f));
        descText.enableWordWrapping = true;
        descText.overflowMode = TextOverflowModes.Truncate;
        StretchFill(descText.gameObject);

        return card;
    }

    void UpdateCardHighlights()
    {
        // subtle scale bump on the card matching a pressed number key
        for (int i = 0; i < cardObjects.Count; i++)
        {
            if (cardObjects[i] == null) continue;
            var rect = cardObjects[i].GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
        }
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

    // helper to create a child rectTransform with anchors
    GameObject CreateChild(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
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

    // simpler overload for stretch children
    GameObject CreateChild(GameObject parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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

    void StretchFill(TMP_Text text)
    {
        StretchFill(text.gameObject);
    }
}
