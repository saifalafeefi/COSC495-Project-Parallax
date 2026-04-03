using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("name of the gameplay scene to load")]
    [SerializeField] private string gameSceneName = "DesktopScene";

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

    [Header("Preview UI (inside previewPanel)")]
    [Tooltip("text showing the difficulty name")]
    [SerializeField] private TMP_Text previewTitle;
    [Tooltip("text showing the difficulty description")]
    [SerializeField] private TMP_Text previewDescription;

    [Header("Fade Settings")]
    [SerializeField] private float fadeSpeed = 4f;

    private enum MenuState { Main, FadeToDifficulty, Difficulty, FadeToPreview, Preview, FadeBackToDifficulty, FadeToGame, FadeToMain }
    private MenuState state = MenuState.Main;
    private Difficulty selectedDifficulty;

    void Start()
    {
        if (mainPanel != null) SetGroup(mainPanel, true);
        if (difficultyPanel != null) SetGroup(difficultyPanel, false);
        if (previewPanel != null) SetGroup(previewPanel, false);
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
                    SceneManager.LoadScene(gameSceneName);
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

    void SetGroup(CanvasGroup group, bool active)
    {
        if (group == null) return;
        group.alpha = active ? 1f : 0f;
        group.interactable = active;
        group.blocksRaycasts = active;
    }
}
