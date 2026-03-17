using UnityEngine;
using TMPro;

public class RegionDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private TMP_Text eventText;
    [SerializeField] private TMP_Text cardPlayText;
    [SerializeField] private TMP_Text gameStateText;
    [SerializeField] private TMP_Text gameOverText;

    [Header("Display Durations")]
    [SerializeField] private float eventDisplayDuration = 6f;
    [SerializeField] private float cardPlayDisplayDuration = 4f;

    [Header("Health Warning Colors")]
    [SerializeField] private Color stressedTextColor = new Color(1f, 0.67f, 0.27f);
    [SerializeField] private Color crisisTextColor = new Color(1f, 0.27f, 0.27f);
    [SerializeField] private Color cooldownTextColor = new Color(0.53f, 0.53f, 0.53f);

    private RegionManager regionManager;
    private GameManager gameManager;
    private RegionSelector regionSelector;

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

        bool isGameOver = gameManager != null && gameManager.GameOver;

        UpdateGameState();
        UpdateRegionInfo(isGameOver);
        UpdateEventText(isGameOver);
        UpdateCardPlayText(isGameOver);
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
                gameStateText.text = $"Round {gameManager.CurrentRound}/10   Actions: {gameManager.ActionsRemaining}/3   Carbon: {gameManager.GetGlobalCarbon():F0}";
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
        string cooldownHex = ColorUtility.ToHtmlStringRGB(cooldownTextColor);

        if (selected != null)
        {
            string neighbors = selected.Neighbors.Count > 0
                ? string.Join(", ", selected.Neighbors.ConvertAll(n => n.RegionName))
                : "none";

            string status = "";
            if (selected.CarbonLevel > 85f) status = $"  <color=#{crisisHex}>CRISIS</color>";
            else if (selected.CarbonLevel > 70f) status = $"  <color=#{stressedHex}>STRESSED</color>";

            // cooldown indicator (only during gameplay)
            string cooldown = "";
            if (!isGameOver && gameManager != null && gameManager.IsTargetedThisRound(selected))
                cooldown = $"\n<color=#{cooldownHex}>Already targeted this round</color>";

            displayText.text = $"<b>{selected.RegionName}</b> ({selected.Trait}){status}\n"
                + $"C: {selected.CarbonLevel:F0}  E: {selected.EconomyLevel:F0}  S: {selected.StabilityLevel:F0}"
                + cooldown
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

    void UpdateEventText(bool isGameOver)
    {
        if (eventText == null || gameManager == null) return;

        if (isGameOver) { eventText.gameObject.SetActive(false); return; }

        if (Time.time - gameManager.LastEventTime < eventDisplayDuration && gameManager.LastEventText != null)
        {
            eventText.text = gameManager.LastEventText;
            eventText.gameObject.SetActive(true);
        }
        else
        {
            eventText.gameObject.SetActive(false);
        }
    }

    void UpdateCardPlayText(bool isGameOver)
    {
        if (cardPlayText == null || regionSelector == null) return;

        if (isGameOver) { cardPlayText.gameObject.SetActive(false); return; }

        if (Time.time - regionSelector.LastPlayTime < cardPlayDisplayDuration && regionSelector.LastPlayResult != null)
        {
            cardPlayText.text = regionSelector.LastPlayResult;
            cardPlayText.gameObject.SetActive(true);
        }
        else
        {
            cardPlayText.gameObject.SetActive(false);
        }
    }
}
