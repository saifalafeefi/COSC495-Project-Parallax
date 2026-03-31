using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RegionDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private TMP_Text cardPlayText;
    [SerializeField] private TMP_Text gameStateText;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private TMP_Text globalStatsText;

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

    // shop slide state per text element
    private Dictionary<TMP_Text, Vector2> originalPositions = new Dictionary<TMP_Text, Vector2>();
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
                gameStateText.text = $"Round {gameManager.CurrentRound}/10   Capital: {gameManager.PoliticalCapital}/{gameManager.MaxCapital}{bonusTag}{stabMult}   Funds: {gameManager.Funds}";
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

    void CacheOriginalPosition(TMP_Text text)
    {
        if (text == null || originalPositions.ContainsKey(text)) return;
        var rect = text.GetComponent<RectTransform>();
        if (rect != null)
            originalPositions[text] = rect.anchoredPosition;
    }

    void SlideElement(TMP_Text text, float offsetX, float offsetY)
    {
        if (text == null) return;
        CacheOriginalPosition(text);
        var rect = text.GetComponent<RectTransform>();
        if (rect == null) return;
        Vector2 origin = originalPositions[text];
        Vector2 target = origin + new Vector2(offsetX * shopSlideBlend, offsetY * shopSlideBlend);
        rect.anchoredPosition = target;
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
        // card play text → slide down
        SlideElement(cardPlayText, 0f, -bottomSlideDistance);
    }
}
