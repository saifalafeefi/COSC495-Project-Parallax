using System;
using System.Collections.Generic;
using UnityEngine;

public enum RegionTrait
{
    Temperate,
    Tropical,
    Arid,
    Frozen,
    Industrial,
    Coastal
}

[Serializable]
public class Region
{
    public string RegionName;
    public Color RegionColor;
    public RegionTrait Trait;
    public float CarbonLevel = 50f;
    public float EconomyLevel = 50f;
    public float StabilityLevel = 50f;

    public List<int> TriangleIndices;
    public Mesh HighlightMesh;

    // regions that share edges with this one
    public List<Region> Neighbors;

    public Region(string name, Color color, List<int> triangleIndices)
    {
        RegionName = name;
        RegionColor = color;
        TriangleIndices = triangleIndices;
        Neighbors = new List<Region>();
    }
}
