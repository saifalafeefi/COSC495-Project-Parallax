using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// slides event banners across the screen like a breaking news ticker
// fast in from right, slow crawl through center, fast out to left
public class EventBanner : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("seconds to slide in from right to center")]
    [SerializeField] private float slideInDuration = 0.6f;
    [Tooltip("seconds to crawl slowly across the center")]
    [SerializeField] private float lingerDuration = 2.5f;
    [Tooltip("seconds to accelerate out to the left")]
    [SerializeField] private float slideOutDuration = 0.5f;
    [Tooltip("how far (pixels) the banner drifts during the linger phase")]
    [SerializeField] private float lingerDrift = 80f;
    [Tooltip("delay between each banner when stacking multiple")]
    [SerializeField] private float staggerDelay = 0.3f;

    [Header("Banner Appearance")]
    [Tooltip("banner width as fraction of screen width")]
    [SerializeField] private float bannerWidthFraction = 0.55f;
    [Tooltip("banner height in pixels")]
    [SerializeField] private float bannerHeight = 120f;
    [Tooltip("vertical spacing between stacked banners")]
    [SerializeField] private float bannerSpacing = 10f;
    [SerializeField] private Color backgroundColor = new Color(0.95f, 0.95f, 0.95f, 0.95f);
    [SerializeField] private Color borderColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private float borderWidth = 4f;

    [Header("Border Pulse")]
    [Tooltip("secondary color the border pulses toward")]
    [SerializeField] private Color borderPulseColor = new Color(1f, 0.4f, 0.1f, 1f);
    [Tooltip("pulses per second")]
    [SerializeField] private float borderPulseSpeed = 3f;

    [Header("Text")]
    [SerializeField] private float titleFontSize = 22f;
    [SerializeField] private float descriptionFontSize = 16f;
    [SerializeField] private Color titleColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color descriptionColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    // true while any banners are animating
    public bool IsPlaying { get; private set; }

    private Canvas canvas;

    // plays one or more event banners — call via StartCoroutine or yield on the returned coroutine
    public Coroutine ShowEvents(List<(string title, string description)> events)
    {
        return StartCoroutine(ShowEventsRoutine(events));
    }

    IEnumerator ShowEventsRoutine(List<(string title, string description)> events)
    {
        if (events == null || events.Count == 0) yield break;

        IsPlaying = true;
        EnsureCanvas();

        float screenW = Screen.width;
        float screenH = Screen.height;
        float bannerW = screenW * bannerWidthFraction;

        // calculate vertical positions so banners are centered as a group
        float totalHeight = events.Count * bannerHeight + (events.Count - 1) * bannerSpacing;
        float startY = totalHeight / 2f - bannerHeight / 2f;

        var bannerObjects = new List<GameObject>();
        var routines = new List<Coroutine>();

        for (int i = 0; i < events.Count; i++)
        {
            float yOffset = startY - i * (bannerHeight + bannerSpacing);
            var (bannerObj, borderImg) = BuildBannerPanel(events[i].title, events[i].description, bannerW);
            bannerObjects.Add(bannerObj);

            var rect = bannerObj.GetComponent<RectTransform>();
            // start offscreen right
            rect.anchoredPosition = new Vector2(screenW, yOffset);

            // stagger: delay each subsequent banner
            float delay = i * staggerDelay;
            routines.Add(StartCoroutine(AnimateBanner(rect, borderImg, yOffset, screenW, bannerW, delay)));
        }

        // wait for all banners to finish
        foreach (var r in routines)
            yield return r;

        // clean up
        foreach (var obj in bannerObjects)
            Destroy(obj);

        IsPlaying = false;
    }

    IEnumerator AnimateBanner(RectTransform rect, UnityEngine.UI.Image borderImg, float yOffset, float screenW, float bannerW, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // target X when banner is centered on screen
        float centerX = 0f;

        // start offscreen right: half screen + half banner so it's fully off
        float startX = screenW / 2f + bannerW / 2f;
        // end offscreen left
        float endX = -(screenW / 2f + bannerW / 2f);

        // tracks total time for the pulse sine wave
        float totalTime = 0f;

        // phase 1: slide in (ease-out — fast then slow)
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            totalTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideInDuration);
            // ease-out cubic: 1 - (1-t)^3
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float x = Mathf.Lerp(startX, centerX + lingerDrift / 2f, eased);
            rect.anchoredPosition = new Vector2(x, yOffset);
            PulseBorder(borderImg, totalTime);
            yield return null;
        }

        // phase 2: slow crawl through center
        elapsed = 0f;
        float lingerStart = centerX + lingerDrift / 2f;
        float lingerEnd = centerX - lingerDrift / 2f;
        while (elapsed < lingerDuration)
        {
            elapsed += Time.deltaTime;
            totalTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lingerDuration);
            float x = Mathf.Lerp(lingerStart, lingerEnd, t);
            rect.anchoredPosition = new Vector2(x, yOffset);
            PulseBorder(borderImg, totalTime);
            yield return null;
        }

        // phase 3: slide out (ease-in — slow then fast)
        elapsed = 0f;
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            totalTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideOutDuration);
            // ease-in cubic: t^3
            float eased = t * t * t;
            float x = Mathf.Lerp(lingerEnd, endX, eased);
            rect.anchoredPosition = new Vector2(x, yOffset);
            PulseBorder(borderImg, totalTime);
            yield return null;
        }
    }

    void PulseBorder(UnityEngine.UI.Image borderImg, float time)
    {
        // sine wave oscillates 0–1, used to lerp between the two border colors
        float pulse = (Mathf.Sin(time * borderPulseSpeed * Mathf.PI * 2f) + 1f) / 2f;
        borderImg.color = Color.Lerp(borderColor, borderPulseColor, pulse);
    }

    void EnsureCanvas()
    {
        if (canvas != null) return;

        var canvasObj = new GameObject("EventBannerCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25; // above reward (20) so banners show on top
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
    }

    (GameObject, UnityEngine.UI.Image) BuildBannerPanel(string title, string description, float width)
    {
        // outer panel with border color (acts as border via padding)
        var panel = new GameObject("EventBannerPanel");
        panel.transform.SetParent(canvas.transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(width, bannerHeight);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = borderColor;
        panelImg.raycastTarget = false;

        // inner panel (white background inset by border width)
        var inner = new GameObject("Inner");
        inner.transform.SetParent(panel.transform, false);

        var innerRect = inner.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(borderWidth, borderWidth);
        innerRect.offsetMax = new Vector2(-borderWidth, -borderWidth);

        var innerImg = inner.AddComponent<UnityEngine.UI.Image>();
        innerImg.color = backgroundColor;
        innerImg.raycastTarget = false;

        // title text (upper portion)
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(inner.transform, false);

        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.45f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(15f, 0f);
        titleRect.offsetMax = new Vector2(-15f, -8f);

        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = titleFontSize;
        titleTmp.color = titleColor;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.BottomLeft;
        titleTmp.enableWordWrapping = true;
        titleTmp.raycastTarget = false;

        // description text (lower portion)
        var descObj = new GameObject("Description");
        descObj.transform.SetParent(inner.transform, false);

        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0.45f);
        descRect.offsetMin = new Vector2(15f, 8f);
        descRect.offsetMax = new Vector2(-15f, 0f);

        var descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.text = description;
        descTmp.fontSize = descriptionFontSize;
        descTmp.color = descriptionColor;
        descTmp.alignment = TextAlignmentOptions.TopLeft;
        descTmp.enableWordWrapping = true;
        descTmp.raycastTarget = false;

        return (panel, panelImg);
    }
}
