using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Carbon Conquest/Event")]
public class EventData : ScriptableObject
{
    public string eventName;
    [TextArea] public string description;
    public RegionTrait[] affectedTraits;
    public float carbonDelta;
    public float economyDelta;
    public float stabilityDelta;

    [Tooltip("if true, hits all regions (ignores trait filter)")]
    public bool targetAll;

    [Tooltip("if > 0, picks this many random matching regions instead of all")]
    public int randomTargetCount;

    // returns the list of regions this event will hit
    public List<Region> GetAffectedRegions(List<Region> allRegions)
    {
        var candidates = new List<Region>();

        if (targetAll)
        {
            candidates.AddRange(allRegions);
        }
        else
        {
            // filter by trait
            foreach (var region in allRegions)
            {
                foreach (var trait in affectedTraits)
                {
                    if (region.Trait == trait)
                    {
                        candidates.Add(region);
                        break;
                    }
                }
            }
        }

        // pick random subset if needed
        if (randomTargetCount > 0 && candidates.Count > randomTargetCount)
        {
            // fisher-yates shuffle then take first N
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temp;
            }
            candidates.RemoveRange(randomTargetCount, candidates.Count - randomTargetCount);
        }

        return candidates;
    }
}
