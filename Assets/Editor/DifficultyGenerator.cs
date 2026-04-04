using UnityEngine;
using UnityEditor;

public class DifficultyGenerator
{
    [MenuItem("Carbon Conquest/Generate Difficulty Presets")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Difficulty"))
            AssetDatabase.CreateFolder("Assets/Resources", "Difficulty");

        CreateEasy();
        CreateNormal();
        CreateHard();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DifficultyGenerator] presets created in Assets/Resources/Difficulty/");
    }

    static void CreateEasy()
    {
        string path = "Assets/Resources/Difficulty/Easy.asset";
        if (AssetDatabase.LoadAssetAtPath<DifficultyPreset>(path) != null) return;

        var p = ScriptableObject.CreateInstance<DifficultyPreset>();
        p.difficulty = Difficulty.Easy;
        p.description = "A forgiving experience.\n\n"
            + "- 10 rounds\n"
            + "- Low carbon drift (+0.5/round)\n"
            + "- Balanced starting stats (C:48 E:52 S:52)\n"
            + "- 10 starting capital, +2 per round\n"
            + "- Relaxed thresholds (crisis at C>90, E<10, S<10)\n"
            + "- Tipping point at 95 avg carbon\n"
            + "- Chain collapse needs 4+ crisis regions\n"
            + "- 100% positive spillover to neighbors\n"
            + "- Policies are 20% more effective\n"
            + "- Rewards after any 1+ carbon drop\n"
            + "- Lower focus event chance (12%)";

        p.totalRounds = 10;
        p.handSize = 6;
        p.carbonDrift = 0.5f;
        p.startingCarbon = 48f;
        p.startingEconomy = 52f;
        p.startingStability = 52f;

        p.startingCapital = 10;
        p.capitalPerRound = 2;

        p.stabilityMidpoint = 50f;
        p.stabilityMultMin = 0.6f;
        p.stabilityMultMax = 1.5f;

        p.fundsIncomeMultiplier = 0.12f;

        p.stressedCarbon = 75f;
        p.stressedEconomy = 25f;
        p.stressedStability = 25f;

        p.crisisCarbon = 90f;
        p.crisisEconomy = 10f;
        p.crisisStability = 10f;

        p.rewardCarbonThreshold = 1f;

        p.skipBasePenalty = 3f;
        p.skipEscalation = 2f;
        p.skipStabilityFraction = 0.5f;
        p.skipCapitalBonusMin = 0;
        p.skipCapitalBonusMax = 4;

        p.defaultSpillover = 1.0f;
        p.policyMultiplier = 1.2f;
        p.focusChancePerPlay = 12f;

        p.tippingPointCarbon = 95f;
        p.chainCollapseCount = 4;

        AssetDatabase.CreateAsset(p, path);
    }

    static void CreateNormal()
    {
        string path = "Assets/Resources/Difficulty/Normal.asset";
        if (AssetDatabase.LoadAssetAtPath<DifficultyPreset>(path) != null) return;

        var p = ScriptableObject.CreateInstance<DifficultyPreset>();
        p.difficulty = Difficulty.Normal;
        p.description = "The standard challenge.\n\n"
            + "- 10 rounds\n"
            + "- Carbon drift +0.5/round\n"
            + "- Balanced starting stats (C:48 E:52 S:52)\n"
            + "- 10 starting capital, +2 per round\n"
            + "- Standard thresholds (crisis at C>85, E<15, S<15)\n"
            + "- Tipping point at 92 avg carbon\n"
            + "- Chain collapse at 3+ crisis regions\n"
            + "- 50% positive spillover to neighbors\n"
            + "- Rewards after 2+ carbon drop\n"
            + "- Standard skip penalties\n"
            + "- 15% focus event chance";

        p.totalRounds = 10;
        p.handSize = 6;
        p.carbonDrift = 0.5f;
        p.startingCarbon = 48f;
        p.startingEconomy = 52f;
        p.startingStability = 52f;

        p.startingCapital = 10;
        p.capitalPerRound = 2;

        p.stabilityMidpoint = 50f;
        p.stabilityMultMin = 0.5f;
        p.stabilityMultMax = 1.5f;

        p.fundsIncomeMultiplier = 0.1f;

        p.stressedCarbon = 70f;
        p.stressedEconomy = 30f;
        p.stressedStability = 30f;

        p.crisisCarbon = 85f;
        p.crisisEconomy = 15f;
        p.crisisStability = 15f;

        p.rewardCarbonThreshold = 2f;

        p.skipBasePenalty = 3f;
        p.skipEscalation = 2f;
        p.skipStabilityFraction = 0.5f;
        p.skipCapitalBonusMin = 0;
        p.skipCapitalBonusMax = 5;

        p.defaultSpillover = 0.5f;
        p.policyMultiplier = 1.0f;
        p.focusChancePerPlay = 15f;

        p.tippingPointCarbon = 92f;
        p.chainCollapseCount = 3;

        AssetDatabase.CreateAsset(p, path);
    }

    static void CreateHard()
    {
        string path = "Assets/Resources/Difficulty/Hard.asset";
        if (AssetDatabase.LoadAssetAtPath<DifficultyPreset>(path) != null) return;

        var p = ScriptableObject.CreateInstance<DifficultyPreset>();
        p.difficulty = Difficulty.Hard;
        p.description = "For seasoned leaders only.\n\n"
            + "- 10 rounds\n"
            + "- Same core stats as Normal\n"
            + "- Only 25% positive spillover to neighbors\n"
            + "- More frequent bad events (30% focus chance)\n"
            + "- Rewards need 3+ carbon drop\n"
            + "- Harsh skip penalties\n"
            + "- Strategic play required";

        // core gameplay matches normal — difficulty comes from events + low spillover
        p.totalRounds = 10;
        p.handSize = 6;
        p.carbonDrift = 0.5f;
        p.startingCarbon = 48f;
        p.startingEconomy = 52f;
        p.startingStability = 52f;

        p.startingCapital = 10;
        p.capitalPerRound = 2;

        p.stabilityMidpoint = 50f;
        p.stabilityMultMin = 0.5f;
        p.stabilityMultMax = 1.5f;

        p.fundsIncomeMultiplier = 0.1f;

        p.stressedCarbon = 70f;
        p.stressedEconomy = 30f;
        p.stressedStability = 30f;

        p.crisisCarbon = 85f;
        p.crisisEconomy = 15f;
        p.crisisStability = 15f;

        p.rewardCarbonThreshold = 3f;

        p.skipBasePenalty = 4f;
        p.skipEscalation = 2.5f;
        p.skipStabilityFraction = 0.6f;
        p.skipCapitalBonusMin = 0;
        p.skipCapitalBonusMax = 3;

        p.defaultSpillover = 0.25f;
        p.policyMultiplier = 1.0f;
        p.focusChancePerPlay = 30f;

        p.tippingPointCarbon = 92f;
        p.chainCollapseCount = 3;

        AssetDatabase.CreateAsset(p, path);
    }
}
