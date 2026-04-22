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
                    // tutorial blocks region selection unless the current step asks for it
                    if (!TutorialManager.CanPerformAction(TutorialAction.SelectRegion)) return;

                    // during tutorial region steps, lock selection to the highlighted region so
                    // the player can't pick a different one than the mascot is pointing at
                    if (TutorialManager.IsActive && regionManager.TutorialTargetRegion != null && region != regionManager.TutorialTargetRegion) return;

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.regionSelect);
                    regionManager.SelectedRegion = region;
                    if (region != null && desktopInteraction != null)
                        desktopInteraction.FocusOnRegion(region, regionManager);

                    TutorialManager.NotifyAction(TutorialAction.SelectRegion);
                }

                return;
            }
        }

        lastHoveredRegion = null;
        regionManager.HoveredRegion = null;
        regionManager.SetHighlight(null);

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // while the tutorial is active and a region is already committed, block *all*
            // user-driven deselect — the tutorial's flow (NotifyAction, panel close, etc)
            // is the only thing allowed to clear the selection, so a stray right-click in
            // empty space can't soft-brick a step that expects the region to stay focused
            if (TutorialManager.IsActive && regionManager.SelectedRegion != null) return;

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
                        // tutorial locks selection in place — only NotifyAction/panel flow can clear it
                        if (TutorialManager.IsActive) return;

                        // tap same region again to deselect and unfocus
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.regionDeselect);
                        regionManager.SelectedRegion = null;
                        // desktop scene running on a phone has no ARPlacement, so try DesktopInteraction first
                        if (desktopInteraction != null) desktopInteraction.Unfocus();
                        else if (arPlacement != null) arPlacement.Unfocus();
                    }
                    else
                    {
                        // tutorial blocks region selection unless the current step asks for it
                        if (!TutorialManager.CanPerformAction(TutorialAction.SelectRegion)) return;

                        // during tutorial region steps, lock selection to the highlighted region so
                        // the player can't pick a different one than the mascot is pointing at
                        if (TutorialManager.IsActive && regionManager.TutorialTargetRegion != null && region != regionManager.TutorialTargetRegion) return;

                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.regionSelect);
                        regionManager.SelectedRegion = region;
                        if (region != null)
                        {
                            if (desktopInteraction != null)
                                desktopInteraction.FocusOnRegion(region, regionManager);
                            else if (arPlacement != null)
                                arPlacement.FocusOnRegion(region, regionManager);
                        }

                        TutorialManager.NotifyAction(TutorialAction.SelectRegion);
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
            // tutorial locks the region in place once it's selected — any tap in empty space
            // would otherwise soft-brick a step that waits on the focused region
            if (TutorialManager.IsActive && regionManager.SelectedRegion != null) return;

            if (regionManager.SelectedRegion != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.regionDeselect);
            regionManager.SelectedRegion = null;
            // desktop scene running on a phone has no ARPlacement, so try DesktopInteraction first
            if (desktopInteraction != null) desktopInteraction.Unfocus();
            else if (arPlacement != null) arPlacement.Unfocus();
        }
    }
}
