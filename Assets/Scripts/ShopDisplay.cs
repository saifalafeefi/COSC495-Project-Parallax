using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ShopDisplay : MonoBehaviour
{
    [Header("Card Appearance")]
    [SerializeField] private Color cardBackground = new Color(0.12f, 0.12f, 0.16f, 0.95f);
    [SerializeField] private Color commonColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color uncommonColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color rareColor = new Color(0.9f, 0.7f, 0.1f);
    [SerializeField] private Color selectedGlow = new Color(1f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color soldOutColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Stat Colors")]
    [SerializeField] private Color carbonColor = new Color(0.75f, 0.45f, 0.3f);
    [SerializeField] private Color economyColor = new Color(0.85f, 0.75f, 0.3f);
    [SerializeField] private Color stabilityColor = new Color(0.4f, 0.6f, 0.85f);

    [Header("Layout")]
    [SerializeField] private int gridColumns = 3;
    [SerializeField] private float cardWidth = 180f;
    [SerializeField] private float cardHeight = 260f;
    [SerializeField] private float cardSpacingX = 20f;
    [SerializeField] private float cardSpacingY = 20f;
    [Tooltip("horizontal offset of the card grid — positive moves right")]
    [SerializeField] private float gridOffsetX = 250f;
    [Tooltip("vertical offset of the card grid")]
    [SerializeField] private float gridOffsetY = 0f;

    [Header("Shopkeeper NPC")]
    [Tooltip("idle sprite shown when shop opens")]
    [SerializeField] private Sprite shopkeeperIdle;
    [Tooltip("sprite shown on successful purchase")]
    [SerializeField] private Sprite shopkeeperSuccess;
    [Tooltip("sprite shown when purchase fails (not enough funds)")]
    [SerializeField] private Sprite shopkeeperFail;
    [Tooltip("how long the success/fail sprite stays before returning to idle")]
    [SerializeField] private float shopkeeperReactDuration = 1.5f;
    [Tooltip("size of the shopkeeper sprite")]
    [SerializeField] private Vector2 shopkeeperSize = new Vector2(300f, 400f);
    [Tooltip("final horizontal offset for the shopkeeper (negative = more left)")]
    [SerializeField] private float shopkeeperOffsetX = -350f;
    [Tooltip("vertical offset for the shopkeeper")]
    [SerializeField] private float shopkeeperOffsetY = -30f;
    [Tooltip("how fast the shopkeeper slides in from the left")]
    [SerializeField] private float shopkeeperSlideSpeed = 4f;
    [Tooltip("how fast the shopkeeper bobs up and down")]
    [SerializeField] private float shopkeeperBobSpeed = 1.5f;
    [Tooltip("how far the shopkeeper bobs up and down")]
    [SerializeField] private float shopkeeperBobAmount = 8f;
    [Tooltip("how fast the shopkeeper tilts side to side")]
    [SerializeField] private float shopkeeperSwaySpeed = 1f;
    [Tooltip("max tilt angle in degrees")]
    [SerializeField] private float shopkeeperSwayAngle = 3f;

    [Header("Cashier Table")]
    [Tooltip("table sprite that slides in from the left, layered above shopkeeper")]
    [SerializeField] private Sprite tableSprite;
    [Tooltip("size of the table image")]
    [SerializeField] private Vector2 tableSize = new Vector2(500f, 200f);
    [Tooltip("final horizontal position")]
    [SerializeField] private float tableOffsetX = -300f;
    [Tooltip("vertical position")]
    [SerializeField] private float tableOffsetY = -150f;
    [Tooltip("how fast the table slides in (higher = faster)")]
    [SerializeField] private float tableSlideSpeed = 4f;

    [Header("Shop Background Image")]
    [Tooltip("sprite that slides up from below the screen when shop opens")]
    [SerializeField] private Sprite shopBackgroundSprite;
    [Tooltip("size of the background image")]
    [SerializeField] private Vector2 shopBgSize = new Vector2(1920f, 600f);
    [Tooltip("final vertical position (anchored to bottom-center)")]
    [SerializeField] private float shopBgFinalY = 0f;
    [Tooltip("how fast the background slides up (higher = faster)")]
    [SerializeField] private float shopBgSlideSpeed = 3f;

    [Header("Shop Title")]
    [Tooltip("font size for the SHOP title")]
    [SerializeField] private float titleFontSize = 32f;
    [Tooltip("offset from default title position (above card grid)")]
    [SerializeField] private Vector2 titleOffset = Vector2.zero;
    [Tooltip("font size for the funds display")]
    [SerializeField] private float fundsFontSize = 18f;
    [Tooltip("offset from default funds position (below title)")]
    [SerializeField] private Vector2 fundsOffset = Vector2.zero;
    [Tooltip("font size for the feedback text")]
    [SerializeField] private float feedbackFontSize = 16f;
    [Tooltip("offset from default feedback position (below card grid)")]
    [SerializeField] private Vector2 feedbackOffset = Vector2.zero;

    [Header("Close Button")]
    [Tooltip("size of the close button")]
    [SerializeField] private Vector2 closeButtonSize = new Vector2(140f, 40f);
    [Tooltip("font size on the close button")]
    [SerializeField] private float closeButtonFontSize = 18f;
    [Tooltip("text shown on the close button")]
    [SerializeField] private string closeButtonText = "Close";
    [Tooltip("offset from default close button position (below feedback)")]
    [SerializeField] private Vector2 closeButtonOffset = Vector2.zero;

    [Header("Card Font Sizes")]
    [Tooltip("font size for card title")]
    [SerializeField] private float cardTitleFontSize = 14f;
    [Tooltip("font size for card stats line")]
    [SerializeField] private float cardStatsFontSize = 11f;
    [Tooltip("font size for card description")]
    [SerializeField] private float cardDescFontSize = 12f;
    [Tooltip("font size for rarity label")]
    [SerializeField] private float cardRarityFontSize = 11f;
    [Tooltip("font size for price badge")]
    [SerializeField] private float cardPriceFontSize = 11f;
    [Tooltip("font size for capital cost badge")]
    [SerializeField] private float cardCapFontSize = 11f;

    [Header("Tween Settings")]
    [SerializeField] private float hoverLift = 20f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float selectedScale = 1.12f;
    [SerializeField] private float tweenSpeed = 10f;
    [SerializeField] private float bounceSpeed = 3f;
    [SerializeField] private float bounceAmount = 6f;
    [SerializeField] private float selectedColorSpeed = 2.5f;
    [SerializeField] private float dealDuration = 0.35f;
    [SerializeField] private float dealStagger = 0.08f;

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
    private float dealFinishTime;

    // true while shop is opening (deal-in) or closing (fade-out)
    public bool IsTransitioning => (showing && Time.time < dealFinishTime) || closing;

    // snapshot of shop cards so we can rebuild sold cards with full visuals
    private List<PolicyData> shopSnapshot = new List<PolicyData>();
    private int shopSnapshotRound = -1;

    // closing animation state
    private bool closing;
    private float closeTime;
    private float closeDuration = 0.4f;
    private Image backdropImage;
    private CanvasGroup rootCanvasGroup;

    // feedback text shown briefly after purchase
    private TMP_Text feedbackText;
    private float feedbackTime;

    // live-update references
    private TMP_Text titleTextRef;
    private TMP_Text fundsTextRef;
    private TMP_Text closeTextRef;
    private RectTransform closeBtnRect;
    private RectTransform titleRect;
    private RectTransform fundsRect;
    private RectTransform feedbackRect;
    private RectTransform closeBtnParentRect;

    private RectTransform containerRect;

    // per-card text references for live font updates
    private List<TMP_Text> cardTitleTexts = new List<TMP_Text>();
    private List<TMP_Text> cardStatsTexts = new List<TMP_Text>();
    private List<TMP_Text> cardDescTexts = new List<TMP_Text>();
    private List<TMP_Text> cardRarityTexts = new List<TMP_Text>();
    private List<TMP_Text> cardPriceTexts = new List<TMP_Text>();
    private List<TMP_Text> cardCapTexts = new List<TMP_Text>();

    // shop background image
    private RectTransform shopBgRect;
    private float shopBgSlideT;

    // cashier table
    private RectTransform tableRect;
    private float tableSlideT;

    // shopkeeper NPC
    private Image shopkeeperImage;
    private RectTransform shopkeeperRect;
    private float shopkeeperReactTime;
    private bool shopkeeperReacting;
    private float shopkeeperSlideT;

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }

        if (gameManager.ShopActive && !showing && !closing)
            ShowShop();
        else if (!gameManager.ShopActive && showing && !closing)
        {
            // block closing until card deal-in animations finish
            if (Time.time < dealFinishTime)
                gameManager.OpenShop();
            else
                StartClosing();
        }

        if (closing)
        {
            UpdateClosing();
            return;
        }

        if (showing)
        {
            AnimateCards();
            UpdateFeedback();
            UpdateShopkeeper();
            UpdateTable();
            UpdateShopBackground();
        }
    }

    void ShowShop()
    {
        showing = true;
        showTime = Time.time;
        selectedIndex = -1;
        hoveredIndex = -1;

        // root canvas above the hand but below reward
        root = new GameObject("ShopUI");
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // dark backdrop
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

        // shop background image — slides up from below the screen
        if (shopBackgroundSprite != null)
        {
            bool mobile = false;
            #if UNITY_EDITOR
            mobile = FindFirstObjectByType<ARPlacement>() != null;
            #elif UNITY_ANDROID || UNITY_IOS
            mobile = true;
            #endif

            var bgObj = new GameObject("ShopBackground");
            bgObj.transform.SetParent(root.transform, false);
            shopBgRect = bgObj.AddComponent<RectTransform>();

            if (mobile)
            {
                // stretch to fill screen width, keep height from shopBgSize
                shopBgRect.anchorMin = new Vector2(0f, 0f);
                shopBgRect.anchorMax = new Vector2(1f, 0f);
                shopBgRect.pivot = new Vector2(0.5f, 0f);
                shopBgRect.offsetMin = new Vector2(0f, -shopBgSize.y);
                shopBgRect.offsetMax = new Vector2(0f, 0f);
                shopBgRect.sizeDelta = new Vector2(0f, shopBgSize.y);
                shopBgRect.anchoredPosition = new Vector2(0f, -shopBgSize.y);
            }
            else
            {
                shopBgRect.anchorMin = new Vector2(0.5f, 0f);
                shopBgRect.anchorMax = new Vector2(0.5f, 0f);
                shopBgRect.pivot = new Vector2(0.5f, 0f);
                shopBgRect.anchoredPosition = new Vector2(0f, -shopBgSize.y);
                shopBgRect.sizeDelta = shopBgSize;
            }

            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = shopBackgroundSprite;
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;

            shopBgSlideT = 0f;
        }

        // calculate grid dimensions for positioning header/footer
        int rows = Mathf.CeilToInt((float)gameManager.ShopCards.Count / gridColumns);
        float gridW = gridColumns * cardWidth + (gridColumns - 1) * cardSpacingX;
        float gridH = rows * cardHeight + (rows - 1) * cardSpacingY;

        // title — centered above the card grid
        var titleObj = CreateUIObj("Title", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(gridOffsetX + titleOffset.x, gridOffsetY + gridH / 2f + 50f + titleOffset.y), new Vector2(gridW, 50f));
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SHOP";
        titleText.fontSize = titleFontSize;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.85f, 0.4f);
        titleText.fontStyle = FontStyles.Bold;
        titleTextRef = titleText;
        titleRect = titleObj.GetComponent<RectTransform>();

        // funds display — below title
        var fundsObj = CreateUIObj("Funds", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(gridOffsetX + fundsOffset.x, gridOffsetY + gridH / 2f + 20f + fundsOffset.y), new Vector2(gridW, 30f));
        var fundsText = fundsObj.AddComponent<TextMeshProUGUI>();
        fundsText.text = $"Funds: {gameManager.Funds}";
        fundsText.fontSize = fundsFontSize;
        fundsText.alignment = TextAlignmentOptions.Center;
        fundsText.color = new Color(0.7f, 0.85f, 0.7f);
        fundsTextRef = fundsText;
        fundsRect = fundsObj.GetComponent<RectTransform>();

        // feedback text — below the grid
        var fbObj = CreateUIObj("Feedback", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(gridOffsetX + feedbackOffset.x, gridOffsetY - gridH / 2f - 30f + feedbackOffset.y), new Vector2(gridW + 100f, 30f));
        feedbackText = fbObj.AddComponent<TextMeshProUGUI>();
        feedbackText.text = "";
        feedbackText.fontSize = feedbackFontSize;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.color = Color.white;
        feedbackRect = fbObj.GetComponent<RectTransform>();

        // card container — positioned to the right
        var container = new GameObject("CardContainer");
        container.transform.SetParent(root.transform, false);
        var contRect = container.AddComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.5f, 0.5f);
        contRect.anchorMax = new Vector2(0.5f, 0.5f);
        contRect.pivot = new Vector2(0.5f, 0.5f);
        contRect.anchoredPosition = new Vector2(gridOffsetX, gridOffsetY);
        contRect.sizeDelta = new Vector2(gridW + 40f, gridH + 40f);
        containerRect = contRect;

        // snapshot shop cards so sold slots can still show full card visuals
        // refresh snapshot when a new round generates fresh stock
        if (shopSnapshotRound != gameManager.CurrentRound)
        {
            shopSnapshot.Clear();
            shopSnapshot.AddRange(gameManager.ShopCards);
            shopSnapshotRound = gameManager.CurrentRound;
        }

        // build shop cards
        cardObjects.Clear();
        cardRects.Clear();
        cardBorders.Clear();
        cardRarityColors.Clear();
        cardTitleTexts.Clear();
        cardStatsTexts.Clear();
        cardDescTexts.Clear();
        cardRarityTexts.Clear();
        cardPriceTexts.Clear();
        cardCapTexts.Clear();

        int cardCount = gameManager.ShopCards.Count;
        for (int i = 0; i < cardCount; i++)
        {
            var policy = gameManager.ShopCards[i];
            bool sold = policy == null;

            // use snapshot for sold cards so we can show the original card
            PolicyData displayPolicy = sold ? shopSnapshot[i] : policy;
            GameObject card;

            if (displayPolicy == null)
            {
                card = BuildSoldCard(i);
            }
            else
            {
                card = BuildCard(displayPolicy, i);
                // if already sold, activate the overlay immediately
                if (sold)
                {
                    var overlay = card.transform.Find("SoldOverlay");
                    if (overlay != null) overlay.gameObject.SetActive(true);
                }
            }

            card.transform.SetParent(container.transform, false);

            var rect = card.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.zero;

            cardObjects.Add(card);
            cardRects.Add(rect);
        }

        // calculate when the last card finishes dealing in
        dealFinishTime = showTime + cardCount * dealStagger + dealDuration;

        // shopkeeper NPC on the left side
        BuildShopkeeper(root.transform);

        // cashier table — created after shopkeeper so it renders on top
        if (tableSprite != null)
        {
            float offscreenX = tableOffsetX - 800f;
            var tblObj = new GameObject("CashierTable");
            tblObj.transform.SetParent(root.transform, false);
            tableRect = tblObj.AddComponent<RectTransform>();
            tableRect.anchorMin = new Vector2(0.5f, 0.5f);
            tableRect.anchorMax = new Vector2(0.5f, 0.5f);
            tableRect.pivot = new Vector2(0.5f, 0.5f);
            tableRect.anchoredPosition = new Vector2(offscreenX, tableOffsetY);
            tableRect.sizeDelta = tableSize;

            var tblImg = tblObj.AddComponent<Image>();
            tblImg.sprite = tableSprite;
            tblImg.preserveAspect = false;
            tblImg.raycastTarget = false;

            tableSlideT = 0f;
        }

        // close button
        BuildCloseButton(root.transform);
    }

    void StartClosing()
    {
        closing = true;
        closeTime = Time.time;

        // add a canvas group for fading if not already present
        if (root != null && rootCanvasGroup == null)
            rootCanvasGroup = root.AddComponent<CanvasGroup>();
    }

    void UpdateClosing()
    {
        float elapsed = Time.time - closeTime;
        float t = Mathf.Clamp01(elapsed / closeDuration);

        // fade out the whole overlay
        if (rootCanvasGroup != null)
            rootCanvasGroup.alpha = 1f - t;

        // slide shopkeeper back out to the left
        if (shopkeeperRect != null)
        {
            float slideOut = Mathf.Lerp(shopkeeperOffsetX, shopkeeperOffsetX - 600f, t * t);
            float bob = Mathf.Sin(Time.time * shopkeeperBobSpeed) * shopkeeperBobAmount;
            float sway = Mathf.Sin(Time.time * shopkeeperSwaySpeed * 0.7f) * shopkeeperSwayAngle;
            shopkeeperRect.anchoredPosition = new Vector2(slideOut, shopkeeperOffsetY + bob);
            shopkeeperRect.localRotation = Quaternion.Euler(0f, 0f, sway);
        }

        // slide table back out to the left
        if (tableRect != null)
        {
            float tblX = Mathf.Lerp(tableOffsetX, tableOffsetX - 800f, t * t);
            tableRect.anchoredPosition = new Vector2(tblX, tableOffsetY);
        }

        // slide background back down
        if (shopBgRect != null)
        {
            // ease-in: slow at first, fast at end
            float bgY = Mathf.Lerp(shopBgFinalY, -shopBgSize.y, t * t);
            shopBgRect.anchoredPosition = new Vector2(0f, bgY);
        }

        if (t >= 1f)
            FinishClosing();
    }

    void FinishClosing()
    {
        closing = false;
        showing = false;
        cardObjects.Clear();
        cardRects.Clear();
        cardBorders.Clear();
        cardRarityColors.Clear();
        cardTitleTexts.Clear();
        cardStatsTexts.Clear();
        cardDescTexts.Clear();
        cardRarityTexts.Clear();
        cardPriceTexts.Clear();
        cardCapTexts.Clear();
        hoveredIndex = -1;
        selectedIndex = -1;
        feedbackText = null;
        titleTextRef = null;
        fundsTextRef = null;
        closeTextRef = null;
        closeBtnRect = null;
        titleRect = null;
        fundsRect = null;
        feedbackRect = null;
        containerRect = null;
        shopkeeperImage = null;
        shopkeeperRect = null;
        shopkeeperReacting = false;
        rootCanvasGroup = null;
        backdropImage = null;
        shopBgRect = null;
        tableRect = null;

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

        // live-update all font sizes, positions, and button sizes from Inspector
        if (titleTextRef != null) titleTextRef.fontSize = titleFontSize;
        if (fundsTextRef != null)
        {
            fundsTextRef.fontSize = fundsFontSize;
            fundsTextRef.text = $"Funds: {gameManager.Funds}";
        }
        if (feedbackText != null) feedbackText.fontSize = feedbackFontSize;
        if (closeTextRef != null)
        {
            closeTextRef.fontSize = closeButtonFontSize;
            closeTextRef.text = closeButtonText;
        }
        if (closeBtnRect != null) closeBtnRect.sizeDelta = closeButtonSize;

        // live-update element positions
        int layoutRows = Mathf.CeilToInt((float)count / gridColumns);
        float layoutGridW = gridColumns * cardWidth + (gridColumns - 1) * cardSpacingX;
        float layoutGridH = layoutRows * cardHeight + (layoutRows - 1) * cardSpacingY;

        if (titleRect != null)
            titleRect.anchoredPosition = new Vector2(gridOffsetX + titleOffset.x, gridOffsetY + layoutGridH / 2f + 50f + titleOffset.y);
        if (fundsRect != null)
            fundsRect.anchoredPosition = new Vector2(gridOffsetX + fundsOffset.x, gridOffsetY + layoutGridH / 2f + 20f + fundsOffset.y);
        if (feedbackRect != null)
            feedbackRect.anchoredPosition = new Vector2(gridOffsetX + feedbackOffset.x, gridOffsetY - layoutGridH / 2f - 30f + feedbackOffset.y);
        if (closeBtnRect != null)
            closeBtnRect.anchoredPosition = new Vector2(gridOffsetX + closeButtonOffset.x, gridOffsetY - layoutGridH / 2f - 65f + closeButtonOffset.y);
        if (containerRect != null)
            containerRect.anchoredPosition = new Vector2(gridOffsetX, gridOffsetY);

        // live-update per-card font sizes
        for (int ci = 0; ci < count; ci++)
        {
            if (ci < cardTitleTexts.Count && cardTitleTexts[ci] != null) cardTitleTexts[ci].fontSize = cardTitleFontSize;
            if (ci < cardStatsTexts.Count && cardStatsTexts[ci] != null) cardStatsTexts[ci].fontSize = cardStatsFontSize;
            if (ci < cardDescTexts.Count && cardDescTexts[ci] != null) cardDescTexts[ci].fontSize = cardDescFontSize;
            if (ci < cardRarityTexts.Count && cardRarityTexts[ci] != null) cardRarityTexts[ci].fontSize = cardRarityFontSize;
            if (ci < cardPriceTexts.Count && cardPriceTexts[ci] != null) cardPriceTexts[ci].fontSize = cardPriceFontSize;
            if (ci < cardCapTexts.Count && cardCapTexts[ci] != null) cardCapTexts[ci].fontSize = cardCapFontSize;
        }

        // grid layout: 3 columns × 2 rows
        int cols = gridColumns;
        int rows = Mathf.CeilToInt((float)count / cols);
        float gridW = cols * cardWidth + (cols - 1) * cardSpacingX;
        float gridH = rows * cardHeight + (rows - 1) * cardSpacingY;

        for (int i = 0; i < count; i++)
        {
            var rect = cardRects[i];
            if (rect == null) continue;

            // live-update card size so Inspector changes apply immediately
            rect.sizeDelta = new Vector2(cardWidth, cardHeight);

            // check if this card has been sold
            bool sold = i < gameManager.ShopCards.Count && gameManager.ShopCards[i] == null;

            int col = i % cols;
            int row = i / cols;

            // position within the grid, centered on the container
            float targetX = -gridW / 2f + cardWidth / 2f + col * (cardWidth + cardSpacingX);
            float targetY = gridH / 2f - cardHeight / 2f - row * (cardHeight + cardSpacingY);
            float targetScale = 1f;

            if (!sold)
            {
                if (i == selectedIndex)
                {
                    float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
                    targetY += hoverLift + bounce;
                    targetScale = selectedScale;
                }
                else if (i == hoveredIndex)
                {
                    targetY += hoverLift;
                    targetScale = hoverScale;
                }
            }

            // deal-in animation
            float elapsed = Time.time - (showTime + i * dealStagger);
            if (elapsed < 0f)
            {
                rect.localScale = Vector3.zero;
                rect.anchoredPosition = Vector2.zero;
            }
            else if (elapsed < dealDuration)
            {
                float raw = elapsed / dealDuration;
                float t = 1f - (1f - raw) * (1f - raw) * (1f - raw);

                float x = Mathf.Lerp(0f, targetX, t);
                float y = Mathf.Lerp(0f, targetY, t);
                rect.anchoredPosition = new Vector2(x, y);

                float scaleT;
                if (raw < 0.7f)
                    scaleT = Mathf.Lerp(0f, targetScale * 1.08f, raw / 0.7f);
                else
                    scaleT = Mathf.Lerp(targetScale * 1.08f, targetScale, (raw - 0.7f) / 0.3f);
                rect.localScale = new Vector3(scaleT, scaleT, 1f);
            }
            else
            {
                float t = Time.deltaTime * tweenSpeed;
                var pos = rect.anchoredPosition;
                pos.x = Mathf.Lerp(pos.x, targetX, t);
                pos.y = Mathf.Lerp(pos.y, targetY, t);
                rect.anchoredPosition = pos;

                float curScale = rect.localScale.x;
                float newScale = Mathf.Lerp(curScale, targetScale, t);
                rect.localScale = new Vector3(newScale, newScale, 1f);
            }

            // border glow
            if (i < cardBorders.Count && cardBorders[i] != null)
            {
                if (sold)
                {
                    cardBorders[i].color = soldOutColor;
                }
                else if (i == selectedIndex)
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

            // z-ordering
            if (i == hoveredIndex)
                rect.SetAsLastSibling();
            else if (i == selectedIndex && hoveredIndex != selectedIndex)
                rect.transform.SetSiblingIndex(Mathf.Max(0, rect.parent.childCount - 2));
        }
    }

    void UpdateFeedback()
    {
        if (feedbackText == null) return;
        float elapsed = Time.time - feedbackTime;
        if (elapsed > 2f)
        {
            feedbackText.color = new Color(feedbackText.color.r, feedbackText.color.g, feedbackText.color.b,
                Mathf.Max(0f, feedbackText.color.a - Time.deltaTime * 2f));
        }
    }

    void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackTime = Time.time;
    }

    public void OnCardHover(int index)
    {
        // ignore sold cards
        if (index >= 0 && index < gameManager.ShopCards.Count && gameManager.ShopCards[index] == null)
            return;
        hoveredIndex = index;
    }

    public void OnCardUnhover(int index)
    {
        if (hoveredIndex == index)
            hoveredIndex = -1;
    }

    public void OnCardClick(int index)
    {
        if (index < 0 || index >= gameManager.ShopCards.Count) return;
        if (gameManager.ShopCards[index] == null) return;

        if (selectedIndex == index)
        {
            // second click: confirm purchase
            string result = gameManager.BuyShopCard(index);
            if (result != null && result.StartsWith("NOT_ENOUGH_FUNDS"))
            {
                ShowFeedback("Not enough funds!", new Color(1f, 0.4f, 0.4f));
                SetShopkeeperReaction(false);
            }
            else if (result != null)
            {
                ShowFeedback(result, new Color(0.4f, 1f, 0.5f));
                SetShopkeeperReaction(true);
                selectedIndex = -1;

                // dim the sold card
                if (index < cardObjects.Count)
                {
                    // mark visually as sold
                    var soldOverlay = cardObjects[index].transform.Find("SoldOverlay");
                    if (soldOverlay != null)
                        soldOverlay.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            selectedIndex = index;
        }
    }

    void BuildCloseButton(Transform parent)
    {
        // position below feedback text
        int closeRows = Mathf.CeilToInt((float)gameManager.ShopCards.Count / gridColumns);
        float closeGridH = closeRows * cardHeight + (closeRows - 1) * cardSpacingY;

        var btnObj = CreateUIObj("CloseButton", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(gridOffsetX + closeButtonOffset.x, gridOffsetY - closeGridH / 2f - 65f + closeButtonOffset.y), closeButtonSize);

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() => gameManager.CloseShop());

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
        text.text = closeButtonText;
        text.fontSize = closeButtonFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.8f, 0.8f, 0.8f);
        closeTextRef = text;
        closeBtnRect = btnObj.GetComponent<RectTransform>();
    }

    void BuildShopkeeper(Transform parent)
    {
        // start offscreen to the left, will slide in
        float offscreenX = shopkeeperOffsetX - 600f;

        var npcObj = CreateUIObj("Shopkeeper", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(offscreenX, shopkeeperOffsetY), shopkeeperSize);

        shopkeeperRect = npcObj.GetComponent<RectTransform>();
        shopkeeperImage = npcObj.AddComponent<Image>();
        shopkeeperImage.preserveAspect = true;
        shopkeeperImage.raycastTarget = false;

        if (shopkeeperIdle != null)
        {
            shopkeeperImage.sprite = shopkeeperIdle;
            // start fully transparent, will fade in during slide
            shopkeeperImage.color = new Color(1f, 1f, 1f, 0f);
        }
        else
        {
            shopkeeperImage.color = new Color(1f, 1f, 1f, 0f);
        }

        shopkeeperReacting = false;
        shopkeeperSlideT = 0f;
    }

    void UpdateShopkeeper()
    {
        if (shopkeeperRect == null) return;

        // slide in from left with ease-out
        if (shopkeeperSlideT < 1f)
        {
            shopkeeperSlideT += Time.deltaTime * shopkeeperSlideSpeed;
            if (shopkeeperSlideT > 1f) shopkeeperSlideT = 1f;
        }
        float eased = 1f - (1f - shopkeeperSlideT) * (1f - shopkeeperSlideT) * (1f - shopkeeperSlideT);
        float slideX = Mathf.Lerp(shopkeeperOffsetX - 600f, shopkeeperOffsetX, eased);

        // fade in opacity alongside the slide
        if (shopkeeperImage != null && shopkeeperSlideT <= 1f)
        {
            Color c = shopkeeperImage.color;
            c.a = eased;
            shopkeeperImage.color = c;
        }

        // bob up and down
        float bob = Mathf.Sin(Time.time * shopkeeperBobSpeed) * shopkeeperBobAmount;

        // sway rotation (slightly offset frequency so bob and sway don't sync perfectly)
        float sway = Mathf.Sin(Time.time * shopkeeperSwaySpeed * 0.7f) * shopkeeperSwayAngle;

        shopkeeperRect.anchoredPosition = new Vector2(slideX, shopkeeperOffsetY + bob);
        shopkeeperRect.localRotation = Quaternion.Euler(0f, 0f, sway);

        // reaction sprite timer
        if (shopkeeperReacting && Time.time - shopkeeperReactTime > shopkeeperReactDuration)
        {
            shopkeeperReacting = false;
            if (shopkeeperIdle != null)
                shopkeeperImage.sprite = shopkeeperIdle;
        }
    }

    void UpdateTable()
    {
        if (tableRect == null) return;

        if (tableSlideT < 1f)
        {
            tableSlideT += Time.deltaTime * tableSlideSpeed;
            if (tableSlideT > 1f) tableSlideT = 1f;
        }

        // ease-out cubic
        float eased = 1f - (1f - tableSlideT) * (1f - tableSlideT) * (1f - tableSlideT);
        float x = Mathf.Lerp(tableOffsetX - 800f, tableOffsetX, eased);
        tableRect.anchoredPosition = new Vector2(x, tableOffsetY);
    }

    void UpdateShopBackground()
    {
        if (shopBgRect == null) return;

        if (shopBgSlideT < 1f)
        {
            shopBgSlideT += Time.deltaTime * shopBgSlideSpeed;
            if (shopBgSlideT > 1f) shopBgSlideT = 1f;
        }

        // ease-out cubic
        float eased = 1f - (1f - shopBgSlideT) * (1f - shopBgSlideT) * (1f - shopBgSlideT);
        float y = Mathf.Lerp(-shopBgSize.y, shopBgFinalY, eased);
        shopBgRect.anchoredPosition = new Vector2(0f, y);
    }

    void SetShopkeeperReaction(bool success)
    {
        if (shopkeeperImage == null) return;

        Sprite reactionSprite = success ? shopkeeperSuccess : shopkeeperFail;
        if (reactionSprite != null)
            shopkeeperImage.sprite = reactionSprite;

        shopkeeperReacting = true;
        shopkeeperReactTime = Time.time;
    }

    // ---- card builder ----

    GameObject BuildCard(PolicyData policy, int index)
    {
        int price = gameManager.GetShopPrice(policy.rarity);
        bool canAfford = gameManager.Funds >= price;

        string cSign = policy.carbonDelta >= 0 ? "+" : "";
        string eSign = policy.economyDelta >= 0 ? "+" : "";
        string sSign = policy.stabilityDelta >= 0 ? "+" : "";

        string cHex = ColorUtility.ToHtmlStringRGB(carbonColor);
        string eHex = ColorUtility.ToHtmlStringRGB(economyColor);
        string sHex = ColorUtility.ToHtmlStringRGB(stabilityColor);

        string statsString = $"<color=#{cHex}>C: {cSign}{policy.carbonDelta:0}</color>"
            + $"  <color=#{eHex}>E: {eSign}{policy.economyDelta:0}</color>"
            + $"  <color=#{sHex}>S: {sSign}{policy.stabilityDelta:0}</color>";

        // card root
        var card = new GameObject($"ShopCard_{index}");
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
        var handler = card.AddComponent<ShopCardHandler>();
        handler.cardIndex = index;
        handler.shopDisplay = this;

        // inner background
        var inner = CreateStretchChild(card, "Inner", 2f);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = cardBackground;
        innerImg.raycastTarget = false;

        // price badge — top-right corner
        var priceBadge = CreateUIObj("PriceBadge", inner.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-4f, -4f), new Vector2(44f, 22f));
        var priceBg = priceBadge.AddComponent<Image>();
        priceBg.color = canAfford ? new Color(0.2f, 0.5f, 0.7f, 0.9f) : new Color(0.5f, 0.2f, 0.2f, 0.9f);
        priceBg.raycastTarget = false;
        var priceText = CreateText(priceBadge, $"{price}F", cardPriceFontSize, TextAlignmentOptions.Center, Color.white);
        priceText.fontStyle = FontStyles.Bold;
        StretchFill(priceText.gameObject);
        cardPriceTexts.Add(priceText);

        // political capital cost badge — top-left
        var capBadge = CreateUIObj("CapBadge", inner.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(4f, -4f), new Vector2(36f, 22f));
        var capBg = capBadge.AddComponent<Image>();
        capBg.color = new Color(0.3f, 0.3f, 0.6f, 0.9f);
        capBg.raycastTarget = false;
        var capText = CreateText(capBadge, $"{policy.politicalCapitalCost}", cardCapFontSize, TextAlignmentOptions.Center, Color.white);
        capText.fontStyle = FontStyles.Bold;
        StretchFill(capText.gameObject);
        cardCapTexts.Add(capText);

        // title
        var titleArea = CreateUIObj("Title", inner.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(-12f, 30f));
        var cardTitle = CreateText(titleArea, policy.policyName, cardTitleFontSize, TextAlignmentOptions.Center, Color.white);
        cardTitle.fontStyle = FontStyles.Bold;
        StretchFill(cardTitle.gameObject);
        cardTitleTexts.Add(cardTitle);

        // rarity label
        var rarityArea = CreateUIObj("Rarity", inner.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -55f), new Vector2(-12f, 18f));
        var cardRarity = CreateText(rarityArea, policy.rarity.ToString(), cardRarityFontSize, TextAlignmentOptions.Center, rarityCol);
        cardRarity.fontStyle = FontStyles.Italic;
        StretchFill(cardRarity.gameObject);
        cardRarityTexts.Add(cardRarity);

        // stats
        var statsArea = CreateUIObj("Stats", inner.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -78f), new Vector2(-8f, 20f));
        var cardStats = CreateText(statsArea, statsString, cardStatsFontSize, TextAlignmentOptions.Center, Color.white);
        cardStats.richText = true;
        StretchFill(cardStats.gameObject);
        cardStatsTexts.Add(cardStats);

        // icon area (compact to leave more room for description)
        var iconArea = CreateUIObj("IconArea", inner.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -105f), new Vector2(-24f, 55f));
        var iconBg = iconArea.AddComponent<Image>();
        iconBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        iconBg.raycastTarget = false;

        if (policy.icon != null)
        {
            var iconImg = CreateStretchChild(iconArea, "Icon", 6f);
            var img = iconImg.AddComponent<Image>();
            img.sprite = policy.icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        else
        {
            var placeholder = CreateText(iconArea, policy.rarity.ToString(), 12, TextAlignmentOptions.Center, rarityCol);
            placeholder.fontStyle = FontStyles.Italic;
            StretchFill(placeholder.gameObject);
        }

        // description — anchored from below icon area to bottom of card
        var descArea = CreateUIObj("Desc", inner.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0f),
            new Vector2(0f, 6f), new Vector2(-12f, -165f));
        var cardDesc = CreateText(descArea, policy.description, cardDescFontSize, TextAlignmentOptions.TopLeft, new Color(0.75f, 0.75f, 0.8f));
        cardDesc.enableWordWrapping = true;
        cardDesc.overflowMode = TextOverflowModes.Truncate;
        StretchFill(cardDesc.gameObject);
        cardDescTexts.Add(cardDesc);

        // sold overlay (hidden initially)
        var soldObj = CreateStretchChild(card, "SoldOverlay", 0f);
        var soldImg = soldObj.AddComponent<Image>();
        soldImg.color = new Color(0f, 0f, 0f, 0.6f);
        soldImg.raycastTarget = false;
        var soldLabel = CreateText(soldObj, "SOLD", 24, TextAlignmentOptions.Center, new Color(0.8f, 0.3f, 0.3f));
        soldLabel.fontStyle = FontStyles.Bold;
        StretchFill(soldLabel.gameObject);
        soldObj.SetActive(false);

        return card;
    }

    GameObject BuildSoldCard(int index)
    {
        var card = new GameObject($"ShopCard_{index}_Sold");
        var cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        cardRect.pivot = new Vector2(0.5f, 0.5f);

        var bgImg = card.AddComponent<Image>();
        bgImg.color = soldOutColor;
        bgImg.raycastTarget = false;

        // border/color tracking (needed for AnimateCards array indexing)
        cardBorders.Add(bgImg);
        cardRarityColors.Add(soldOutColor);

        // null entries for text lists (sold cards have no editable text)
        cardTitleTexts.Add(null);
        cardStatsTexts.Add(null);
        cardDescTexts.Add(null);
        cardRarityTexts.Add(null);
        cardPriceTexts.Add(null);
        cardCapTexts.Add(null);

        // "SOLD" label
        var soldLabel = CreateText(card, "SOLD", 24, TextAlignmentOptions.Center, new Color(0.8f, 0.3f, 0.3f));
        soldLabel.fontStyle = FontStyles.Bold;
        StretchFill(soldLabel.gameObject);

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

// pointer event handler for shop cards
public class ShopCardHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public int cardIndex;
    [HideInInspector] public ShopDisplay shopDisplay;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shopDisplay != null)
            shopDisplay.OnCardHover(cardIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shopDisplay != null)
            shopDisplay.OnCardUnhover(cardIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shopDisplay == null) return;
        if (eventData.button == PointerEventData.InputButton.Left)
            shopDisplay.OnCardClick(cardIndex);
    }
}
