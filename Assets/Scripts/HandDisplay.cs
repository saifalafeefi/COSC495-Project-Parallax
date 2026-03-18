using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HandDisplay : MonoBehaviour
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
    [SerializeField] private Color bonusColor = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color penaltyColor = new Color(0.9f, 0.4f, 0.4f);

    [Header("Tween Settings")]
    [SerializeField] private float cardWidth = 190f;
    [SerializeField] private float cardHeight = 260f;
    [SerializeField] private float cardSpacing = 16f;
    [SerializeField] private float restY = -140f;
    [SerializeField] private float hoverY = 10f;
    [SerializeField] private float selectedY = 20f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float selectedScale = 1.12f;
    [SerializeField] private float tweenSpeed = 12f;
    [SerializeField] private float bounceSpeed = 3f;
    [SerializeField] private float bounceAmount = 6f;
    [SerializeField] private float selectedColorSpeed = 2.5f;

    private GameManager gameManager;
    private RegionManager regionManager;
    private Canvas canvas;
    private RectTransform cardContainer;

    private List<GameObject> cardObjects = new List<GameObject>();
    private List<RectTransform> cardRects = new List<RectTransform>();
    private List<Image> cardBorders = new List<Image>();
    private List<Color> cardRarityColors = new List<Color>();

    private int hoveredIndex = -1;
    private int selectedIndex = -1;

    private int lastHandCount = -1;
    private int lastRound = -1;
    private Region lastSelectedRegion;

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

        // don't update cards while paused
        if (PauseMenu.IsPaused) return;

        var hand = gameManager.CurrentHand;
        int handCount = hand != null ? hand.Count : 0;
        int round = gameManager.CurrentRound;
        Region selectedRegion = regionManager != null ? regionManager.SelectedRegion : null;

        if (handCount != lastHandCount || round != lastRound || selectedRegion != lastSelectedRegion)
        {
            lastHandCount = handCount;
            lastRound = round;
            lastSelectedRegion = selectedRegion;

            // keep selected card valid
            if (selectedIndex >= handCount)
                selectedIndex = -1;

            RebuildCards();
        }

        AnimateCards();
    }

    void BuildCanvas()
    {
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

        // container anchored to bottom center, no layout group (manual positioning)
        var containerObj = new GameObject("CardContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        cardContainer = containerObj.AddComponent<RectTransform>();
        cardContainer.anchorMin = new Vector2(0.5f, 0f);
        cardContainer.anchorMax = new Vector2(0.5f, 0f);
        cardContainer.pivot = new Vector2(0.5f, 0f);
        cardContainer.anchoredPosition = Vector2.zero;
        cardContainer.sizeDelta = new Vector2(800f, 400f);
    }

    void RebuildCards()
    {
        foreach (var obj in cardObjects)
        {
            if (obj != null) Destroy(obj);
        }
        cardObjects.Clear();
        cardRects.Clear();
        cardBorders.Clear();
        cardRarityColors.Clear();

        if (gameManager.CurrentHand == null) return;

        for (int i = 0; i < gameManager.CurrentHand.Count; i++)
        {
            var card = gameManager.CurrentHand[i];
            var cardObj = BuildCard(card, i);
            cardObj.transform.SetParent(cardContainer, false);
            cardObjects.Add(cardObj);
            cardRects.Add(cardObj.GetComponent<RectTransform>());
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
            float targetY;
            float targetScale;

            if (i == selectedIndex)
            {
                // bounce animation on selected card
                float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
                targetY = selectedY + bounce;
                targetScale = selectedScale;
            }
            else if (i == hoveredIndex)
            {
                targetY = hoverY;
                targetScale = hoverScale;
            }
            else
            {
                targetY = restY;
                targetScale = 1f;
            }

            // lerp position and scale
            float t = Time.deltaTime * tweenSpeed;
            var pos = rect.anchoredPosition;
            pos.x = Mathf.Lerp(pos.x, targetX, t);
            pos.y = Mathf.Lerp(pos.y, targetY, t);
            rect.anchoredPosition = pos;

            var s = rect.localScale;
            float curScale = s.x;
            float newScale = Mathf.Lerp(curScale, targetScale, t);
            rect.localScale = new Vector3(newScale, newScale, 1f);

            // border color: tween between rarity color and glow when selected
            if (i < cardBorders.Count && cardBorders[i] != null)
            {
                if (i == selectedIndex)
                {
                    // pulse between rarity color and glow
                    float pulse = (Mathf.Sin(Time.time * selectedColorSpeed) + 1f) / 2f;
                    cardBorders[i].color = Color.Lerp(cardRarityColors[i], selectedGlow, pulse);
                }
                else
                {
                    cardBorders[i].color = Color.Lerp(cardBorders[i].color, cardRarityColors[i], t);
                }
            }

            // hovered card always on top, selected behind it
            if (i == hoveredIndex)
                rect.SetAsLastSibling();
            else if (i == selectedIndex && hoveredIndex != selectedIndex)
                rect.transform.SetSiblingIndex(rect.parent.childCount - 2);
        }
    }

    // called by CardInteraction component on each card
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
        if (PauseMenu.IsPaused) return;

        if (selectedIndex == index)
        {
            // second click on same card: confirm play
            TryPlayCard(index);
        }
        else
        {
            // first click: select this card
            selectedIndex = index;
        }
    }

    void TryPlayCard(int index)
    {
        if (gameManager == null || gameManager.GameOver) return;

        Region target = regionManager != null ? regionManager.SelectedRegion : null;
        if (target == null) return;

        string result = gameManager.PlayCard(index, target);

        // update the selector's last play result
        var selector = FindFirstObjectByType<RegionSelector>();
        if (selector != null)
        {
            selector.LastPlayResult = result;
            selector.LastPlayTime = Time.time;
        }

        selectedIndex = -1;
        hoveredIndex = -1;
    }

    GameObject BuildCard(PolicyData policy, int index)
    {
        Region selected = regionManager != null ? regionManager.SelectedRegion : null;

        // get modified deltas if a region is selected
        float carbon, economy, stability;
        policy.GetModifiedDeltas(selected, out carbon, out economy, out stability);
        float baseCarbonDelta = policy.carbonDelta;
        float baseEconDelta = policy.economyDelta;
        float baseStabDelta = policy.stabilityDelta;

        // card root
        var card = new GameObject($"Card_{index}");
        var cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        cardRect.pivot = new Vector2(0.5f, 0f);
        // start off-screen
        cardRect.anchoredPosition = new Vector2(0f, restY - 40f);

        // border background
        var borderImg = card.AddComponent<Image>();
        Color rarityCol = GetRarityColor(policy.rarity);
        borderImg.color = rarityCol;
        cardBorders.Add(borderImg);
        cardRarityColors.Add(rarityCol);

        // make card interactive
        var interaction = card.AddComponent<CardInteraction>();
        interaction.cardIndex = index;
        interaction.handDisplay = this;

        // inner background
        var inner = CreateChild(card, "Inner", new Vector2(0, 0), new Vector2(-4f, -4f));
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = cardBackground;
        innerImg.raycastTarget = false;

        // -- stats section at top --
        // carbon: lower is better, economy/stability: higher is better
        string statsString = BuildStatLine("Carbon", carbon, baseCarbonDelta, carbonColor, selected, true)
            + "\n" + BuildStatLine("Economy", economy, baseEconDelta, economyColor, selected, false)
            + "\n" + BuildStatLine("Stability", stability, baseStabDelta, stabilityColor, selected, false);

        var statsArea = CreateChild(inner, "Stats",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(-12f, 52f));
        var statsText = CreateText(statsArea, statsString, 11, TextAlignmentOptions.TopLeft, Color.white);
        statsText.richText = true;
        statsText.lineSpacing = -4f;
        StretchFill(statsText.gameObject, 6f);

        // -- title --
        var title = CreateChild(inner, "Title",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -62f), new Vector2(-12f, 30f));
        var titleText = CreateText(title, policy.policyName, 15, TextAlignmentOptions.Center, Color.white);
        titleText.fontStyle = FontStyles.Bold;
        StretchFill(titleText.gameObject);

        // -- icon area --
        var iconArea = CreateChild(inner, "IconArea",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(-24f, 80f));
        var iconBg = iconArea.AddComponent<Image>();
        iconBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        iconBg.raycastTarget = false;

        if (policy.icon != null)
        {
            var iconImg = CreateChild(iconArea, "Icon", Vector2.zero, Vector2.zero);
            var img = iconImg.AddComponent<Image>();
            img.sprite = policy.icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            StretchFill(iconImg, 8f);
        }
        else
        {
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

    // lowerIsBetter: true for carbon (negative = good), false for economy/stability (positive = good)
    string BuildStatLine(string label, float modifiedValue, float baseValue, Color labelColor, Region selected, bool lowerIsBetter)
    {
        string hex = ColorUtility.ToHtmlStringRGB(labelColor);
        string baseSign = baseValue >= 0 ? "+" : "";

        if (selected != null)
        {
            float diff = modifiedValue - baseValue;
            if (Mathf.Abs(diff) > 0.01f)
            {
                // show: base → final (trait bonus)
                bool isGood = lowerIsBetter ? diff < 0 : diff > 0;
                string colorHex = isGood
                    ? ColorUtility.ToHtmlStringRGB(bonusColor)
                    : ColorUtility.ToHtmlStringRGB(penaltyColor);
                string modSign = modifiedValue >= 0 ? "+" : "";
                string diffSign = diff >= 0 ? "+" : "";
                return $"<color=#{hex}>{label}: {baseSign}{baseValue:0} → {modSign}{modifiedValue:0}</color>"
                    + $" <color=#{colorHex}>({diffSign}{diff:0})</color>";
            }
        }

        return $"<color=#{hex}>{label}: {baseSign}{baseValue:0}</color>";
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
