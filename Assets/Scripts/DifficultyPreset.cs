using UnityEngine;

[CreateAssetMenu(fileName = "NewDifficulty", menuName = "Carbon Conquest/Difficulty Preset")]
public class DifficultyPreset : ScriptableObject
{
    public Difficulty difficulty;

    [Header("Display")]
    [TextArea(3, 6)]
    public string description;

    [Header("Game Settings")]
    public int totalRounds = 10;
    public int handSize = 6;
    public float carbonDrift = 1f;
    public float startingCarbon = 50f;
    public float startingEconomy = 50f;
    public float startingStability = 50f;

    [Header("Political Capital")]
    public int startingCapital = 10;
    public int capitalPerRound = 1;

    [Header("Stability → Capital Multiplier")]
    public float stabilityMidpoint = 50f;
    public float stabilityMultMin = 0.5f;
    public float stabilityMultMax = 1.5f;

    [Header("Funds")]
    public float fundsIncomeMultiplier = 0.1f;

    [Header("Stressed Thresholds")]
    public float stressedCarbon = 70f;
    public float stressedEconomy = 30f;
    public float stressedStability = 30f;

    [Header("Crisis Thresholds")]
    public float crisisCarbon = 85f;
    public float crisisEconomy = 15f;
    public float crisisStability = 15f;

    [Header("Reward")]
    public float rewardCarbonThreshold = 2f;

    [Header("Skip Penalty")]
    public float skipBasePenalty = 3f;
    public float skipEscalation = 2f;
    public float skipStabilityFraction = 0.5f;
    public int skipCapitalBonusMin = 0;
    public int skipCapitalBonusMax = 5;

    [Header("Spillover")]
    public float defaultSpillover = 0.25f;

    [Header("Focus System")]
    public float focusChancePerPlay = 20f;
}
