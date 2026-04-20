using UnityEngine;
using UnityEngine.EventSystems;

public enum TabAction
{
    None,
    Shop,
    Dashboard,
    Pause,
    SkipRound
}

public class SideTabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Tween Settings")]
    [Tooltip("how far offscreen the button sits when retracted (x = right, y = up)")]
    [SerializeField] private Vector2 hiddenOffset = new Vector2(80f, 0f);
    [Tooltip("how long the slide animation takes in seconds")]
    [SerializeField] private float tweenDuration = 0.25f;

    [Header("Action")]
    [Tooltip("what this tab button does when clicked")]
    [SerializeField] private TabAction action = TabAction.None;

    // the tutorial reads this to find and highlight specific tab buttons by action
    public TabAction Action => action;

    [Header("Hover")]
    [Tooltip("disable hover slide-in/out (for buttons that should always be fully visible)")]
    [SerializeField] private bool disableHover = false;

    [Header("Shop Hide")]
    [Tooltip("extra offset to push the button offscreen when shop is open (x = horizontal, y = vertical)")]
    [SerializeField] private Vector2 shopHideOffset = new Vector2(200f, 0f);
    [SerializeField] private float shopSlideSpeed = 6f;

    private RectTransform rect;
    private Vector2 shownPos;
    private Vector2 hiddenPos;
    private float tweenProgress = 1f;
    private bool hovering;
    private float shopSlideBlend;
    private bool isMobile;

    private GameManager gameManager;

    // fired when the button is clicked — assign listener in code or use the action enum
    public System.Action OnClicked;

    void Awake()
    {
        rect = GetComponent<RectTransform>();

        #if UNITY_EDITOR
        isMobile = FindFirstObjectByType<ARPlacement>() != null;
        #elif UNITY_ANDROID || UNITY_IOS
        isMobile = true;
        #endif

        shownPos = rect.anchoredPosition;

        if (isMobile || disableHover)
        {
            hiddenPos = shownPos;
            tweenProgress = 1f;
        }
        else
        {
            hiddenPos = shownPos + hiddenOffset;
            rect.anchoredPosition = hiddenPos;
            tweenProgress = 0f;
        }
    }

    void Update()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        // shop slide
        float shopTarget = (gameManager != null && gameManager.ShopActive) ? 1f : 0f;
        shopSlideBlend = Mathf.Lerp(shopSlideBlend, shopTarget, shopSlideSpeed * Time.unscaledDeltaTime);
        if (Mathf.Abs(shopSlideBlend - shopTarget) < 0.005f) shopSlideBlend = shopTarget;

        // hover tween
        float hoverTarget = hovering ? 1f : 0f;
        bool hoverSettled = Mathf.Abs(tweenProgress - hoverTarget) < 0.001f;
        bool shopSettled = Mathf.Abs(shopSlideBlend - shopTarget) < 0.001f;

        if (hoverSettled && shopSettled) return;

        if (!hoverSettled)
        {
            float step = Time.unscaledDeltaTime / Mathf.Max(tweenDuration, 0.01f);
            tweenProgress = Mathf.MoveTowards(tweenProgress, hoverTarget, step);
        }

        float t = 1f - Mathf.Pow(1f - tweenProgress, 3f);
        rect.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, t) + shopHideOffset * shopSlideBlend;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        // block during shop transition (opening/closing tween)
        var shopDisplay = FindFirstObjectByType<ShopDisplay>();
        if (shopDisplay != null && shopDisplay.IsTransitioning) return;

        // pause only blocked during reward, everything else blocked during banners too
        if (gameManager != null)
        {
            if (action == TabAction.Pause)
            {
                if (gameManager.RewardActive) return;
            }
            else
            {
                if (gameManager.BannerActive || gameManager.RewardActive) return;
            }
        }

        if (OnClicked != null)
        {
            OnClicked.Invoke();
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);

        if (gameManager == null) return;

        switch (action)
        {
            case TabAction.Shop:
                // close is always allowed; open is gated by the tutorial
                if (gameManager.ShopActive)
                {
                    gameManager.CloseShop();
                }
                else
                {
                    if (!TutorialManager.CanPerformAction(TutorialAction.OpenShop)) return;
                    gameManager.OpenShop();
                    TutorialManager.NotifyAction(TutorialAction.OpenShop);
                }
                break;
            case TabAction.Dashboard:
                // both open and close are gated by the tutorial so the mascot can script each step
                if (gameManager.DashboardActive)
                {
                    if (!TutorialManager.CanPerformAction(TutorialAction.CloseDashboard)) return;
                    gameManager.CloseDashboard();
                    TutorialManager.NotifyAction(TutorialAction.CloseDashboard);
                }
                else
                {
                    if (!TutorialManager.CanPerformAction(TutorialAction.OpenDashboard)) return;
                    gameManager.OpenDashboard();
                    TutorialManager.NotifyAction(TutorialAction.OpenDashboard);
                }
                break;
            case TabAction.Pause:
                // pause stays available at any time so the player can bail out of the tutorial
                var pauseMenu = FindFirstObjectByType<PauseMenu>();
                if (pauseMenu != null)
                    pauseMenu.TogglePause();
                break;
            case TabAction.SkipRound:
                if (!TutorialManager.CanPerformAction(TutorialAction.SkipRound)) return;
                if (!gameManager.GameOver)
                    gameManager.SkipRound();
                TutorialManager.NotifyAction(TutorialAction.SkipRound);
                break;
        }
    }
}
