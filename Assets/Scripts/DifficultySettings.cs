using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

// static class that persists the chosen difficulty across scene loads
public static class DifficultySettings
{
    public static Difficulty Current { get; set; } = Difficulty.Normal;
}
