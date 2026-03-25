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
    [Tooltip("path inside Resources/ to normal events")]
    [SerializeField] private string normalEventsPath = "Events/Normal";
    [Tooltip("path inside Resources/ to focus events")]
    [SerializeField] private string focusEventsPath = "Events/Focus";

    [Header("Game Settings")]
    [SerializeField] private int totalRounds = 10;
    [SerializeField] private int handSize = 6;

    [Header("Focus System")]
    [Tooltip("chance per play above average to trigger a focus event (e.g. 20 = 20% per extra play)")]
    [SerializeField] private float focusChancePerPlay = 20f;

    public int CurrentRound { get; private set; }
    public int ActionsRemaining { get; private set; }
    public int HandSize { get; private set; }
    public bool GameOver { get; private set; }
    public string GameOverReason { get; private set; }
    public float FinalScore { get; private set; }
    public string FinalRating { get; private set; }
    public string ScoreBreakdown { get; private set; }

    // cards in hand this round
    public List<PolicyData> CurrentHand { get; private set; }

    // last event that fired, for UI display
    public string LastEventText { get; private set; }
    public float LastEventTime { get; private set; }

    // round summary shown briefly after end-of-round processing
    public string RoundSummaryText { get; private set; }
    public float RoundSummaryTime { get; private set; }

    // rolling log of recent events
    public List<string> EventLog { get; private set; } = new List<string>();
    private const int maxEventLog = 5;

    private EventData[] allEvents;
    private List<PolicyData> deck;
    private List<PolicyData> discardPile;

    // snapshot taken at start of round so summary captures card play + end-of-round changes
    private float snapshotGlobalCarbon;
    private Dictionary<Region, int> snapshotStatus = new Dictionary<Region, int>();

    public int DeckCount => deck != null ? deck.Count : 0;
    public int DiscardCount => discardPile != null ? discardPile.Count : 0;
    private int crisisCount;
    private bool chainCollapseWarning;

    // last focus warning text for UI
    public string LastWarningText { get; private set; }
    public float LastWarningTime { get; private set; }

    // tracks how many cards each region has received across all rounds
    private Dictionary<Region, int> totalPlaysPerRegion = new Dictionary<Region, int>();

    // regions warned about over-targeting — value = number of punishments applied (0 = warned only)
    private Dictionary<Region, int> focusWarnedRegions = new Dictionary<Region, int>();


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
        focusWarnedRegions.Clear();
        LastWarningText = null;
        LastWarningTime = 0f;
        CurrentRound = 0;
        FinalScore = 0;
        FinalRating = null;

        // auto-load policies from Resources folders
        var commonPolicies = Resources.LoadAll<PolicyData>(commonPath);
        var uncommonPolicies = Resources.LoadAll<PolicyData>(uncommonPath);
        var rarePolicies = Resources.LoadAll<PolicyData>(rarePath);
        var normalEvents = Resources.LoadAll<EventData>(normalEventsPath);
        var focusEvents = Resources.LoadAll<EventData>(focusEventsPath);

        // combine all events into one array
        allEvents = new EventData[normalEvents.Length + focusEvents.Length];
        normalEvents.CopyTo(allEvents, 0);
        focusEvents.CopyTo(allEvents, normalEvents.Length);

        deck = new List<PolicyData>();
        deck.AddRange(commonPolicies);
        deck.AddRange(uncommonPolicies);
        deck.AddRange(rarePolicies);

        Debug.Log($"[GameManager] loaded {commonPolicies.Length} common, {uncommonPolicies.Length} uncommon, {rarePolicies.Length} rare, {normalEvents.Length} normal events, {focusEvents.Length} focus events");

        discardPile = new List<PolicyData>();
        CurrentHand = new List<PolicyData>();
        totalPlaysPerRegion.Clear();

        ShuffleDeck();
        Debug.Log($"[GameManager] deck built with {deck.Count} cards");

        StartRound();
    }

    void StartRound()
    {
        CurrentRound++;

        // snapshot region status before player acts so the summary captures everything
        SnapshotStatus();

        HandSize = handSize;
        ActionsRemaining = HandSize;

        CurrentHand.Clear();
        for (int i = 0; i < HandSize; i++)
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

        // track plays for focus system
        if (!totalPlaysPerRegion.ContainsKey(target))
            totalPlaysPerRegion[target] = 0;
        totalPlaysPerRegion[target]++;

        // real-time decay: playing on a region reduces all OTHER regions' counts by 1
        var decayKeys = new List<Region>(totalPlaysPerRegion.Keys);
        foreach (var r in decayKeys)
        {
            if (r == target) continue;
            totalPlaysPerRegion[r]--;
            if (totalPlaysPerRegion[r] <= 0)
            {
                totalPlaysPerRegion.Remove(r);
                focusWarnedRegions.Remove(r);
            }
            else if (GetFocusPercent(r) <= 0f)
            {
                // focus % dropped to 0 — clear warning so they get a fresh one next time
                focusWarnedRegions.Remove(r);
            }
        }

        // check focus: warn or punish immediately on card play
        string focusResult = CheckFocusOnPlay(target);

        // move card to discard
        discardPile.Add(card);
        CurrentHand.RemoveAt(cardIndex);
        ActionsRemaining--;

        string result = $"{card.policyName} → {target.RegionName}";
        if (focusResult != null)
            result += $"\n{focusResult}";

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
        {
            BuildRoundSummary(regions, snapshotGlobalCarbon, snapshotStatus);
            return;
        }

        if (CurrentRound >= totalRounds)
        {
            // survived all rounds, calculate score
            CalculateScore(regions);
            GameOver = true;
            GameOverReason = "You survived all 10 rounds.";
            BuildRoundSummary(regions, snapshotGlobalCarbon, snapshotStatus);
            Debug.Log($"[GameManager] game complete! score: {FinalScore:F0} — {FinalRating}");
            return;
        }

        // draw and apply event only if the game continues
        ApplyRandomEvent(regions);

        // build round summary AFTER event so it captures everything
        BuildRoundSummary(regions, snapshotGlobalCarbon, snapshotStatus);

        StartRound();
    }

    void SnapshotStatus()
    {
        snapshotGlobalCarbon = GetGlobalCarbon();
        snapshotStatus.Clear();

        if (regionManager == null || regionManager.Regions == null) return;

        foreach (var r in regionManager.Regions)
        {
            if (r.CarbonLevel > 85f) snapshotStatus[r] = 2;
            else if (r.CarbonLevel > 70f) snapshotStatus[r] = 1;
            else snapshotStatus[r] = 0;
        }
    }

    void BuildRoundSummary(List<Region> regions, float oldGlobalCarbon, Dictionary<Region, int> oldStatus)
    {
        float newGlobalCarbon = GetGlobalCarbon();
        float carbonDiff = newGlobalCarbon - oldGlobalCarbon;
        string carbonDir = carbonDiff >= 0 ? "+" : "";
        string summary = $"Round {CurrentRound} End — Carbon: {carbonDir}{carbonDiff:F1}";

        var crisisNames = new List<string>();
        var stressedNames = new List<string>();
        var recovered = new List<string>();

        foreach (var r in regions)
        {
            int newSt = r.CarbonLevel > 85f ? 2 : (r.CarbonLevel > 70f ? 1 : 0);
            int oldSt = oldStatus[r];

            if (newSt == 2)
            {
                string label = newSt > oldSt ? $"{r.RegionName} (!)" : r.RegionName;
                crisisNames.Add(label);
            }
            else if (newSt == 1)
            {
                string label = newSt > oldSt ? $"{r.RegionName} (!)" : r.RegionName;
                stressedNames.Add(label);
            }
            else if (newSt < oldSt)
            {
                recovered.Add(r.RegionName);
            }
        }

        if (crisisNames.Count > 0)
            summary += $"\nCrisis: {string.Join(", ", crisisNames)}";
        if (stressedNames.Count > 0)
            summary += $"\nStressed: {string.Join(", ", stressedNames)}";
        if (recovered.Count > 0)
            summary += $"\nRecovered: {string.Join(", ", recovered)}";

        RoundSummaryText = summary;
        RoundSummaryTime = Time.time;
    }

    // called per card play — rolls focus % chance, warns first then punishes
    string CheckFocusOnPlay(Region target)
    {
        if (allEvents == null) return null;

        float chance = GetFocusPercent(target);
        if (chance <= 0f) return null;

        // roll against the focus %
        float roll = Random.Range(0f, 100f);
        if (roll >= chance) return null;

        // pick a random focus event to apply
        var focusEvents = new List<EventData>();
        foreach (var evt in allEvents)
        {
            if (evt.IsFocusEvent())
                focusEvents.Add(evt);
        }
        if (focusEvents.Count == 0) return null;

        var matchedEvent = focusEvents[Random.Range(0, focusEvents.Count)];

        if (focusWarnedRegions.ContainsKey(target))
        {
            // already warned — punish with escalating multiplier: x1, x2, x5 (capped)
            focusWarnedRegions[target]++;
            int hits = focusWarnedRegions[target];
            float multiplier = hits >= 3 ? 5f : hits;

            target.CarbonLevel = Mathf.Clamp(target.CarbonLevel + matchedEvent.carbonDelta * multiplier, 0f, 100f);
            target.EconomyLevel = Mathf.Clamp(target.EconomyLevel + matchedEvent.economyDelta * multiplier, 0f, 100f);
            target.StabilityLevel = Mathf.Clamp(target.StabilityLevel + matchedEvent.stabilityDelta * multiplier, 0f, 100f);

            string severity = multiplier > 1f ? $" (x{multiplier:F0})" : "";
            string punishText = $"{matchedEvent.eventName}!{severity} {matchedEvent.description}";
            LastWarningText = punishText;
            LastWarningTime = Time.time;

            string logEntry = $"R{CurrentRound}: {matchedEvent.eventName}{severity} ({target.RegionName})";
            EventLog.Add(logEntry);
            if (EventLog.Count > maxEventLog)
                EventLog.RemoveAt(0);

            Debug.Log($"[GameManager] FOCUS PUNISHMENT: {matchedEvent.eventName} x{multiplier} hit {target.RegionName} (rolled {roll:F0} < {chance:F0}%)");
            return punishText;
        }
        else
        {
            // first trigger — warn (shown persistently on region panel, not as timed popup)
            focusWarnedRegions[target] = 0;
            Debug.LogWarning($"[GameManager] focus warning on {target.RegionName} (rolled {roll:F0} < {chance:F0}%)");
            return null;
        }
    }

    void ApplyRandomEvent(List<Region> regions)
    {
        if (allEvents == null || allEvents.Length == 0) return;

        // build pool of normal events (exclude focus events)
        var pool = new List<EventData>();
        foreach (var e in allEvents)
        {
            if (!e.IsFocusEvent())
                pool.Add(e);
        }
        if (pool.Count == 0) return;

        var evt = pool[Random.Range(0, pool.Count)];
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

    public float GetGlobalCarbon()
    {
        if (regionManager == null || regionManager.Regions == null || regionManager.Regions.Count == 0)
            return 0f;

        float total = 0f;
        foreach (var r in regionManager.Regions)
            total += r.CarbonLevel;
        return total / regionManager.Regions.Count;
    }

    // how many cards have been played on this region total
    public int GetPlayCount(Region region)
    {
        return totalPlaysPerRegion.ContainsKey(region) ? totalPlaysPerRegion[region] : 0;
    }

    // average plays across all regions
    public float GetAveragePlayCount()
    {
        if (regionManager == null || regionManager.Regions == null || regionManager.Regions.Count == 0)
            return 0f;

        int total = 0;
        foreach (var r in regionManager.Regions)
            total += GetPlayCount(r);
        return (float)total / regionManager.Regions.Count;
    }

    // true if this region has been warned about over-targeting
    public bool IsRegionFocusWarned(Region region)
    {
        return focusWarnedRegions.ContainsKey(region);
    }

    // returns the focus chance (0-100) for a region based on plays above average
    public float GetFocusPercent(Region region)
    {
        float avg = GetAveragePlayCount();
        int plays = GetPlayCount(region);
        float above = plays - avg;
        if (above <= 0f) return 0f;
        return Mathf.Clamp(above * focusChancePerPlay, 0f, 100f);
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
