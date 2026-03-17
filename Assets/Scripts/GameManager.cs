using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Policy Deck")]
    [SerializeField] private PolicyData[] commonPolicies;
    [SerializeField] private PolicyData[] uncommonPolicies;
    [SerializeField] private PolicyData[] rarePolicies;

    [Header("Events")]
    [SerializeField] private EventData[] allEvents;

    [Header("Game Settings")]
    [SerializeField] private int totalRounds = 10;
    [SerializeField] private int actionsPerRound = 3;

    public int CurrentRound { get; private set; }
    public int ActionsRemaining { get; private set; }
    public bool GameOver { get; private set; }
    public string GameOverReason { get; private set; }
    public float FinalScore { get; private set; }
    public string FinalRating { get; private set; }

    // the 3 cards in hand this round
    public List<PolicyData> CurrentHand { get; private set; }

    // last event that fired, for UI display
    public string LastEventText { get; private set; }
    public float LastEventTime { get; private set; }

    private List<PolicyData> deck;
    private List<PolicyData> discardPile;
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
    }

    void StartGame()
    {
        GameOver = false;
        GameOverReason = null;
        crisisCount = 0;
        chainCollapseWarning = false;
        CurrentRound = 0;
        FinalScore = 0;
        FinalRating = null;

        // build deck: all 6 common + 2 random uncommon + 1 random rare
        deck = new List<PolicyData>();

        foreach (var p in commonPolicies)
            deck.Add(p);

        // pick 2 random uncommon
        var uncommonPool = new List<PolicyData>(uncommonPolicies);
        for (int i = 0; i < 2 && uncommonPool.Count > 0; i++)
        {
            int idx = Random.Range(0, uncommonPool.Count);
            deck.Add(uncommonPool[idx]);
            uncommonPool.RemoveAt(idx);
        }

        // pick 1 random rare
        if (rarePolicies.Length > 0)
        {
            int idx = Random.Range(0, rarePolicies.Length);
            deck.Add(rarePolicies[idx]);
        }

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

        // 25% spillover to neighbors
        foreach (var neighbor in target.Neighbors)
        {
            neighbor.CarbonLevel = Mathf.Clamp(neighbor.CarbonLevel + carbon * 0.25f, 0f, 100f);
            neighbor.EconomyLevel = Mathf.Clamp(neighbor.EconomyLevel + economy * 0.25f, 0f, 100f);
            neighbor.StabilityLevel = Mathf.Clamp(neighbor.StabilityLevel + stability * 0.25f, 0f, 100f);
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

        // 6. draw and apply 1 random event
        ApplyRandomEvent(regions);

        Debug.Log($"[GameManager] end of round {CurrentRound} | global carbon: {GetGlobalCarbon():F1}");

        // 7. check game over
        if (CheckGameOver(regions))
            return;

        if (CurrentRound >= totalRounds)
        {
            // survived all rounds, calculate score
            CalculateScore(regions);
            GameOver = true;
            GameOverReason = FinalRating;
            Debug.Log($"[GameManager] game complete! score: {FinalScore:F0} — {FinalRating}");
            return;
        }

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
        int crisisRegions = 0;
        foreach (var r in regions)
        {
            if (r.CarbonLevel > 85f)
                crisisRegions++;
        }

        if (crisisRegions >= 3)
        {
            if (chainCollapseWarning)
            {
                GameOver = true;
                GameOverReason = "Chain Collapse — 3+ regions in crisis for consecutive rounds.";
                Debug.Log($"[GameManager] GAME OVER: {GameOverReason}");
                return true;
            }
            else
            {
                chainCollapseWarning = true;
                Debug.LogWarning("[GameManager] WARNING: 3+ regions in crisis — collapse imminent next round!");
            }
        }
        else
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

        FinalScore = (thriving * 25f) + (healthy * 15f)
            + (avgStability * 0.5f) + (avgEconomy * 0.5f)
            - (globalCarbon * 1.5f) - (crisisCount * 10f);

        if (FinalScore >= 100f) FinalRating = "Sustainable Future (Gold)";
        else if (FinalScore >= 75f) FinalRating = "Stable Transition (Silver)";
        else if (FinalScore >= 50f) FinalRating = "Fragile Balance (Bronze)";
        else FinalRating = "Environmental Failure";
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
