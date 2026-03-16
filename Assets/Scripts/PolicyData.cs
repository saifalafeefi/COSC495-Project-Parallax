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
                // reforestation is 50% more effective in tropical regions
                if (policyName.Contains("Reforestation"))
                    carbon *= 1.5f;
                break;

            case RegionTrait.Arid:
                // reforestation is 50% less effective in arid regions
                if (policyName.Contains("Reforestation"))
                    carbon *= 0.5f;
                break;

            case RegionTrait.Industrial:
                // economy policies are 50% more effective, carbon penalties are 50% harsher
                economy *= 1.5f;
                if (carbon > 0) carbon *= 1.5f;
                break;

            case RegionTrait.Frozen:
                // industrial policies are 50% less effective
                if (policyName.Contains("Industrial") || policyName.Contains("Coal"))
                {
                    carbon *= 0.5f;
                    economy *= 0.5f;
                    stability *= 0.5f;
                }
                break;
        }
    }
}
