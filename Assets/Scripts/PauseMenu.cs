using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Game Over UI")]
    [Tooltip("restart button shown on the game over screen")]
    [SerializeField] private Button gameOverRestartButton;

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
        if (gameOverRestartButton != null)
            gameOverRestartButton.onClick.AddListener(DoRestart);

        // make sure everything starts hidden
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
        if (gameOverRestartButton != null)
            gameOverRestartButton.gameObject.SetActive(false);
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
            // block pause while reward/shop/banner popup is showing
            if (gameManager.RewardActive || gameManager.BannerActive)
            { }
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
            }
            else
            {
                if (gameOverRestartButton != null)
                    gameOverRestartButton.gameObject.SetActive(false);
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

        if (gameManager != null)
            gameManager.RestartGame();
    }

    void OnDestroy()
    {
        // restore time scale if this gets destroyed
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
