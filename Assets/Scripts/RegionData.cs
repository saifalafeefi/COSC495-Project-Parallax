using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RegionData", menuName = "Carbon Conquest/Region Data")]
public class RegionData : ScriptableObject
{
    [Serializable]
    public class RegionEntry
    {
        public string regionName;
        public Color regionColor;
        public int[] triangleIndices;
    }

    public List<RegionEntry> regions = new List<RegionEntry>();
}
