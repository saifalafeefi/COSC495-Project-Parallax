using UnityEngine;
using UnityEditor;
using System.IO;

public class PolicyEventGenerator
{
    [MenuItem("Carbon Conquest/Generate All Policies and Events")]
    public static void GenerateAll()
    {
        GeneratePolicies();
        GenerateEvents();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Generator] all policies and events created in Assets/Resources/");
    }

    static void GeneratePolicies()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Policies"))
            AssetDatabase.CreateFolder("Assets/Resources", "Policies");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Policies/Common"))
            AssetDatabase.CreateFolder("Assets/Resources/Policies", "Common");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Policies/Uncommon"))
            AssetDatabase.CreateFolder("Assets/Resources/Policies", "Uncommon");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Policies/Rare"))
            AssetDatabase.CreateFolder("Assets/Resources/Policies", "Rare");

        string common = "Assets/Resources/Policies/Common";
        string uncommon = "Assets/Resources/Policies/Uncommon";
        string rare = "Assets/Resources/Policies/Rare";

        // -- common (cost 1-2) --
        CreatePolicy(common, "Solar Infrastructure", "Build solar farms to reduce emissions.",
            -12f, -6f, 5f, PolicyRarity.Common, cost: 2);
        CreatePolicy(common, "Reforestation", "Plant forests to absorb carbon and boost morale.",
            -8f, -3f, 8f, PolicyRarity.Common, cost: 1);
        CreatePolicy(common, "Industrial Expansion", "Expand factories for economic growth at environmental cost.",
            15f, 20f, -5f, PolicyRarity.Common, cost: 1);
        CreatePolicy(common, "Coal Subsidy", "Cheap energy now, consequences later.",
            18f, 15f, -8f, PolicyRarity.Common, cost: 1);
        CreatePolicy(common, "Carbon Tax", "Tax polluters. Effective but unpopular.",
            -10f, -8f, -2f, PolicyRarity.Common, cost: 2);
        CreatePolicy(common, "Public Transport", "Invest in buses and trains.",
            -6f, -5f, 6f, PolicyRarity.Common, cost: 1);
        CreatePolicy(common, "Wind Farms", "Harness wind energy across open terrain.",
            -9f, -4f, 3f, PolicyRarity.Common, cost: 1);
        CreatePolicy(common, "Waste Management", "Reduce emissions through recycling programs.",
            -5f, -2f, 4f, PolicyRarity.Common, cost: 1);

        // -- uncommon (cost 2-3) --
        CreatePolicy(uncommon, "Nuclear Energy", "Massive carbon reduction but expensive and controversial.",
            -15f, -12f, -4f, PolicyRarity.Uncommon, cost: 3);
        CreatePolicy(uncommon, "Eco Tourism", "Turn natural beauty into profit.",
            -4f, 10f, 4f, PolicyRarity.Uncommon, cost: 2);
        CreatePolicy(uncommon, "Emergency Rations", "Stabilize a region in crisis at any cost.",
            5f, -5f, 15f, PolicyRarity.Uncommon, cost: 2);
        CreatePolicy(uncommon, "Carbon Capture", "Deploy experimental carbon scrubbing tech.",
            -14f, -10f, 2f, PolicyRarity.Uncommon, cost: 3);
        CreatePolicy(uncommon, "Trade Agreement", "Open borders boost trade but increase shipping emissions.",
            3f, 15f, 6f, PolicyRarity.Uncommon, cost: 2);
        CreatePolicy(uncommon, "Desalination Plant", "Fresh water for all, at an energy cost.",
            4f, 8f, 10f, PolicyRarity.Uncommon, cost: 2);

        // -- rare (cost 3-4) --
        CreatePolicy(rare, "Green New Deal", "Revolutionary overhaul. Expensive but transformative.",
            -20f, -15f, 10f, PolicyRarity.Rare, cost: 4);
        CreatePolicy(rare, "Corporate Bailout", "Prop up the economy. The planet pays the price.",
            10f, 25f, -12f, PolicyRarity.Rare, cost: 3);
        CreatePolicy(rare, "Global Reforestation Pact", "Every region plants trees together. 50% spillover.",
            -10f, -8f, 12f, PolicyRarity.Rare, spillover: 0.5f, cost: 4);
        CreatePolicy(rare, "Tech Embargo", "Ban all fossil fuel imports. Brutal but effective.",
            -15f, -20f, -8f, PolicyRarity.Rare, cost: 3);
    }

    static void GenerateEvents()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Events"))
            AssetDatabase.CreateFolder("Assets/Resources", "Events");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Events/Normal"))
            AssetDatabase.CreateFolder("Assets/Resources/Events", "Normal");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Events/Focus"))
            AssetDatabase.CreateFolder("Assets/Resources/Events", "Focus");

        string normal = "Assets/Resources/Events/Normal";
        string focus = "Assets/Resources/Events/Focus";

        // normal events — picked randomly each round
        CreateEvent(normal, "Heat Wave", "Record temperatures sweep the globe.",
            new RegionTrait[] { RegionTrait.Tropical, RegionTrait.Arid, RegionTrait.Temperate },
            8f, 0f, -5f, false, 0);

        CreateEvent(normal, "Ice Shelf Collapse", "Massive ice shelves break apart, accelerating warming.",
            new RegionTrait[] { RegionTrait.Frozen },
            10f, 0f, -8f, false, 0);

        CreateEvent(normal, "Coastal Flooding", "Rising seas devastate coastal cities.",
            new RegionTrait[] { RegionTrait.Coastal },
            0f, -12f, -8f, false, 0);

        CreateEvent(normal, "Water Crisis", "Drought cripples agriculture and sparks unrest.",
            new RegionTrait[] { RegionTrait.Arid },
            0f, -8f, -10f, false, 0);

        CreateEvent(normal, "Green Tech Breakthrough", "Scientists discover a cheaper renewable energy source.",
            new RegionTrait[] { }, -5f, 3f, 0f, true, 0);

        CreateEvent(normal, "Global Recession", "Markets crash worldwide.",
            new RegionTrait[] { }, 0f, -10f, -5f, true, 0);

        CreateEvent(normal, "Activist Movement", "A global climate movement boosts public support for green policies.",
            new RegionTrait[] { }, -3f, 0f, 5f, true, 0);

        CreateEvent(normal, "Oil Discovery", "New oil reserves found. Tempting but dangerous.",
            new RegionTrait[] { RegionTrait.Industrial, RegionTrait.Arid },
            12f, 15f, 0f, false, 1);

        CreateEvent(normal, "Volcanic Eruption", "Volcanic ash disrupts air travel and agriculture.",
            new RegionTrait[] { }, 6f, -8f, 0f, true, 1);

        CreateEvent(normal, "International Summit", "World leaders agree to modest emission targets.",
            new RegionTrait[] { }, -4f, 0f, 3f, true, 0);

        CreateEvent(normal, "Wildfire Season", "Uncontrolled fires rage across forests.",
            new RegionTrait[] { RegionTrait.Tropical, RegionTrait.Temperate },
            10f, -6f, 0f, false, 0);

        CreateEvent(normal, "Coral Reef Die-Off", "Warming oceans kill marine ecosystems.",
            new RegionTrait[] { RegionTrait.Coastal },
            0f, -10f, -5f, false, 0);

        CreateEvent(normal, "Refugee Crisis", "Climate refugees overwhelm neighboring regions.",
            new RegionTrait[] { }, 0f, 0f, -12f, true, 2);

        CreateEvent(normal, "Carbon Sink Discovery", "Scientists find a massive natural carbon sink.",
            new RegionTrait[] { RegionTrait.Tropical },
            -10f, 5f, 0f, false, 1);

        CreateEvent(normal, "Permafrost Thaw", "Melting permafrost releases ancient methane.",
            new RegionTrait[] { RegionTrait.Frozen },
            15f, 0f, -6f, false, 0);

        CreateEvent(normal, "Solar Boom", "Desert solar farms exceed all projections.",
            new RegionTrait[] { RegionTrait.Arid },
            -8f, 6f, 0f, false, 0);

        // focus events — warn then punish for over-targeting a region
        CreateEvent(focus, "Civil Unrest", "Overworked citizens revolt against constant government intervention.",
            new RegionTrait[] { }, 0f, -6f, -12f, false, 0, focusThreshold: 3);

        CreateEvent(focus, "Resource Depletion", "Intensive policy focus has drained local resources.",
            new RegionTrait[] { }, 0f, -15f, -5f, false, 0, focusThreshold: 4);
    }

    static void CreatePolicy(string folder, string pName, string desc,
        float carbon, float economy, float stability, PolicyRarity rarity, float spillover = 0f, int cost = 1)
    {
        string safeName = pName.Replace(" ", "_");
        string path = $"{folder}/{safeName}.asset";

        // skip if already exists
        if (AssetDatabase.LoadAssetAtPath<PolicyData>(path) != null)
        {
            Debug.Log($"[Generator] skipping {pName} — already exists");
            return;
        }

        var policy = ScriptableObject.CreateInstance<PolicyData>();
        policy.policyName = pName;
        policy.description = desc;
        policy.carbonDelta = carbon;
        policy.economyDelta = economy;
        policy.stabilityDelta = stability;
        policy.rarity = rarity;
        policy.spilloverOverride = spillover;
        policy.politicalCapitalCost = cost;

        AssetDatabase.CreateAsset(policy, path);
        Debug.Log($"[Generator] created policy: {pName}");
    }

    static void CreateEvent(string folder, string eName, string desc,
        RegionTrait[] traits, float carbon, float economy, float stability,
        bool targetAll, int randomCount, int focusThreshold = 0)
    {
        string safeName = eName.Replace(" ", "_").Replace("-", "_");
        string path = $"{folder}/{safeName}.asset";

        if (AssetDatabase.LoadAssetAtPath<EventData>(path) != null)
        {
            Debug.Log($"[Generator] skipping {eName} — already exists");
            return;
        }

        var evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = eName;
        evt.description = desc;
        evt.affectedTraits = traits;
        evt.carbonDelta = carbon;
        evt.economyDelta = economy;
        evt.stabilityDelta = stability;
        evt.targetAll = targetAll;
        evt.randomTargetCount = randomCount;
        evt.focusThreshold = focusThreshold;

        AssetDatabase.CreateAsset(evt, path);
        Debug.Log($"[Generator] created event: {eName}");
    }
}
