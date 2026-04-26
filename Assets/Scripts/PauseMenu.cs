using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause UI (assign in Inspector)")]
    [Tooltip("full-screen overlay panel, hidden by default")]
    [SerializeField] private GameObject pauseOverlay;

    [Tooltip("resume button inside the pause overlay")]
    [SerializeField] private Button resumeButton;

    [Tooltip("restart button inside the pause overlay")]
    [SerializeField] private Button pauseRestartButton;

    [Tooltip("main menu button inside the pause overlay")]
    [SerializeField] private Button mainMenuButton;

    [Tooltip("settings button inside the pause overlay (opens the in-game SettingsDisplay)")]
    [SerializeField] private Button settingsButton;

    [Tooltip("in-game SettingsDisplay instance (separate from main menu's; should have Hide Mobile Tab enabled)")]
    [SerializeField] private SettingsDisplay settingsDisplay;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Game Over UI")]
    [Tooltip("restart button shown on the game over screen")]
    [SerializeField] private Button gameOverRestartButton;

    [Tooltip("main menu button shown on the game over screen")]
    [SerializeField] private Button gameOverMainMenuButton;

    public static bool IsPaused { get; private set; }

    private GameManager gameManager;
    private DesktopInteraction desktopInteraction;
    private ARPlacement arPlacement;
    private RegionManager regionManager;
    private bool wasGameOver;

    void Start()
    {
        // wire up button callbacks
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (pauseRestartButton != null)
            pauseRestartButton.onClick.AddListener(DoRestart);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (gameOverRestartButton != null)
            gameOverRestartButton.onClick.AddListener(DoRestart);
        if (gameOverMainMenuButton != null)
            gameOverMainMenuButton.onClick.AddListener(GoToMainMenu);
        // make sure everything starts hidden
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
        if (gameOverRestartButton != null)
            gameOverRestartButton.gameObject.SetActive(false);
        if (gameOverMainMenuButton != null)
            gameOverMainMenuButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }

        if (desktopInteraction == null && arPlacement == null)
        {
            desktopInteraction = FindFirstObjectByType<DesktopInteraction>();
            if (desktopInteraction == null)
                arPlacement = FindFirstObjectByType<ARPlacement>();
        }
        if (regionManager == null)
            regionManager = FindFirstObjectByType<RegionManager>();

        // ESC priority: unfocus region first, then toggle pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // settings panel is always the top-most modal — ESC closes it instead of touching pause state
            if (settingsDisplay != null && settingsDisplay.IsShowing)
            {
                settingsDisplay.Hide();
                return;
            }

            // block all ESC while skip confirm popup is showing (popup handles its own ESC)
            var skipPopup = FindFirstObjectByType<SkipConfirmPopup>();
            if (skipPopup != null && skipPopup.IsShowing)
            { }
            // block all ESC actions while shop is tweening open/closed
            else if (FindFirstObjectByType<ShopDisplay>() is ShopDisplay sd && sd.IsTransitioning)
            { }
            // block pause while reward popup is showing
            else if (gameManager.RewardActive)
            { }
            else if (gameManager.BannerActive)
            {
                // banner is playing — only allow pause toggle, nothing else
                if (!gameManager.GameOver)
                {
                    if (IsPaused) Resume();
                    else Pause();
                }
            }
            else if (gameManager.DashboardActive)
            {
                gameManager.CloseDashboard();
            }
            else if (gameManager.ShopActive)
            {
                gameManager.CloseShop();
            }
            else if (!gameManager.GameOver)
            {
                if (IsPaused)
                {
                    Resume();
                }
                else if ((desktopInteraction != null && desktopInteraction.IsFocused) ||
                         (arPlacement != null && arPlacement.IsFocused))
                {
                    // unfocus and deselect region instead of pausing
                    if (desktopInteraction != null) desktopInteraction.Unfocus();
                    else if (arPlacement != null) arPlacement.Unfocus();
                    if (regionManager != null)
                        regionManager.SelectedRegion = null;
                }
                else
                {
                    Pause();
                }
            }
        }

        // show/hide the game over restart button
        bool isGameOver = gameManager.GameOver;
        if (isGameOver != wasGameOver)
        {
            wasGameOver = isGameOver;

            if (isGameOver)
            {
                // unpause if we were paused when game ended
                if (IsPaused) Resume();

                if (gameOverRestartButton != null)
                    gameOverRestartButton.gameObject.SetActive(true);
                if (gameOverMainMenuButton != null)
                    gameOverMainMenuButton.gameObject.SetActive(true);
            }
            else
            {
                if (gameOverRestartButton != null)
                    gameOverRestartButton.gameObject.SetActive(false);
                if (gameOverMainMenuButton != null)
                    gameOverMainMenuButton.gameObject.SetActive(false);
            }
        }
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.panelOpen);

        if (pauseOverlay != null)
            pauseOverlay.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.panelClose);

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
    }

    public void DoRestart()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        IsPaused = false;
        Time.timeScale = 1f;
        wasGameOver = false;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
        if (gameOverRestartButton != null)
            gameOverRestartButton.gameObject.SetActive(false);
        if (gameOverMainMenuButton != null)
            gameOverMainMenuButton.gameObject.SetActive(false);

        if (gameManager != null)
            gameManager.RestartGame();
    }

    public void OpenSettings()
    {
        if (settingsDisplay == null) return;
        settingsDisplay.Show();
        // lift above pause overlay so settings draws on top
        var canvas = settingsDisplay.GetComponentInChildren<Canvas>();
        if (canvas != null) canvas.sortingOrder = 51;
    }

    public void GoToMainMenu()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnDestroy()
    {
        // restore time scale if this gets destroyed
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
