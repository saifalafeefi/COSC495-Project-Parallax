using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("name of the gameplay scene to load")]
    [SerializeField] private string gameSceneName = "DesktopScene";

    [Header("Earth Spin")]
    [Tooltip("optional — assign the earth object in the menu scene to auto-spin it")]
    [SerializeField] private GameObject earthObject;
    [SerializeField] private float spinSpeed = 8f;

    [Header("Panels (assign in Inspector)")]
    [Tooltip("the main menu panel (title, start, quit) — needs CanvasGroup")]
    [SerializeField] private CanvasGroup mainPanel;
    [Tooltip("the difficulty selection panel (easy, normal, hard, back) — needs CanvasGroup")]
    [SerializeField] private CanvasGroup difficultyPanel;

    [Header("Fade Settings")]
    [SerializeField] private float fadeSpeed = 4f;

    private enum MenuState { Main, FadeToDifficulty, Difficulty, FadeToMain, FadeToGame }
    private MenuState state = MenuState.Main;

    void Start()
    {
        if (mainPanel != null) SetGroup(mainPanel, true);
        if (difficultyPanel != null) SetGroup(difficultyPanel, false);
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
                // fade in main panel
                mainPanel.alpha = Mathf.MoveTowards(mainPanel.alpha, 1f, t);
                break;

            case MenuState.FadeToDifficulty:
                // fade out main panel, then show difficulty
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
                // fade in difficulty panel
                difficultyPanel.alpha = Mathf.MoveTowards(difficultyPanel.alpha, 1f, t);
                break;

            case MenuState.FadeToMain:
                // fade out difficulty panel, then show main
                difficultyPanel.alpha = Mathf.MoveTowards(difficultyPanel.alpha, 0f, t);
                if (difficultyPanel.alpha <= 0.01f)
                {
                    SetGroup(difficultyPanel, false);
                    SetGroup(mainPanel, true);
                    mainPanel.alpha = 0f;
                    state = MenuState.Main;
                }
                break;

            case MenuState.FadeToGame:
                // fade out difficulty panel, then load game
                difficultyPanel.alpha = Mathf.MoveTowards(difficultyPanel.alpha, 0f, t);
                if (difficultyPanel.alpha <= 0.01f)
                    SceneManager.LoadScene(gameSceneName);
                break;
        }
    }

    // -- button callbacks (wire in Inspector) --

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

    public void SelectEasy()
    {
        DifficultySettings.Current = Difficulty.Easy;
        StartGameWithFade();
    }

    public void SelectNormal()
    {
        DifficultySettings.Current = Difficulty.Normal;
        StartGameWithFade();
    }

    public void SelectHard()
    {
        DifficultySettings.Current = Difficulty.Hard;
        StartGameWithFade();
    }

    public void OnBack()
    {
        state = MenuState.FadeToMain;
    }

    void StartGameWithFade()
    {
        if (difficultyPanel != null) difficultyPanel.interactable = false;
        state = MenuState.FadeToGame;
    }

    void SetGroup(CanvasGroup group, bool active)
    {
        group.alpha = active ? 1f : 0f;
        group.interactable = active;
        group.blocksRaycasts = active;
    }
}
