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

    [Header("Codex")]
    [Tooltip("the codex display component (attach to any GameObject)")]
    [SerializeField] private CodexDisplay codexDisplay;

    [Header("Settings")]
    [Tooltip("the settings display component (attach to any GameObject)")]
    [SerializeField] private SettingsDisplay settingsDisplay;

    [Header("Fade Settings")]
    [SerializeField] private float fadeSpeed = 4f;

    private enum MenuState { Main, FadeToDifficulty, Difficulty, FadeToPreview, Preview, FadeBackToDifficulty, FadeToGame, FadeToMain, FadeToSettings, Settings, FadeSettingsBack, FadeToCodex, Codex, FadeCodexBack, FadeToTutorial }
    private MenuState state = MenuState.Main;
    private Difficulty selectedDifficulty;

    void Start()
    {
        if (mainPanel != null) SetGroup(mainPanel, true);
        if (difficultyPanel != null) SetGroup(difficultyPanel, false);
        if (previewPanel != null) SetGroup(previewPanel, false);
        if (settingsPanel != null) SetGroup(settingsPanel, false);

        // always clear the tutorial flag on menu load so normal play starts fresh next time
        TutorialSettings.IsTutorial = false;

        // apply all saved settings on menu load
        SettingsManager.ApplyAll();
    }

    void Update()
    {
        // spin the decorative earth
        if (earthObject != null)
        {
            float angle = spinSpeed * SettingsManager.SpinSpeed * Time.deltaTime;
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

            case MenuState.FadeToTutorial:
                // tutorial skips difficulty select — fade the main panel straight out into the game scene.
                // always load the desktop scene regardless of platform: the tutorial is a scripted walkthrough
                // that doesn't benefit from AR placement, and keeping it on one scene avoids double-maintaining steps
                mainPanel.alpha = Mathf.MoveTowards(mainPanel.alpha, 0f, t);
                if (mainPanel.alpha <= 0.01f)
                    SceneManager.LoadScene(desktopSceneName);
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

            case MenuState.FadeToCodex:
                mainPanel.alpha = Mathf.MoveTowards(mainPanel.alpha, 0f, t);
                if (mainPanel.alpha <= 0.01f)
                {
                    SetGroup(mainPanel, false);
                    if (codexDisplay != null) codexDisplay.Show();
                    state = MenuState.Codex;
                }
                break;

            case MenuState.Codex:
                break;

            case MenuState.FadeCodexBack:
                SetGroup(mainPanel, true);
                mainPanel.alpha = 0f;
                state = MenuState.Main;
                break;
        }
    }

    // -- button callbacks (wire in Inspector) --

    // main panel
    public void OnStartGame()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        state = MenuState.FadeToDifficulty;
    }

    // starts the scripted tutorial — skips difficulty select and loads straight into the game scene
    // tutorial runs on a fixed difficulty regardless of menu selection; TutorialManager overrides deck, events, and state
    public void OnTutorial()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
            // fade menu music out during the panel fade so it's silent by scene load
            AudioManager.Instance.FadeOutMusic();
        }

        // force a known baseline so the tutorial runs predictably
        DifficultySettings.Current = Difficulty.Normal;
        TutorialSettings.IsTutorial = true;

        state = MenuState.FadeToTutorial;
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        Debug.Log("[MainMenu] quit");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // difficulty panel — click to preview
    public void SelectEasy() { if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick); ShowPreview(Difficulty.Easy); }
    public void SelectNormal() { if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick); ShowPreview(Difficulty.Normal); }
    public void SelectHard() { if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick); ShowPreview(Difficulty.Hard); }

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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
            // fade out menu music during the panel fade so it's silent by scene load
            AudioManager.Instance.FadeOutMusic();
        }
        DifficultySettings.Current = selectedDifficulty;
        if (previewPanel != null) previewPanel.interactable = false;
        state = MenuState.FadeToGame;
    }

    public void OnPreviewBack()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        state = MenuState.FadeBackToDifficulty;
    }

    // difficulty panel back to main
    public void OnBack()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        state = MenuState.FadeToMain;
    }

    // -- settings --

    public void OnSettings()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        if (settingsDisplay != null)
        {
            // runtime overlay renders on top — no state change needed
            settingsDisplay.Show();
        }
        else
        {
            state = MenuState.FadeToSettings;
        }
    }

    public void OnSettingsBack()
    {
        if (settingsDisplay != null)
        {
            // settings display is an overlay — just hide it, main panel is still visible
            if (settingsDisplay.IsShowing) settingsDisplay.Hide();
        }
        else
        {
            state = MenuState.FadeSettingsBack;
        }
    }

    // codex
    public void OnCodex()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        state = MenuState.FadeToCodex;
    }

    public void OnCodexBack()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        if (codexDisplay != null) codexDisplay.Hide();
        state = MenuState.FadeCodexBack;
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
