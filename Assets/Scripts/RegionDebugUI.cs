using UnityEngine;
using TMPro;

public class RegionDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;

    private RegionManager regionManager;

    void Update()
    {
        if (regionManager == null)
        {
            regionManager = FindFirstObjectByType<RegionManager>();
            if (regionManager == null) return;
        }

        Region selected = regionManager.SelectedRegion;
        Region hovered = regionManager.HoveredRegion;

        if (selected != null)
        {
            string neighborNames = selected.Neighbors.Count > 0
                ? string.Join(", ", selected.Neighbors.ConvertAll(n => n.RegionName))
                : "none";

            displayText.text = $"<b>{selected.RegionName}</b>  [{selected.Trait}]\n"
                + $"Carbon:    {selected.CarbonLevel:F0}\n"
                + $"Economy:   {selected.EconomyLevel:F0}\n"
                + $"Stability: {selected.StabilityLevel:F0}\n"
                + $"Neighbors: {neighborNames}";

            if (hovered != null && hovered != selected)
                displayText.text += $"\n\n<i>Hovering: {hovered.RegionName} [{hovered.Trait}]</i>";
        }
        else if (hovered != null)
        {
            displayText.text = $"<i>{hovered.RegionName}</i>  [{hovered.Trait}]\nRight-click to select";
        }
        else
        {
            displayText.text = "Hover over a region on Earth";
        }
    }
}
