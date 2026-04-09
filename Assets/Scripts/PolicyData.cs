using System.Collections.Generic;
using UnityEngine;

public enum PolicyRarity
{
    Common,
    Uncommon,
    Rare
}

[CreateAssetMenu(menuName = "Carbon Conquest/Policy")]
public class PolicyData : ScriptableObject
{
    public string policyName;
    [TextArea] public string description;
    public float carbonDelta;
    public float economyDelta;
    public float stabilityDelta;
    public PolicyRarity rarity;
    public Sprite icon;

    [Tooltip("political capital cost to play this card (0 = free)")]
    public int politicalCapitalCost = 1;

    [Tooltip("overrides the default spillover % to neighbors (0 = use GameManager default)")]
    public float spilloverOverride;

    // returns adjusted deltas based on region trait
    public void GetModifiedDeltas(Region target, out float carbon, out float economy, out float stability)
    {
        carbon = carbonDelta;
        economy = economyDelta;
        stability = stabilityDelta;

        if (target == null) return;

        switch (target.Trait)
        {
            case RegionTrait.Tropical:
                // reforestation thrives in tropical regions
                if (policyName.Contains("Reforestation"))
                    carbon *= 1.5f;
                // warm climate boosts solar output
                if (policyName.Contains("Solar"))
                    carbon *= 1.3f;
                // dense population makes public transport very effective
                if (policyName.Contains("Transport"))
                {
                    carbon *= 1.3f;
                    stability *= 1.3f;
                }
                break;

            case RegionTrait.Arid:
                // reforestation struggles in dry climate
                if (policyName.Contains("Reforestation"))
                    carbon *= 0.5f;
                // wind and solar excel in open desert
                if (policyName.Contains("Wind") || policyName.Contains("Solar"))
                {
                    carbon *= 1.5f;
                    economy *= 1.3f;
                }
                // desalination is critical in arid regions
                if (policyName.Contains("Desalination"))
                    stability *= 1.5f;
                break;

            case RegionTrait.Industrial:
                // economy gains amplified, but carbon cost is harsher
                economy *= 1.5f;
                if (carbon > 0) carbon *= 1.5f;
                // carbon capture and nuclear are more effective at scale
                if (policyName.Contains("Carbon Capture") || policyName.Contains("Nuclear"))
                    carbon *= 1.5f;
                // waste management has more to work with
                if (policyName.Contains("Waste"))
                    carbon *= 1.3f;
                break;

            case RegionTrait.Frozen:
                // industrial and coal policies are less effective in harsh conditions
                if (policyName.Contains("Industrial") || policyName.Contains("Coal"))
                {
                    carbon *= 0.5f;
                    economy *= 0.5f;
                    stability *= 0.5f;
                }
                // nuclear energy thrives — no cooling issues
                if (policyName.Contains("Nuclear"))
                    carbon *= 1.3f;
                // reforestation is weak in frozen soil
                if (policyName.Contains("Reforestation"))
                    carbon *= 0.5f;
                break;

            case RegionTrait.Coastal:
                // trade flourishes along coasts
                if (policyName.Contains("Trade"))
                {
                    economy *= 1.5f;
                    stability *= 1.3f;
                }
                // desalination is natural fit for coastal regions
                if (policyName.Contains("Desalination"))
                    stability *= 1.5f;
                // eco tourism draws visitors to beaches
                if (policyName.Contains("Tourism"))
                    economy *= 1.5f;
                // wind farms work well on coastlines
                if (policyName.Contains("Wind"))
                    carbon *= 1.3f;
                break;

            case RegionTrait.Temperate:
                // balanced climate makes most green policies slightly better
                if (policyName.Contains("Reforestation") || policyName.Contains("Wind") || policyName.Contains("Solar"))
                    carbon *= 1.2f;
                // public transport networks thrive in temperate cities
                if (policyName.Contains("Transport"))
                {
                    carbon *= 1.3f;
                    stability *= 1.3f;
                }
                // stable climate makes eco tourism viable
                if (policyName.Contains("Tourism"))
                    economy *= 1.3f;
                break;
        }
    }

    // cached list of traits that benefit this card
    private List<RegionTrait> cachedBeneficialTraits;

    // returns all region traits that produce a net-positive effect on this card
    public List<RegionTrait> GetBeneficialTraits()
    {
        if (cachedBeneficialTraits != null) return cachedBeneficialTraits;

        cachedBeneficialTraits = new List<RegionTrait>();
        var allTraits = new[]
        {
            RegionTrait.Temperate, RegionTrait.Tropical, RegionTrait.Arid,
            RegionTrait.Frozen, RegionTrait.Industrial, RegionTrait.Coastal
        };

        foreach (var trait in allTraits)
        {
            var dummy = new Region("dummy", Color.white, new List<int>());
            dummy.Trait = trait;
            GetModifiedDeltas(dummy, out float mc, out float me, out float ms);

            // carbon: lower is better (more negative = more reduction)
            // economy/stability: higher is better
            float carbonBenefit = carbonDelta - mc;
            float econBenefit = me - economyDelta;
            float stabBenefit = ms - stabilityDelta;

            // trait counts as beneficial if the overall effect is positive
            float netBenefit = carbonBenefit + econBenefit + stabBenefit;
            if (netBenefit > 0.01f)
                cachedBeneficialTraits.Add(trait);
        }

        return cachedBeneficialTraits;
    }
}
