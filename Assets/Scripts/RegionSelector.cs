using UnityEngine;
using UnityEngine.InputSystem;

public class RegionSelector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private RegionManager regionManager;
    private DesktopInteraction desktopInteraction;

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

        if (desktopInteraction == null)
            desktopInteraction = FindFirstObjectByType<DesktopInteraction>();

        if (mainCamera == null)
            mainCamera = Camera.main;

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

                    // get the direction from earth center to the region and focus the camera there
                    if (region != null && desktopInteraction != null)
                    {
                        Vector3 earthPos = regionManager.transform.position;
                        Vector3 regionCenter = regionManager.GetRegionWorldCenter(region);
                        Vector3 dir = (regionCenter - earthPos).normalized;
                        desktopInteraction.FocusOnDirection(dir);
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
        }
    }
}
