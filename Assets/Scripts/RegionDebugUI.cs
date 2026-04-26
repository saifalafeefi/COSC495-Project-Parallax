using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RegionDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private TMP_Text cardPlayText;
    [SerializeField] private TMP_Text gameStateText;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private TMP_Text globalStatsText;

    [Header("Region Info Background")]
    [Tooltip("optional semi-transparent panel behind the region info text. auto-hides when no region is hovered/selected.")]
    [SerializeField] private Image regionInfoBackground;
    [Tooltip("inner fill color (alpha controls transparency)")]
    [SerializeField] private Color bgFillColor = new Color(0.05f, 0.07f, 0.12f, 0.78f);
    [Tooltip("border / outline color")]
    [SerializeField] private Color bgBorderColor = new Color(0.85f, 0.7f, 0.35f, 0.95f);
    [Tooltip("subtle inner accent line color (set alpha=0 to disable)")]
    [SerializeField] private Color bgAccentColor = new Color(1f, 1f, 1f, 0.08f);
    [Tooltip("corner radius in pixels (rounded corners)")]
    [SerializeField, Range(0, 64)] private int bgCornerRadius = 14;
    [Tooltip("border thickness in pixels")]
    [SerializeField, Range(0, 12)] private int bgBorderThickness = 2;
    [Tooltip("sprite resolution — leave 64 unless you need huge corners")]
    [SerializeField] private int bgSpriteSize = 64;
    [Tooltip("padding around the text (x = left/right, y = top/bottom) in pixels")]
    [SerializeField] private Vector2 bgPadding = new Vector2(24f, 16f);

    private Sprite generatedBgSprite;
    private int cachedBgKey = -1;

    [Header("Display Durations")]
    [Tooltip("how long the card play text stays fully visible before fading")]
    [SerializeField] private float cardPlayHoldDuration = 1f;
    [Tooltip("how long the fade-out takes after the hold")]
    [SerializeField] private float cardPlayFadeDuration = 1f;
    [Header("Health Warning Colors")]
    [SerializeField] private Color stressedTextColor = new Color(1f, 0.67f, 0.27f);
    [SerializeField] private Color crisisTextColor = new Color(1f, 0.27f, 0.27f);
    [Header("Preview Colors")]
    [SerializeField] private Color previewGoodColor = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color previewBadColor = new Color(0.9f, 0.4f, 0.4f);

    [Header("Shop Hide (slide offscreen)")]
    [Tooltip("how fast UI slides off/on screen when shop opens/closes")]
    [SerializeField] private float shopSlideSpeed = 6f;
    [Tooltip("how far the top bar slides up")]
    [SerializeField] private float topSlideDistance = 200f;
    [Tooltip("how far the bottom elements slide down")]
    [SerializeField] private float bottomSlideDistance = 300f;
    [Tooltip("how far the left panel slides left")]
    [SerializeField] private float leftSlideDistance = 500f;

    private RegionManager regionManager;
    private GameManager gameManager;
    private RegionSelector regionSelector;
    private HandDisplay handDisplay;

    // shop slide state per rect element
    private Dictionary<RectTransform, Vector2> originalPositions = new Dictionary<RectTransform, Vector2>();
    private float shopSlideBlend; // 0 = normal, 1 = fully hidden

    void Update()
    {
        if (regionManager == null)
        {
            regionManager = FindFirstObjectByType<RegionManager>();
            if (regionManager == null) return;
        }

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (regionSelector == null)
            regionSelector = FindFirstObjectByType<RegionSelector>();

        if (handDisplay == null)
            handDisplay = FindFirstObjectByType<HandDisplay>();

        bool isGameOver = gameManager != null && gameManager.GameOver;

        UpdateGameState();
        UpdateRegionInfo(isGameOver);
        UpdateCardPlayText(isGameOver);
        UpdateGlobalStats(isGameOver);
        UpdateShopSlide();
    }

    void UpdateGameState()
    {
        if (gameManager == null) return;

        // round/actions bar (hide on game over)
        if (gameStateText != null)
        {
            if (gameManager.GameOver)
            {
                gameStateText.gameObject.SetActive(false);
            }
            else
            {
                string stabMult = gameManager.StabilityMultiplier != 1f ? $" (x{gameManager.StabilityMultiplier:F1})" : "";
                string bonusTag = gameManager.AppliedCapitalBonus > 0 ? $" (+{gameManager.AppliedCapitalBonus})" : "";
                string diffLabel = DifficultySettings.Current != Difficulty.Normal ? $"  [{DifficultySettings.Current}]" : "";
                gameStateText.text = $"Round {gameManager.CurrentRound}/{gameManager.TotalRounds}   Capital: {gameManager.PoliticalCapital}/{gameManager.MaxCapital}{bonusTag}{stabMult}   Funds: {gameManager.Funds}{diffLabel}";
                gameStateText.gameObject.SetActive(true);
            }
        }

        // game over screen (separate TMP, position/style it center screen in Inspector)
        if (gameOverText != null)
        {
            if (gameManager.GameOver)
            {
                string text = $"GAME OVER\n{gameManager.GameOverReason}";
                if (gameManager.FinalRating != null)
                    text += $"\n\n{gameManager.FinalRating}\nScore: {gameManager.FinalScore:F0}\n\n{gameManager.ScoreBreakdown}";
                gameOverText.text = text;
                gameOverText.gameObject.SetActive(true);
            }
            else
            {
                gameOverText.gameObject.SetActive(false);
            }
        }
    }

    void UpdateRegionInfo(bool isGameOver)
    {
        if (displayText == null) return;

        Region selected = regionManager.SelectedRegion;
        Region hovered = regionManager.HoveredRegion;

        string crisisHex = ColorUtility.ToHtmlStringRGB(crisisTextColor);
        string stressedHex = ColorUtility.ToHtmlStringRGB(stressedTextColor);

        if (selected != null)
        {
            string neighbors = selected.Neighbors.Count > 0
                ? string.Join(", ", selected.Neighbors.ConvertAll(n => n.RegionName))
                : "none";

            string status = "";
            if (gameManager != null && gameManager.IsCrisis(selected)) status = $"  <color=#{crisisHex}>CRISIS</color>";
            else if (gameManager != null && gameManager.IsStressed(selected)) status = $"  <color=#{stressedHex}>STRESSED</color>";

            string preview = BuildCardPreview(selected, isGameOver);

            // show focus risk and warning during gameplay
            string focusInfo = "";
            string focusWarning = "";
            if (!isGameOver && gameManager != null)
            {
                float focusPct = gameManager.GetFocusPercent(selected);
                if (focusPct > 0f)
                {
                    string focusHex = focusPct >= 60f ? crisisHex : stressedHex;
                    focusInfo = $"  <color=#{focusHex}>Focus: {focusPct:F0}%</color>";
                }

                // persistent warning when region has been flagged
                if (gameManager.IsRegionFocusWarned(selected))
                    focusWarning = $"\n<color=#{stressedHex}>WARNING: Over-governed! Continued focus risks penalties.</color>";
            }

            displayText.text = $"<b>{selected.RegionName}</b> ({selected.Trait}){status}\n"
                + $"C: {selected.CarbonLevel:F0}  E: {selected.EconomyLevel:F0}  S: {selected.StabilityLevel:F0}{focusInfo}"
                + preview
                + focusWarning
                + $"\nNeighbors: {neighbors}";
        }
        else if (hovered != null)
        {
            displayText.text = $"{hovered.RegionName} ({hovered.Trait})\n"
                + $"C: {hovered.CarbonLevel:F0}  E: {hovered.EconomyLevel:F0}  S: {hovered.StabilityLevel:F0}";
        }
        else
        {
            displayText.text = "";
        }

        // hide background when there's nothing to show + apply live styling
        if (regionInfoBackground != null)
        {
            regionInfoBackground.enabled = !string.IsNullOrEmpty(displayText.text);
            ApplyBackgroundStyle();
            SyncBackgroundToText();
        }
    }

    // resize + reposition the background so it hugs the text with padding.
    // copies anchors/pivot from the text once so designers only have to position the text.
    void SyncBackgroundToText()
    {
        if (regionInfoBackground == null || displayText == null) return;
        if (!regionInfoBackground.enabled) return;

        var textRect = displayText.rectTransform;
        var bgRect = regionInfoBackground.rectTransform;

        // height-only auto-fit: keep whatever width was set in the inspector,
        // only resize height to match text + padding
        displayText.ForceMeshUpdate();
        float h = displayText.preferredHeight + bgPadding.y * 2f;
        Vector2 size = bgRect.sizeDelta;
        size.y = h;
        bgRect.sizeDelta = size;
    }

    // builds a 9-sliced rounded-rect sprite with border + accent and assigns it to the bg image.
    // re-runs only when style fields change (cheap on most frames).
    void ApplyBackgroundStyle()
    {
        if (regionInfoBackground == null) return;

        int size = Mathf.Max(16, bgSpriteSize);
        int radius = Mathf.Clamp(bgCornerRadius, 0, size / 2 - 1);
        int border = Mathf.Clamp(bgBorderThickness, 0, radius);

        // hash inputs so we only rebuild when something visible changes
        int key = size * 73856093 ^ radius * 19349663 ^ border * 83492791
            ^ bgFillColor.GetHashCode() ^ bgBorderColor.GetHashCode() ^ bgAccentColor.GetHashCode();
        if (key != cachedBgKey || generatedBgSprite == null)
        {
            generatedBgSprite = BuildRoundedRectSprite(size, radius, border, bgFillColor, bgBorderColor, bgAccentColor);
            cachedBgKey = key;
        }

        regionInfoBackground.sprite = generatedBgSprite;
        regionInfoBackground.type = Image.Type.Sliced;
        regionInfoBackground.pixelsPerUnitMultiplier = 1f;
        // color tinting kept neutral — actual colors are baked into the sprite
        regionInfoBackground.color = Color.white;
    }

    static Sprite BuildRoundedRectSprite(int size, int radius, int border, Color fill, Color borderCol, Color accent)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color[size * size];
        float r = radius;
        float r2 = r * r;
        float innerR = Mathf.Max(0f, r - border);
        float innerR2 = innerR * innerR;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // distance from nearest corner center (only relevant inside the corner box)
                float dx = 0f, dy = 0f;
                if (x < radius) dx = radius - 0.5f - x;
                else if (x >= size - radius) dx = x - (size - radius) + 0.5f;
                if (y < radius) dy = radius - 0.5f - y;
                else if (y >= size - radius) dy = y - (size - radius) + 0.5f;

                float d2 = dx * dx + dy * dy;
                bool inCorner = (x < radius || x >= size - radius) && (y < radius || y >= size - radius);

                Color c;
                if (inCorner)
                {
                    if (d2 > r2) c = new Color(0, 0, 0, 0);                // outside the rounded corner → transparent
                    else if (d2 > innerR2 && border > 0) c = borderCol;    // border ring
                    else c = fill;                                          // inside corner fill
                }
                else
                {
                    // straight edges
                    int edgeDist = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                    if (edgeDist < border && border > 0) c = borderCol;
                    else c = fill;
                }

                // soft inner accent: thin highlight line just inside the border (top edge only feel)
                if (accent.a > 0f && c.a > 0f)
                {
                    int edgeDist = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                    if (edgeDist == border + 1)
                        c = Color.Lerp(c, accent, accent.a);
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // 9-slice borders just outside the corner radius so the middle stretches cleanly
        int slice = radius + border + 1;
        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(slice, slice, slice, slice));
        sprite.name = "RegionInfoBgGenerated";
        return sprite;
    }

    void UpdateCardPlayText(bool isGameOver)
    {
        if (cardPlayText == null || regionSelector == null) return;

        if (isGameOver) { cardPlayText.gameObject.SetActive(false); return; }

        float elapsed = Time.time - regionSelector.LastPlayTime;
        float totalDuration = cardPlayHoldDuration + cardPlayFadeDuration;

        if (elapsed < totalDuration && regionSelector.LastPlayResult != null)
        {
            cardPlayText.text = regionSelector.LastPlayResult;
            cardPlayText.gameObject.SetActive(true);

            // fade out after hold period
            if (elapsed > cardPlayHoldDuration)
            {
                float fadeT = (elapsed - cardPlayHoldDuration) / cardPlayFadeDuration;
                float alpha = 1f - Mathf.Clamp01(fadeT);
                cardPlayText.alpha = alpha;
            }
            else
            {
                cardPlayText.alpha = 1f;
            }
        }
        else
        {
            cardPlayText.gameObject.SetActive(false);
            cardPlayText.alpha = 1f;
        }
    }

    // shows projected stat changes when a card is selected
    string BuildCardPreview(Region region, bool isGameOver)
    {
        if (isGameOver || handDisplay == null) return "";

        var card = handDisplay.SelectedCard;
        if (card == null) return "";

        card.GetModifiedDeltas(region, out float carbon, out float economy, out float stability);

        float newC = Mathf.Clamp(region.CarbonLevel + carbon, 0f, 100f);
        float newE = Mathf.Clamp(region.EconomyLevel + economy, 0f, 100f);
        float newS = Mathf.Clamp(region.StabilityLevel + stability, 0f, 100f);

        string goodHex = ColorUtility.ToHtmlStringRGB(previewGoodColor);
        string badHex = ColorUtility.ToHtmlStringRGB(previewBadColor);

        // carbon: lower is better, economy/stability: higher is better
        string cColor = carbon <= 0 ? goodHex : badHex;
        string eColor = economy >= 0 ? goodHex : badHex;
        string sColor = stability >= 0 ? goodHex : badHex;

        return $"\n<b>{card.policyName}:</b>"
            + $" <color=#{cColor}>C:{newC:F0}</color>"
            + $" <color=#{eColor}>E:{newE:F0}</color>"
            + $" <color=#{sColor}>S:{newS:F0}</color>";
    }

    void UpdateGlobalStats(bool isGameOver)
    {
        if (globalStatsText == null || gameManager == null || regionManager == null) return;

        if (isGameOver) { globalStatsText.gameObject.SetActive(false); return; }

        var regions = regionManager.Regions;
        if (regions == null || regions.Count == 0) { globalStatsText.gameObject.SetActive(false); return; }

        float totalCarbon = 0f, totalEcon = 0f, totalStab = 0f;
        int stressed = 0, crisis = 0;
        foreach (var r in regions)
        {
            totalCarbon += r.CarbonLevel;
            totalEcon += r.EconomyLevel;
            totalStab += r.StabilityLevel;
            if (gameManager != null && gameManager.IsCrisis(r)) crisis++;
            else if (gameManager != null && gameManager.IsStressed(r)) stressed++;
        }

        int count = regions.Count;
        string stressedHex = ColorUtility.ToHtmlStringRGB(stressedTextColor);
        string crisisHex = ColorUtility.ToHtmlStringRGB(crisisTextColor);

        string text = $"Avg C: {totalCarbon / count:F0}  E: {totalEcon / count:F0}  S: {totalStab / count:F0}";
        if (stressed > 0) text += $"  <color=#{stressedHex}>Stressed: {stressed}</color>";
        if (crisis > 0) text += $"  <color=#{crisisHex}>Crisis: {crisis}</color>";

        // event log
        if (gameManager.EventLog.Count > 0)
        {
            text += "\n<size=85%>";
            foreach (var entry in gameManager.EventLog)
                text += $"\n  {entry}";
            text += "</size>";
        }

        globalStatsText.text = text;
        globalStatsText.gameObject.SetActive(true);
    }

    void CacheOriginalPosition(RectTransform rect)
    {
        if (rect == null || originalPositions.ContainsKey(rect)) return;
        originalPositions[rect] = rect.anchoredPosition;
    }

    void SlideRect(RectTransform rect, float offsetX, float offsetY)
    {
        if (rect == null) return;
        CacheOriginalPosition(rect);
        Vector2 origin = originalPositions[rect];
        Vector2 target = origin + new Vector2(offsetX * shopSlideBlend, offsetY * shopSlideBlend);
        rect.anchoredPosition = target;
    }

    void SlideElement(TMP_Text text, float offsetX, float offsetY)
    {
        if (text == null) return;
        SlideRect(text.GetComponent<RectTransform>(), offsetX, offsetY);
    }

    void UpdateShopSlide()
    {
        if (gameManager == null) return;

        float target = gameManager.ShopActive ? 1f : 0f;
        shopSlideBlend = Mathf.Lerp(shopSlideBlend, target, shopSlideSpeed * Time.deltaTime);
        if (Mathf.Abs(shopSlideBlend - target) < 0.005f) shopSlideBlend = target;

        // skip work when fully restored
        if (shopSlideBlend < 0.001f) return;

        // top bar → slide up
        SlideElement(gameStateText, 0f, topSlideDistance);
        // global stats → slide down
        SlideElement(globalStatsText, 0f, -bottomSlideDistance);
        // region info → slide left
        SlideElement(displayText, -leftSlideDistance, 0f);
        if (regionInfoBackground != null)
            SlideRect(regionInfoBackground.rectTransform, -leftSlideDistance, 0f);
        // card play text → slide down
        SlideElement(cardPlayText, 0f, -bottomSlideDistance);
    }
}
