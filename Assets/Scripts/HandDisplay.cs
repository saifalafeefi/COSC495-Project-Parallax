using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HandDisplay : MonoBehaviour
{
    [Header("Card Appearance")]
    [SerializeField] private Color cardBackground = new Color(0.12f, 0.12f, 0.16f, 0.95f);

    [Header("Stat Colors")]
    [SerializeField] private Color carbonColor = new Color(0.75f, 0.45f, 0.3f);
    [SerializeField] private Color economyColor = new Color(0.85f, 0.75f, 0.3f);
    [SerializeField] private Color stabilityColor = new Color(0.4f, 0.6f, 0.85f);
    [SerializeField] private Color bonusColor = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color penaltyColor = new Color(0.9f, 0.4f, 0.4f);

    [Header("Tween Settings")]
    [SerializeField] private float cardWidth = 190f;
    [SerializeField] private float cardHeight = 260f;
    [SerializeField] private float cardSpacing = 16f;
    [SerializeField] private float restY = -140f;
    [SerializeField] private float hoverY = 10f;
    [SerializeField] private float selectedY = 20f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float selectedScale = 1.12f;
    [SerializeField] private float tweenSpeed = 12f;
    [SerializeField] private float bounceSpeed = 3f;
    [SerializeField] private float bounceAmount = 6f;
    [SerializeField] private float fanAngle = 5f;
    [SerializeField] private float fanYOffset = 20f;

    [Header("Deal Animation")]
    [SerializeField] private float dealStartX = -1100f;
    [SerializeField] private float dealStartY = -300f;
    [SerializeField] private float dealDuration = 0.5f;
    [SerializeField] private float dealStagger = 0.12f;

    [Header("Shop Hide")]
    [Tooltip("how far the card hand slides down when shop is open")]
    [SerializeField] private float shopSlideDistance = 400f;
    [SerializeField] private float shopSlideSpeed = 6f;

    // tutorial card glow color, padding, pulse speed, and alpha range are all driven by
    // TutorialManager's Global Highlight Style — edit them in one place on the tutorial object

    private GameManager gameManager;
    private RegionManager regionManager;
    private Canvas canvas;
    private RectTransform cardContainer;

    private List<GameObject> cardObjects = new List<GameObject>();
    private List<RectTransform> cardRects = new List<RectTransform>();
    private List<TraitBorder> cardTraitBorders = new List<TraitBorder>();
    // per-card tutorial glow image, kept so Update() can pulse it without rebuilding
    private List<Image> cardTutorialGlows = new List<Image>();
    // per-card deal animation: time when deal started
    private List<float> cardDealTimes = new List<float>();
    // track which PolicyData each card slot holds so we can match across rebuilds
    private List<PolicyData> cardPolicies = new List<PolicyData>();

    // reject flash when card is too expensive
    private int rejectFlashIndex = -1;

    private int hoveredIndex = -1;
    private int selectedIndex = -1;

    // keeps cards fanned briefly after unhover to prevent jitter
    private float lastFanTime;
    private const float fanCooldown = 0.4f;

    private float shopSlideBlend;

    // on mobile, cards are always fanned (no hover)
    private bool isMobile;

    // expose the currently selected card for other scripts (e.g. stat preview)
    public PolicyData SelectedCard
    {
        get
        {
            if (selectedIndex < 0 || gameManager == null || gameManager.CurrentHand == null)
                return null;
            if (selectedIndex >= gameManager.CurrentHand.Count)
                return null;
            return gameManager.CurrentHand[selectedIndex];
        }
    }

    private int lastHandCount = -1;
    private int lastRound = -1;
    private List<PolicyData> lastHandRef;
    private Region lastSelectedRegion;

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }

        if (regionManager == null)
            regionManager = FindFirstObjectByType<RegionManager>();

        if (canvas == null)
            BuildCanvas();

        // don't update cards while paused
        if (PauseMenu.IsPaused) return;

        // hide cards during event banners so shop-bought cards don't linger on screen
        if (gameManager.BannerActive)
        {
            if (cardContainer != null) cardContainer.gameObject.SetActive(false);
            return;
        }
        else if (cardContainer != null && !cardContainer.gameObject.activeSelf)
        {
            cardContainer.gameObject.SetActive(true);
        }

        // slide cards down when shop is open
        if (cardContainer != null)
        {
            float shopTarget = gameManager.ShopActive ? 1f : 0f;
            shopSlideBlend = Mathf.Lerp(shopSlideBlend, shopTarget, shopSlideSpeed * Time.deltaTime);
            var cPos = cardContainer.anchoredPosition;
            cPos.y = -shopSlideDistance * shopSlideBlend;
            cardContainer.anchoredPosition = cPos;
        }

        var hand = gameManager.CurrentHand;
        int handCount = hand != null ? hand.Count : 0;
        int round = gameManager.CurrentRound;
        Region selectedRegion = regionManager != null ? regionManager.SelectedRegion : null;

        if (round != lastRound || hand != lastHandRef)
        {
            // new round or restart: full rebuild with deal animation
            lastRound = round;
            lastHandCount = handCount;
            lastHandRef = hand;
            lastSelectedRegion = selectedRegion;
            selectedIndex = -1;
            RebuildCards();
        }
        else if (handCount != lastHandCount)
        {
            // card played: refresh content, preserve positions (no deal animation)
            lastHandCount = handCount;
            lastSelectedRegion = selectedRegion;

            if (selectedIndex >= handCount)
                selectedIndex = -1;

            RefreshCardContent();
        }
        else if (selectedRegion != lastSelectedRegion)
        {
            // skip stat preview refresh while overlays are open — prevents shop cards
            // from getting rebuilt and misbehaving when focusing regions via dashboard
            if (gameManager.DashboardActive || gameManager.ShopActive || gameManager.RewardActive)
            {
                lastSelectedRegion = selectedRegion;
            }
            else
            {
                // just refresh card content (stat previews) without resetting deal animation
                lastSelectedRegion = selectedRegion;
                RefreshCardContent();
            }
        }

        AnimateCards();
        UpdateTutorialGlows();
    }

    // animates the per-card tutorial glow — pulsing when the card index matches the tutorial's target slot, transparent otherwise
    // style (color / alpha pulse / speed) is read from TutorialManager's Global Highlight Style every frame
    // so live inspector tweaks on the tutorial object apply immediately
    void UpdateTutorialGlows()
    {
        int target = TutorialManager.HighlightedCardIndex;
        Color glowColor = TutorialManager.GlobalHighlightColor;
        float pulse = (Mathf.Sin(Time.time * TutorialManager.GlobalHighlightPulseSpeed) + 1f) / 2f;
        float alpha = Mathf.Lerp(TutorialManager.GlobalHighlightMinAlpha, TutorialManager.GlobalHighlightMaxAlpha, pulse);
        float pad = TutorialManager.GlobalHighlightPadding;

        for (int i = 0; i < cardTutorialGlows.Count; i++)
        {
            var img = cardTutorialGlows[i];
            if (img == null) continue;

            // live padding so the glow reflects Global Highlight Padding tweaks instantly
            img.rectTransform.offsetMin = new Vector2(-pad, -pad);
            img.rectTransform.offsetMax = new Vector2(pad, pad);

            float a = (i == target) ? alpha : 0f;
            img.color = new Color(glowColor.r, glowColor.g, glowColor.b, a);
        }
    }

    void BuildCanvas()
    {
        #if UNITY_EDITOR
        isMobile = FindFirstObjectByType<ARPlacement>() != null;
        #elif UNITY_ANDROID || UNITY_IOS
        isMobile = true;
        #endif

        var canvasObj = new GameObject("HandCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // container anchored to bottom center, no layout group (manual positioning)
        var containerObj = new GameObject("CardContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        cardContainer = containerObj.AddComponent<RectTransform>();
        cardContainer.anchorMin = new Vector2(0.5f, 0f);
        cardContainer.anchorMax = new Vector2(0.5f, 0f);
        cardContainer.pivot = new Vector2(0.5f, 0f);
        cardContainer.anchoredPosition = Vector2.zero;
        cardContainer.sizeDelta = new Vector2(800f, 400f);
    }

    void RebuildCards()
    {
        foreach (var obj in cardObjects)
        {
            if (obj != null) Destroy(obj);
        }
        cardObjects.Clear();
        cardRects.Clear();
        cardTraitBorders.Clear();
        cardTutorialGlows.Clear();
        cardDealTimes.Clear();
        cardPolicies.Clear();

        if (gameManager.CurrentHand == null) return;

        // play deal sound once for the whole hand
        if (AudioManager.Instance != null && gameManager.CurrentHand.Count > 0)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.cardDeal);

        for (int i = 0; i < gameManager.CurrentHand.Count; i++)
        {
            var card = gameManager.CurrentHand[i];
            var cardObj = BuildCard(card, i);
            cardObj.transform.SetParent(cardContainer, false);

            // start off-screen left
            var rect = cardObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(dealStartX, dealStartY);
            rect.localEulerAngles = new Vector3(0f, 0f, 15f);

            cardObjects.Add(cardObj);
            cardRects.Add(rect);
            cardDealTimes.Add(Time.time + i * dealStagger);
            cardPolicies.Add(card);
        }
    }

    void RefreshCardContent()
    {
        // save state by index — avoids duplicate PolicyData references (same ScriptableObject
        // in hand from both deal and shop purchase) colliding in a dictionary
        var savedPositions = new List<(Vector2 pos, Vector3 scale, Vector3 rot, float dealTime)>();

        for (int i = 0; i < cardRects.Count; i++)
        {
            if (cardRects[i] == null)
            {
                savedPositions.Add((Vector2.zero, Vector3.one, Vector3.zero, 0f));
                continue;
            }
            var rect = cardRects[i];
            float dt = i < cardDealTimes.Count ? cardDealTimes[i] : 0f;
            savedPositions.Add((rect.anchoredPosition, rect.localScale, rect.localEulerAngles, dt));
        }

        foreach (var obj in cardObjects)
        {
            if (obj != null) Destroy(obj);
        }
        cardObjects.Clear();
        cardRects.Clear();
        cardTraitBorders.Clear();
        cardTutorialGlows.Clear();
        cardDealTimes.Clear();
        cardPolicies.Clear();

        if (gameManager.CurrentHand == null) return;

        for (int i = 0; i < gameManager.CurrentHand.Count; i++)
        {
            var card = gameManager.CurrentHand[i];
            var cardObj = BuildCard(card, i);
            cardObj.transform.SetParent(cardContainer, false);

            var rect = cardObj.GetComponent<RectTransform>();

            // restore position from old state if this index existed before
            if (i < savedPositions.Count)
            {
                var state = savedPositions[i];
                rect.anchoredPosition = state.pos;
                rect.localScale = state.scale;
                rect.localEulerAngles = state.rot;
            }

            cardObjects.Add(cardObj);
            cardRects.Add(rect);
            cardDealTimes.Add(i < savedPositions.Count ? savedPositions[i].dealTime : 0f);
            cardPolicies.Add(card);
        }
    }

    void AnimateCards()
    {
        int count = cardRects.Count;
        if (count == 0) return;

        // on mobile always fan out (no hover). on desktop fan when hovered/selected with cooldown.
        bool anyActive = isMobile || hoveredIndex >= 0 || selectedIndex >= 0 || Time.time - lastFanTime < fanCooldown;
        float effectiveSpacing = anyActive ? cardSpacing : -cardWidth * 0.7f;
        float totalWidth = count * cardWidth + (count - 1) * effectiveSpacing;
        float startX = -totalWidth / 2f + cardWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            var rect = cardRects[i];
            if (rect == null) continue;

            float targetX = startX + i * (cardWidth + effectiveSpacing);
            float targetY;
            float targetScale;
            float targetRotation;

            // fan spread: -1 for left, 0 for center, +1 for right
            float fanPos = count > 1 ? (i - (count - 1) / 2f) / ((count - 1) / 2f) : 0f;

            if (i == selectedIndex)
            {
                // bounce animation on selected card
                float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
                targetY = selectedY + bounce;
                targetScale = selectedScale;
                targetRotation = 0f;
            }
            else if (i == hoveredIndex)
            {
                targetY = hoverY;
                targetScale = hoverScale;
                targetRotation = 0f;
            }
            else
            {
                // fan arc: outer cards dip lower
                targetY = restY - Mathf.Abs(fanPos) * fanYOffset;
                targetScale = 1f;
                targetRotation = -fanPos * fanAngle;
            }

            // deal animation: ease-out from off-screen to hand
            float dealT = 1f;
            float adjustedDealDuration = dealDuration / SettingsManager.DealSpeed;
            if (i < cardDealTimes.Count)
            {
                float elapsed = Time.time - cardDealTimes[i];
                if (elapsed < adjustedDealDuration)
                {
                    // ease-out: fast start, slow finish (1 - (1-t)^3)
                    float raw = elapsed / adjustedDealDuration;
                    dealT = 1f - (1f - raw) * (1f - raw) * (1f - raw);
                }
            }

            if (dealT < 1f)
            {
                // still dealing — interpolate from start to target
                float x = Mathf.Lerp(dealStartX, targetX, dealT);
                float y = Mathf.Lerp(dealStartY, targetY, dealT);
                rect.anchoredPosition = new Vector2(x, y);

                float rot = Mathf.Lerp(15f, targetRotation, dealT);
                rect.localEulerAngles = new Vector3(0f, 0f, rot);

                rect.localScale = new Vector3(dealT, dealT, 1f);
            }
            else
            {
                // normal hand positioning
                float t = Time.deltaTime * tweenSpeed;
                var pos = rect.anchoredPosition;
                pos.x = Mathf.Lerp(pos.x, targetX, t);
                pos.y = Mathf.Lerp(pos.y, targetY, t);
                rect.anchoredPosition = pos;

                var s = rect.localScale;
                float curScale = s.x;
                float newScale = Mathf.Lerp(curScale, targetScale, t);
                rect.localScale = new Vector3(newScale, newScale, 1f);

                float curRot = rect.localEulerAngles.z;
                if (curRot > 180f) curRot -= 360f;
                float newRot = Mathf.Lerp(curRot, targetRotation, t);
                rect.localEulerAngles = new Vector3(0f, 0f, newRot);
            }

            // trait border state
            if (dealT >= 1f && i < cardTraitBorders.Count && cardTraitBorders[i] != null)
            {
                cardTraitBorders[i].SetSelected(i == selectedIndex);
                if (i == rejectFlashIndex)
                {
                    cardTraitBorders[i].FlashReject();
                    rejectFlashIndex = -1;
                }
            }

            // z-order: hovered on top, selected behind it, otherwise first card on top (reverse order)
            if (i == hoveredIndex)
                rect.SetAsLastSibling();
            else if (i == selectedIndex && hoveredIndex != selectedIndex)
                rect.transform.SetSiblingIndex(rect.parent.childCount - 2);
            else if (!anyActive)
                rect.transform.SetSiblingIndex(count - 1 - i);
        }
    }

    // called by CardInteraction component on each card
    public void OnCardHover(int index)
    {
        hoveredIndex = index;
        lastFanTime = Time.time;
    }

    public void OnCardUnhover(int index)
    {
        if (hoveredIndex == index)
            hoveredIndex = -1;
    }

    public void OnCardDeselect()
    {
        if (PauseMenu.IsPaused) return;
        if (gameManager != null && (gameManager.RewardActive || gameManager.ShopActive || gameManager.DashboardActive || gameManager.BannerActive)) return;

        // during the tutorial, never let the player deselect — HighlightedCardIndex clears the
        // moment the step advances off SelectCard, but the card is still sitting selected, so
        // a tutorial-wide lock is the only thing that actually holds
        if (TutorialManager.IsActive) return;

        if (selectedIndex >= 0 && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.cardDeselect);
        selectedIndex = -1;
    }

    public void OnCardClick(int index)
    {
        if (PauseMenu.IsPaused) return;
        if (gameManager != null && (gameManager.RewardActive || gameManager.ShopActive || gameManager.DashboardActive || gameManager.BannerActive)) return;

        // during tutorial card steps, lock clicks to the highlighted card so the player can't
        // derail the script by picking a different card than the mascot is talking about
        if (TutorialManager.IsActive && TutorialManager.HighlightedCardIndex >= 0 && index != TutorialManager.HighlightedCardIndex)
            return;

        if (selectedIndex == index)
        {
            // second click on same card: confirm play
            // tutorial blocks card play unless the current step asks for it
            if (!TutorialManager.CanPerformAction(TutorialAction.PlayCard)) return;
            TryPlayCard(index);
        }
        else
        {
            // first click: select this card
            // tutorial blocks card selection unless the current step asks for it
            if (!TutorialManager.CanPerformAction(TutorialAction.SelectCard)) return;

            selectedIndex = index;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.cardSelect);

            TutorialManager.NotifyAction(TutorialAction.SelectCard);
        }
    }

    void TryPlayCard(int index)
    {
        if (gameManager == null || gameManager.GameOver) return;

        Region target = regionManager != null ? regionManager.SelectedRegion : null;
        if (target == null) return;

        string result = gameManager.PlayCard(index, target);
        var selector = FindFirstObjectByType<RegionSelector>();

        // not enough capital — flash the card red and show message
        if (result != null && result.StartsWith("NOT_ENOUGH_CAPITAL"))
        {
            rejectFlashIndex = index;
            selectedIndex = -1;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.cardReject);

            if (selector != null)
            {
                selector.LastPlayResult = $"Not enough Political Capital!";
                selector.LastPlayTime = Time.time;
            }
            return;
        }

        // update the selector's last play result
        if (selector != null)
        {
            selector.LastPlayResult = result;
            selector.LastPlayTime = Time.time;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.cardPlay);

        selectedIndex = -1;
        hoveredIndex = -1;

        // tell the tutorial a card was played successfully
        TutorialManager.NotifyAction(TutorialAction.PlayCard);
    }

    GameObject BuildCard(PolicyData policy, int index)
    {
        Region selected = regionManager != null ? regionManager.SelectedRegion : null;

        // get modified deltas if a region is selected
        float carbon, economy, stability;
        policy.GetModifiedDeltas(selected, out carbon, out economy, out stability);
        float baseCarbonDelta = policy.carbonDelta;
        float baseEconDelta = policy.economyDelta;
        float baseStabDelta = policy.stabilityDelta;

        // card root
        var card = new GameObject($"Card_{index}");
        var cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        cardRect.pivot = new Vector2(0.5f, 0f);
        // start off-screen
        cardRect.anchoredPosition = new Vector2(0f, restY - 40f);

        // transparent root image for raycast target
        var borderImg = card.AddComponent<Image>();
        borderImg.color = Color.clear;

        // tutorial glow sits behind everything, extends past the card edges by TutorialManager's global padding
        // disabled by default — UpdateTutorialGlows() pulses it live and keeps the padding in sync
        float glowPad = TutorialManager.GlobalHighlightPadding;
        Color glowCol = TutorialManager.GlobalHighlightColor;
        var glowObj = new GameObject("TutorialGlow");
        glowObj.transform.SetParent(card.transform, false);
        var glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-glowPad, -glowPad);
        glowRect.offsetMax = new Vector2(glowPad, glowPad);
        var glowImg = glowObj.AddComponent<Image>();
        glowImg.color = new Color(glowCol.r, glowCol.g, glowCol.b, 0f);
        glowImg.raycastTarget = false;
        glowObj.transform.SetAsFirstSibling();
        cardTutorialGlows.Add(glowImg);

        // trait-colored border
        float thickness = TraitColorConfig.Instance != null ? TraitColorConfig.Instance.borderThickness : 4f;
        var traitBorder = card.AddComponent<TraitBorder>();
        var traitColors = BuildTraitColors(policy);
        traitBorder.Initialize(traitColors,
            TraitColorConfig.Instance != null ? TraitColorConfig.Instance.rotationSpeed : 0.5f,
            TraitColorConfig.Instance != null ? TraitColorConfig.Instance.selectedGlow : new Color(1f, 0.9f, 0.4f),
            thickness);
        cardTraitBorders.Add(traitBorder);

        // make card interactive
        var interaction = card.AddComponent<CardInteraction>();
        interaction.cardIndex = index;
        interaction.handDisplay = this;

        // inner background
        var inner = CreateChild(card, "Inner", new Vector2(thickness, thickness), new Vector2(-thickness, -thickness));
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = cardBackground;
        innerImg.raycastTarget = false;

        // -- cost badge top-right --
        var costBadge = CreateChild(inner, "CostBadge",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-4f, -4f), new Vector2(28f, 28f));
        var costBg = costBadge.AddComponent<Image>();
        costBg.color = new Color(0.15f, 0.35f, 0.6f, 0.9f);
        costBg.raycastTarget = false;
        string costLabel = policy.politicalCapitalCost == 0 ? "FREE" : policy.politicalCapitalCost.ToString();
        float costFontSize = policy.politicalCapitalCost == 0 ? 9f : 14f;
        var costText = CreateText(costBadge, costLabel, costFontSize, TextAlignmentOptions.Center, Color.white);
        costText.fontStyle = FontStyles.Bold;
        StretchFill(costText.gameObject);

        // -- stats section at top --
        // carbon: lower is better, economy/stability: higher is better
        string statsString = BuildStatLine("Carbon", carbon, baseCarbonDelta, carbonColor, selected, true)
            + "\n" + BuildStatLine("Economy", economy, baseEconDelta, economyColor, selected, false)
            + "\n" + BuildStatLine("Stability", stability, baseStabDelta, stabilityColor, selected, false);

        var statsArea = CreateChild(inner, "Stats",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(-12f, 52f));
        var statsText = CreateText(statsArea, statsString, 11, TextAlignmentOptions.TopLeft, Color.white);
        statsText.richText = true;
        statsText.lineSpacing = -4f;
        StretchFill(statsText.gameObject, 6f);

        // -- title --
        var title = CreateChild(inner, "Title",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -62f), new Vector2(-12f, 30f));
        var titleText = CreateText(title, policy.policyName, 15, TextAlignmentOptions.Center, Color.white);
        titleText.fontStyle = FontStyles.Bold;
        StretchFill(titleText.gameObject);

        // -- icon area --
        var iconArea = CreateChild(inner, "IconArea",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(-24f, 80f));
        var iconBg = iconArea.AddComponent<Image>();
        iconBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        iconBg.raycastTarget = false;

        if (policy.icon != null)
        {
            var iconImg = CreateChild(iconArea, "Icon", Vector2.zero, Vector2.zero);
            var img = iconImg.AddComponent<Image>();
            img.sprite = policy.icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            StretchFill(iconImg, 8f);
        }
        else
        {
            var placeholder = CreateText(iconArea, policy.rarity.ToString(), 13, TextAlignmentOptions.Center,
                TraitColorConfig.Instance != null ? TraitColorConfig.Instance.GetRarityColor(policy.rarity) : Color.gray);
            placeholder.fontStyle = FontStyles.Italic;
            StretchFill(placeholder.gameObject);
        }

        // -- description --
        var descArea = CreateChild(inner, "Desc",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0f),
            new Vector2(0f, 6f), new Vector2(-12f, -180f));
        var descText = CreateText(descArea, policy.description, 11, TextAlignmentOptions.TopLeft, new Color(0.75f, 0.75f, 0.8f));
        descText.enableWordWrapping = true;
        descText.overflowMode = TextOverflowModes.Truncate;
        StretchFill(descText.gameObject);

        return card;
    }

    // lowerIsBetter: true for carbon (negative = good), false for economy/stability (positive = good)
    string BuildStatLine(string label, float modifiedValue, float baseValue, Color labelColor, Region selected, bool lowerIsBetter)
    {
        string hex = ColorUtility.ToHtmlStringRGB(labelColor);
        string baseSign = baseValue >= 0 ? "+" : "";

        if (selected != null)
        {
            float diff = modifiedValue - baseValue;
            if (Mathf.Abs(diff) > 0.01f)
            {
                // show: base → final (trait bonus)
                bool isGood = lowerIsBetter ? diff < 0 : diff > 0;
                string colorHex = isGood
                    ? ColorUtility.ToHtmlStringRGB(bonusColor)
                    : ColorUtility.ToHtmlStringRGB(penaltyColor);
                string modSign = modifiedValue >= 0 ? "+" : "";
                string diffSign = diff >= 0 ? "+" : "";
                return $"<color=#{hex}>{label}: {baseSign}{baseValue:0} → {modSign}{modifiedValue:0}</color>"
                    + $" <color=#{colorHex}>({diffSign}{diff:0})</color>";
            }
        }

        return $"<color=#{hex}>{label}: {baseSign}{baseValue:0}</color>";
    }

    List<Color> BuildTraitColors(PolicyData policy)
    {
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
        return colors;
    }

    GameObject CreateChild(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return obj;
    }

    GameObject CreateChild(GameObject parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return obj;
    }

    TMP_Text CreateText(GameObject parent, string content, float fontSize, TextAlignmentOptions alignment, Color color)
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

    void StretchFill(GameObject obj, float inset = 0f)
    {
        var rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    void StretchFill(TMP_Text text)
    {
        StretchFill(text.gameObject);
    }
}
