using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Data Folders (Resources)")]
    [Tooltip("path inside Resources/ to common policies")]
    [SerializeField] private string commonPath = "Policies/Common";
    [Tooltip("path inside Resources/ to uncommon policies")]
    [SerializeField] private string uncommonPath = "Policies/Uncommon";
    [Tooltip("path inside Resources/ to rare policies")]
    [SerializeField] private string rarePath = "Policies/Rare";
    [Tooltip("path inside Resources/ to events")]
    [SerializeField] private string eventsPath = "Events";

    [Header("Game Settings")]
    [SerializeField] private int totalRounds = 10;
    [SerializeField] private int actionsPerRound = 3;

    public int CurrentRound { get; private set; }
    public int ActionsRemaining { get; private set; }
    public bool GameOver { get; private set; }
    public string GameOverReason { get; private set; }
    public float FinalScore { get; private set; }
    public string FinalRating { get; private set; }
    public string ScoreBreakdown { get; private set; }

    // the 3 cards in hand this round
    public List<PolicyData> CurrentHand { get; private set; }

    // last event that fired, for UI display
    public string LastEventText { get; private set; }
    public float LastEventTime { get; private set; }

    // rolling log of recent events
    public List<string> EventLog { get; private set; } = new List<string>();
    private const int maxEventLog = 5;

    private EventData[] allEvents;
    private List<PolicyData> deck;
    private List<PolicyData> discardPile;

    public int DeckCount => deck != null ? deck.Count : 0;
    public int DiscardCount => discardPile != null ? discardPile.Count : 0;
    private int crisisCount;
    private bool chainCollapseWarning;

    // tracks which regions already received a card this round
    private HashSet<Region> targetedThisRound;

    private RegionManager regionManager;

    private bool started;

    void Update()
    {
        // wait for the earth to be spawned before starting
        if (!started)
        {
            regionManager = FindFirstObjectByType<RegionManager>();
            if (regionManager == null) return;

            started = true;
            StartGame();
        }

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        // debug: press G to force game over for testing
        if (kb.gKey.wasPressedThisFrame && !GameOver)
        {
            CalculateScore(regionManager.Regions);
            GameOver = true;
            GameOverReason = "[DEBUG] Forced game over.";
            Debug.Log("[GameManager] DEBUG: forced game over");
        }

        // press space to skip remaining actions and end round early
        if (kb.spaceKey.wasPressedThisFrame && !GameOver && !PauseMenu.IsPaused && ActionsRemaining > 0)
        {
            // discard remaining hand
            foreach (var card in CurrentHand)
                discardPile.Add(card);
            CurrentHand.Clear();
            ActionsRemaining = 0;
            Debug.Log("[GameManager] skipped remaining actions");
            EndRound();
        }
    }

    void StartGame()
    {
        GameOver = false;
        GameOverReason = null;
        crisisCount = 0;
        EventLog.Clear();
        chainCollapseWarning = false;
        CurrentRound = 0;
        FinalScore = 0;
        FinalRating = null;

        // auto-load policies from Resources folders
        var commonPolicies = Resources.LoadAll<PolicyData>(commonPath);
        var uncommonPolicies = Resources.LoadAll<PolicyData>(uncommonPath);
        var rarePolicies = Resources.LoadAll<PolicyData>(rarePath);
        allEvents = Resources.LoadAll<EventData>(eventsPath);

        deck = new List<PolicyData>();
        deck.AddRange(commonPolicies);
        deck.AddRange(uncommonPolicies);
        deck.AddRange(rarePolicies);

        Debug.Log($"[GameManager] loaded {commonPolicies.Length} common, {uncommonPolicies.Length} uncommon, {rarePolicies.Length} rare, {allEvents.Length} events");

        discardPile = new List<PolicyData>();
        CurrentHand = new List<PolicyData>();
        targetedThisRound = new HashSet<Region>();

        ShuffleDeck();
        Debug.Log($"[GameManager] deck built with {deck.Count} cards");

        StartRound();
    }

    void StartRound()
    {
        CurrentRound++;
        ActionsRemaining = actionsPerRound;
        targetedThisRound.Clear();

        // draw 3 cards
        CurrentHand.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (deck.Count == 0)
            {
                // reshuffle discard into deck
                deck.AddRange(discardPile);
                discardPile.Clear();
                ShuffleDeck();
                Debug.Log("[GameManager] reshuffled discard pile into deck");
            }

            if (deck.Count > 0)
            {
                CurrentHand.Add(deck[0]);
                deck.RemoveAt(0);
            }
        }

        string cardNames = string.Join(", ", CurrentHand.ConvertAll(c => c.policyName));
        Debug.Log($"[GameManager] round {CurrentRound} | drew: {cardNames}");
    }

    // called when the player presses 1, 2, or 3 with a region selected
    public string PlayCard(int cardIndex, Region target)
    {
        if (GameOver) return "Game is over.";
        if (cardIndex < 0 || cardIndex >= CurrentHand.Count) return "Invalid card.";
        if (target == null) return "No region selected.";
        if (targetedThisRound.Contains(target)) return $"{target.RegionName} already targeted this round.";

        var card = CurrentHand[cardIndex];
        card.GetModifiedDeltas(target, out float carbon, out float economy, out float stability);

        // apply to target region
        float oldCarbon = target.CarbonLevel;
        float oldEconomy = target.EconomyLevel;
        float oldStability = target.StabilityLevel;

        target.CarbonLevel = Mathf.Clamp(target.CarbonLevel + carbon, 0f, 100f);
        target.EconomyLevel = Mathf.Clamp(target.EconomyLevel + economy, 0f, 100f);
        target.StabilityLevel = Mathf.Clamp(target.StabilityLevel + stability, 0f, 100f);

        // spillover to neighbors
        float spill = card.GetSpillover();
        foreach (var neighbor in target.Neighbors)
        {
            neighbor.CarbonLevel = Mathf.Clamp(neighbor.CarbonLevel + carbon * spill, 0f, 100f);
            neighbor.EconomyLevel = Mathf.Clamp(neighbor.EconomyLevel + economy * spill, 0f, 100f);
            neighbor.StabilityLevel = Mathf.Clamp(neighbor.StabilityLevel + stability * spill, 0f, 100f);
        }

        // move card to discard and mark region as targeted
        discardPile.Add(card);
        CurrentHand.RemoveAt(cardIndex);
        ActionsRemaining--;
        targetedThisRound.Add(target);

        string result = $"{card.policyName} → {target.RegionName}";

        Debug.Log($"[GameManager] {result}");

        if (ActionsRemaining <= 0)
            EndRound();

        return result;
    }

    void EndRound()
    {
        var regions = regionManager.Regions;
        if (regions == null) return;

        // 0. global carbon drift — the world gets worse on its own
        foreach (var r in regions)
            r.CarbonLevel = Mathf.Clamp(r.CarbonLevel + 1f, 0f, 100f);

        // 1. passive trait effects
        foreach (var r in regions)
        {
            switch (r.Trait)
            {
                case RegionTrait.Tropical:
                    r.CarbonLevel = Mathf.Clamp(r.CarbonLevel - 1f, 0f, 100f);
                    break;
                case RegionTrait.Temperate:
                    r.StabilityLevel = Mathf.Clamp(r.StabilityLevel + 2f, 0f, 100f);
                    break;
                case RegionTrait.Coastal:
                    r.EconomyLevel = Mathf.Clamp(r.EconomyLevel + 3f, 0f, 100f);
                    break;
                case RegionTrait.Industrial:
                    r.CarbonLevel = Mathf.Clamp(r.CarbonLevel + 2f, 0f, 100f);
                    break;
            }
        }

        // 2. crisis spillover: carbon > 85 spreads to neighbors
        foreach (var r in regions)
        {
            if (r.CarbonLevel > 85f)
            {
                foreach (var n in r.Neighbors)
                {
                    n.CarbonLevel = Mathf.Clamp(n.CarbonLevel + 5f, 0f, 100f);
                    n.StabilityLevel = Mathf.Clamp(n.StabilityLevel - 3f, 0f, 100f);
                }
            }
        }

        // 3. recovery
        foreach (var r in regions)
        {
            if (r.CarbonLevel < 30f)
                r.CarbonLevel = Mathf.Clamp(r.CarbonLevel - 2f, 0f, 100f);
            if (r.StabilityLevel < 20f)
                r.StabilityLevel = Mathf.Clamp(r.StabilityLevel + 3f, 0f, 100f);
        }

        // 4. penalties
        foreach (var r in regions)
        {
            if (r.CarbonLevel > 70f)
                r.StabilityLevel = Mathf.Clamp(r.StabilityLevel - 2f, 0f, 100f);

            if (r.CarbonLevel > 85f)
            {
                r.EconomyLevel = Mathf.Clamp(r.EconomyLevel - 10f, 0f, 100f);
                r.StabilityLevel = Mathf.Clamp(r.StabilityLevel - 10f, 0f, 100f);
                crisisCount++;
            }
        }

        // 5. positive spillover: carbon < 20 helps neighbors
        foreach (var r in regions)
        {
            if (r.CarbonLevel < 20f)
            {
                foreach (var n in r.Neighbors)
                    n.StabilityLevel = Mathf.Clamp(n.StabilityLevel + 2f, 0f, 100f);
            }
        }

        // 6. check game over before event on final round
        Debug.Log($"[GameManager] end of round {CurrentRound} | global carbon: {GetGlobalCarbon():F1}");

        if (CheckGameOver(regions))
            return;

        if (CurrentRound >= totalRounds)
        {
            // survived all rounds, calculate score
            CalculateScore(regions);
            GameOver = true;
            GameOverReason = "You survived all 10 rounds.";
            Debug.Log($"[GameManager] game complete! score: {FinalScore:F0} — {FinalRating}");
            return;
        }

        // draw and apply event only if the game continues
        ApplyRandomEvent(regions);

        StartRound();
    }

    void ApplyRandomEvent(List<Region> regions)
    {
        if (allEvents == null || allEvents.Length == 0) return;

        var evt = allEvents[Random.Range(0, allEvents.Length)];
        var affected = evt.GetAffectedRegions(regions);

        foreach (var r in affected)
        {
            r.CarbonLevel = Mathf.Clamp(r.CarbonLevel + evt.carbonDelta, 0f, 100f);
            r.EconomyLevel = Mathf.Clamp(r.EconomyLevel + evt.economyDelta, 0f, 100f);
            r.StabilityLevel = Mathf.Clamp(r.StabilityLevel + evt.stabilityDelta, 0f, 100f);
        }

        LastEventText = $"{evt.eventName}\n{evt.description}\n({affected.Count} region{(affected.Count != 1 ? "s" : "")} affected)";
        LastEventTime = Time.time;

        // add to rolling event log
        string logEntry = $"R{CurrentRound}: {evt.eventName} ({affected.Count} region{(affected.Count != 1 ? "s" : "")})";
        EventLog.Add(logEntry);
        if (EventLog.Count > maxEventLog)
            EventLog.RemoveAt(0);
    }

    bool CheckGameOver(List<Region> regions)
    {
        float globalCarbon = GetGlobalCarbon();

        // tipping point
        if (globalCarbon > 90f)
        {
            GameOver = true;
            GameOverReason = "Tipping Point — global carbon exceeded 90.";
            Debug.Log($"[GameManager] GAME OVER: {GameOverReason}");
            return true;
        }

        // chain collapse: 3+ regions in crisis
        var crisisRegionNames = new List<string>();
        foreach (var r in regions)
        {
            if (r.CarbonLevel > 85f)
                crisisRegionNames.Add(r.RegionName);
        }

        if (crisisRegionNames.Count >= 3)
        {
            if (chainCollapseWarning)
            {
                string names = string.Join(", ", crisisRegionNames);
                GameOver = true;
                GameOverReason = $"Chain Collapse — {crisisRegionNames.Count} regions in crisis for consecutive rounds:\n{names}";
                Debug.Log($"[GameManager] GAME OVER: {GameOverReason}");
                return true;
            }
            else
            {
                chainCollapseWarning = true;
                Debug.LogWarning("[GameManager] WARNING: 3+ regions in crisis — collapse imminent next round!");
            }
        }
        else if (crisisRegionNames.Count < 3)
        {
            chainCollapseWarning = false;
        }

        return false;
    }

    void CalculateScore(List<Region> regions)
    {
        int thriving = 0;
        int healthy = 0;
        float totalStability = 0f;
        float totalEconomy = 0f;

        foreach (var r in regions)
        {
            if (r.CarbonLevel < 30f) thriving++;
            if (r.CarbonLevel < 50f) healthy++;
            totalStability += r.StabilityLevel;
            totalEconomy += r.EconomyLevel;
        }

        float avgStability = totalStability / regions.Count;
        float avgEconomy = totalEconomy / regions.Count;
        float globalCarbon = GetGlobalCarbon();

        float thrivingPts = thriving * 25f;
        float healthyPts = healthy * 15f;
        float stabilityPts = avgStability * 0.5f;
        float economyPts = avgEconomy * 0.5f;
        float carbonPenalty = globalCarbon * 1.5f;
        float crisisPenalty = crisisCount * 10f;

        FinalScore = thrivingPts + healthyPts + stabilityPts + economyPts - carbonPenalty - crisisPenalty;

        ScoreBreakdown = $"Thriving regions ({thriving}): +{thrivingPts:F0}\n"
            + $"Healthy regions ({healthy}): +{healthyPts:F0}\n"
            + $"Avg Stability ({avgStability:F0}): +{stabilityPts:F0}\n"
            + $"Avg Economy ({avgEconomy:F0}): +{economyPts:F0}\n"
            + $"Global Carbon ({globalCarbon:F0}): -{carbonPenalty:F0}\n"
            + $"Crisis events ({crisisCount}): -{crisisPenalty:F0}";

        if (FinalScore >= 100f) FinalRating = "Sustainable Future (Gold)";
        else if (FinalScore >= 75f) FinalRating = "Stable Transition (Silver)";
        else if (FinalScore >= 50f) FinalRating = "Fragile Balance (Bronze)";
        else FinalRating = "Environmental Failure";
    }

    // called by PauseMenu to start a completely fresh game
    public void RestartGame()
    {
        // reset region stats
        if (regionManager != null)
            regionManager.ResetAllRegions();

        // clear event display
        LastEventText = null;
        LastEventTime = 0f;

        StartGame();
    }

    // returns true if a region already got a card this round
    public bool IsTargetedThisRound(Region region)
    {
        return targetedThisRound != null && targetedThisRound.Contains(region);
    }

    public float GetGlobalCarbon()
    {
        if (regionManager == null || regionManager.Regions == null || regionManager.Regions.Count == 0)
            return 0f;

        float total = 0f;
        foreach (var r in regionManager.Regions)
            total += r.CarbonLevel;
        return total / regionManager.Regions.Count;
    }

    void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }
}
