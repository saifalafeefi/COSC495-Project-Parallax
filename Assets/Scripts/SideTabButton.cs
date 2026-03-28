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

    private RectTransform rect;
    private float shownX;
    private float hiddenX;
    private float tweenProgress = 1f;
    private bool hovering;

    private GameManager gameManager;

    // fired when the button is clicked — assign listener in code or use the action enum
    public System.Action OnClicked;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        shownX = rect.anchoredPosition.x;
        hiddenX = shownX + hiddenOffsetX;

        // start in hidden position
        var pos = rect.anchoredPosition;
        pos.x = hiddenX;
        rect.anchoredPosition = pos;
        tweenProgress = 0f;
    }

    void Update()
    {
        float target = hovering ? 1f : 0f;
        if (Mathf.Abs(tweenProgress - target) < 0.001f) return;

        float step = Time.unscaledDeltaTime / Mathf.Max(tweenDuration, 0.01f);
        tweenProgress = Mathf.MoveTowards(tweenProgress, target, step);

        // ease out cubic
        float t = 1f - Mathf.Pow(1f - tweenProgress, 3f);
        var pos = rect.anchoredPosition;
        pos.x = Mathf.Lerp(hiddenX, shownX, t);
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
