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

        if (desktopInteraction == null)
            desktopInteraction = FindFirstObjectByType<DesktopInteraction>();
        if (regionManager == null)
            regionManager = FindFirstObjectByType<RegionManager>();

        // ESC priority: unfocus region first, then toggle pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // block pause while reward popup is showing
            if (gameManager.RewardActive)
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
                else if (desktopInteraction != null && desktopInteraction.IsFocused)
                {
                    // unfocus camera and deselect region instead of pausing
                    desktopInteraction.Unfocus();
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

    void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
    }

    public void DoRestart()
    {
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

    public void GoToMainMenu()
    {
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
