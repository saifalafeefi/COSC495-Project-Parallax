using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class RegionSelector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private RegionManager regionManager;
    private DesktopInteraction desktopInteraction;
    private ARPlacement arPlacement;
    private bool isMobile;
    private bool touchStartedOnUI;

    // stores the last card play result for the debug UI to read
    public string LastPlayResult { get; set; }
    public float LastPlayTime { get; set; }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        #if UNITY_EDITOR
        isMobile = FindFirstObjectByType<ARPlacement>() != null;
        #elif UNITY_ANDROID || UNITY_IOS
        isMobile = true;
        #endif
    }

    void Update()
    {
        if (regionManager == null)
        {
            regionManager = FindFirstObjectByType<RegionManager>();
            if (regionManager == null) return;
        }

        if (desktopInteraction == null && arPlacement == null)
        {
            desktopInteraction = FindFirstObjectByType<DesktopInteraction>();
            if (desktopInteraction == null)
                arPlacement = FindFirstObjectByType<ARPlacement>();
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (PauseMenu.IsPaused) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null && (gm.RewardActive || gm.ShopActive || gm.DashboardActive || gm.BannerActive)) return;

        if (isMobile)
            UpdateTouch();
        else
            UpdateMouse();
    }

    private Region lastHoveredRegion;

    void UpdateMouse()
    {
        if (Mouse.current == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<RegionManager>() == regionManager)
            {
                Region region = regionManager.GetRegionAtHit(hit.triangleIndex);

                // play hover sound when entering a new region
                if (region != lastHoveredRegion && region != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.regionHover);
                lastHoveredRegion = region;

                regionManager.HoveredRegion = region;
                regionManager.SetHighlight(region);

                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.regionSelect);
                    regionManager.SelectedRegion = region;
                    if (region != null && desktopInteraction != null)
                        desktopInteraction.FocusOnRegion(region, regionManager);
                }

                return;
            }
        }

        lastHoveredRegion = null;
        regionManager.HoveredRegion = null;
        regionManager.SetHighlight(null);

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (regionManager.SelectedRegion != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.regionDeselect);
            regionManager.SelectedRegion = null;
            if (desktopInteraction != null)
                desktopInteraction.Unfocus();
        }
    }

    void UpdateTouch()
    {
        if (Touch.activeTouches.Count == 0)
        {
            regionManager.HoveredRegion = null;
            regionManager.SetHighlight(null);
            return;
        }

        var touch = Touch.activeTouches[0];

        // track if the touch started on UI so we don't process it as a region tap
        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            touchStartedOnUI = EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.touchId);
        }

        if (touchStartedOnUI) return;

        // continuous hover highlight while finger is on screen
        Ray ray = mainCamera.ScreenPointToRay(touch.screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<RegionManager>() == regionManager)
            {
                Region region = regionManager.GetRegionAtHit(hit.triangleIndex);
                regionManager.HoveredRegion = region;
                regionManager.SetHighlight(region);

                // tap to select region (on finger lift for clean feel)
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    if (regionManager.SelectedRegion == region)
                    {
                        // tap same region again to deselect and unfocus
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.regionDeselect);
                        regionManager.SelectedRegion = null;
                        if (arPlacement != null) arPlacement.Unfocus();
                    }
                    else
                    {
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.regionSelect);
                        regionManager.SelectedRegion = region;
                        if (region != null && arPlacement != null)
                            arPlacement.FocusOnRegion(region, regionManager);
                    }
                }

                return;
            }
        }

        regionManager.HoveredRegion = null;
        regionManager.SetHighlight(null);

        // tap empty space to deselect and unfocus
        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
        {
            if (regionManager.SelectedRegion != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.regionDeselect);
            regionManager.SelectedRegion = null;
            if (arPlacement != null) arPlacement.Unfocus();
        }
    }
}
