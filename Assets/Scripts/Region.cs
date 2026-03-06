using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Region
{
    public string RegionName;
    public Color RegionColor;
    public float CarbonLevel = 50f;
    public float EconomyLevel = 50f;
    public float StabilityLevel = 50f;

    public List<int> TriangleIndices;
    public Mesh HighlightMesh;

    public Region(string name, Color color, List<int> triangleIndices)
    {
        RegionName = name;
        RegionColor = color;
        TriangleIndices = triangleIndices;
    }
}
