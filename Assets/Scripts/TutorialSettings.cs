using UnityEngine;

// static flag that signals the game scene to run in tutorial mode
// set by the main menu Tutorial button, read by TutorialManager on scene load
// cleared automatically when the main menu loads, so normal play always starts fresh
public static class TutorialSettings
{
    public static bool IsTutorial { get; set; } = false;
}
