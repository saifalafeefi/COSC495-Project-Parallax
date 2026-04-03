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
        p.description = "A forgiving experience.\n"
            + "- 12 rounds to save the planet\n"
            + "- Lower carbon drift (+0.5/round)\n"
            + "- More political capital\n"
            + "- Relaxed crisis thresholds\n"
            + "- Rewards come easier";

        p.totalRounds = 12;
        p.handSize = 6;
        p.carbonDrift = 0.5f;
        p.startingCarbon = 45f;
        p.startingEconomy = 55f;
        p.startingStability = 55f;

        p.startingCapital = 12;
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

        p.skipBasePenalty = 2f;
        p.skipEscalation = 1.5f;
        p.skipStabilityFraction = 0.3f;
        p.skipCapitalBonusMin = 1;
        p.skipCapitalBonusMax = 5;

        p.defaultSpillover = 0.3f;
        p.focusChancePerPlay = 15f;

        AssetDatabase.CreateAsset(p, path);
    }

    static void CreateNormal()
    {
        string path = "Assets/Resources/Difficulty/Normal.asset";
        if (AssetDatabase.LoadAssetAtPath<DifficultyPreset>(path) != null) return;

        var p = ScriptableObject.CreateInstance<DifficultyPreset>();
        p.difficulty = Difficulty.Normal;
        p.description = "The standard challenge.\n"
            + "- 10 rounds\n"
            + "- Balanced carbon drift (+1/round)\n"
            + "- Standard political capital\n"
            + "- Default thresholds\n"
            + "- Moderate reward requirements";

        p.totalRounds = 10;
        p.handSize = 6;
        p.carbonDrift = 1f;
        p.startingCarbon = 50f;
        p.startingEconomy = 50f;
        p.startingStability = 50f;

        p.startingCapital = 10;
        p.capitalPerRound = 1;

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

        p.defaultSpillover = 0.25f;
        p.focusChancePerPlay = 20f;

        AssetDatabase.CreateAsset(p, path);
    }

    static void CreateHard()
    {
        string path = "Assets/Resources/Difficulty/Hard.asset";
        if (AssetDatabase.LoadAssetAtPath<DifficultyPreset>(path) != null) return;

        var p = ScriptableObject.CreateInstance<DifficultyPreset>();
        p.difficulty = Difficulty.Hard;
        p.description = "For seasoned leaders only.\n"
            + "- 8 rounds — every move counts\n"
            + "- Harsh carbon drift (+2/round)\n"
            + "- Less political capital\n"
            + "- Tight crisis thresholds\n"
            + "- Rewards require big carbon drops";

        p.totalRounds = 8;
        p.handSize = 6;
        p.carbonDrift = 2f;
        p.startingCarbon = 55f;
        p.startingEconomy = 45f;
        p.startingStability = 45f;

        p.startingCapital = 8;
        p.capitalPerRound = 1;

        p.stabilityMidpoint = 50f;
        p.stabilityMultMin = 0.4f;
        p.stabilityMultMax = 1.3f;

        p.fundsIncomeMultiplier = 0.08f;

        p.stressedCarbon = 65f;
        p.stressedEconomy = 35f;
        p.stressedStability = 35f;

        p.crisisCarbon = 80f;
        p.crisisEconomy = 20f;
        p.crisisStability = 20f;

        p.rewardCarbonThreshold = 4f;

        p.skipBasePenalty = 5f;
        p.skipEscalation = 2.5f;
        p.skipStabilityFraction = 0.7f;
        p.skipCapitalBonusMin = 0;
        p.skipCapitalBonusMax = 3;

        p.defaultSpillover = 0.2f;
        p.focusChancePerPlay = 25f;

        AssetDatabase.CreateAsset(p, path);
    }
}
