using UnityEngine;
using TMPro;

public class RegionDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private TMP_Text eventText;
    [SerializeField] private TMP_Text cardPlayText;
    [SerializeField] private TMP_Text gameStateText;

    [Header("Display Durations")]
    [SerializeField] private float eventDisplayDuration = 6f;
    [SerializeField] private float cardPlayDisplayDuration = 4f;

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

        UpdateGameState();
        UpdateRegionInfo();
        UpdateEventText();
        UpdateCardPlayText();
    }

    void UpdateGameState()
    {
        if (gameStateText == null || gameManager == null) return;

        if (gameManager.GameOver)
        {
            gameStateText.text = $"GAME OVER\n{gameManager.GameOverReason}";
            if (gameManager.FinalRating != null)
                gameStateText.text += $"\n{gameManager.FinalScore:F0} — {gameManager.FinalRating}";
        }
        else
        {
            gameStateText.text = $"Round {gameManager.CurrentRound}/10   Actions: {gameManager.ActionsRemaining}/3   Carbon: {gameManager.GetGlobalCarbon():F0}";
        }
    }

    void UpdateRegionInfo()
    {
        if (displayText == null) return;

        Region selected = regionManager.SelectedRegion;
        Region hovered = regionManager.HoveredRegion;

        if (selected != null)
        {
            string neighbors = selected.Neighbors.Count > 0
                ? string.Join(", ", selected.Neighbors.ConvertAll(n => n.RegionName))
                : "none";

            displayText.text = $"<b>{selected.RegionName}</b> ({selected.Trait})\n"
                + $"C: {selected.CarbonLevel:F0}  E: {selected.EconomyLevel:F0}  S: {selected.StabilityLevel:F0}\n"
                + $"Neighbors: {neighbors}";
        }
        else if (hovered != null)
        {
            displayText.text = $"{hovered.RegionName} ({hovered.Trait})";
        }
        else
        {
            displayText.text = "";
        }
    }

    void UpdateEventText()
    {
        if (eventText == null || gameManager == null) return;

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

    void UpdateCardPlayText()
    {
        if (cardPlayText == null || regionSelector == null) return;

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
