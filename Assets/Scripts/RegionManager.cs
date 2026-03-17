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

    [Tooltip("Color of the selection glow for the currently selected region.")]
    [SerializeField]
    private Color selectionColor = new Color(0.2f, 0.8f, 1f, 0.5f);

    [Header("Trait Assignment Thresholds")]
    [Tooltip("latitude above this is frozen (absolute value)")]
    [SerializeField, Range(30f, 80f)]
    private float frozenLatitude = 60f;

    [Tooltip("latitude below this qualifies for tropical (absolute value)")]
    [SerializeField, Range(10f, 40f)]
    private float tropicalLatitude = 25f;

    [Tooltip("latitude below this qualifies for arid (absolute value)")]
    [SerializeField, Range(15f, 50f)]
    private float aridLatitude = 40f;

    [Tooltip("regions with fewer triangles than this may be tagged coastal")]
    [SerializeField, Range(5, 80)]
    private int coastalMaxTris = 35;

    [Tooltip("how close two region centers must be to count as neighbors (angular distance squared)")]
    [SerializeField, Range(0.01f, 0.5f)]
    private float neighborProximity = 0.15f;

    public List<Region> Regions { get; private set; }
    public Region SelectedRegion { get; set; }
    public Region HoveredRegion { get; set; }

    private Texture2D earthTexture;
    private Mesh earthMesh;

    private Dictionary<int, Region> triangleToRegion;

    [Header("Status Pulse")]
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField] private StatusBorderConfig[] stressedBorders = new StatusBorderConfig[]
    {
        new StatusBorderConfig { width = 0.012f, height = 0.004f, color = new Color(1f, 0.6f, 0.2f, 0.4f) }
    };
    [SerializeField] private StatusBorderConfig[] crisisBorders = new StatusBorderConfig[]
    {
        new StatusBorderConfig { width = 0.010f, height = 0.004f, color = new Color(1f, 0.2f, 0.1f, 0.5f) },
        new StatusBorderConfig { width = 0.020f, height = 0.006f, color = new Color(1f, 0.1f, 0.05f, 0.25f) }
    };

    private GameObject highlightObject;
    private MeshFilter highlightMeshFilter;

    private GameObject selectionObject;
    private MeshFilter selectionMeshFilter;
    private Region currentlyShownSelection;

    // one overlay object per border ring per status level
    private List<PulseLayer> stressedLayers = new List<PulseLayer>();
    private List<PulseLayer> crisisLayers = new List<PulseLayer>();

    private Vector3[] allVertices;
    private int[] allTriangles;

    // cached outline meshes per region, keyed by (width, height) so we don't rebuild every frame
    private Dictionary<(float, float), Dictionary<Region, Mesh>> outlineMeshCache
        = new Dictionary<(float, float), Dictionary<Region, Mesh>>();

    [System.Serializable]
    public struct StatusBorderConfig
    {
        [Tooltip("thickness of the border line")]
        public float width;
        [Tooltip("how far the border floats above the surface")]
        public float height;
        public Color color;
    }

    private class PulseLayer
    {
        public GameObject obj;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public Color baseColor;
    }

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
    }

    void Update()
    {
        UpdateStatusPulse();
    }

    void UpdateStatusPulse()
    {
        if (Regions == null) return;

        // split regions by status
        var stressedRegions = new List<Region>();
        var crisisRegions = new List<Region>();

        foreach (var r in Regions)
        {
            if (r.CarbonLevel > 85f)
                crisisRegions.Add(r);
            else if (r.CarbonLevel > 70f)
                stressedRegions.Add(r);
        }

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float fastPulse = (Mathf.Sin(Time.time * pulseSpeed * 1.8f) + 1f) / 2f;

        UpdatePulseLayers(stressedLayers, stressedBorders, stressedRegions, pulse);
        UpdatePulseLayers(crisisLayers, crisisBorders, crisisRegions, fastPulse);
    }

    void UpdatePulseLayers(List<PulseLayer> layers, StatusBorderConfig[] configs, List<Region> regions, float pulse)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (i >= configs.Length) { layer.obj.SetActive(false); continue; }

            var config = configs[i];

            if (regions.Count > 0)
            {
                // get or build cached outline meshes for this width/height combo
                var key = (config.width, config.height);
                var combined = GetCombinedOutlineMesh(key, regions);

                layer.meshFilter.mesh = combined;
                float alpha = Mathf.Lerp(0.05f, config.color.a, pulse);
                layer.meshRenderer.material.SetColor("_BaseColor",
                    new Color(config.color.r, config.color.g, config.color.b, alpha));
                layer.obj.SetActive(true);
            }
            else
            {
                layer.obj.SetActive(false);
            }
        }
    }

    Mesh GetCombinedOutlineMesh((float, float) key, List<Region> regions)
    {
        // build per-region outline meshes for this size if not cached
        if (!outlineMeshCache.ContainsKey(key))
            outlineMeshCache[key] = new Dictionary<Region, Mesh>();

        var cache = outlineMeshCache[key];
        var meshes = new List<Mesh>();

        foreach (var r in regions)
        {
            if (!cache.TryGetValue(r, out Mesh m))
            {
                m = BuildOutlineMesh(r.TriangleIndices, key.Item1, key.Item2);
                cache[r] = m;
            }
            meshes.Add(m);
        }

        var combine = new CombineInstance[meshes.Count];
        for (int i = 0; i < meshes.Count; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = Matrix4x4.identity;
        }

        var combined = new Mesh();
        combined.CombineMeshes(combine, true, true);
        return combined;
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
        FetchMeshData();

        Regions = new List<Region>();
        triangleToRegion = new Dictionary<int, Region>();

        foreach (var entry in bakedRegionData.regions)
        {
            var tris = new List<int>(entry.triangleIndices);
            var region = new Region(entry.regionName, entry.regionColor, tris);
            region.Trait = entry.trait;
            region.HighlightMesh = BuildHighlightMesh(tris);
            region.FillMesh = BuildFillMesh(tris);
            Regions.Add(region);

            if (region.Trait == RegionTrait.Industrial)
                region.CarbonLevel = 60f;

            foreach (int tri in tris)
                triangleToRegion[tri] = region;
        }

        // rebuild neighbor graph from triangle adjacency
        int triCount = allTriangles.Length / 3;
        var remap = BuildVertexRemap(allVertices);
        var edgeToTris = new Dictionary<(int, int), List<int>>();

        for (int t = 0; t < triCount; t++)
        {
            if (!triangleToRegion.ContainsKey(t)) continue;
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

        BuildNeighborGraph(adjacency, triCount);
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
        FetchMeshData();

        if (earthMesh == null)
        {
            Debug.LogError("[RegionManager] no mesh found on any child MeshFilter");
            return;
        }

        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("[RegionManager] no Renderer found on any child");
            return;
        }

        earthTexture = renderer.sharedMaterial.mainTexture as Texture2D;
        if (earthTexture == null)
        {
            Debug.LogError("[RegionManager] texture is null or not a Texture2D. Make sure isReadable is enabled on the texture.");
            return;
        }

        if (!earthTexture.isReadable)
        {
            Debug.LogError("[RegionManager] texture is not readable. Enable Read/Write in the texture import settings.");
            return;
        }

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
            region.FillMesh = BuildFillMesh(componentTris);
            Regions.Add(region);

            foreach (int tri in componentTris)
                triangleToRegion[tri] = region;
        }

        // assign a trait to each region based on its color and latitude
        foreach (var region in Regions)
            region.Trait = AssignTrait(region);

        // set starting carbon higher for industrial regions
        foreach (var region in Regions)
        {
            if (region.Trait == RegionTrait.Industrial)
                region.CarbonLevel = 60f;
        }

        // figure out which regions are neighbors by checking if they share triangle edges
        BuildNeighborGraph(adjacency, triCount);
    }

    RegionTrait AssignTrait(Region region)
    {
        // get the average latitude of this region
        float latSum = 0f;
        foreach (int t in region.TriangleIndices)
        {
            Vector3 c = TriCenter(t);
            latSum += Mathf.Asin(Mathf.Clamp(c.y, -1f, 1f)) * Mathf.Rad2Deg;
        }
        float avgLat = latSum / region.TriangleIndices.Count;
        float absLat = Mathf.Abs(avgLat);

        // use color hue + latitude to pick a trait
        Color.RGBToHSV(region.RegionColor, out float h, out float s, out float v);

        // frozen: high latitude regions (near poles)
        if (absLat > frozenLatitude)
            return RegionTrait.Frozen;

        // arid: yellow/brown hues in mid-low latitudes
        if (h >= 0.05f && h < 0.18f && absLat < aridLatitude)
            return RegionTrait.Arid;

        // tropical: green hues near equator
        if (h >= 0.18f && h < 0.45f && s > 0.2f && absLat < tropicalLatitude)
            return RegionTrait.Tropical;

        // coastal: smaller regions at mid latitudes tend to be along coastlines
        if (region.TriangleIndices.Count < coastalMaxTris && absLat < 50f)
            return RegionTrait.Coastal;

        // industrial: brown/dark hues at mid latitudes
        if ((h < 0.05f || h > 0.85f || (h >= 0.05f && h < 0.12f && s > 0.2f)) && absLat >= tropicalLatitude && absLat < frozenLatitude)
            return RegionTrait.Industrial;

        // everything else is temperate
        return RegionTrait.Temperate;
    }

    void BuildNeighborGraph(List<int>[] adjacency, int triCount)
    {
        // two regions are neighbors if any of their triangles share an edge with
        // a triangle from a different region (including across ocean/filtered gaps)
        // we check direct triangle adjacency first, then also check if regions are
        // close enough geographically to be considered neighbors

        // first pass: direct adjacency through the triangle graph
        var neighborSet = new Dictionary<Region, HashSet<Region>>();
        foreach (var region in Regions)
            neighborSet[region] = new HashSet<Region>();

        foreach (var region in Regions)
        {
            foreach (int tri in region.TriangleIndices)
            {
                foreach (int adj in adjacency[tri])
                {
                    if (triangleToRegion.TryGetValue(adj, out Region other) && other != region)
                    {
                        neighborSet[region].Add(other);
                        neighborSet[other].Add(region);
                    }
                }
            }
        }

        // second pass: regions within a certain angular distance are also neighbors
        // this catches regions separated by thin ocean strips or filtered triangles
        float proximityThreshold = neighborProximity;
        for (int i = 0; i < Regions.Count; i++)
        {
            Vector3 centerA = RegionCenter(Regions[i]);
            for (int j = i + 1; j < Regions.Count; j++)
            {
                if (neighborSet[Regions[i]].Contains(Regions[j])) continue;

                Vector3 centerB = RegionCenter(Regions[j]);
                if ((centerA - centerB).sqrMagnitude < proximityThreshold)
                {
                    neighborSet[Regions[i]].Add(Regions[j]);
                    neighborSet[Regions[j]].Add(Regions[i]);
                }
            }
        }

        foreach (var region in Regions)
            region.Neighbors = new List<Region>(neighborSet[region]);
    }

    // gets the average direction of a region on the unit sphere (local space)
    Vector3 RegionCenter(Region region)
    {
        Vector3 sum = Vector3.zero;
        foreach (int t in region.TriangleIndices)
            sum += TriCenter(t);
        return (sum / region.TriangleIndices.Count).normalized;
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
        return BuildOutlineMesh(tris, 0.006f, 0.003f);
    }

    Mesh BuildOutlineMesh(List<int> tris, float outlineWidth, float outlineHeight)
    {
        // build an outline mesh from the boundary edges of this region
        var remap = BuildVertexRemap(allVertices);

        // find boundary edges: edges where only one side belongs to this region
        var edgeCount = new Dictionary<(int, int), int>();
        foreach (int t in tris)
        {
            int r0 = remap[allTriangles[t * 3]];
            int r1 = remap[allTriangles[t * 3 + 1]];
            int r2 = remap[allTriangles[t * 3 + 2]];

            IncrementEdge(edgeCount, r0, r1);
            IncrementEdge(edgeCount, r1, r2);
            IncrementEdge(edgeCount, r2, r0);
        }

        // edges that appear only once are boundary edges
        var newVerts = new List<Vector3>();
        var newTris = new List<int>();

        foreach (var (edge, count) in edgeCount)
        {
            if (count > 1) continue;

            // find the actual vertex positions for this edge
            Vector3 a = FindVertexByRemap(remap, edge.Item1);
            Vector3 b = FindVertexByRemap(remap, edge.Item2);

            // push both points outward from sphere center
            Vector3 aOut = a + a.normalized * outlineHeight;
            Vector3 bOut = b + b.normalized * outlineHeight;

            // widen the edge into a thin quad strip
            Vector3 mid = ((a + b) / 2f).normalized;
            Vector3 edgeDir = (b - a).normalized;
            Vector3 widthDir = Vector3.Cross(edgeDir, mid).normalized * outlineWidth;

            int baseIdx = newVerts.Count;
            newVerts.Add(aOut - widthDir);
            newVerts.Add(aOut + widthDir);
            newVerts.Add(bOut + widthDir);
            newVerts.Add(bOut - widthDir);

            // two triangles for the quad
            newTris.Add(baseIdx);
            newTris.Add(baseIdx + 1);
            newTris.Add(baseIdx + 2);
            newTris.Add(baseIdx);
            newTris.Add(baseIdx + 2);
            newTris.Add(baseIdx + 3);
        }

        var mesh = new Mesh();
        mesh.SetVertices(newVerts);
        mesh.SetTriangles(newTris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    Mesh BuildFillMesh(List<int> tris)
    {
        // solid fill mesh: duplicates each triangle pushed slightly outward from the sphere
        float offset = 0.002f;
        var verts = new List<Vector3>();
        var indices = new List<int>();

        foreach (int t in tris)
        {
            int i0 = allTriangles[t * 3];
            int i1 = allTriangles[t * 3 + 1];
            int i2 = allTriangles[t * 3 + 2];

            Vector3 v0 = allVertices[i0];
            Vector3 v1 = allVertices[i1];
            Vector3 v2 = allVertices[i2];

            // push outward along each vertex normal (radial direction on a sphere)
            int baseIdx = verts.Count;
            verts.Add(v0 + v0.normalized * offset);
            verts.Add(v1 + v1.normalized * offset);
            verts.Add(v2 + v2.normalized * offset);

            indices.Add(baseIdx);
            indices.Add(baseIdx + 1);
            indices.Add(baseIdx + 2);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    void IncrementEdge(Dictionary<(int, int), int> edgeCount, int v0, int v1)
    {
        var key = v0 < v1 ? (v0, v1) : (v1, v0);
        if (edgeCount.ContainsKey(key))
            edgeCount[key]++;
        else
            edgeCount[key] = 1;
    }

    Vector3 FindVertexByRemap(int[] remap, int remappedIdx)
    {
        // the remapped index IS an original vertex index (the first one found at that position)
        return allVertices[remappedIdx];
    }

    void SetupHighlightOverlay()
    {
        // hover highlight (subtle white glow)
        highlightObject = new GameObject("RegionHighlight");
        highlightObject.transform.SetParent(transform, false);

        highlightMeshFilter = highlightObject.AddComponent<MeshFilter>();
        var highlightRenderer = highlightObject.AddComponent<MeshRenderer>();

        var hoverMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        hoverMat.SetColor("_BaseColor", highlightColor);
        hoverMat.SetFloat("_Surface", 1);
        hoverMat.SetFloat("_Blend", 0);
        hoverMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        hoverMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        hoverMat.SetInt("_ZWrite", 0);
        hoverMat.renderQueue = 3000;

        highlightRenderer.material = hoverMat;
        highlightObject.SetActive(false);

        // selection highlight (stronger colored glow, renders behind hover)
        selectionObject = new GameObject("RegionSelection");
        selectionObject.transform.SetParent(transform, false);

        selectionMeshFilter = selectionObject.AddComponent<MeshFilter>();
        var selectionRenderer = selectionObject.AddComponent<MeshRenderer>();

        var selectMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        selectMat.SetColor("_BaseColor", selectionColor);
        selectMat.SetFloat("_Surface", 1);
        selectMat.SetFloat("_Blend", 0);
        selectMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        selectMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        selectMat.SetInt("_ZWrite", 0);
        selectMat.renderQueue = 2999;

        selectionRenderer.material = selectMat;
        selectionObject.SetActive(false);

        // create pulse layers for each border config
        int queue = 2995;
        foreach (var config in stressedBorders)
            stressedLayers.Add(CreatePulseLayer($"StressedBorder_{stressedLayers.Count}", config.color, queue++));
        foreach (var config in crisisBorders)
            crisisLayers.Add(CreatePulseLayer($"CrisisBorder_{crisisLayers.Count}", config.color, queue++));
    }

    PulseLayer CreatePulseLayer(string name, Color color, int renderQueue)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);

        var mf = obj.AddComponent<MeshFilter>();
        var mr = obj.AddComponent<MeshRenderer>();

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = renderQueue;

        mr.material = mat;
        obj.SetActive(false);

        return new PulseLayer { obj = obj, meshFilter = mf, meshRenderer = mr, baseColor = color };
    }

    public void SetHighlight(Region region)
    {
        // hover highlight
        if (highlightObject != null)
        {
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

        // selection highlight stays on the selected region
        UpdateSelectionHighlight();
    }

    void UpdateSelectionHighlight()
    {
        if (selectionObject == null) return;

        if (SelectedRegion != null && SelectedRegion.HighlightMesh != null)
        {
            if (currentlyShownSelection != SelectedRegion)
            {
                selectionMeshFilter.mesh = SelectedRegion.HighlightMesh;
                currentlyShownSelection = SelectedRegion;
            }
            selectionObject.SetActive(true);
        }
        else
        {
            selectionObject.SetActive(false);
            currentlyShownSelection = null;
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
