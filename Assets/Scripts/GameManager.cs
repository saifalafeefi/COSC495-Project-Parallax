using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// stores one event that affected a region, for the dashboard history
[System.Serializable]
public struct RegionEventRecord
{
    public int round;
    public string eventName;
    public float carbonDelta;
    public float economyDelta;
    public float stabilityDelta;
}

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

    [Header("Political Capital")]
    [Tooltip("starting political capital on round 1")]
    [SerializeField] private int startingCapital = 10;
    [Tooltip("extra capital gained each round")]
    [SerializeField] private int capitalPerRound = 1;

    [Header("Stability → Capital Multiplier")]
    [Tooltip("avg stability at which multiplier is 1.0 (no change)")]
    [SerializeField] private float stabilityMidpoint = 50f;
    [Tooltip("minimum capital multiplier when stability is 0")]
    [SerializeField] private float stabilityMultMin = 0.5f;
    [Tooltip("maximum capital multiplier when stability is 100")]
    [SerializeField] private float stabilityMultMax = 1.5f;

    [Header("Funds (Economy → Income)")]
    [Tooltip("multiplier for income: income = avgEconomy * this")]
    [SerializeField] private float fundsIncomeMultiplier = 0.1f;

    [Header("Stressed Thresholds (any stat crossing = stressed)")]
    [Tooltip("carbon above this = stressed")]
    [SerializeField] private float stressedCarbon = 70f;
    [Tooltip("economy below this = stressed")]
    [SerializeField] private float stressedEconomy = 30f;
    [Tooltip("stability below this = stressed")]
    [SerializeField] private float stressedStability = 30f;

    [Header("Crisis Thresholds (any stat crossing = crisis)")]
    [Tooltip("carbon above this = crisis")]
    [SerializeField] private float crisisCarbon = 85f;
    [Tooltip("economy below this = crisis")]
    [SerializeField] private float crisisEconomy = 15f;
    [Tooltip("stability below this = crisis")]
    [SerializeField] private float crisisStability = 15f;

    [Header("Reward System")]
    [Tooltip("global carbon must drop by at least this much in a round to trigger a card reward (0 = any decrease)")]
    [SerializeField] private float rewardCarbonThreshold = 2f;

    [Header("Skip Penalty")]
    [Tooltip("base carbon added to all regions when player skips with capital to spare")]
    [SerializeField] private float skipBasePenalty = 3f;
    [Tooltip("multiplier per consecutive wasteful skip (e.g. 2 = doubles each skip)")]
    [SerializeField] private float skipEscalation = 2f;
    [Tooltip("fraction of carbon penalty applied as stability loss (e.g. 0.5 = half)")]
    [SerializeField] private float skipStabilityFraction = 0.5f;
    [Tooltip("minimum bonus capital for skipping early")]
    [SerializeField] private int skipCapitalBonusMin = 0;
    [Tooltip("maximum bonus capital for skipping early (exclusive)")]
    [SerializeField] private int skipCapitalBonusMax = 5;

    [Header("Spillover")]
    [Tooltip("default fraction of positive effects that spill to neighbors (0.25 = 25%)")]
    [SerializeField] private float defaultSpillover = 0.25f;

    [Header("Focus System")]
    [Tooltip("chance per play above average to trigger a focus event (e.g. 20 = 20% per extra play)")]
    [SerializeField] private float focusChancePerPlay = 20f;

    public int CurrentRound { get; private set; }
    public int PoliticalCapital { get; private set; }
    public int MaxCapital { get; private set; }
    public int HandSize { get; private set; }
    public int Funds { get; private set; }
    public int LastIncome { get; private set; }
    public float StabilityMultiplier { get; private set; }
    // returns true if the region meets any stressed condition
    public bool IsStressed(Region r)
    {
        return r.CarbonLevel > stressedCarbon || r.EconomyLevel < stressedEconomy || r.StabilityLevel < stressedStability;
    }

    // returns true if the region meets any crisis condition
    public bool IsCrisis(Region r)
    {
        return r.CarbonLevel > crisisCarbon || r.EconomyLevel < crisisEconomy || r.StabilityLevel < crisisStability;
    }
    public bool GameOver { get; private set; }
    public string GameOverReason { get; private set; }
    public float FinalScore { get; private set; }
    public string FinalRating { get; private set; }
    public string ScoreBreakdown { get; private set; }

    // cards in hand this round
    public List<PolicyData> CurrentHand { get; private set; }

    // pending banner events to show at start of next round
    private List<(string title, string description)> pendingBannerEvents = new List<(string, string)>();

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

    // reward system — offer card pick after a net-positive round
    public bool RewardActive { get; private set; }
    public List<PolicyData> RewardChoices { get; private set; } = new List<PolicyData>();
    private bool rewardPending;

    // dashboard system
    public bool DashboardActive { get; private set; }

    // per-region event history: region -> list of (round, eventName, carbon, economy, stability)
    public Dictionary<Region, List<RegionEventRecord>> RegionEventHistory { get; private set; }
        = new Dictionary<Region, List<RegionEventRecord>>();

    // event banner system — true while banners are animating
    public bool BannerActive { get; private set; }

    // shop system
    public bool ShopActive { get; private set; }
    public List<PolicyData> ShopCards { get; private set; } = new List<PolicyData>();
    private int shopStockedRound = -1;

    // cards bought from shop — persist in hand until played
    private HashSet<PolicyData> shopBoughtCards = new HashSet<PolicyData>();

    public bool IsShopCard(PolicyData card) => shopBoughtCards.Contains(card);
    private PolicyData[] allPolicies;

    // tracks consecutive rounds where player had capital to spare but skipped
    private int consecutiveWastefulSkips;
    private int cardsPlayedThisRound;
    private int capitalWhenSkipped;

    // bonus capital from previous skip trade-off
    private int bankedCapitalBonus;
    // how much bonus was applied this round (for UI display)
    public int AppliedCapitalBonus { get; private set; }

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

        // press space to skip remaining capital and end round early
        // check if there are non-shop cards to skip
        bool hasNonShopCards = false;
        if (CurrentHand != null)
        {
            foreach (var c in CurrentHand)
            {
                if (!shopBoughtCards.Contains(c)) { hasNonShopCards = true; break; }
            }
        }

        if (kb.spaceKey.wasPressedThisFrame && !GameOver && !PauseMenu.IsPaused && !RewardActive && !ShopActive && !DashboardActive && !BannerActive && hasNonShopCards)
        {
            // check if player could have played at least one non-shop card (wasteful skip)
            bool couldPlay = false;
            foreach (var c in CurrentHand)
            {
                if (!shopBoughtCards.Contains(c) && c.politicalCapitalCost <= PoliticalCapital) { couldPlay = true; break; }
            }
            capitalWhenSkipped = couldPlay ? PoliticalCapital : 0;

            // discard remaining hand, but keep shop-bought cards
            var kept = new List<PolicyData>();
            foreach (var card in CurrentHand)
            {
                if (shopBoughtCards.Contains(card))
                    kept.Add(card);
                else
                    discardPile.Add(card);
            }
            CurrentHand.Clear();
            CurrentHand.AddRange(kept);
            PoliticalCapital = 0;
            Debug.Log("[GameManager] skipped remaining capital");
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
        Funds = 0;
        LastIncome = 0;
        consecutiveWastefulSkips = 0;
        bankedCapitalBonus = 0;
        AppliedCapitalBonus = 0;
        StabilityMultiplier = 1f;
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

        // cache all policies for reward card pool
        allPolicies = new PolicyData[deck.Count];
        deck.CopyTo(allPolicies);

        Debug.Log($"[GameManager] loaded {commonPolicies.Length} common, {uncommonPolicies.Length} uncommon, {rarePolicies.Length} rare, {normalEvents.Length} normal events, {focusEvents.Length} focus events");

        discardPile = new List<PolicyData>();
        CurrentHand = new List<PolicyData>();
        totalPlaysPerRegion.Clear();
        rewardPending = false;
        RewardActive = false;
        RewardChoices.Clear();
        ShopActive = false;
        ShopCards.Clear();
        shopBoughtCards.Clear();
        shopStockedRound = -1;
        DashboardActive = false;
        RegionEventHistory.Clear();

        ShuffleDeck();
        Debug.Log($"[GameManager] deck built with {deck.Count} cards");

        StartRound();
    }

    void StartRound()
    {
        CurrentRound++;
        cardsPlayedThisRound = 0;

        // snapshot region status before player acts so the summary captures everything
        SnapshotStatus();

        HandSize = handSize;

        // stability affects how much political capital the player gets this round
        float avgStability = GetAverageStability();
        float t = Mathf.Clamp01(avgStability / 100f);
        StabilityMultiplier = Mathf.Lerp(stabilityMultMin, stabilityMultMax, t);

        int baseCapital = startingCapital + (CurrentRound - 1) * capitalPerRound;
        AppliedCapitalBonus = bankedCapitalBonus;
        MaxCapital = Mathf.Max(1, Mathf.RoundToInt(baseCapital * StabilityMultiplier)) + AppliedCapitalBonus;
        PoliticalCapital = MaxCapital;
        bankedCapitalBonus = 0;

        // economy generates funds income each round
        float avgEconomy = GetAverageEconomy();
        LastIncome = Mathf.RoundToInt(avgEconomy * fundsIncomeMultiplier);
        Funds += LastIncome;

        // preserve shop-bought cards, discard the rest
        var shopKept = new List<PolicyData>();
        foreach (var card in CurrentHand)
        {
            if (shopBoughtCards.Contains(card))
                shopKept.Add(card);
            else
                discardPile.Add(card);
        }
        CurrentHand.Clear();
        CurrentHand.AddRange(shopKept);

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
        Debug.Log($"[GameManager] round {CurrentRound} | capital: {PoliticalCapital} (x{StabilityMultiplier:F1}) | income: +{LastIncome} funds (total: {Funds}) | drew: {cardNames}");

        // show reward popup if last round was net-positive
        if (rewardPending)
        {
            rewardPending = false;
            GenerateRewardChoices();
            RewardActive = true;
            Debug.Log("[GameManager] reward popup triggered — pick 1 of 3");
        }
    }

    // called when the player presses 1, 2, or 3 with a region selected
    public string PlayCard(int cardIndex, Region target)
    {
        if (GameOver) return "Game is over.";
        if (BannerActive) return "Event in progress.";
        if (RewardActive) return "Choose a reward first.";
        if (ShopActive) return "Close the shop first.";
        if (DashboardActive) return "Close the dashboard first.";
        if (cardIndex < 0 || cardIndex >= CurrentHand.Count) return "Invalid card.";
        if (target == null) return "No region selected.";

        var card = CurrentHand[cardIndex];

        // check political capital cost
        if (card.politicalCapitalCost > PoliticalCapital)
            return $"NOT_ENOUGH_CAPITAL|{card.politicalCapitalCost}|{PoliticalCapital}";
        card.GetModifiedDeltas(target, out float carbon, out float economy, out float stability);

        // apply to target region
        float oldCarbon = target.CarbonLevel;
        float oldEconomy = target.EconomyLevel;
        float oldStability = target.StabilityLevel;

        target.CarbonLevel = Mathf.Clamp(target.CarbonLevel + carbon, 0f, 100f);
        target.EconomyLevel = Mathf.Clamp(target.EconomyLevel + economy, 0f, 100f);
        target.StabilityLevel = Mathf.Clamp(target.StabilityLevel + stability, 0f, 100f);

        // spillover to neighbors — only positive effects spread
        float spill = card.spilloverOverride > 0 ? card.spilloverOverride : defaultSpillover;
        float spillCarbon = Mathf.Min(carbon, 0f) * spill;      // negative carbon = good
        float spillEconomy = Mathf.Max(economy, 0f) * spill;    // positive economy = good
        float spillStability = Mathf.Max(stability, 0f) * spill; // positive stability = good
        foreach (var neighbor in target.Neighbors)
        {
            neighbor.CarbonLevel = Mathf.Clamp(neighbor.CarbonLevel + spillCarbon, 0f, 100f);
            neighbor.EconomyLevel = Mathf.Clamp(neighbor.EconomyLevel + spillEconomy, 0f, 100f);
            neighbor.StabilityLevel = Mathf.Clamp(neighbor.StabilityLevel + spillStability, 0f, 100f);
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

        // move card to discard and deduct capital
        shopBoughtCards.Remove(card);
        discardPile.Add(card);
        CurrentHand.RemoveAt(cardIndex);
        PoliticalCapital -= card.politicalCapitalCost;
        cardsPlayedThisRound++;

        string result = $"{card.policyName} ({card.politicalCapitalCost}) → {target.RegionName}";
        if (focusResult != null)
            result += $"\n{focusResult}";

        Debug.Log($"[GameManager] {result} | capital: {PoliticalCapital}/{MaxCapital}");

        // end round when only shop-bought cards remain (they persist)
        bool onlyShopCards = true;
        foreach (var c in CurrentHand)
        {
            if (!shopBoughtCards.Contains(c)) { onlyShopCards = false; break; }
        }
        if (CurrentHand.Count == 0 || onlyShopCards)
            EndRound();

        return result;
    }

    void EndRound()
    {
        var regions = regionManager.Regions;
        if (regions == null) return;

        // wasteful skip penalty: player had capital to spare but chose to skip
        if (capitalWhenSkipped > 0)
        {
            consecutiveWastefulSkips++;
            float penalty = skipBasePenalty * Mathf.Pow(skipEscalation, consecutiveWastefulSkips - 1);
            float stabPenalty = penalty * skipStabilityFraction;
            foreach (var r in regions)
            {
                r.CarbonLevel = Mathf.Clamp(r.CarbonLevel + penalty, 0f, 100f);
                r.StabilityLevel = Mathf.Clamp(r.StabilityLevel - stabPenalty, 0f, 100f);
                RecordRegionEvent(r, "Neglect Penalty", penalty, 0f, -stabPenalty);
            }

            // trade-off: random capital bonus banked for next round
            bankedCapitalBonus = Random.Range(skipCapitalBonusMin, skipCapitalBonusMax);

            string desc = $"+{penalty:F0} carbon, -{stabPenalty:F0} stability to all regions";
            if (bankedCapitalBonus > 0)
                desc += $" | +{bankedCapitalBonus} bonus capital next round";
            if (consecutiveWastefulSkips >= 2)
                desc += $" [x{consecutiveWastefulSkips} consecutive]";

            // show as banner alongside normal events
            pendingBannerEvents.Add(("Neglect Penalty", desc));

            string logEntry = $"R{CurrentRound}: Neglect penalty (skip x{consecutiveWastefulSkips}, +{bankedCapitalBonus} capital banked)";
            EventLog.Add(logEntry);
            if (EventLog.Count > maxEventLog)
                EventLog.RemoveAt(0);

            Debug.Log($"[GameManager] wasteful skip penalty: +{penalty:F0} carbon, -{stabPenalty:F0} stability (streak: {consecutiveWastefulSkips}, banked: +{bankedCapitalBonus})");
        }
        else
        {
            consecutiveWastefulSkips = 0;
        }
        capitalWhenSkipped = 0;

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

        // 2. crisis spillover: crisis regions spread problems to neighbors
        foreach (var r in regions)
        {
            if (IsCrisis(r))
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

        // 4. penalties based on health score
        foreach (var r in regions)
        {
            if (IsStressed(r))
                r.StabilityLevel = Mathf.Clamp(r.StabilityLevel - 2f, 0f, 100f);

            if (IsCrisis(r))
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

        // queue reward if global carbon dropped enough this round
        float endCarbon = GetGlobalCarbon();
        float carbonDrop = snapshotGlobalCarbon - endCarbon;
        if (carbonDrop >= rewardCarbonThreshold)
        {
            rewardPending = true;
            Debug.Log($"[GameManager] net-positive round! carbon {snapshotGlobalCarbon:F1} → {endCarbon:F1} (drop: {carbonDrop:F1}, threshold: {rewardCarbonThreshold})");
        }

        // show event banners before starting the next round
        if (pendingBannerEvents.Count > 0)
        {
            StartCoroutine(ShowBannersThenStartRound());
        }
        else
        {
            StartRound();
        }
    }

    IEnumerator ShowBannersThenStartRound()
    {
        BannerActive = true;

        var banner = FindFirstObjectByType<EventBanner>();
        if (banner != null)
        {
            var events = new List<(string, string)>(pendingBannerEvents);
            pendingBannerEvents.Clear();
            yield return banner.ShowEvents(events);
        }
        else
        {
            pendingBannerEvents.Clear();
        }

        BannerActive = false;
        StartRound();
    }

    void SnapshotStatus()
    {
        snapshotGlobalCarbon = GetGlobalCarbon();
        snapshotStatus.Clear();

        if (regionManager == null || regionManager.Regions == null) return;

        foreach (var r in regionManager.Regions)
        {
            if (IsCrisis(r)) snapshotStatus[r] = 2;
            else if (IsStressed(r)) snapshotStatus[r] = 1;
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
            int newSt = IsCrisis(r) ? 2 : (IsStressed(r) ? 1 : 0);
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

        // show income and capital modifier for the upcoming round
        if (LastIncome > 0)
            summary += $"\nIncome: +{LastIncome} funds";
        if (StabilityMultiplier != 1f)
            summary += $"\nStability modifier: x{StabilityMultiplier:F1} capital";

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

            RecordRegionEvent(target, matchedEvent.eventName,
                matchedEvent.carbonDelta * multiplier, matchedEvent.economyDelta * multiplier, matchedEvent.stabilityDelta * multiplier);

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

            // record for dashboard history
            RecordRegionEvent(r, evt.eventName, evt.carbonDelta, evt.economyDelta, evt.stabilityDelta);
        }

        // queue for the breaking news banner
        pendingBannerEvents.Add((evt.eventName, $"{evt.description} ({affected.Count} region{(affected.Count != 1 ? "s" : "")} affected)"));

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
            if (IsCrisis(r))
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

        float fundsPts = Funds * 0.5f;
        FinalScore = thrivingPts + healthyPts + stabilityPts + economyPts + fundsPts - carbonPenalty - crisisPenalty;

        ScoreBreakdown = $"Thriving regions ({thriving}): +{thrivingPts:F0}\n"
            + $"Healthy regions ({healthy}): +{healthyPts:F0}\n"
            + $"Avg Stability ({avgStability:F0}): +{stabilityPts:F0}\n"
            + $"Avg Economy ({avgEconomy:F0}): +{economyPts:F0}\n"
            + $"Funds ({Funds}): +{fundsPts:F0}\n"
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

        // stop any running banner animation
        BannerActive = false;
        pendingBannerEvents.Clear();
        StopAllCoroutines();

        StartGame();
    }

    // player picks one of the 3 offered reward cards — added straight to hand as a free copy
    public void ClaimReward(int index)
    {
        if (!RewardActive || index < 0 || index >= RewardChoices.Count) return;
        var original = RewardChoices[index];

        // create a runtime clone with 0 cost so reward cards are permanently free
        var freeCard = Instantiate(original);
        freeCard.politicalCapitalCost = 0;
        freeCard.policyName = original.policyName + " (Reward)";

        CurrentHand.Add(freeCard);
        Debug.Log($"[GameManager] reward claimed: {freeCard.policyName} (hand now {CurrentHand.Count})");
        RewardActive = false;
        RewardChoices.Clear();
    }

    // player skips the reward
    public void SkipReward()
    {
        if (!RewardActive) return;
        Debug.Log("[GameManager] reward skipped");
        RewardActive = false;
        RewardChoices.Clear();
    }

    void GenerateRewardChoices()
    {
        RewardChoices.Clear();
        if (allPolicies == null || allPolicies.Length == 0) return;

        for (int i = 0; i < 3; i++)
            RewardChoices.Add(allPolicies[Random.Range(0, allPolicies.Length)]);
    }

    // shop: open/close, stock generation, purchase

    public void OpenShop()
    {
        if (GameOver || RewardActive || ShopActive) return;
        // only generate stock if we haven't this round
        if (shopStockedRound != CurrentRound)
        {
            GenerateShopStock();
            shopStockedRound = CurrentRound;
        }
        ShopActive = true;
        Debug.Log("[GameManager] shop opened");
    }

    public void CloseShop()
    {
        if (!ShopActive) return;
        ShopActive = false;
        Debug.Log("[GameManager] shop closed");
    }

    // returns the funds price for a given rarity
    public int GetShopPrice(PolicyRarity rarity)
    {
        switch (rarity)
        {
            case PolicyRarity.Common: return 5;
            case PolicyRarity.Uncommon: return 10;
            case PolicyRarity.Rare: return 25;
            default: return 5;
        }
    }

    // buy a card from the shop — adds to deck, deducts funds
    public string BuyShopCard(int index)
    {
        if (!ShopActive || index < 0 || index >= ShopCards.Count) return null;

        var card = ShopCards[index];
        if (card == null) return "Already sold.";

        int price = GetShopPrice(card.rarity);
        if (Funds < price)
            return $"NOT_ENOUGH_FUNDS|{price}|{Funds}";

        Funds -= price;
        CurrentHand.Add(card);
        shopBoughtCards.Add(card);
        ShopCards[index] = null;

        Debug.Log($"[GameManager] bought {card.policyName} for {price} funds → hand (remaining: {Funds})");
        return $"Purchased {card.policyName} for {price} funds!";
    }

    void GenerateShopStock()
    {
        ShopCards.Clear();
        if (allPolicies == null || allPolicies.Length == 0) return;

        // separate policies by rarity
        var commons = new List<PolicyData>();
        var uncommons = new List<PolicyData>();
        var rares = new List<PolicyData>();

        foreach (var p in allPolicies)
        {
            switch (p.rarity)
            {
                case PolicyRarity.Common: commons.Add(p); break;
                case PolicyRarity.Uncommon: uncommons.Add(p); break;
                case PolicyRarity.Rare: rares.Add(p); break;
            }
        }

        // guarantee 1 of each rarity
        if (commons.Count > 0) ShopCards.Add(commons[Random.Range(0, commons.Count)]);
        if (uncommons.Count > 0) ShopCards.Add(uncommons[Random.Range(0, uncommons.Count)]);
        if (rares.Count > 0) ShopCards.Add(rares[Random.Range(0, rares.Count)]);

        // fill remaining slots with weighted random (60% common, 30% uncommon, 10% rare)
        while (ShopCards.Count < 6)
        {
            float roll = Random.Range(0f, 1f);
            if (roll < 0.6f && commons.Count > 0)
                ShopCards.Add(commons[Random.Range(0, commons.Count)]);
            else if (roll < 0.9f && uncommons.Count > 0)
                ShopCards.Add(uncommons[Random.Range(0, uncommons.Count)]);
            else if (rares.Count > 0)
                ShopCards.Add(rares[Random.Range(0, rares.Count)]);
            else if (commons.Count > 0)
                ShopCards.Add(commons[Random.Range(0, commons.Count)]);
        }

        // shuffle the stock so guaranteed slots aren't always first
        for (int i = ShopCards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = ShopCards[i];
            ShopCards[i] = ShopCards[j];
            ShopCards[j] = temp;
        }
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

    public float GetAverageStability()
    {
        if (regionManager == null || regionManager.Regions == null || regionManager.Regions.Count == 0)
            return 50f;

        float total = 0f;
        foreach (var r in regionManager.Regions)
            total += r.StabilityLevel;
        return total / regionManager.Regions.Count;
    }

    public float GetAverageEconomy()
    {
        if (regionManager == null || regionManager.Regions == null || regionManager.Regions.Count == 0)
            return 50f;

        float total = 0f;
        foreach (var r in regionManager.Regions)
            total += r.EconomyLevel;
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

    // dashboard: open/close

    public void OpenDashboard()
    {
        if (GameOver || RewardActive || ShopActive || DashboardActive) return;
        DashboardActive = true;
        Debug.Log("[GameManager] dashboard opened");
    }

    public void CloseDashboard()
    {
        if (!DashboardActive) return;
        DashboardActive = false;
        Debug.Log("[GameManager] dashboard closed");
    }

    // record an event that affected a region for dashboard history
    void RecordRegionEvent(Region region, string eventName, float carbon, float economy, float stability)
    {
        if (!RegionEventHistory.ContainsKey(region))
            RegionEventHistory[region] = new List<RegionEventRecord>();

        RegionEventHistory[region].Add(new RegionEventRecord
        {
            round = CurrentRound,
            eventName = eventName,
            carbonDelta = carbon,
            economyDelta = economy,
            stabilityDelta = stability
        });
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
