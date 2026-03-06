using UnityEngine;
using UnityEngine.InputSystem;

public class RegionSelector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private RegionManager regionManager;

    void Update()
    {
        if (regionManager == null)
        {
            regionManager = FindFirstObjectByType<RegionManager>();
            if (regionManager == null) return;
        }

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
                    regionManager.SelectedRegion = region;

                return;
            }
        }

        regionManager.HoveredRegion = null;
        regionManager.SetHighlight(null);
    }
}
