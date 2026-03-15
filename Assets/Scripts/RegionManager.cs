using System.Collections.Generic;
using UnityEngine;

public class RegionManager : MonoBehaviour
{
    [Tooltip("Baked region data asset. If assigned, algorithms are skipped entirely.")]
    [SerializeField] private RegionData bakedRegionData;

    [Header("Algorithm Settings (ignored if baked data assigned)")]
    [Tooltip("How aggressively to merge similar colors into one region.")]
    [SerializeField, Range(0.001f, 0.3f)]
    private float colorMergeThreshold = 0.05f;

    [Tooltip("Min dot product between face normal and outward direction. Higher = stricter, filters more side faces.")]
    [SerializeField, Range(0f, 0.9f)]
    private float normalThreshold = 0.3f;

    [Tooltip("Color of the highlight glow when hovering a region.")]
    [SerializeField]
    private Color highlightColor = new Color(1f, 1f, 1f, 0.4f);

    public List<Region> Regions { get; private set; }
    public Region SelectedRegion { get; set; }
    public Region HoveredRegion { get; set; }

    private Texture2D earthTexture;
    private Mesh earthMesh;

    private Dictionary<int, Region> triangleToRegion;

    private GameObject highlightObject;
    private MeshFilter highlightMeshFilter;

    private Vector3[] allVertices;
    private int[] allTriangles;

    private static readonly string[] GreenPrefixes = { "Verd", "Sylv", "Moss", "Fern", "Leaf", "Ivy", "Elm", "Thorn" };
    private static readonly string[] GreenSuffixes = { "ania", "grove", "reach", "wood", "vale", "glen", "holm", "mire" };
    private static readonly string[] YellowPrefixes = { "Dune", "Sand", "Arid", "Sun", "Gold", "Amber", "Dry", "Dust" };
    private static readonly string[] YellowSuffixes = { "fall", "holm", "mere", "waste", "ridge", "barren", "vale", "mark" };
    private static readonly string[] WhitePrefixes = { "Frost", "Ice", "Snow", "Pale", "Glace", "Sleet", "Hail", "Rime" };
    private static readonly string[] WhiteSuffixes = { "holm", "peak", "fall", "reach", "land", "crest", "haven", "spire" };
    private static readonly string[] BrownPrefixes = { "Iron", "Stone", "Crag", "Rock", "Ore", "Flint", "Rust", "Clay" };
    private static readonly string[] BrownSuffixes = { "ridge", "mark", "moor", "deep", "hold", "forge", "dale", "keep" };
    private static readonly string[] GenericPrefixes = { "Ark", "Nyx", "Zeph", "Kael", "Vor", "Ash", "Dusk", "Wynn" };
    private static readonly string[] GenericSuffixes = { "ora", "heim", "thar", "ion", "ald", "wyn", "rath", "mir" };

    void Awake()
    {
        SetupMeshCollider();

        if (bakedRegionData != null && bakedRegionData.regions.Count > 0)
            LoadBakedRegions();
        else
            RunDiscovery();

        SetupHighlightOverlay();
        Debug.Log($"[RegionManager] {Regions.Count} regions ({(bakedRegionData != null ? "baked" : "runtime")})");
        foreach (var r in Regions)
        {
            float minLat = 90, maxLat = -90, minLon = 180, maxLon = -180;
            foreach (int t in r.TriangleIndices)
            {
                Vector3 c = TriCenter(t);
                float lat = Mathf.Asin(Mathf.Clamp(c.y, -1f, 1f)) * Mathf.Rad2Deg;
                float lon = Mathf.Atan2(c.x, c.z) * Mathf.Rad2Deg;
                if (lat < minLat) minLat = lat;
                if (lat > maxLat) maxLat = lat;
                if (lon < minLon) minLon = lon;
                if (lon > maxLon) maxLon = lon;
            }
            Debug.Log($"  {r.RegionName} | Tris: {r.TriangleIndices.Count} | Lat: {minLat:F0} to {maxLat:F0} | Lon: {minLon:F0} to {maxLon:F0}");
        }
    }

    void SetupMeshCollider()
    {
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null) Destroy(sphere);

        var existingMC = GetComponent<MeshCollider>();
        if (existingMC != null) Destroy(existingMC);

