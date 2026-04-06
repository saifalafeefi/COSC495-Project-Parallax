using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("scene to load on desktop/PC builds")]
    [SerializeField] private string desktopSceneName = "DesktopScene";
    [Tooltip("scene to load on mobile/AR builds")]
    [SerializeField] private string arSceneName = "ARGameScene";

    #if UNITY_EDITOR
    [Header("Editor Testing")]
    [Tooltip("enable to simulate AR mode in the editor")]
    [SerializeField] private bool simulateAR = false;
    #endif

    [Header("Earth Spin")]
    [Tooltip("optional — assign the earth object in the menu scene to auto-spin it")]
    [SerializeField] private GameObject earthObject;
    [SerializeField] private float spinSpeed = 8f;

    [Header("Panels (assign in Inspector — each needs CanvasGroup)")]
    [Tooltip("the main menu panel (title, start, quit)")]
    [SerializeField] private CanvasGroup mainPanel;
    [Tooltip("the difficulty selection panel (easy, normal, hard, back)")]
    [SerializeField] private CanvasGroup difficultyPanel;
    [Tooltip("the difficulty preview panel (description text, confirm button)")]
    [SerializeField] private CanvasGroup previewPanel;
    [Tooltip("the settings panel (fps selection, back)")]
    [SerializeField] private CanvasGroup settingsPanel;

    [Header("Preview UI (inside previewPanel)")]
    [Tooltip("text showing the difficulty name")]
    [SerializeField] private TMP_Text previewTitle;
    [Tooltip("text showing the difficulty description")]
    [SerializeField] private TMP_Text previewDescription;

    [Header("Settings UI (inside settingsPanel)")]
    [Tooltip("text showing the current FPS selection")]
    [SerializeField] private TMP_Text fpsValueText;

    [Header("Fade Settings")]
    [SerializeField] private float fadeSpeed = 4f;

    private enum MenuState { Main, FadeToDifficulty, Difficulty, FadeToPreview, Preview, FadeBackToDifficulty, FadeToGame, FadeToMain, FadeToSettings, Settings, FadeSettingsBack }
    private MenuState state = MenuState.Main;
    private Difficulty selectedDifficulty;

    private static readonly int[] fpsOptions = { 30, 60, 75, 90, 120, 165, 240 };
    private int fpsIndex = 1; // default 60
    private int deviceMaxFps;

    void Start()
    {
        if (mainPanel != null) SetGroup(mainPanel, true);
        if (difficultyPanel != null) SetGroup(difficultyPanel, false);
        if (previewPanel != null) SetGroup(previewPanel, false);
        if (settingsPanel != null) SetGroup(settingsPanel, false);

        // detect device max refresh rate
        deviceMaxFps = Mathf.Max(60, (int)Screen.currentResolution.refreshRateRatio.value);

        // load saved fps preference
        int savedFps = PlayerPrefs.GetInt("TargetFPS", 60);
        fpsIndex = FindClosestFpsIndex(savedFps);
        ApplyFps();
    }

    void Update()
    {
        // spin the decorative earth
        if (earthObject != null)
        {
            float angle = spinSpeed * Time.deltaTime;
            if (Mathf.Abs(angle) > 0.0001f)
                earthObject.transform.Rotate(0f, angle, 0f, Space.World);
        }

        float t = fadeSpeed * Time.deltaTime;

        switch (state)
        {
            case MenuState.Main:
                mainPanel.alpha = Mathf.MoveTowards(mainPanel.alpha, 1f, t);
                break;

            case MenuState.FadeToDifficulty:
                mainPanel.alpha = Mathf.MoveTowards(mainPanel.alpha, 0f, t);
                if (mainPanel.alpha <= 0.01f)
                {
                    SetGroup(mainPanel, false);
                    SetGroup(difficultyPanel, true);
                    difficultyPanel.alpha = 0f;
                    state = MenuState.Difficulty;
                }
                break;

            case MenuState.Difficulty:
                difficultyPanel.alpha = Mathf.MoveTowards(difficultyPanel.alpha, 1f, t);
                break;

            case MenuState.FadeToPreview:
                difficultyPanel.alpha = Mathf.MoveTowards(difficultyPanel.alpha, 0f, t);
                if (difficultyPanel.alpha <= 0.01f)
                {
                    SetGroup(difficultyPanel, false);
                    SetGroup(previewPanel, true);
                    previewPanel.alpha = 0f;
                    state = MenuState.Preview;
                }
                break;

            case MenuState.Preview:
                previewPanel.alpha = Mathf.MoveTowards(previewPanel.alpha, 1f, t);
                break;

            case MenuState.FadeBackToDifficulty:
                previewPanel.alpha = Mathf.MoveTowards(previewPanel.alpha, 0f, t);
                if (previewPanel.alpha <= 0.01f)
                {
                    SetGroup(previewPanel, false);
                    SetGroup(difficultyPanel, true);
                    difficultyPanel.alpha = 0f;
                    state = MenuState.Difficulty;
                }
                break;

            case MenuState.FadeToMain:
                // fade out whichever panel is active
                if (difficultyPanel != null && difficultyPanel.alpha > 0f)
                    difficultyPanel.alpha = Mathf.MoveTowards(difficultyPanel.alpha, 0f, t);
                if (previewPanel != null && previewPanel.alpha > 0f)
                    previewPanel.alpha = Mathf.MoveTowards(previewPanel.alpha, 0f, t);

                bool done = (difficultyPanel == null || difficultyPanel.alpha <= 0.01f)
                         && (previewPanel == null || previewPanel.alpha <= 0.01f);
                if (done)
                {
                    SetGroup(difficultyPanel, false);
                    SetGroup(previewPanel, false);
                    SetGroup(mainPanel, true);
                    mainPanel.alpha = 0f;
                    state = MenuState.Main;
                }
                break;

            case MenuState.FadeToGame:
                previewPanel.alpha = Mathf.MoveTowards(previewPanel.alpha, 0f, t);
                if (previewPanel.alpha <= 0.01f)
                    SceneManager.LoadScene(IsMobilePlatform() ? arSceneName : desktopSceneName);
                break;

            case MenuState.FadeToSettings:
                mainPanel.alpha = Mathf.MoveTowards(mainPanel.alpha, 0f, t);
                if (mainPanel.alpha <= 0.01f)
                {
                    SetGroup(mainPanel, false);
                    SetGroup(settingsPanel, true);
                    settingsPanel.alpha = 0f;
                    state = MenuState.Settings;
                }
                break;

            case MenuState.Settings:
                settingsPanel.alpha = Mathf.MoveTowards(settingsPanel.alpha, 1f, t);
                break;

            case MenuState.FadeSettingsBack:
                settingsPanel.alpha = Mathf.MoveTowards(settingsPanel.alpha, 0f, t);
                if (settingsPanel.alpha <= 0.01f)
                {
                    SetGroup(settingsPanel, false);
                    SetGroup(mainPanel, true);
                    mainPanel.alpha = 0f;
                    state = MenuState.Main;
                }
                break;
        }
    }

    // -- button callbacks (wire in Inspector) --

    // main panel
    public void OnStartGame()
    {
        state = MenuState.FadeToDifficulty;
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] quit");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // difficulty panel — click to preview
    public void SelectEasy() { ShowPreview(Difficulty.Easy); }
    public void SelectNormal() { ShowPreview(Difficulty.Normal); }
    public void SelectHard() { ShowPreview(Difficulty.Hard); }

    void ShowPreview(Difficulty diff)
    {
        selectedDifficulty = diff;

        // load the preset to show its description
        var presets = Resources.LoadAll<DifficultyPreset>("Difficulty");
        DifficultyPreset match = null;
        foreach (var p in presets)
        {
            if (p.difficulty == diff) { match = p; break; }
        }

        if (previewTitle != null)
            previewTitle.text = diff.ToString();
        if (previewDescription != null)
            previewDescription.text = match != null ? match.description : "No description available.";

        state = MenuState.FadeToPreview;
    }

    // preview panel
    public void OnConfirm()
    {
        DifficultySettings.Current = selectedDifficulty;
        if (previewPanel != null) previewPanel.interactable = false;
        state = MenuState.FadeToGame;
    }

    public void OnPreviewBack()
    {
        state = MenuState.FadeBackToDifficulty;
    }

    // difficulty panel back to main
    public void OnBack()
    {
        state = MenuState.FadeToMain;
    }

    // -- settings --

    public void OnSettings()
    {
        state = MenuState.FadeToSettings;
    }

    public void OnSettingsBack()
    {
        state = MenuState.FadeSettingsBack;
    }

    // cycle fps left (<)
    public void OnFpsLeft()
    {
        fpsIndex--;
        if (fpsIndex < 0) fpsIndex = 0;
        ApplyFps();
    }

    // cycle fps right (>)
    public void OnFpsRight()
    {
        fpsIndex++;
        // cap to highest option that doesn't exceed device max
        int maxIndex = fpsOptions.Length - 1;
        for (int i = fpsOptions.Length - 1; i >= 0; i--)
        {
            if (fpsOptions[i] <= deviceMaxFps) { maxIndex = i; break; }
        }
        if (fpsIndex > maxIndex) fpsIndex = maxIndex;
        ApplyFps();
    }

    void ApplyFps()
    {
        int fps = fpsOptions[fpsIndex];
        if (fps > deviceMaxFps) fps = deviceMaxFps;
        Application.targetFrameRate = fps;
        PlayerPrefs.SetInt("TargetFPS", fps);
        PlayerPrefs.Save();
        UpdateFpsText();
    }

    void UpdateFpsText()
    {
        if (fpsValueText == null) return;
        int fps = fpsOptions[fpsIndex];
        if (fps > deviceMaxFps) fps = deviceMaxFps;
        fpsValueText.text = fps.ToString();
    }

    int FindClosestFpsIndex(int targetFps)
    {
        int closest = 0;
        int minDiff = Mathf.Abs(fpsOptions[0] - targetFps);
        for (int i = 1; i < fpsOptions.Length; i++)
        {
            if (fpsOptions[i] > deviceMaxFps) break;
            int diff = Mathf.Abs(fpsOptions[i] - targetFps);
            if (diff < minDiff) { minDiff = diff; closest = i; }
        }
        return closest;
    }

    void SetGroup(CanvasGroup group, bool active)
    {
        if (group == null) return;
        group.alpha = active ? 1f : 0f;
        group.interactable = active;
        group.blocksRaycasts = active;
    }

    bool IsMobilePlatform()
    {
        #if UNITY_EDITOR
        return simulateAR;
        #elif UNITY_ANDROID || UNITY_IOS
        return true;
        #else
        return false;
        #endif
    }
}
