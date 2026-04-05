using UnityEngine;
using UnityEngine.InputSystem;

public class RegionSelector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private RegionManager regionManager;
    private DesktopInteraction desktopInteraction;
    private ARPlacement arPlacement;

    // stores the last card play result for the debug UI to read
    public string LastPlayResult { get; set; }
    public float LastPlayTime { get; set; }

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

        // don't process hover/select while paused or reward popup is showing
        if (PauseMenu.IsPaused) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null && (gm.RewardActive || gm.ShopActive || gm.DashboardActive || gm.BannerActive)) return;

        if (Mouse.current == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<RegionManager>() == regionManager)
            {
                Region region = regionManager.GetRegionAtHit(hit.triangleIndex);
                regionManager.HoveredRegion = region;
                regionManager.SetHighlight(region);

                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    regionManager.SelectedRegion = region;

                    // focus on the region — camera orbits (desktop) or earth rotates (AR)
                    if (region != null)
                    {
                        if (desktopInteraction != null)
                            desktopInteraction.FocusOnRegion(region, regionManager);
                        else if (arPlacement != null)
                            arPlacement.FocusOnRegion(region, regionManager);
                    }
                }

                return;
            }
        }

        regionManager.HoveredRegion = null;
        regionManager.SetHighlight(null);

        // right-click on empty space or ocean to unfocus and deselect
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            regionManager.SelectedRegion = null;
            if (desktopInteraction != null)
                desktopInteraction.Unfocus();
            else if (arPlacement != null)
                arPlacement.Unfocus();
        }
    }
}