        var meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            earthMesh = meshFilter.sharedMesh;
            allVertices = earthMesh.vertices;
            allTriangles = earthMesh.triangles;
            var mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = earthMesh;
        }
    }

    void LoadBakedRegions()
    {
        if (allVertices == null) FetchMeshData();

        Regions = new List<Region>();
        triangleToRegion = new Dictionary<int, Region>();

        foreach (var entry in bakedRegionData.regions)
        {
            var tris = new List<int>(entry.triangleIndices);
            var region = new Region(entry.regionName, entry.regionColor, tris);
            region.HighlightMesh = BuildHighlightMesh(tris);
            Regions.Add(region);

            foreach (int tri in tris)
                triangleToRegion[tri] = region;
        }
    }

    void FetchMeshData()
    {
        var meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            earthMesh = meshFilter.sharedMesh;
            allVertices = earthMesh.vertices;
            allTriangles = earthMesh.triangles;
        }
    }

    public void RunDiscovery()
    {
        if (allVertices == null) FetchMeshData();

        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        earthTexture = renderer.sharedMaterial.mainTexture as Texture2D;
        if (earthTexture == null || earthMesh == null) return;

        var uvList = new List<Vector2>();
        earthMesh.GetUVs(0, uvList);

        int triCount = allTriangles.Length / 3;
        float thresholdSq = colorMergeThreshold * colorMergeThreshold;

        var remap = BuildVertexRemap(allVertices);

        // go through each triangle and figure out what color it is, skip ocean and side faces
        var colorClusters = new List<Color>();
        var triColorIdx = new int[triCount];

        for (int t = 0; t < triCount; t++)
        {
            int i0 = allTriangles[t * 3], i1 = allTriangles[t * 3 + 1], i2 = allTriangles[t * 3 + 2];
            Vector2 uvCenter = (uvList[i0] + uvList[i1] + uvList[i2]) / 3f;
            Color texColor = earthTexture.GetPixelBilinear(uvCenter.x, uvCenter.y);

            if (IsOceanColor(texColor))
            {
                triColorIdx[t] = -1;
                continue;
            }

            // skip triangles that face sideways, they are just height detail not actual land
            Vector3 v0 = allVertices[i0], v1 = allVertices[i1], v2 = allVertices[i2];
            Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            Vector3 outward = ((v0 + v1 + v2) / 3f).normalized;
            if (Vector3.Dot(faceNormal, outward) < normalThreshold)
            {
                triColorIdx[t] = -1;
                continue;
            }

            int clusterIdx = -1;
            for (int c = 0; c < colorClusters.Count; c++)
            {
                if (ColorDistanceSq(texColor, colorClusters[c]) < thresholdSq)
                {
                    clusterIdx = c;
                    break;
                }
            }
            if (clusterIdx < 0)
            {
                clusterIdx = colorClusters.Count;
                colorClusters.Add(new Color(texColor.r, texColor.g, texColor.b, 1f));
            }
            triColorIdx[t] = clusterIdx;
        }

        // build a map of which triangles share edges so we know what's connected
        var edgeToTris = new Dictionary<(int, int), List<int>>();

        for (int t = 0; t < triCount; t++)
        {
            if (triColorIdx[t] < 0) continue;

            int r0 = remap[allTriangles[t * 3]];
            int r1 = remap[allTriangles[t * 3 + 1]];
            int r2 = remap[allTriangles[t * 3 + 2]];

            AddEdge(edgeToTris, r0, r1, t);
            AddEdge(edgeToTris, r1, r2, t);
            AddEdge(edgeToTris, r2, r0, t);
        }

        var adjacency = new List<int>[triCount];
        for (int t = 0; t < triCount; t++)
            adjacency[t] = new List<int>();

        foreach (var tris in edgeToTris.Values)
        {
            for (int a = 0; a < tris.Count; a++)
                for (int b = a + 1; b < tris.Count; b++)
                {
                    adjacency[tris[a]].Add(tris[b]);
                    adjacency[tris[b]].Add(tris[a]);
                }
        }

        // walk through connected triangles of the same color to find each landmass
        var components = new List<(int colorIdx, List<int> tris)>();
        var visited = new bool[triCount];

        for (int t = 0; t < triCount; t++)
        {
            if (visited[t] || triColorIdx[t] < 0) continue;

            int colorIdx = triColorIdx[t];
            var componentTris = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(t);
            visited[t] = true;

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                componentTris.Add(cur);

                foreach (int neighbor in adjacency[cur])
                {
                    if (!visited[neighbor] && triColorIdx[neighbor] == colorIdx)
                    {
                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            components.Add((colorIdx, componentTris));
        }

        // turn each landmass into a region with a random name
        Regions = new List<Region>();
        triangleToRegion = new Dictionary<int, Region>();
        var usedNames = new HashSet<string>();

        // sort so the biggest regions show up first
        components.Sort((a, b) => b.tris.Count.CompareTo(a.tris.Count));

        foreach (var (colorIdx, componentTris) in components)
        {
            // ignore tiny scraps that are too small to be a real region
            if (componentTris.Count < 3) continue;

            Color clusterColor = colorClusters[colorIdx];
            string name = GenerateUniqueName(clusterColor, componentTris[0], usedNames);
            usedNames.Add(name);

            var region = new Region(name, clusterColor, componentTris);
            region.HighlightMesh = BuildHighlightMesh(componentTris);
            Regions.Add(region);

            foreach (int tri in componentTris)
                triangleToRegion[tri] = region;
        }
    }

    int[] BuildVertexRemap(Vector3[] vertices)
    {
        var posToIndex = new Dictionary<string, int>();
        var remap = new int[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            string key = $"{Mathf.Round(vertices[i].x * 1000f)},{Mathf.Round(vertices[i].y * 1000f)},{Mathf.Round(vertices[i].z * 1000f)}";

            if (posToIndex.TryGetValue(key, out int existing))
                remap[i] = existing;
            else
            {
                posToIndex[key] = i;
                remap[i] = i;
            }
        }

        return remap;
    }

    Vector3 TriCenter(int triIdx)
    {
        int i0 = allTriangles[triIdx * 3], i1 = allTriangles[triIdx * 3 + 1], i2 = allTriangles[triIdx * 3 + 2];
        return ((allVertices[i0] + allVertices[i1] + allVertices[i2]) / 3f).normalized;
    }

    void AddEdge(Dictionary<(int, int), List<int>> edgeToTris, int v0, int v1, int triIdx)
    {
        var key = v0 < v1 ? (v0, v1) : (v1, v0);

        if (!edgeToTris.TryGetValue(key, out var list))
        {
            list = new List<int>();
            edgeToTris[key] = list;
        }
        list.Add(triIdx);
    }

    Mesh BuildHighlightMesh(List<int> tris)
    {
        var newVerts = new List<Vector3>();
        var newTris = new List<int>();

        foreach (int t in tris)
        {
            int baseIdx = newVerts.Count;
            for (int j = 0; j < 3; j++)
            {
                Vector3 v = allVertices[allTriangles[t * 3 + j]];
                v += v.normalized * 0.002f;
                newVerts.Add(v);
            }
            newTris.Add(baseIdx);
            newTris.Add(baseIdx + 1);
            newTris.Add(baseIdx + 2);
        }

        var mesh = new Mesh();
        mesh.SetVertices(newVerts);
        mesh.SetTriangles(newTris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    void SetupHighlightOverlay()
    {
        highlightObject = new GameObject("RegionHighlight");
        highlightObject.transform.SetParent(transform, false);

        highlightMeshFilter = highlightObject.AddComponent<MeshFilter>();
        var highlightRenderer = highlightObject.AddComponent<MeshRenderer>();

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", highlightColor);
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;

        highlightRenderer.material = mat;
        highlightObject.SetActive(false);
    }

    public void SetHighlight(Region region)
    {
        if (highlightObject == null) return;

        if (region != null && region.HighlightMesh != null)
        {
            highlightMeshFilter.mesh = region.HighlightMesh;
            highlightObject.SetActive(true);
        }
        else
        {
            highlightObject.SetActive(false);
        }
    }

    public Region GetRegionAtHit(int triangleIndex)
    {
        if (triangleToRegion == null) return null;
        triangleToRegion.TryGetValue(triangleIndex, out Region region);
        return region;
    }

    // returns the average center of a region's triangles in world space
    public Vector3 GetRegionWorldCenter(Region region)
    {
        Vector3 sum = Vector3.zero;
        foreach (int t in region.TriangleIndices)
            sum += TriCenter(t);
        Vector3 localDir = (sum / region.TriangleIndices.Count).normalized;
        return transform.TransformPoint(localDir);
    }

    bool IsOceanColor(Color color)
    {
        return color.b > 0.4f && color.b > color.r * 1.3f && color.b > color.g * 1.2f;
    }

    float ColorDistanceSq(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return dr * dr + dg * dg + db * db;
    }

    string GenerateUniqueName(Color color, int seedExtra, HashSet<string> usedNames)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);

        int seed = ((int)(color.r * 255) << 16) | ((int)(color.g * 255) << 8) | (int)(color.b * 255);
        seed ^= (seedExtra * 7919);
        var rng = new System.Random(seed);

        string[] prefixes, suffixes;

        if (s < 0.15f && v > 0.8f)
        {
            prefixes = WhitePrefixes; suffixes = WhiteSuffixes;
        }
        else if (h >= 0.05f && h < 0.18f)
        {
            prefixes = YellowPrefixes; suffixes = YellowSuffixes;
        }
        else if (h >= 0.18f && h < 0.45f && s > 0.2f)
        {
            prefixes = GreenPrefixes; suffixes = GreenSuffixes;
        }
        else if (h >= 0.02f && h < 0.12f && s > 0.2f)
        {
            prefixes = BrownPrefixes; suffixes = BrownSuffixes;
        }
        else
        {
            prefixes = GenericPrefixes; suffixes = GenericSuffixes;
        }

        for (int attempt = 0; attempt < 64; attempt++)
        {
            string name = prefixes[rng.Next(prefixes.Length)] + suffixes[rng.Next(suffixes.Length)];
            if (!usedNames.Contains(name))
                return name;
        }

        string baseName = prefixes[rng.Next(prefixes.Length)] + suffixes[rng.Next(suffixes.Length)];
        int counter = 2;
        while (usedNames.Contains(baseName + " " + counter))
            counter++;
        return baseName + " " + counter;
    }
}
