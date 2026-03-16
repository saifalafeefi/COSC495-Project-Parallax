using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RegionManager))]
public class RegionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        var manager = (RegionManager)target;

        if (GUILayout.Button("Bake Regions to Asset", GUILayout.Height(30)))
        {
            BakeRegions(manager);
        }

        EditorGUILayout.HelpBox(
            "Click 'Bake Regions to Asset' while NOT in Play mode.\n" +
            "The prefab must have a child with MeshFilter + Renderer.\n" +
            "The texture must have Read/Write enabled in import settings.\n" +
            "After baking, the asset auto-assigns to 'Baked Region Data'.",
            MessageType.Info);
    }

    void BakeRegions(RegionManager manager)
    {
        // Run discovery in edit mode
        manager.RunDiscovery();

        if (manager.Regions == null || manager.Regions.Count == 0)
        {
            EditorUtility.DisplayDialog("Bake Regions", "No regions found. Check your mesh and texture setup.", "OK");
            return;
        }

        var asset = ScriptableObject.CreateInstance<RegionData>();

        foreach (var region in manager.Regions)
        {
            var entry = new RegionData.RegionEntry
            {
                regionName = region.RegionName,
                regionColor = region.RegionColor,
                trait = region.Trait,
                triangleIndices = region.TriangleIndices.ToArray()
            };
            asset.regions.Add(entry);
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Region Data", "RegionData", "asset", "Choose where to save the baked region data");

        if (string.IsNullOrEmpty(path)) return;

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        // Auto-assign to the manager
        var so = new SerializedObject(manager);
        var prop = so.FindProperty("bakedRegionData");
        if (prop != null)
        {
            prop.objectReferenceValue = asset;
            so.ApplyModifiedProperties();
        }

        EditorUtility.DisplayDialog("Bake Regions",
            $"Saved {asset.regions.Count} regions to {path}", "OK");

        Debug.Log($"[RegionManager] Baked {asset.regions.Count} regions:");
        foreach (var r in asset.regions)
            Debug.Log($"  {r.regionName} | {r.regionColor} | {r.triangleIndices.Length} tris");
    }
}
