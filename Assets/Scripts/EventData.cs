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

    [Header("Focus Targeting")]
    [Tooltip("targets regions with plays exceeding average by this amount (0 = disabled)")]
    public int focusThreshold;

    // returns the list of regions this event will hit using focus targeting
    public List<Region> GetAffectedRegions(List<Region> allRegions, GameManager gm)
    {
        // focus targeting takes priority
        if (focusThreshold > 0 && gm != null)
        {
            var candidates = new List<Region>();
            float avg = gm.GetAveragePlayCount();
            foreach (var r in allRegions)
            {
                if (gm.GetPlayCount(r) >= avg + focusThreshold)
                    candidates.Add(r);
            }
            return ApplyRandomSubset(candidates);
        }

        // default: trait-based or target-all
        return GetAffectedRegions(allRegions);
    }

    // standard targeting for normal events (trait filter or target-all)
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

        return ApplyRandomSubset(candidates);
    }

    // pick random subset if randomTargetCount is set
    List<Region> ApplyRandomSubset(List<Region> candidates)
    {
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

    // returns true if this is a focus event (not part of the random pool)
    public bool IsFocusEvent()
    {
        return focusThreshold > 0;
    }
}
