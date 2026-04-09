using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CodexDisplay : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    [SerializeField] private Color panelBorderColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    [SerializeField] private Color tabInactiveColor = new Color(0.3f, 0.3f, 0.35f, 1f);
    [SerializeField] private Color rowNormalColor = new Color(0.14f, 0.14f, 0.18f, 0.9f);
    [SerializeField] private Color rowHoverColor = new Color(0.22f, 0.22f, 0.28f, 0.95f);
    [SerializeField] private Color carbonColor = new Color(0.75f, 0.45f, 0.3f);
    [SerializeField] private Color economyColor = new Color(0.85f, 0.75f, 0.3f);
    [SerializeField] private Color stabilityColor = new Color(0.4f, 0.6f, 0.85f);
    [SerializeField] private Color cardBgColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
    [SerializeField] private Color focusBadgeColor = new Color(0.8f, 0.4f, 0.1f);
    [SerializeField] private Color globalBadgeColor = new Color(0.3f, 0.6f, 0.9f);
    [SerializeField] private Color backButtonColor = new Color(0.4f, 0.5f, 0.6f);
    [SerializeField] private Color detailBgColor = new Color(0.1f, 0.1f, 0.14f, 0.95f);
    [SerializeField] private Color traitBonusColor = new Color(0.3f, 0.8f, 0.4f);
    [SerializeField] private Color traitPenaltyColor = new Color(0.9f, 0.3f, 0.3f);

    [Header("Panel Size")]
    [SerializeField] private float panelWidth = 1200f;
    [SerializeField] private float panelHeight = 700f;
    [SerializeField] private float borderWidth = 3f;

    [Header("Tab Bar")]
    [SerializeField] private float tabHeight = 45f;
    [SerializeField] private float tabWidth = 180f;
    [SerializeField] private float tabSpacing = 10f;
    [SerializeField] private float tabFontSize = 22f;

    [Header("Event List")]
    [SerializeField] private float eventRowHeight = 55f;
    [SerializeField] private float eventRowSpacing = 4f;
    [SerializeField] private float eventNameFontSize = 18f;
    [SerializeField] private float eventStatFontSize = 15f;
    [SerializeField] private float eventBadgeFontSize = 13f;

    [Header("Policy Cards")]
    [SerializeField] private float cardWidth = 170f;
    [SerializeField] private float cardHeight = 230f;
    [SerializeField] private float cardSpacing = 10f;
    [SerializeField] private float cardBorderSize = 4f;
    [SerializeField] private float cardNameFontSize = 14f;
    [SerializeField] private float cardStatFontSize = 12f;
    [SerializeField] private float cardCostFontSize = 14f;
    [SerializeField] private float rarityLabelFontSize = 20f;
    [SerializeField] private float rarityLabelHeight = 30f;
    [SerializeField] private float policyRowHeight = 240f;

    [Header("Detail View")]
    [SerializeField] private float detailTitleFontSize = 30f;
    [SerializeField] private float detailBodyFontSize = 18f;
    [SerializeField] private float detailStatFontSize = 20f;
    [SerializeField] private float detailTraitFontSize = 16f;
    [SerializeField] private float detailPadding = 30f;

    [Header("Back Button")]
    [SerializeField] private float backButtonWidth = 140f;
    [SerializeField] private float backButtonHeight = 45f;
    [SerializeField] private float backButtonFontSize = 20f;

    [Header("Title")]
    [SerializeField] private float titleFontSize = 34f;

    [Header("Scroll")]
    [SerializeField] private float scrollForce = 800f;
    [SerializeField] private float scrollDamping = 8f;

    [Header("Scrollbar")]
    [SerializeField] private float scrollbarWidth = 8f;
    [SerializeField] private Color scrollbarTrackColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
    [SerializeField] private Color scrollbarHandleColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 5f;

    private enum CodexState { EventList, PolicyList, EventDetail, PolicyDetail }
    private CodexState state = CodexState.EventList;

    private GameObject root;
    private CanvasGroup rootCanvasGroup;
    private bool showing;
    private bool closing;
    private float fadeTarget;

    // cached data
    private PolicyData[] commonPolicies;
    private PolicyData[] uncommonPolicies;
    private PolicyData[] rarePolicies;
    private EventData[] allEvents;

    // ui references for tab switching
    private GameObject eventsView;
    private GameObject policiesView;
    private GameObject eventDetailView;
    private GameObject policyDetailView;
    private Image eventsTabImg;
    private Image policiesTabImg;
    private TMP_Text eventsTabText;
    private TMP_Text policiesTabText;

    // back button reference
    private TMP_Text backBtnText;

    public bool IsShowing => showing;

    public void Show()
    {
        if (showing) return;
        showing = true;
        closing = false;
        LoadData();
        BuildUI();
        fadeTarget = 1f;
        if (rootCanvasGroup != null) rootCanvasGroup.alpha = 0f;
    }

    public void Hide()
    {
        if (!showing) return;
        // start fade out instead of instant destroy
        closing = true;
        fadeTarget = 0f;
    }

    void Update()
    {
        if (rootCanvasGroup == null) return;

        rootCanvasGroup.alpha = Mathf.MoveTowards(rootCanvasGroup.alpha, fadeTarget, fadeSpeed * Time.deltaTime);

        // destroy after fade out completes
        if (closing && rootCanvasGroup.alpha <= 0.01f)
        {
            showing = false;
            closing = false;
            if (root != null) Destroy(root);
            root = null;
            rootCanvasGroup = null;
        }
    }

    void LoadData()
    {
        commonPolicies = Resources.LoadAll<PolicyData>("Policies/Common");
        uncommonPolicies = Resources.LoadAll<PolicyData>("Policies/Uncommon");
        rarePolicies = Resources.LoadAll<PolicyData>("Policies/Rare");

        var normal = Resources.LoadAll<EventData>("Events/Normal");
        var focus = Resources.LoadAll<EventData>("Events/Focus");
        var combined = new List<EventData>();
        combined.AddRange(normal);
        combined.AddRange(focus);
        combined.Sort((a, b) =>
        {
            // focus events go last
            int fa = a.IsFocusEvent() ? 1 : 0;
            int fb = b.IsFocusEvent() ? 1 : 0;
            if (fa != fb) return fa.CompareTo(fb);
            return string.Compare(a.eventName, b.eventName, System.StringComparison.Ordinal);
        });
        allEvents = combined.ToArray();

        // sort policies alphabetically within each rarity
        System.Array.Sort(commonPolicies, (a, b) => string.Compare(a.policyName, b.policyName, System.StringComparison.Ordinal));
        System.Array.Sort(uncommonPolicies, (a, b) => string.Compare(a.policyName, b.policyName, System.StringComparison.Ordinal));
        System.Array.Sort(rarePolicies, (a, b) => string.Compare(a.policyName, b.policyName, System.StringComparison.Ordinal));
    }

    void BuildUI()
    {
        if (root != null) Destroy(root);

        // canvas
        root = new GameObject("CodexCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        rootCanvasGroup = root.AddComponent<CanvasGroup>();
        rootCanvasGroup.alpha = 0f;

        // dark backdrop
        var backdrop = CreateObj("Backdrop", root.transform);
        StretchFill(backdrop);
        var bdImg = backdrop.AddComponent<Image>();
        bdImg.color = backdropColor;
        bdImg.raycastTarget = true;

        // panel border
        var border = CreateObj("Border", root.transform);
        SetAnchors(border, 0.5f, 0.5f, 0.5f, 0.5f);
        border.GetComponent<RectTransform>().sizeDelta = new Vector2(panelWidth + borderWidth * 2, panelHeight + borderWidth * 2);
        border.AddComponent<Image>().color = panelBorderColor;

        // main panel
        var panel = CreateObj("Panel", root.transform);
        SetAnchors(panel, 0.5f, 0.5f, 0.5f, 0.5f);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(panelWidth, panelHeight);
        panel.AddComponent<Image>().color = panelColor;

        // title
        var titleObj = CreateObj("Title", panel.transform);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(0f, 45f);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "CODEX";
        titleTmp.fontSize = titleFontSize;
        titleTmp.color = panelBorderColor;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontStyle = FontStyles.Bold;

        // tab bar — center the two tabs as a group
        float tabBarY = -60f;
        float totalTabWidth = tabWidth * 2f + tabSpacing;
        float tabLeftX = -totalTabWidth / 2f + tabWidth / 2f;

        // events tab
        var eventsTab = CreateButton(panel.transform, "EventsTab", "EVENTS", tabActiveColor, tabLeftX, tabBarY, tabWidth, tabHeight, tabFontSize, () => SwitchToEvents());
        eventsTabImg = eventsTab.GetComponent<Image>();
        eventsTabText = eventsTab.GetComponentInChildren<TextMeshProUGUI>();

        // policies tab
        var policiesTab = CreateButton(panel.transform, "PoliciesTab", "POLICIES", tabInactiveColor, tabLeftX + tabWidth + tabSpacing, tabBarY, tabWidth, tabHeight, tabFontSize, () => SwitchToPolicies());
        policiesTabImg = policiesTab.GetComponent<Image>();
        policiesTabText = policiesTab.GetComponentInChildren<TextMeshProUGUI>();

        // content area (below tabs, above back button)
        float contentTop = tabBarY - tabHeight - 10f;
        float contentBottom = backButtonHeight + 20f;

        // events view
        eventsView = CreateObj("EventsView", panel.transform);
        var evRect = eventsView.GetComponent<RectTransform>();
        evRect.anchorMin = new Vector2(0f, 0f);
        evRect.anchorMax = new Vector2(1f, 1f);
        evRect.offsetMin = new Vector2(20f, contentBottom);
        evRect.offsetMax = new Vector2(-20f, contentTop);
        BuildEventsList(eventsView.transform);

        // policies view
        policiesView = CreateObj("PoliciesView", panel.transform);
        var polRect = policiesView.GetComponent<RectTransform>();
        polRect.anchorMin = new Vector2(0f, 0f);
        polRect.anchorMax = new Vector2(1f, 1f);
        polRect.offsetMin = new Vector2(20f, contentBottom);
        polRect.offsetMax = new Vector2(-20f, contentTop);
        BuildPoliciesView(policiesView.transform);
        policiesView.SetActive(false);

        // event detail view
        eventDetailView = CreateObj("EventDetail", panel.transform);
        var edRect = eventDetailView.GetComponent<RectTransform>();
        edRect.anchorMin = new Vector2(0f, 0f);
        edRect.anchorMax = new Vector2(1f, 1f);
        edRect.offsetMin = new Vector2(20f, contentBottom);
        edRect.offsetMax = new Vector2(-20f, contentTop);
        eventDetailView.SetActive(false);

        // policy detail view
        policyDetailView = CreateObj("PolicyDetail", panel.transform);
        var pdRect = policyDetailView.GetComponent<RectTransform>();
        pdRect.anchorMin = new Vector2(0f, 0f);
        pdRect.anchorMax = new Vector2(1f, 1f);
        pdRect.offsetMin = new Vector2(20f, contentBottom);
        pdRect.offsetMax = new Vector2(-20f, contentTop);
        policyDetailView.SetActive(false);

        // back button
        var backBtn = CreateButton(panel.transform, "BackBtn", "BACK", backButtonColor, 0f, 0f, backButtonWidth, backButtonHeight, backButtonFontSize, () => OnBack());
        var backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0f, 10f);
        backBtnText = backBtn.GetComponentInChildren<TextMeshProUGUI>();

        state = CodexState.EventList;
        UpdateBackButton();
    }

    // -- event list --

    void BuildEventsList(Transform parent)
    {
        // scroll rect
        var scrollObj = CreateObj("Scroll", parent);
        StretchFill(scrollObj);
        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 0f;
        scroll.inertia = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var smoother = scrollObj.AddComponent<SmoothScrollHandler>();
        smoother.scrollRect = scroll;
        smoother.scrollForce = scrollForce;
        smoother.damping = scrollDamping;

        // viewport
        var viewport = CreateObj("Viewport", scrollObj.transform);
        StretchFill(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        // content
        var content = CreateObj("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        float totalHeight = allEvents.Length * (eventRowHeight + eventRowSpacing);
        contentRect.sizeDelta = new Vector2(0f, totalHeight);
        scroll.content = contentRect;

        AddVerticalScrollbar(scroll, scrollObj.transform);

        for (int i = 0; i < allEvents.Length; i++)
        {
            BuildEventRow(content.transform, allEvents[i], i);
        }
    }

    void BuildEventRow(Transform parent, EventData evt, int index)
    {
        float yPos = -(index * (eventRowHeight + eventRowSpacing));

        var row = CreateObj($"Event_{index}", parent);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, yPos);
        rowRect.sizeDelta = new Vector2(-10f, eventRowHeight);

        var rowImg = row.AddComponent<Image>();
        rowImg.color = rowNormalColor;
        rowImg.raycastTarget = true;

        // hover handler
        var handler = row.AddComponent<CodexRowHandler>();
        handler.normalColor = rowNormalColor;
        handler.hoverColor = rowHoverColor;
        handler.image = rowImg;
        handler.onClick = () => ShowEventDetail(evt);

        // name (left side)
        var nameObj = CreateObj("Name", row.transform);
        var nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(0.4f, 1f);
        nameRect.offsetMin = new Vector2(12f, 4f);
        nameRect.offsetMax = new Vector2(0f, -4f);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = evt.eventName;
        nameTmp.fontSize = eventNameFontSize;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.enableWordWrapping = false;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;

        // badge / traits (center)
        var badgeObj = CreateObj("Badge", row.transform);
        var badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.4f, 0f);
        badgeRect.anchorMax = new Vector2(0.7f, 1f);
        badgeRect.offsetMin = new Vector2(4f, 4f);
        badgeRect.offsetMax = new Vector2(-4f, -4f);
        var badgeTmp = badgeObj.AddComponent<TextMeshProUGUI>();
        badgeTmp.fontSize = eventBadgeFontSize;
        badgeTmp.alignment = TextAlignmentOptions.Midline;
        badgeTmp.enableWordWrapping = false;

        if (evt.IsFocusEvent())
        {
            badgeTmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(focusBadgeColor)}>FOCUS EVENT</color>";
        }
        else if (evt.targetAll)
        {
            badgeTmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(globalBadgeColor)}>GLOBAL</color>";
        }
        else if (evt.affectedTraits != null && evt.affectedTraits.Length > 0)
        {
            var traitNames = new List<string>();
            foreach (var t in evt.affectedTraits)
                traitNames.Add(t.ToString());
            badgeTmp.text = string.Join(", ", traitNames);
            badgeTmp.color = new Color(0.7f, 0.7f, 0.7f);
        }
        else
        {
            badgeTmp.text = "";
        }

        // stat deltas (right side)
        var statsObj = CreateObj("Stats", row.transform);
        var statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.7f, 0f);
        statsRect.anchorMax = new Vector2(1f, 1f);
        statsRect.offsetMin = new Vector2(4f, 4f);
        statsRect.offsetMax = new Vector2(-12f, -4f);
        var statsTmp = statsObj.AddComponent<TextMeshProUGUI>();
        statsTmp.fontSize = eventStatFontSize;
        statsTmp.alignment = TextAlignmentOptions.MidlineRight;
        statsTmp.enableWordWrapping = false;
        statsTmp.richText = true;

        string cStr = FormatDelta(evt.carbonDelta, carbonColor, true);
        string eStr = FormatDelta(evt.economyDelta, economyColor, false);
        string sStr = FormatDelta(evt.stabilityDelta, stabilityColor, false);
        statsTmp.text = $"C:{cStr}  E:{eStr}  S:{sStr}";
    }

    // -- policies view --

    void BuildPoliciesView(Transform parent)
    {
        // vertical layout: 3 rarity sections stacked
        var scrollObj = CreateObj("Scroll", parent);
        StretchFill(scrollObj);
        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 0f;
        scroll.inertia = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var smoother = scrollObj.AddComponent<SmoothScrollHandler>();
        smoother.scrollRect = scroll;
        smoother.scrollForce = scrollForce;
        smoother.damping = scrollDamping;

        var viewport = CreateObj("Viewport", scrollObj.transform);
        StretchFill(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = CreateObj("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        float sectionHeight = rarityLabelHeight + policyRowHeight + 15f;
        float totalHeight = sectionHeight * 3f;
        contentRect.sizeDelta = new Vector2(0f, totalHeight);
        scroll.content = contentRect;

        AddVerticalScrollbar(scroll, scrollObj.transform);

        float yOff = 0f;
        var tc = TraitColorConfig.Instance;
        BuildRaritySection(content.transform, "COMMON", tc != null ? tc.commonColor : Color.gray, commonPolicies, ref yOff);
        BuildRaritySection(content.transform, "UNCOMMON", tc != null ? tc.uncommonColor : Color.green, uncommonPolicies, ref yOff);
        BuildRaritySection(content.transform, "RARE", tc != null ? tc.rareColor : Color.blue, rarePolicies, ref yOff);
    }

    void BuildRaritySection(Transform parent, string label, Color rarityCol, PolicyData[] policies, ref float yOff)
    {
        // rarity label
        var labelObj = CreateObj($"Label_{label}", parent);
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(10f, -yOff);
        labelRect.sizeDelta = new Vector2(-20f, rarityLabelHeight);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = rarityLabelFontSize;
        labelTmp.color = rarityCol;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.fontStyle = FontStyles.Bold;
        yOff += rarityLabelHeight;

        // horizontal scroll area for cards
        var scrollObj = CreateObj($"Scroll_{label}", parent);
        var scrollRect = scrollObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 1f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.pivot = new Vector2(0f, 1f);
        scrollRect.anchoredPosition = new Vector2(0f, -yOff);
        scrollRect.sizeDelta = new Vector2(0f, policyRowHeight);

        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.vertical = false;
        scroll.horizontal = true;
        scroll.scrollSensitivity = 0f;
        scroll.inertia = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var smoother = scrollObj.AddComponent<SmoothScrollHandler>();
        smoother.scrollRect = scroll;
        smoother.scrollForce = scrollForce;
        smoother.damping = scrollDamping;
        smoother.horizontal = true;

        // transparent image so this area captures pointer events for horizontal scroll
        var scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0f);
        scrollBg.raycastTarget = true;

        var viewport = CreateObj("Viewport", scrollObj.transform);
        StretchFill(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = CreateObj("Content", viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0.5f);
        contentRt.anchorMax = new Vector2(0f, 0.5f);
        contentRt.pivot = new Vector2(0f, 0.5f);
        contentRt.anchoredPosition = Vector2.zero;

        float totalWidth = policies.Length * (cardWidth + cardSpacing);
        contentRt.sizeDelta = new Vector2(totalWidth, policyRowHeight);
        scroll.content = contentRt;

        AddHorizontalScrollbar(scroll, scrollObj.transform);

        for (int i = 0; i < policies.Length; i++)
        {
            BuildPolicyCard(content.transform, policies[i], i);
        }

        yOff += policyRowHeight + 15f;
    }

    void BuildPolicyCard(Transform parent, PolicyData policy, int index)
    {
        float xPos = index * (cardWidth + cardSpacing) + cardSpacing / 2f;

        // border
        var card = CreateObj($"Card_{index}", parent);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 0.5f);
        cardRect.anchorMax = new Vector2(0f, 0.5f);
        cardRect.pivot = new Vector2(0f, 0.5f);
        cardRect.anchoredPosition = new Vector2(xPos, 0f);
        cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

        var borderImg = card.AddComponent<Image>();
        borderImg.color = Color.clear;
        borderImg.raycastTarget = true;

        // trait-colored border
        float thickness = TraitColorConfig.Instance != null ? TraitColorConfig.Instance.borderThickness : cardBorderSize;
        var traitBorder = card.AddComponent<TraitBorder>();
        var colors = new List<Color>();
        var traits = policy.GetBeneficialTraits();
        if (TraitColorConfig.Instance != null)
        {
            foreach (var trait in traits)
                colors.Add(TraitColorConfig.Instance.GetTraitColor(trait));
            if (colors.Count == 0)
                colors.Add(TraitColorConfig.Instance.fallbackColor);
        }
        else
        {
            colors.Add(Color.gray);
        }
        traitBorder.Initialize(colors,
            TraitColorConfig.Instance != null ? TraitColorConfig.Instance.rotationSpeed : 0.5f,
            TraitColorConfig.Instance != null ? TraitColorConfig.Instance.selectedGlow : new Color(1f, 0.9f, 0.4f),
            thickness);

        // hover + click
        var handler = card.AddComponent<CodexRowHandler>();
        handler.traitBorder = traitBorder;
        handler.onClick = () => ShowPolicyDetail(policy);

        // inner background
        var inner = CreateObj("Inner", card.transform);
        var innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(thickness, thickness);
        innerRect.offsetMax = new Vector2(-thickness, -thickness);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = cardBgColor;
        innerImg.raycastTarget = false;

        // cost badge (top-right)
        var costObj = CreateObj("Cost", inner.transform);
        var costRect = costObj.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(1f, 1f);
        costRect.anchorMax = new Vector2(1f, 1f);
        costRect.pivot = new Vector2(1f, 1f);
        costRect.anchoredPosition = new Vector2(-4f, -4f);
        costRect.sizeDelta = new Vector2(30f, 22f);
        var costBg = costObj.AddComponent<Image>();
        costBg.color = new Color(0.2f, 0.4f, 0.7f);
        costBg.raycastTarget = false;
        var costTmp = CreateTextChild(costObj.transform, "CostText", policy.politicalCapitalCost.ToString(), cardCostFontSize, TextAlignmentOptions.Center, Color.white);

        // card name
        var nameObj = CreateObj("Name", inner.transform);
        var nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -28f);
        nameRect.sizeDelta = new Vector2(-10f, 40f);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = policy.policyName;
        nameTmp.fontSize = cardNameFontSize;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.enableWordWrapping = true;

        // stat preview
        var statsObj = CreateObj("Stats", inner.transform);
        var statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 0f);
        statsRect.anchorMax = new Vector2(1f, 0f);
        statsRect.pivot = new Vector2(0.5f, 0f);
        statsRect.anchoredPosition = new Vector2(0f, 8f);
        statsRect.sizeDelta = new Vector2(-8f, 60f);
        var statsTmp = statsObj.AddComponent<TextMeshProUGUI>();
        statsTmp.fontSize = cardStatFontSize;
        statsTmp.alignment = TextAlignmentOptions.Center;
        statsTmp.enableWordWrapping = true;
        statsTmp.richText = true;

        string cStr = FormatDelta(policy.carbonDelta, carbonColor, true);
        string eStr = FormatDelta(policy.economyDelta, economyColor, false);
        string sStr = FormatDelta(policy.stabilityDelta, stabilityColor, false);
        statsTmp.text = $"C: {cStr}\nE: {eStr}\nS: {sStr}";
    }

    // -- detail views --

    void ShowEventDetail(EventData evt)
    {
        state = CodexState.EventDetail;
        eventsView.SetActive(false);
        policiesView.SetActive(false);
        eventDetailView.SetActive(true);
        policyDetailView.SetActive(false);
        UpdateBackButton();

        // clear old content
        foreach (Transform child in eventDetailView.transform)
            Destroy(child.gameObject);

        var detailBg = CreateObj("DetailBg", eventDetailView.transform);
        StretchFill(detailBg);
        detailBg.AddComponent<Image>().color = detailBgColor;

        float yPos = -detailPadding;

        // event name
        var titleObj = CreateTextInDetail(eventDetailView.transform, evt.eventName, detailTitleFontSize, FontStyles.Bold, Color.white, ref yPos, 40f);

        // focus badge
        if (evt.IsFocusEvent())
        {
            CreateTextInDetail(eventDetailView.transform,
                $"<color=#{ColorUtility.ToHtmlStringRGB(focusBadgeColor)}>FOCUS EVENT (threshold: {evt.focusThreshold}+ plays above average)</color>",
                detailTraitFontSize, FontStyles.Normal, focusBadgeColor, ref yPos, 25f);
        }

        yPos -= 10f;

        // description
        CreateTextInDetail(eventDetailView.transform, evt.description, detailBodyFontSize, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f), ref yPos, 80f);

        yPos -= 15f;

        // stat deltas
        CreateTextInDetail(eventDetailView.transform, "EFFECTS", detailStatFontSize, FontStyles.Bold, panelBorderColor, ref yPos, 30f);
        string effects = "";
        if (evt.carbonDelta != 0) effects += $"Carbon: {FormatDelta(evt.carbonDelta, carbonColor, true)}\n";
        if (evt.economyDelta != 0) effects += $"Economy: {FormatDelta(evt.economyDelta, economyColor, false)}\n";
        if (evt.stabilityDelta != 0) effects += $"Stability: {FormatDelta(evt.stabilityDelta, stabilityColor, false)}\n";
        if (effects.Length == 0) effects = "No stat changes";
        CreateTextInDetail(eventDetailView.transform, effects.TrimEnd('\n'), detailBodyFontSize, FontStyles.Normal, Color.white, ref yPos, 70f);

        yPos -= 15f;

        // targeting info
        CreateTextInDetail(eventDetailView.transform, "TARGETING", detailStatFontSize, FontStyles.Bold, panelBorderColor, ref yPos, 30f);

        string targetInfo;
        if (evt.IsFocusEvent())
        {
            targetInfo = $"Targets regions with {evt.focusThreshold}+ plays above average";
        }
        else if (evt.targetAll)
        {
            targetInfo = "Affects ALL regions globally";
        }
        else if (evt.affectedTraits != null && evt.affectedTraits.Length > 0)
        {
            var traitNames = new List<string>();
            foreach (var t in evt.affectedTraits) traitNames.Add(t.ToString());
            targetInfo = $"Affects regions with traits: {string.Join(", ", traitNames)}";
        }
        else
        {
            targetInfo = "No specific targeting";
        }

        if (evt.randomTargetCount > 0)
            targetInfo += $"\nPicks up to {evt.randomTargetCount} random target(s)";

        CreateTextInDetail(eventDetailView.transform, targetInfo, detailBodyFontSize, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f), ref yPos, 50f);
    }

    void ShowPolicyDetail(PolicyData policy)
    {
        state = CodexState.PolicyDetail;
        eventsView.SetActive(false);
        policiesView.SetActive(false);
        eventDetailView.SetActive(false);
        policyDetailView.SetActive(true);
        UpdateBackButton();

        // clear old content
        foreach (Transform child in policyDetailView.transform)
            Destroy(child.gameObject);

        // scrollable container for detail content
        var scrollObj = CreateObj("Scroll", policyDetailView.transform);
        StretchFill(scrollObj);
        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 0f;
        scroll.inertia = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var smoother = scrollObj.AddComponent<SmoothScrollHandler>();
        smoother.scrollRect = scroll;
        smoother.scrollForce = scrollForce;
        smoother.damping = scrollDamping;

        var viewport = CreateObj("Viewport", scrollObj.transform);
        StretchFill(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = CreateObj("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        var detailBg = CreateObj("DetailBg", content.transform);
        var detailBgRect = detailBg.GetComponent<RectTransform>();
        detailBgRect.anchorMin = new Vector2(0f, 1f);
        detailBgRect.anchorMax = new Vector2(1f, 1f);
        detailBgRect.pivot = new Vector2(0.5f, 1f);
        detailBgRect.anchoredPosition = Vector2.zero;

        float yPos = -detailPadding;
        Color rarityCol = GetRarityColor(policy.rarity);

        // rarity + name
        CreateTextInParent(content.transform, $"[{policy.rarity}]", detailTraitFontSize, FontStyles.Bold, rarityCol, ref yPos, 25f);
        CreateTextInParent(content.transform, policy.policyName, detailTitleFontSize, FontStyles.Bold, Color.white, ref yPos, 40f);

        // cost
        CreateTextInParent(content.transform, $"Political Capital Cost: {policy.politicalCapitalCost}", detailBodyFontSize, FontStyles.Normal, new Color(0.5f, 0.7f, 1f), ref yPos, 25f);

        yPos -= 10f;

        // description
        CreateTextInParent(content.transform, policy.description, detailBodyFontSize, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f), ref yPos, 80f);

        yPos -= 15f;

        // base stats
        CreateTextInParent(content.transform, "BASE EFFECTS", detailStatFontSize, FontStyles.Bold, panelBorderColor, ref yPos, 30f);
        string baseStats = $"Carbon: {FormatDelta(policy.carbonDelta, carbonColor, true)}\n" +
                          $"Economy: {FormatDelta(policy.economyDelta, economyColor, false)}\n" +
                          $"Stability: {FormatDelta(policy.stabilityDelta, stabilityColor, false)}";
        CreateTextInParent(content.transform, baseStats, detailBodyFontSize, FontStyles.Normal, Color.white, ref yPos, 70f);

        yPos -= 15f;

        // trait bonus table
        CreateTextInParent(content.transform, "TRAIT MODIFIERS", detailStatFontSize, FontStyles.Bold, panelBorderColor, ref yPos, 30f);

        bool anyModifier = false;
        var traits = new[] { RegionTrait.Temperate, RegionTrait.Tropical, RegionTrait.Arid, RegionTrait.Frozen, RegionTrait.Industrial, RegionTrait.Coastal };

        foreach (var trait in traits)
        {
            var dummy = new Region("dummy", Color.white, new List<int>());
            dummy.Trait = trait;
            policy.GetModifiedDeltas(dummy, out float mc, out float me, out float ms);

            // check if any delta actually changed
            bool cChanged = !Mathf.Approximately(mc, policy.carbonDelta);
            bool eChanged = !Mathf.Approximately(me, policy.economyDelta);
            bool sChanged = !Mathf.Approximately(ms, policy.stabilityDelta);

            if (!cChanged && !eChanged && !sChanged) continue;

            anyModifier = true;
            string line = $"<b>{trait}:</b>  ";
            if (cChanged) line += $"C: {FormatModifiedDelta(policy.carbonDelta, mc, carbonColor, true)}  ";
            if (eChanged) line += $"E: {FormatModifiedDelta(policy.economyDelta, me, economyColor, false)}  ";
            if (sChanged) line += $"S: {FormatModifiedDelta(policy.stabilityDelta, ms, stabilityColor, false)}  ";

            CreateTextInParent(content.transform, line, detailTraitFontSize, FontStyles.Normal, Color.white, ref yPos, 28f);
        }

        if (!anyModifier)
        {
            CreateTextInParent(content.transform, "No trait modifiers for this policy.", detailTraitFontSize, FontStyles.Italic, new Color(0.6f, 0.6f, 0.6f), ref yPos, 25f);
        }

        // set content height for scrolling
        float totalContentHeight = Mathf.Abs(yPos) + detailPadding;
        contentRect.sizeDelta = new Vector2(0f, totalContentHeight);
        detailBgRect.sizeDelta = new Vector2(0f, totalContentHeight);
        detailBg.AddComponent<Image>().color = detailBgColor;

        scroll.content = contentRect;
        AddVerticalScrollbar(scroll, scrollObj.transform);
    }

    // -- tab switching --

    void SwitchToEvents()
    {
        state = CodexState.EventList;
        eventsView.SetActive(true);
        policiesView.SetActive(false);
        eventDetailView.SetActive(false);
        policyDetailView.SetActive(false);
        eventsTabImg.color = tabActiveColor;
        policiesTabImg.color = tabInactiveColor;
        UpdateBackButton();
    }

    void SwitchToPolicies()
    {
        state = CodexState.PolicyList;
        eventsView.SetActive(false);
        policiesView.SetActive(true);
        eventDetailView.SetActive(false);
        policyDetailView.SetActive(false);
        eventsTabImg.color = tabInactiveColor;
        policiesTabImg.color = tabActiveColor;
        UpdateBackButton();
    }

    // -- navigation --

    void OnBack()
    {
        switch (state)
        {
            case CodexState.EventDetail:
                SwitchToEvents();
                break;
            case CodexState.PolicyDetail:
                SwitchToPolicies();
                break;
            default:
                // close codex — find MainMenu and call back
                var mainMenu = FindFirstObjectByType<MainMenu>();
                if (mainMenu != null) mainMenu.OnCodexBack();
                Hide();
                break;
        }
    }

    void UpdateBackButton()
    {
        if (backBtnText == null) return;
        bool inDetail = state == CodexState.EventDetail || state == CodexState.PolicyDetail;
        backBtnText.text = inDetail ? "BACK" : "CLOSE";
    }

    // -- formatting helpers --

    string FormatDelta(float value, Color color, bool lowerIsBetter)
    {
        if (Mathf.Approximately(value, 0f)) return "<color=#999999>0</color>";
        string sign = value > 0 ? "+" : "";
        // for carbon, negative is good. for economy/stability, positive is good.
        bool isGood = lowerIsBetter ? value < 0 : value > 0;
        Color displayColor = isGood ? traitBonusColor : traitPenaltyColor;
        return $"<color=#{ColorUtility.ToHtmlStringRGB(displayColor)}>{sign}{value:F0}</color>";
    }

    string FormatModifiedDelta(float baseDelta, float modifiedDelta, Color labelColor, bool lowerIsBetter)
    {
        string baseStr = baseDelta > 0 ? $"+{baseDelta:F0}" : $"{baseDelta:F0}";
        string modStr = modifiedDelta > 0 ? $"+{modifiedDelta:F0}" : $"{modifiedDelta:F0}";

        // determine if the modification is beneficial
        bool isBetter;
        if (lowerIsBetter)
            isBetter = modifiedDelta < baseDelta;
        else
            isBetter = modifiedDelta > baseDelta;

        Color modColor = isBetter ? traitBonusColor : traitPenaltyColor;
        return $"{baseStr} -> <color=#{ColorUtility.ToHtmlStringRGB(modColor)}>{modStr}</color>";
    }

    Color GetRarityColor(PolicyRarity rarity)
    {
        if (TraitColorConfig.Instance != null)
            return TraitColorConfig.Instance.GetRarityColor(rarity);
        return Color.gray;
    }

    // -- scrollbar helpers --

    void AddVerticalScrollbar(ScrollRect scroll, Transform parent)
    {
        // track
        var track = CreateObj("VScrollbar", parent);
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = new Vector2(1f, 1f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.offsetMin = new Vector2(-scrollbarWidth, 0f);
        trackRect.offsetMax = Vector2.zero;
        var trackImg = track.AddComponent<Image>();
        trackImg.color = scrollbarTrackColor;
        trackImg.raycastTarget = false;

        // handle area
        var slideArea = CreateObj("SlidingArea", track.transform);
        StretchFill(slideArea);

        // handle
        var handle = CreateObj("Handle", slideArea.transform);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = scrollbarHandleColor;
        handleImg.raycastTarget = true;

        var scrollbar = track.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRect;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing = 0f;
    }

    void AddHorizontalScrollbar(ScrollRect scroll, Transform parent)
    {
        // track
        var track = CreateObj("HScrollbar", parent);
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f);
        trackRect.anchorMax = new Vector2(1f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = new Vector2(0f, scrollbarWidth);
        var trackImg = track.AddComponent<Image>();
        trackImg.color = scrollbarTrackColor;
        trackImg.raycastTarget = false;

        // handle area
        var slideArea = CreateObj("SlidingArea", track.transform);
        StretchFill(slideArea);

        // handle
        var handle = CreateObj("Handle", slideArea.transform);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = scrollbarHandleColor;
        handleImg.raycastTarget = true;

        var scrollbar = track.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRect;
        scrollbar.direction = Scrollbar.Direction.LeftToRight;

        scroll.horizontalScrollbar = scrollbar;
        scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.horizontalScrollbarSpacing = 0f;
    }

    // -- ui construction helpers --

    GameObject CreateObj(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    void StretchFill(GameObject obj)
    {
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void SetAnchors(GameObject obj, float minX, float minY, float maxX, float maxY)
    {
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
    }

    GameObject CreateButton(Transform parent, string name, string label, Color bgColor, float x, float y, float w, float h, float fontSize, System.Action onClick)
    {
        var btnObj = CreateObj(name, parent);
        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 1f);
        btnRect.anchorMax = new Vector2(0.5f, 1f);
        btnRect.pivot = new Vector2(0.5f, 1f);
        btnRect.anchoredPosition = new Vector2(x, y);
        btnRect.sizeDelta = new Vector2(w, h);

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = bgColor;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() => onClick());

        var textObj = CreateObj("Text", btnObj.transform);
        StretchFill(textObj);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        return btnObj;
    }

    TMP_Text CreateTextChild(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        var obj = CreateObj(name, parent);
        StretchFill(obj);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    GameObject CreateTextInDetail(Transform parent, string text, float fontSize, FontStyles style, Color color, ref float yPos, float height)
    {
        return CreateTextInParent(parent, text, fontSize, style, color, ref yPos, height);
    }

    GameObject CreateTextInParent(Transform parent, string text, float fontSize, FontStyles style, Color color, ref float yPos, float height)
    {
        var obj = CreateObj("Text", parent);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(-detailPadding * 2, height);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        yPos -= height;
        return obj;
    }
}

// hover + click handler for codex list items and cards
public class CodexRowHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public Color normalColor;
    [HideInInspector] public Color hoverColor;
    [HideInInspector] public Image image;
    [HideInInspector] public TraitBorder traitBorder;
    [HideInInspector] public System.Action onClick;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (traitBorder != null)
            traitBorder.SetHovered(true);
        else if (image != null)
            image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (traitBorder != null)
            traitBorder.SetHovered(false);
        else if (image != null)
            image.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        onClick?.Invoke();
    }
}
