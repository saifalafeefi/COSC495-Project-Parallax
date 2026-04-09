using UnityEngine;

[CreateAssetMenu(menuName = "Carbon Conquest/Trait Color Config")]
public class TraitColorConfig : ScriptableObject
{
    private static TraitColorConfig instance;

    // auto-loads from Resources/TraitColors/ — no Inspector wiring needed
    public static TraitColorConfig Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<TraitColorConfig>("TraitColors/TraitColorConfig");
            return instance;
        }
    }

    [System.Serializable]
    public struct TraitColor
    {
        public RegionTrait trait;
        public Color color;
    }

    [Tooltip("color for each region trait shown on card borders")]
    public TraitColor[] traitColors = new TraitColor[]
    {
        new TraitColor { trait = RegionTrait.Temperate, color = new Color(0.4f, 0.8f, 0.3f) },
        new TraitColor { trait = RegionTrait.Tropical, color = new Color(0.1f, 0.7f, 0.2f) },
        new TraitColor { trait = RegionTrait.Arid, color = new Color(0.9f, 0.7f, 0.2f) },
        new TraitColor { trait = RegionTrait.Frozen, color = new Color(0.5f, 0.7f, 1.0f) },
        new TraitColor { trait = RegionTrait.Industrial, color = new Color(0.6f, 0.4f, 0.2f) },
        new TraitColor { trait = RegionTrait.Coastal, color = new Color(0.2f, 0.5f, 0.9f) },
    };

    [Tooltip("speed of border color rotation in revolutions per second")]
    public float rotationSpeed = 0.5f;

    [Tooltip("glow color for selected card pulse")]
    public Color selectedGlow = new Color(1f, 0.9f, 0.4f, 1f);

    [Tooltip("border color for cards with no beneficial traits")]
    public Color fallbackColor = Color.black;

    [Tooltip("border thickness in pixels")]
    public float borderThickness = 4f;

    [Header("Rarity Colors")]
    [Tooltip("text color for common rarity labels")]
    public Color commonColor = new Color(0.6f, 0.6f, 0.6f);
    [Tooltip("text color for uncommon rarity labels")]
    public Color uncommonColor = new Color(0.2f, 0.8f, 0.2f);
    [Tooltip("text color for rare rarity labels")]
    public Color rareColor = new Color(0.3f, 0.5f, 1f);

    public Color GetRarityColor(PolicyRarity rarity)
    {
        switch (rarity)
        {
            case PolicyRarity.Common: return commonColor;
            case PolicyRarity.Uncommon: return uncommonColor;
            case PolicyRarity.Rare: return rareColor;
            default: return commonColor;
        }
    }

    public Color GetTraitColor(RegionTrait trait)
    {
        foreach (var tc in traitColors)
            if (tc.trait == trait) return tc.color;
        return fallbackColor;
    }
}
