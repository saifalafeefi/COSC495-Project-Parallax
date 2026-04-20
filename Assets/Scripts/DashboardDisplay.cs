using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DashboardDisplay : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color panelBackground = new Color(0.08f, 0.08f, 0.12f, 0.92f);
    [SerializeField] private Color listItemNormal = new Color(0.14f, 0.14f, 0.18f, 0.9f);
    [SerializeField] private Color listItemHovered = new Color(0.2f, 0.2f, 0.28f, 0.95f);
    [SerializeField] private Color listItemSelected = new Color(0.25f, 0.22f, 0.12f, 0.95f);
    [SerializeField] private Color healthyColor = new Color(0.3f, 0.8f, 0.4f);
    [SerializeField] private Color stressedColor = new Color(0.9f, 0.75f, 0.2f);
    [SerializeField] private Color crisisColor = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color carbonColor = new Color(0.75f, 0.45f, 0.3f);
    [SerializeField] private Color economyColor = new Color(0.85f, 0.75f, 0.3f);
    [SerializeField] private Color stabilityColor = new Color(0.4f, 0.6f, 0.85f);

    [Header("Layout")]
    [SerializeField] private float listWidth = 260f;
    [SerializeField] private float detailWidth = 380f;
    [SerializeField] private float panelHeight = 550f;
    [SerializeField] private float listItemHeight = 52f;
    [SerializeField] private float panelOffsetX = 0f;
    [SerializeField] private float panelOffsetY = 0f;

    [Header("Detail Panel Spacing")]
    [Tooltip("vertical gap between detail panel elements")]
    [SerializeField] private float detailSpacing = 6f;
    [Tooltip("extra gap before section headers (event history)")]
    [SerializeField] private float detailSectionGap = 12f;
    [Tooltip("top padding inside the detail panel")]
    [SerializeField] private float detailTopPadding = 10f;
    [Tooltip("height of region name row")]
    [SerializeField] private float detailTitleHeight = 28f;
    [Tooltip("height of trait row")]
    [SerializeField] private float detailTraitHeight = 20f;
    [Tooltip("height of stats text row")]
    [SerializeField] private float detailStatsHeight = 18f;
    [Tooltip("height of status row")]
    [SerializeField] private float detailStatusHeight = 20f;
    [Tooltip("height of neighbors row")]
    [SerializeField] private float detailNeighborsHeight = 18f;
    [Tooltip("height of focus info row")]
    [SerializeField] private float detailFocusHeight = 18f;
    [Tooltip("height of event history header row")]
    [SerializeField] private float detailHistHeaderHeight = 18f;

    [Header("Font Sizes")]
    [SerializeField] private float titleFontSize = 22f;
    [SerializeField] private float listNameFontSize = 13f;
    [SerializeField] private float listStatsFontSize = 10f;
    [SerializeField] private float sortButtonFontSize = 10f;
    [SerializeField] private float detailNameFontSize = 20f;
    [SerializeField] private float detailTraitFontSize = 14f;
    [SerializeField] private float detailStatsFontSize = 12f;
    [SerializeField] private float detailStatusFontSize = 14f;
    [SerializeField] private float detailNeighborsFontSize = 11f;
    [SerializeField] private float detailFocusFontSize = 11f;
    [SerializeField] private float eventHistoryHeaderFontSize = 13f;
    [SerializeField] private float eventHistoryFontSize = 11f;
    [SerializeField] private float statBarLabelFontSize = 11f;

    [Header("Scrolling")]
    [Tooltip("force applied per scroll wheel tick (higher = faster scroll)")]
    [SerializeField] private float scrollForce = 800f;
    [Tooltip("how quickly scroll velocity decays (higher = stops faster)")]
    [SerializeField] private float scrollDamping = 8f;

    [Header("Sort Button Colors")]
    [SerializeField] private Color sortButtonNormal = new Color(0.18f, 0.18f, 0.24f, 0.9f);
    [SerializeField] private Color sortButtonActive = new Color(0.35f, 0.3f, 0.15f, 0.95f);

    [Header("Tween")]
    [SerializeField] private float fadeDuration = 0.3f;

    private GameManager gameManager;
    private RegionManager regionManager;
    private DesktopInteraction desktopInteraction;
    private ARPlacement arPlacement;

    private GameObject root;
    private CanvasGroup rootCanvasGroup;
    private bool showing;
    private bool closing;
    private float showTime;
    private float closeTime;

    private int selectedIndex = -1;
    private int hoveredIndex = -1;

    // cached UI references for live updates
    private List<Image> listItemImages = new List<Image>();
    private List<TMP_Text> listNameTexts = new List<TMP_Text>();
    private List<TMP_Text> listStatTexts = new List<TMP_Text>();
    private List<Image> listStatusBars = new List<Image>();

    // detail panel references
    private TMP_Text dashboardTitle;
    private TMP_Text detailTitle;
    private TMP_Text detailTrait;
    private TMP_Text detailStats;
    private TMP_Text detailStatus;
    private TMP_Text detailNeighbors;
    private TMP_Text detailFocus;
    private TMP_Text detailEventHistory;
    private TMP_Text eventHistoryHeader;
    private Image carbonBar;
    private Image economyBar;
    private Image stabilityBar;
    private List<TMP_Text> sortButtonTexts = new List<TMP_Text>();
    private List<TMP_Text> statBarLabelTexts = new List<TMP_Text>();

    // container rects for live layout repositioning
    private RectTransform titleRect, traitRect, statsRect, statusRect, neighborsRect, focusRect;
    private RectTransform histHeaderRect, histPanelRect;
    private RectTransform[] statBarRects = new RectTransform[3]; // carbon, economy, stability rows

    private enum SortMode { Name, Status, Carbon, Economy, Stability, Trait }
    private SortMode currentSort = SortMode.Name;
    private bool sortDescending;

    private List<Region> regionList = new List<Region>();
    private List<Region> masterRegionList = new List<Region>();

    // sort button references for highlighting active one
    private Dictionary<SortMode, Image> sortButtonImages = new Dictionary<SortMode, Image>();

    // scroll rects for sensitivity
    private ScrollRect listScroll;
    private ScrollRect historyScroll;

    // reference to list content so we can rebuild on sort
    private Transform listContentTransform;

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }
        if (regionManager == null)
            regionManager = FindFirstObjectByType<RegionManager>();
        if (desktopInteraction == null && arPlacement == null)
        {
            desktopInteraction = FindFirstObjectByType<DesktopInteraction>();
            if (desktopInteraction == null)
                arPlacement = FindFirstObjectByType<ARPlacement>();
        }

        if (gameManager.DashboardActive && !showing && !closing)
            ShowDashboard();
        else if (!gameManager.DashboardActive && showing && !closing)
            StartClosing();

        if (closing)
        {
            UpdateClosing();
            return;
        }

        if (showing)
        {
            UpdateListItems();
            UpdateDetailPanel();
            UpdateFadeIn();
            UpdateFontSizes();
        }
    }

    void ShowDashboard()
    {
        showing = true;
        showTime = Time.time;
        selectedIndex = -1;
        hoveredIndex = -1;

        // cache region list
        masterRegionList.Clear();
        regionList.Clear();
        if (regionManager != null && regionManager.Regions != null)
        {
            masterRegionList.AddRange(regionManager.Regions);
            regionList.AddRange(regionManager.Regions);
        }
        currentSort = SortMode.Name;
        sortDescending = false;
        sortButtonImages.Clear();
        ApplySort();

        // root canvas
        root = new GameObject("DashboardUI");
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        rootCanvasGroup = root.AddComponent<CanvasGroup>();
        rootCanvasGroup.alpha = 0f;

        // dark backdrop
        var backdrop = CreateStretchChild(root, "Backdrop");
        var bdImg = backdrop.AddComponent<Image>();
        bdImg.color = new Color(0f, 0f, 0f, 0.7f);
        bdImg.raycastTarget = true;

        // main panel container centered on screen
        float totalWidth = listWidth + detailWidth + 12f;
        var panel = CreateUIObj("Panel", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(panelOffsetX, panelOffsetY), new Vector2(totalWidth, panelHeight));

        // title bar
        var titleBar = CreateUIObj("TitleBar", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, 0f), new Vector2(0f, 40f));
        var titleBg = titleBar.AddComponent<Image>();
        titleBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        titleBg.raycastTarget = false;
        dashboardTitle = CreateText(titleBar, "DASHBOARD", titleFontSize, TextAlignmentOptions.Center,
            new Color(0.9f, 0.85f, 0.5f));
        dashboardTitle.fontStyle = FontStyles.Bold;
        StretchFill(dashboardTitle.gameObject);

        // left panel: region list (scrollable)
        var listPanel = CreateUIObj("ListPanel", panel.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -44f), new Vector2(listWidth, -(44f + 4f)));
        var listBg = listPanel.AddComponent<Image>();
        listBg.color = panelBackground;
        listBg.raycastTarget = true;

        // sort buttons row above the list
        float sortBarHeight = 28f;
        var sortBar = CreateUIObj("SortBar", panel.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -44f), new Vector2(listWidth, sortBarHeight));
        var sortBarBg = sortBar.AddComponent<Image>();
        sortBarBg.color = new Color(0.1f, 0.1f, 0.14f, 0.95f);
        sortBarBg.raycastTarget = false;

        BuildSortButtons(sortBar.transform, listWidth, sortBarHeight);

        // scroll view for region list (below sort bar)
        float listTop = 44f + sortBarHeight + 2f;
        var listPanelAdj = listPanel.GetComponent<RectTransform>();
        listPanelAdj.anchoredPosition = new Vector2(0f, -listTop);
        listPanelAdj.sizeDelta = new Vector2(listWidth, -(listTop + 4f));

        var scrollObj = CreateUIObj("Scroll", listPanel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        listScroll = scrollObj.AddComponent<ScrollRect>();
        listScroll.vertical = true;
        listScroll.horizontal = false;
        listScroll.scrollSensitivity = 0f; // disabled — smooth handler takes over
        listScroll.inertia = false;
        listScroll.movementType = ScrollRect.MovementType.Clamped;

        var listSmoother = scrollObj.AddComponent<SmoothScrollHandler>();
        listSmoother.scrollRect = listScroll;
        listSmoother.scrollForce = scrollForce;
        listSmoother.damping = scrollDamping;

        var viewport = CreateUIObj("Viewport", scrollObj.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        viewport.AddComponent<RectMask2D>();
        listScroll.viewport = viewport.GetComponent<RectTransform>();

        var content = CreateUIObj("Content", viewport.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, regionList.Count * (listItemHeight + 4f)));
        listScroll.content = content.GetComponent<RectTransform>();
        listContentTransform = content.transform;

        // build region list items
        listItemImages.Clear();
        listNameTexts.Clear();
        listStatTexts.Clear();
        listStatusBars.Clear();

        for (int i = 0; i < regionList.Count; i++)
        {
            BuildListItem(content.transform, i);
        }

        // right panel: detail view
        var detailPanel = CreateUIObj("DetailPanel", panel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -44f), new Vector2(-(listWidth + 12f), -(44f + 4f)));
        var detailBg = detailPanel.AddComponent<Image>();
        detailBg.color = panelBackground;
        detailBg.raycastTarget = true;

        BuildDetailPanel(detailPanel.transform);

        // close button top-right
        var closeBtn = CreateUIObj("CloseBtn", panel.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(36f, 36f));
        var closeBg = closeBtn.AddComponent<Image>();
        closeBg.color = new Color(0.4f, 0.2f, 0.2f, 0.9f);
        var btn = closeBtn.AddComponent<Button>();
        btn.targetGraphic = closeBg;
        btn.onClick.AddListener(() => {
            if (!TutorialManager.CanPerformAction(TutorialAction.CloseDashboard)) return;
            gameManager.CloseDashboard();
            TutorialManager.NotifyAction(TutorialAction.CloseDashboard);
        });
        var closeText = CreateText(closeBtn, "X", 18, TextAlignmentOptions.Center, Color.white);
        closeText.fontStyle = FontStyles.Bold;
        StretchFill(closeText.gameObject);
    }

    void BuildSortButtons(Transform parent, float totalWidth, float height)
    {
        string[] labels = { "Name", "Status", "C", "E", "S", "Trait" };
        SortMode[] modes = { SortMode.Name, SortMode.Status, SortMode.Carbon, SortMode.Economy, SortMode.Stability, SortMode.Trait };
        float btnWidth = totalWidth / labels.Length;

        for (int i = 0; i < labels.Length; i++)
        {
            int idx = i; // capture for closure
            var btnObj = CreateUIObj($"Sort_{labels[i]}", parent,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(i * btnWidth, 0f), new Vector2(btnWidth - 1f, 0f));

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = i == 0 ? sortButtonActive : sortButtonNormal;
            sortButtonImages[modes[i]] = btnImg;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => OnSortClicked(modes[idx]));

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.28f, 0.28f, 0.34f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.2f);
            btn.colors = colors;

            // label color matches stat for C/E/S
            Color labelColor = Color.white;
            if (modes[i] == SortMode.Carbon) labelColor = carbonColor;
            else if (modes[i] == SortMode.Economy) labelColor = economyColor;
            else if (modes[i] == SortMode.Stability) labelColor = stabilityColor;

            var label = CreateText(btnObj, labels[i], sortButtonFontSize, TextAlignmentOptions.Center, labelColor);
            label.fontStyle = FontStyles.Bold;
            StretchFill(label.gameObject);
            sortButtonTexts.Add(label);
        }
    }

    void OnSortClicked(SortMode mode)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.dashboardSort);

        if (currentSort == mode)
            sortDescending = !sortDescending;
        else
        {
            currentSort = mode;
            // default descending for stats (high carbon = bad, want it on top), ascending for name/trait
            sortDescending = mode != SortMode.Name && mode != SortMode.Trait;
        }

        // track selected region so we can reselect after rebuild
        Region selectedRegion = (selectedIndex >= 0 && selectedIndex < regionList.Count)
            ? regionList[selectedIndex] : null;

        ApplySort();
        RebuildList();

        // restore selection
        if (selectedRegion != null)
            selectedIndex = regionList.IndexOf(selectedRegion);
        else
            selectedIndex = -1;

        // update button highlights
        foreach (var kvp in sortButtonImages)
        {
            if (kvp.Value != null)
                kvp.Value.color = kvp.Key == currentSort ? sortButtonActive : sortButtonNormal;
        }
    }

    void ApplySort()
    {
        regionList.Clear();
        regionList.AddRange(masterRegionList);

        switch (currentSort)
        {
            case SortMode.Name:
                regionList.Sort((a, b) => string.Compare(a.RegionName, b.RegionName));
                break;
            case SortMode.Status:
                // crisis first, then stressed, then healthy
                regionList.Sort((a, b) =>
                {
                    int sa = gameManager.IsCrisis(a) ? 2 : gameManager.IsStressed(a) ? 1 : 0;
                    int sb = gameManager.IsCrisis(b) ? 2 : gameManager.IsStressed(b) ? 1 : 0;
                    return sb.CompareTo(sa);
                });
                break;
            case SortMode.Carbon:
                regionList.Sort((a, b) => b.CarbonLevel.CompareTo(a.CarbonLevel));
                break;
            case SortMode.Economy:
                regionList.Sort((a, b) => a.EconomyLevel.CompareTo(b.EconomyLevel));
                break;
            case SortMode.Stability:
                regionList.Sort((a, b) => a.StabilityLevel.CompareTo(b.StabilityLevel));
                break;
            case SortMode.Trait:
                regionList.Sort((a, b) => string.Compare(a.Trait.ToString(), b.Trait.ToString()));
                break;
        }

        if (sortDescending && (currentSort == SortMode.Name || currentSort == SortMode.Trait))
            regionList.Reverse();
        else if (!sortDescending && (currentSort != SortMode.Name && currentSort != SortMode.Trait))
            regionList.Reverse();
    }

    void RebuildList()
    {
        if (listContentTransform == null) return;

        // destroy old items
        for (int i = listContentTransform.childCount - 1; i >= 0; i--)
            Destroy(listContentTransform.GetChild(i).gameObject);

        listItemImages.Clear();
        listNameTexts.Clear();
        listStatTexts.Clear();
        listStatusBars.Clear();

        // resize content
        var contentRect = listContentTransform.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, regionList.Count * (listItemHeight + 4f));

        for (int i = 0; i < regionList.Count; i++)
            BuildListItem(listContentTransform, i);
    }

    void BuildListItem(Transform parent, int index)
    {
        var region = regionList[index];
        float yPos = -index * (listItemHeight + 4f);

        var item = CreateUIObj($"Item_{index}", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, yPos), new Vector2(-8f, listItemHeight));

        var itemImg = item.AddComponent<Image>();
        itemImg.color = listItemNormal;
        listItemImages.Add(itemImg);

        // pointer events
        var handler = item.AddComponent<DashboardListHandler>();
        handler.index = index;
        handler.dashboard = this;

        // status bar on left edge
        var statusBar = CreateUIObj("StatusBar", item.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0f), new Vector2(4f, 0f));
        var statusImg = statusBar.AddComponent<Image>();
        statusImg.color = healthyColor;
        statusImg.raycastTarget = false;
        listStatusBars.Add(statusImg);

        // region name
        var nameObj = CreateUIObj("Name", item.transform,
            new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
            new Vector2(10f, 0f), new Vector2(-14f, 0f));
        var nameText = CreateText(nameObj, region.RegionName, listNameFontSize, TextAlignmentOptions.Left, Color.white);
        nameText.fontStyle = FontStyles.Bold;
        StretchFill(nameText.gameObject);
        listNameTexts.Add(nameText);

        // trait + mini stats
        var statsObj = CreateUIObj("Stats", item.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(10f, 0f), new Vector2(-14f, 0f));
        var statsText = CreateText(statsObj, "", listStatsFontSize, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
        statsText.richText = true;
        StretchFill(statsText.gameObject);
        listStatTexts.Add(statsText);
    }

    void BuildDetailPanel(Transform parent)
    {
        float y = -detailTopPadding;

        // region name
        var titleObj = CreateUIObj("Title", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, detailTitleHeight));
        titleRect = titleObj.GetComponent<RectTransform>();
        detailTitle = CreateText(titleObj, "Select a region", detailNameFontSize, TextAlignmentOptions.Center, Color.white);
        detailTitle.fontStyle = FontStyles.Bold;
        StretchFill(detailTitle.gameObject);
        y -= detailTitleHeight + detailSpacing;

        // trait
        var traitObj = CreateUIObj("Trait", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, detailTraitHeight));
        traitRect = traitObj.GetComponent<RectTransform>();
        detailTrait = CreateText(traitObj, "", detailTraitFontSize, TextAlignmentOptions.Center, new Color(0.7f, 0.8f, 0.7f));
        detailTrait.fontStyle = FontStyles.Italic;
        StretchFill(detailTrait.gameObject);
        y -= detailTraitHeight + detailSectionGap;

        // stat bars
        y = BuildStatBar(parent, "C", carbonColor, y, out carbonBar, out statBarRects[0]);
        y = BuildStatBar(parent, "E", economyColor, y, out economyBar, out statBarRects[1]);
        y = BuildStatBar(parent, "S", stabilityColor, y, out stabilityBar, out statBarRects[2]);
        y -= detailSpacing;

        // stats text (exact numbers)
        var statsObj = CreateUIObj("StatsText", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, detailStatsHeight));
        statsRect = statsObj.GetComponent<RectTransform>();
        detailStats = CreateText(statsObj, "", detailStatsFontSize, TextAlignmentOptions.Center, Color.white);
        detailStats.richText = true;
        StretchFill(detailStats.gameObject);
        y -= detailStatsHeight + detailSpacing;

        // status
        var statusObj = CreateUIObj("Status", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, detailStatusHeight));
        statusRect = statusObj.GetComponent<RectTransform>();
        detailStatus = CreateText(statusObj, "", detailStatusFontSize, TextAlignmentOptions.Center, healthyColor);
        detailStatus.fontStyle = FontStyles.Bold;
        StretchFill(detailStatus.gameObject);
        y -= detailStatusHeight + detailSpacing;

        // neighbors
        var neighborsObj = CreateUIObj("Neighbors", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, detailNeighborsHeight));
        neighborsRect = neighborsObj.GetComponent<RectTransform>();
        detailNeighbors = CreateText(neighborsObj, "", detailNeighborsFontSize, TextAlignmentOptions.Center, new Color(0.65f, 0.65f, 0.7f));
        StretchFill(detailNeighbors.gameObject);
        y -= detailNeighborsHeight + detailSpacing;

        // focus info
        var focusObj = CreateUIObj("Focus", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, detailFocusHeight));
        focusRect = focusObj.GetComponent<RectTransform>();
        detailFocus = CreateText(focusObj, "", detailFocusFontSize, TextAlignmentOptions.Center, new Color(0.9f, 0.6f, 0.2f));
        StretchFill(detailFocus.gameObject);
        y -= detailFocusHeight + detailSectionGap;

        // event history header
        var histHeader = CreateUIObj("HistHeader", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, detailHistHeaderHeight));
        histHeaderRect = histHeader.GetComponent<RectTransform>();
        eventHistoryHeader = CreateText(histHeader, "Event History", eventHistoryHeaderFontSize, TextAlignmentOptions.Left,
            new Color(0.8f, 0.8f, 0.5f));
        eventHistoryHeader.fontStyle = FontStyles.Bold;
        StretchFill(eventHistoryHeader.gameObject);
        y -= detailHistHeaderHeight + detailSpacing;

        // event history scrollable area
        float remainingHeight = panelHeight - 44f - Mathf.Abs(y) - 8f;
        var histPanelObj = CreateUIObj("HistPanel", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, remainingHeight));
        histPanelRect = histPanelObj.GetComponent<RectTransform>();

        historyScroll = histPanelObj.AddComponent<ScrollRect>();
        historyScroll.vertical = true;
        historyScroll.horizontal = false;
        historyScroll.scrollSensitivity = 0f;
        historyScroll.inertia = false;
        historyScroll.movementType = ScrollRect.MovementType.Clamped;

        var histSmoother = histPanelObj.AddComponent<SmoothScrollHandler>();
        histSmoother.scrollRect = historyScroll;
        histSmoother.scrollForce = scrollForce;
        histSmoother.damping = scrollDamping;

        var histViewport = CreateUIObj("Viewport", histPanelObj.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        histViewport.AddComponent<RectMask2D>();
        historyScroll.viewport = histViewport.GetComponent<RectTransform>();

        var histContent = CreateUIObj("Content", histViewport.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 0f));
        historyScroll.content = histContent.GetComponent<RectTransform>();

        detailEventHistory = CreateText(histContent, "No events yet.", eventHistoryFontSize,
            TextAlignmentOptions.TopLeft, new Color(0.65f, 0.65f, 0.7f));
        detailEventHistory.enableWordWrapping = true;
        detailEventHistory.richText = true;
        StretchFill(detailEventHistory.gameObject);
    }

    float BuildStatBar(Transform parent, string label, Color color, float y, out Image bar, out RectTransform rowRect)
    {
        float barHeight = 16f;
        float labelWidth = 70f;

        var row = CreateUIObj($"{label}Row", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(-16f, barHeight));

        // label
        var labelObj = CreateUIObj("Label", row.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0f), new Vector2(labelWidth, 0f));
        var labelText = CreateText(labelObj, label, statBarLabelFontSize, TextAlignmentOptions.Left, color);
        StretchFill(labelText.gameObject);
        statBarLabelTexts.Add(labelText);

        // bar background
        var barBg = CreateUIObj("BarBg", row.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
            new Vector2(labelWidth, 0f), new Vector2(-(labelWidth + 4f), -4f));
        var bgImg = barBg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        bgImg.raycastTarget = false;

        // bar fill
        var barFill = CreateUIObj("BarFill", barBg.transform,
            new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero);
        bar = barFill.AddComponent<Image>();
        bar.color = color;
        bar.raycastTarget = false;

        rowRect = row.GetComponent<RectTransform>();
        return y - barHeight - 4f;
    }

    void UpdateListItems()
    {
        for (int i = 0; i < regionList.Count; i++)
        {
            var region = regionList[i];

            // update background color based on hover/selection
            if (i < listItemImages.Count && listItemImages[i] != null)
            {
                Color targetColor;
                if (i == selectedIndex)
                    targetColor = listItemSelected;
                else if (i == hoveredIndex)
                    targetColor = listItemHovered;
                else
                    targetColor = listItemNormal;

                listItemImages[i].color = Color.Lerp(listItemImages[i].color, targetColor, Time.deltaTime * 10f);
            }

            // update status bar color
            if (i < listStatusBars.Count && listStatusBars[i] != null)
            {
                if (gameManager.IsCrisis(region))
                    listStatusBars[i].color = crisisColor;
                else if (gameManager.IsStressed(region))
                    listStatusBars[i].color = stressedColor;
                else
                    listStatusBars[i].color = healthyColor;
            }

            // update stats text
            if (i < listStatTexts.Count && listStatTexts[i] != null)
            {
                string cHex = ColorUtility.ToHtmlStringRGB(carbonColor);
                string eHex = ColorUtility.ToHtmlStringRGB(economyColor);
                string sHex = ColorUtility.ToHtmlStringRGB(stabilityColor);
                listStatTexts[i].text = $"{region.Trait}  " +
                    $"<color=#{cHex}>C:{region.CarbonLevel:F0}</color> " +
                    $"<color=#{eHex}>E:{region.EconomyLevel:F0}</color> " +
                    $"<color=#{sHex}>S:{region.StabilityLevel:F0}</color>";
            }
        }
    }

    void UpdateDetailPanel()
    {
        if (selectedIndex < 0 || selectedIndex >= regionList.Count)
        {
            if (detailTitle != null) detailTitle.text = "Select a region";
            if (detailTrait != null) detailTrait.text = "";
            if (detailStats != null) detailStats.text = "";
            if (detailStatus != null) detailStatus.text = "";
            if (detailNeighbors != null) detailNeighbors.text = "";
            if (detailFocus != null) detailFocus.text = "";
            if (detailEventHistory != null) detailEventHistory.text = "No events yet.";
            if (carbonBar != null) SetBarFill(carbonBar, 0f);
            if (economyBar != null) SetBarFill(economyBar, 0f);
            if (stabilityBar != null) SetBarFill(stabilityBar, 0f);
            return;
        }

        var region = regionList[selectedIndex];

        // title
        detailTitle.text = region.RegionName;

        // trait
        detailTrait.text = region.Trait.ToString();

        // stat bars (lerp for smooth updates)
        SetBarFill(carbonBar, region.CarbonLevel / 100f);
        SetBarFill(economyBar, region.EconomyLevel / 100f);
        SetBarFill(stabilityBar, region.StabilityLevel / 100f);

        // exact stats
        string cHex = ColorUtility.ToHtmlStringRGB(carbonColor);
        string eHex = ColorUtility.ToHtmlStringRGB(economyColor);
        string sHex = ColorUtility.ToHtmlStringRGB(stabilityColor);
        detailStats.text = $"<color=#{cHex}>Carbon: {region.CarbonLevel:F1}</color>  " +
            $"<color=#{eHex}>Economy: {region.EconomyLevel:F1}</color>  " +
            $"<color=#{sHex}>Stability: {region.StabilityLevel:F1}</color>";

        // status
        if (gameManager.IsCrisis(region))
        {
            detailStatus.text = "CRISIS";
            detailStatus.color = crisisColor;
        }
        else if (gameManager.IsStressed(region))
        {
            detailStatus.text = "STRESSED";
            detailStatus.color = stressedColor;
        }
        else
        {
            detailStatus.text = "Healthy";
            detailStatus.color = healthyColor;
        }

        // neighbors
        if (region.Neighbors != null && region.Neighbors.Count > 0)
        {
            var names = new List<string>();
            foreach (var n in region.Neighbors)
                names.Add(n.RegionName);
            detailNeighbors.text = "Neighbors: " + string.Join(", ", names);
        }
        else
        {
            detailNeighbors.text = "No neighbors";
        }

        // focus
        float focusPct = gameManager.GetFocusPercent(region);
        int plays = gameManager.GetPlayCount(region);
        if (focusPct > 0f || plays > 0)
        {
            string focusStr = $"Plays: {plays}";
            if (focusPct > 0f)
                focusStr += $"  |  Focus: {focusPct:F0}%";
            if (gameManager.IsRegionFocusWarned(region))
                focusStr += "  [WARNED]";
            detailFocus.text = focusStr;
        }
        else
        {
            detailFocus.text = "";
        }

        // event history
        if (gameManager.RegionEventHistory.ContainsKey(region))
        {
            var events = gameManager.RegionEventHistory[region];
            if (events.Count == 0)
            {
                detailEventHistory.text = "No events hit this region.";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                // show most recent first
                for (int i = events.Count - 1; i >= 0; i--)
                {
                    var e = events[i];
                    string cSign = e.carbonDelta >= 0 ? "+" : "";
                    string eSign = e.economyDelta >= 0 ? "+" : "";
                    string sSign = e.stabilityDelta >= 0 ? "+" : "";

                    sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.8f, 0.8f, 0.5f))}>R{e.round}</color> ");
                    sb.Append($"{e.eventName}  ");
                    sb.Append($"<color=#{cHex}>{cSign}{e.carbonDelta:F0}</color> ");
                    sb.Append($"<color=#{eHex}>{eSign}{e.economyDelta:F0}</color> ");
                    sb.Append($"<color=#{sHex}>{sSign}{e.stabilityDelta:F0}</color>");
                    if (i > 0) sb.Append("\n");
                }
                detailEventHistory.text = sb.ToString();

                // resize content to fit
                float lineHeight = 16f;
                var contentRect = detailEventHistory.transform.parent.GetComponent<RectTransform>();
                if (contentRect != null)
                    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, events.Count * lineHeight + 8f);
            }
        }
        else
        {
            detailEventHistory.text = "No events hit this region.";
        }
    }

    void SetBarFill(Image bar, float fill)
    {
        if (bar == null) return;
        var rect = bar.GetComponent<RectTransform>();
        rect.anchorMax = new Vector2(Mathf.Lerp(rect.anchorMax.x, fill, Time.deltaTime * 8f), 1f);
    }

    void UpdateFadeIn()
    {
        if (rootCanvasGroup == null) return;
        float t = Mathf.Clamp01((Time.time - showTime) / fadeDuration);
        rootCanvasGroup.alpha = t;
    }

    // pushes inspector font size values to all text components every frame
    // so you can tweak sizes in play mode and see changes instantly
    void UpdateFontSizes()
    {
        if (dashboardTitle != null) dashboardTitle.fontSize = titleFontSize;
        if (detailTitle != null) detailTitle.fontSize = detailNameFontSize;
        if (detailTrait != null) detailTrait.fontSize = detailTraitFontSize;
        if (detailStats != null) detailStats.fontSize = detailStatsFontSize;
        if (detailStatus != null) detailStatus.fontSize = detailStatusFontSize;
        if (detailNeighbors != null) detailNeighbors.fontSize = detailNeighborsFontSize;
        if (detailFocus != null) detailFocus.fontSize = detailFocusFontSize;
        if (detailEventHistory != null) detailEventHistory.fontSize = eventHistoryFontSize;
        if (eventHistoryHeader != null) eventHistoryHeader.fontSize = eventHistoryHeaderFontSize;

        foreach (var t in listNameTexts)
            if (t != null) t.fontSize = listNameFontSize;
        foreach (var t in listStatTexts)
            if (t != null) t.fontSize = listStatsFontSize;
        foreach (var t in sortButtonTexts)
            if (t != null) t.fontSize = sortButtonFontSize;
        foreach (var t in statBarLabelTexts)
            if (t != null) t.fontSize = statBarLabelFontSize;

        // live-reposition detail panel elements based on Inspector spacing values
        UpdateDetailLayout();
    }

    void UpdateDetailLayout()
    {
        float y = -detailTopPadding;

        if (titleRect != null) { SetRow(titleRect, y, detailTitleHeight); y -= detailTitleHeight + detailSpacing; }
        if (traitRect != null) { SetRow(traitRect, y, detailTraitHeight); y -= detailTraitHeight + detailSectionGap; }

        float barHeight = 16f;
        float barGap = 4f;
        for (int i = 0; i < 3; i++)
        {
            if (statBarRects[i] != null) { SetRow(statBarRects[i], y, barHeight); y -= barHeight + barGap; }
        }
        y -= detailSpacing - barGap; // adjust since bars use their own gap

        if (statsRect != null) { SetRow(statsRect, y, detailStatsHeight); y -= detailStatsHeight + detailSpacing; }
        if (statusRect != null) { SetRow(statusRect, y, detailStatusHeight); y -= detailStatusHeight + detailSpacing; }
        if (neighborsRect != null) { SetRow(neighborsRect, y, detailNeighborsHeight); y -= detailNeighborsHeight + detailSpacing; }
        if (focusRect != null) { SetRow(focusRect, y, detailFocusHeight); y -= detailFocusHeight + detailSectionGap; }
        if (histHeaderRect != null) { SetRow(histHeaderRect, y, detailHistHeaderHeight); y -= detailHistHeaderHeight + detailSpacing; }

        if (histPanelRect != null)
        {
            float remaining = panelHeight - 44f - Mathf.Abs(y) - 8f;
            histPanelRect.anchoredPosition = new Vector2(0f, y);
            histPanelRect.sizeDelta = new Vector2(-16f, remaining);
        }
    }

    void SetRow(RectTransform rect, float y, float height)
    {
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    void StartClosing()
    {
        closing = true;
        closeTime = Time.time;
        if (root != null && rootCanvasGroup == null)
            rootCanvasGroup = root.AddComponent<CanvasGroup>();
    }

    void UpdateClosing()
    {
        float t = Mathf.Clamp01((Time.time - closeTime) / fadeDuration);
        if (rootCanvasGroup != null)
            rootCanvasGroup.alpha = 1f - t;

        if (t >= 1f)
            FinishClosing();
    }

    void FinishClosing()
    {
        closing = false;
        showing = false;
        selectedIndex = -1;
        hoveredIndex = -1;
        rootCanvasGroup = null;

        listItemImages.Clear();
        listNameTexts.Clear();
        listStatTexts.Clear();
        listStatusBars.Clear();
        sortButtonTexts.Clear();
        statBarLabelTexts.Clear();

        dashboardTitle = null;
        detailTitle = null;
        detailTrait = null;
        detailStats = null;
        detailStatus = null;
        detailNeighbors = null;
        detailFocus = null;
        detailEventHistory = null;
        eventHistoryHeader = null;
        carbonBar = null;
        economyBar = null;
        stabilityBar = null;
        listScroll = null;
        historyScroll = null;
        listContentTransform = null;
        sortButtonImages.Clear();

        if (root != null)
        {
            Destroy(root);
            root = null;
        }
    }

    // called by list item handler
    public void OnItemHover(int index) { hoveredIndex = index; }
    public void OnItemUnhover(int index) { if (hoveredIndex == index) hoveredIndex = -1; }

    public void OnItemClick(int index)
    {
        if (index < 0 || index >= regionList.Count) return;
        selectedIndex = index;

        // focus on the selected region
        var region = regionList[index];
        if (regionManager != null)
        {
            if (desktopInteraction != null)
                desktopInteraction.FocusOnRegion(region, regionManager);
            else if (arPlacement != null)
                arPlacement.FocusOnRegion(region, regionManager);
        }
    }

    // ---- UI helpers ----

    GameObject CreateUIObj(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return obj;
    }

    GameObject CreateStretchChild(GameObject parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    TMP_Text CreateText(GameObject parent, string content, float fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent.transform, false);
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    void StretchFill(GameObject obj)
    {
        var rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

// intercepts scroll wheel and applies smooth velocity instead of instant jumps
public class SmoothScrollHandler : MonoBehaviour, IScrollHandler
{
    public ScrollRect scrollRect;
    public float scrollForce = 800f;
    public float damping = 8f;

    [Tooltip("when true, scroll wheel moves content horizontally instead of vertically")]
    public bool horizontal;

    private float velocity;

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect == null) return;

        // skip if a nested scroll handler already consumed this event
        if (eventData.used) return;

        // add velocity from scroll wheel input
        velocity += eventData.scrollDelta.y * scrollForce;

        // block the default scroll rect jump and prevent parent handlers from firing
        eventData.Use();
    }

    void Update()
    {
        if (scrollRect == null) return;

        var content = scrollRect.content;
        if (content == null || scrollRect.viewport == null) return;

        if (horizontal)
        {
            float contentWidth = content.rect.width;
            float viewportWidth = scrollRect.viewport.rect.width;
            float minX = -(contentWidth - viewportWidth);
            if (minX > 0f) minX = 0f;

            if (Mathf.Abs(velocity) > 0.5f)
            {
                var pos = content.anchoredPosition;
                pos.x += velocity * Time.deltaTime;

                if (pos.x > 0f)
                {
                    pos.x = Mathf.Lerp(pos.x, 0f, Time.deltaTime * 20f);
                    velocity *= 0.5f;
                }
                else if (pos.x < minX)
                {
                    pos.x = Mathf.Lerp(pos.x, minX, Time.deltaTime * 20f);
                    velocity *= 0.5f;
                }

                content.anchoredPosition = pos;
                velocity = Mathf.Lerp(velocity, 0f, Time.deltaTime * damping);
            }
            else
            {
                velocity = 0f;
            }

            var clamped = content.anchoredPosition;
            clamped.x = Mathf.Clamp(clamped.x, minX, 0f);
            content.anchoredPosition = clamped;
        }
        else
        {
            float contentHeight = content.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            float maxY = contentHeight - viewportHeight;
            if (maxY < 0f) maxY = 0f;

            if (Mathf.Abs(velocity) > 0.5f)
            {
                var pos = content.anchoredPosition;
                pos.y -= velocity * Time.deltaTime;

                if (pos.y < 0f)
                {
                    pos.y = Mathf.Lerp(pos.y, 0f, Time.deltaTime * 20f);
                    velocity *= 0.5f;
                }
                else if (pos.y > maxY)
                {
                    pos.y = Mathf.Lerp(pos.y, maxY, Time.deltaTime * 20f);
                    velocity *= 0.5f;
                }

                content.anchoredPosition = pos;
                velocity = Mathf.Lerp(velocity, 0f, Time.deltaTime * damping);
            }
            else
            {
                velocity = 0f;
            }

            var clamped = content.anchoredPosition;
            clamped.y = Mathf.Clamp(clamped.y, 0f, maxY);
            content.anchoredPosition = clamped;
        }
    }
}

// pointer event handler for dashboard region list items
public class DashboardListHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public int index;
    [HideInInspector] public DashboardDisplay dashboard;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dashboard != null) dashboard.OnItemHover(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dashboard != null) dashboard.OnItemUnhover(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dashboard != null && eventData.button == PointerEventData.InputButton.Left)
            dashboard.OnItemClick(index);
    }
}
