using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RegionSelector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private RegionManager regionManager;
    private DesktopInteraction desktopInteraction;
    private GameManager gameManager;

    // stores the last card play result for the debug UI to read
    public string LastPlayResult { get; private set; }
    public float LastPlayTime { get; private set; }

    void Update()
    {
        if (regionManager == null)
        {
            regionManager = FindFirstObjectByType<RegionManager>();
            if (regionManager == null) return;
        }

        if (desktopInteraction == null)
            desktopInteraction = FindFirstObjectByType<DesktopInteraction>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (Mouse.current == null) return;

        // number keys 1/2/3 play cards on the selected region
        if (gameManager != null && regionManager.SelectedRegion != null && !gameManager.GameOver
            && gameManager.CurrentHand != null && gameManager.CurrentHand.Count > 0)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                int cardIndex = -1;
                if (keyboard.digit1Key.wasPressedThisFrame) cardIndex = 0;
                else if (keyboard.digit2Key.wasPressedThisFrame) cardIndex = 1;
                else if (keyboard.digit3Key.wasPressedThisFrame) cardIndex = 2;

                if (cardIndex >= 0 && cardIndex < gameManager.CurrentHand.Count)
                {
                    LastPlayResult = gameManager.PlayCard(cardIndex, regionManager.SelectedRegion);
                    LastPlayTime = Time.time;
                }
            }
        }

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
