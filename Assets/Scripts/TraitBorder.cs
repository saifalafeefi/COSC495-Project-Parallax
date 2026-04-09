using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TraitBorder : MonoBehaviour
{
    // many small segments around the perimeter for smooth color motion
    private Image[] segments;
    private int segmentCount;
    private List<Color> traitColors = new List<Color>();
    private float rotationSpeed;

    private bool isHovered;
    private bool isRejectFlashing;
    private float rejectFlashStart;
    private Color? overrideColor;

    private const float rejectFlashDuration = 0.6f;
    private static readonly Color rejectColor = new Color(1f, 0.2f, 0.2f);
    private const float hoverBrighten = 0.2f;

    // segments per side — more = smoother motion
    private const int segsPerSide = 16;

    public void Initialize(List<Color> colors, float speed, Color selectedGlow, float borderThickness)
    {
        traitColors = colors;
        rotationSpeed = speed;

        // root image = black outline (covers full card, peeks out 1px around color segments)
        var rootImg = GetComponent<Image>();
        if (rootImg != null) rootImg.color = Color.black;

        BuildSegments(borderThickness);
    }

    void BuildSegments(float thickness)
    {
        // inset color segments 1px from the card edge so the black root shows as outline
        float inset = 1f;
        float colorThickness = thickness - inset;

        segmentCount = segsPerSide * 4;
        segments = new Image[segmentCount];
        int idx = 0;

        // top: left to right
        for (int i = 0; i < segsPerSide; i++)
        {
            float t0 = (float)i / segsPerSide;
            float t1 = (float)(i + 1) / segsPerSide;
            segments[idx++] = CreateSegment($"T{i}",
                new Vector2(t0, 1), new Vector2(t1, 1),
                new Vector2(inset, -thickness), new Vector2(-inset, -inset));
        }

        // right: top to bottom
        for (int i = 0; i < segsPerSide; i++)
        {
            float t0 = 1f - (float)i / segsPerSide;
            float t1 = 1f - (float)(i + 1) / segsPerSide;
            segments[idx++] = CreateSegment($"R{i}",
                new Vector2(1, t1), new Vector2(1, t0),
                new Vector2(-thickness, inset), new Vector2(-inset, -inset));
        }

        // bottom: right to left
        for (int i = 0; i < segsPerSide; i++)
        {
            float t0 = 1f - (float)i / segsPerSide;
            float t1 = 1f - (float)(i + 1) / segsPerSide;
            segments[idx++] = CreateSegment($"B{i}",
                new Vector2(t1, 0), new Vector2(t0, 0),
                new Vector2(inset, inset), new Vector2(-inset, thickness));
        }

        // left: bottom to top
        for (int i = 0; i < segsPerSide; i++)
        {
            float t0 = (float)i / segsPerSide;
            float t1 = (float)(i + 1) / segsPerSide;
            segments[idx++] = CreateSegment($"L{i}",
                new Vector2(0, t0), new Vector2(0, t1),
                new Vector2(inset, inset), new Vector2(thickness, -inset));
        }
    }

    Image CreateSegment(string name, Vector2 anchorMin, Vector2 anchorMax,
                        Vector2 offsetMin, Vector2 offsetMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var img = obj.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    void Update()
    {
        if (traitColors.Count == 0 || segments == null) return;

        // override color takes priority (sold-out cards etc.)
        if (overrideColor.HasValue)
        {
            for (int i = 0; i < segmentCount; i++)
                if (segments[i] != null) segments[i].color = overrideColor.Value;
            return;
        }

        // reject flash overrides everything temporarily
        if (isRejectFlashing)
        {
            float elapsed = Time.unscaledTime - rejectFlashStart;
            if (elapsed >= rejectFlashDuration)
            {
                isRejectFlashing = false;
            }
            else
            {
                float t = elapsed / rejectFlashDuration;
                Color flashCol = Color.Lerp(rejectColor, traitColors[0], t);
                for (int i = 0; i < segmentCount; i++)
                    if (segments[i] != null) segments[i].color = flashCol;
                return;
            }
        }

        if (traitColors.Count == 1)
        {
            Color finalCol = ApplyEffects(traitColors[0]);
            for (int i = 0; i < segmentCount; i++)
                if (segments[i] != null) segments[i].color = finalCol;
        }
        else
        {
            // each segment samples a position on the perimeter
            // colors are distinct blocks that slide smoothly
            float phase = (Time.unscaledTime * rotationSpeed) % 1f;
            int colorCount = traitColors.Count;

            for (int i = 0; i < segmentCount; i++)
            {
                if (segments[i] == null) continue;
                // perimeter position 0..1 offset by phase
                float pos = ((float)i / segmentCount + phase) % 1f;
                // hard snap to color — floor gives distinct bands
                int colorIdx = Mathf.FloorToInt(pos * colorCount) % colorCount;
                segments[i].color = ApplyEffects(traitColors[colorIdx]);
            }
        }
    }

    Color ApplyEffects(Color baseCol)
    {
        if (isHovered)
        {
            return new Color(
                Mathf.Min(1f, baseCol.r + hoverBrighten),
                Mathf.Min(1f, baseCol.g + hoverBrighten),
                Mathf.Min(1f, baseCol.b + hoverBrighten),
                baseCol.a);
        }
        return baseCol;
    }

    public void SetSelected(bool selected) { }
    public void SetHovered(bool hovered) { isHovered = hovered; }
    public void FlashReject() { isRejectFlashing = true; rejectFlashStart = Time.unscaledTime; }
    public void SetOverrideColor(Color? color) { overrideColor = color; }
}
