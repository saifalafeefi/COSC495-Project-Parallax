using UnityEngine;
using UnityEngine.EventSystems;

public enum TabAction
{
    None,
    Shop,
    Dashboard
}

public class SideTabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Tween Settings")]
    [Tooltip("how far offscreen the button sits when retracted (positive = further right)")]
    [SerializeField] private float hiddenOffsetX = 80f;
    [Tooltip("how long the slide animation takes in seconds")]
    [SerializeField] private float tweenDuration = 0.25f;

    [Header("Action")]
    [Tooltip("what this tab button does when clicked")]
    [SerializeField] private TabAction action = TabAction.None;

    [Header("Shop Hide")]
    [Tooltip("extra offset to push the button fully offscreen when shop is open")]
    [SerializeField] private float shopHideOffset = 200f;
    [SerializeField] private float shopSlideSpeed = 6f;

    private RectTransform rect;
    private float shownX;
    private float hiddenX;
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

        if (isMobile)
        {
            // on mobile, buttons are always visible (no hover to reveal)
            shownX = rect.anchoredPosition.x;
            hiddenX = shownX;
            tweenProgress = 1f;
        }
        else
        {
            shownX = rect.anchoredPosition.x;
            hiddenX = shownX + hiddenOffsetX;

            // start in hidden position
            var pos = rect.anchoredPosition;
            pos.x = hiddenX;
            rect.anchoredPosition = pos;
            tweenProgress = 0f;
        }
    }

    void Update()
    {
        // shop slide — always update even when hover tween is settled
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
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

        // ease out cubic
        float t = 1f - Mathf.Pow(1f - tweenProgress, 3f);
        var pos = rect.anchoredPosition;
        pos.x = Mathf.Lerp(hiddenX, shownX, t) + shopHideOffset * shopSlideBlend;
        rect.anchoredPosition = pos;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
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

        // block interaction during banners and other overlays
        if (gameManager != null && (gameManager.BannerActive || gameManager.RewardActive)) return;

        if (OnClicked != null)
        {
            OnClicked.Invoke();
            return;
        }

        if (gameManager == null) return;

        switch (action)
        {
            case TabAction.Shop:
                if (gameManager.ShopActive)
                    gameManager.CloseShop();
                else
                    gameManager.OpenShop();
                break;
            case TabAction.Dashboard:
                if (gameManager.DashboardActive)
                    gameManager.CloseDashboard();
                else
                    gameManager.OpenDashboard();
                break;
        }
    }
}
