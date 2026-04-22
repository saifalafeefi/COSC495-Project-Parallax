using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class DesktopInteraction : MonoBehaviour
{
    [SerializeField] private DesktopPlacement desktopPlacement;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float orbitSpeed = 5f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 10f;

    [Header("Touch Controls")]
    [Tooltip("how far a single finger must move before it registers as a drag. lower = snappier orbit, higher = more forgiving for taps")]
    [SerializeField] private float touchDragThresholdPx = 10f;
    [Tooltip("converts raw pinch-distance-change (pixels/frame) into the same units the mouse-scroll path consumes. raise for snappier pinch zoom")]
    [SerializeField] private float pinchZoomMultiplier = 0.2f;

    [Header("Region Focus")]
    [Tooltip("how close the camera gets when focusing on a region")]
    [SerializeField] private float focusDistance = 5f;

    [Tooltip("how smooth the camera tweens to the focused region (lower = slower)")]
    [SerializeField, Range(0.5f, 20f)]
    private float focusSmoothSpeed = 4f;

    [Tooltip("orbit speed multiplier while focused on a region (0.3 = 30% of normal speed)")]
    [SerializeField, Range(0.05f, 1f)]
    private float focusedOrbitMultiplier = 0.3f;

    [Tooltip("max degrees the camera can drift from the focused region center before stopping")]
    [SerializeField, Range(5f, 60f)]
    private float focusDeadzoneAngle = 25f;

    [Header("Shop Camera")]
    [Tooltip("how far to pull back from current distance when shop opens")]
    [SerializeField] private float shopExtraDistance = 4f;

    [Tooltip("horizontal look offset in degrees (positive = earth moves left on screen)")]
    [SerializeField] private float shopLookOffsetX = 25f;

    [Tooltip("vertical look offset in degrees (positive = earth moves down on screen)")]
    [SerializeField] private float shopLookOffsetY = -10f;

    [Tooltip("how smooth the camera tweens to/from the shop view")]
    [SerializeField, Range(0.5f, 20f)]
    private float shopSmoothSpeed = 4f;

    private float currentDistance;
    private float yaw;
    private float pitch;

    // the yaw/pitch/distance we're tweening toward
    private float targetYaw;
    private float targetPitch;
    private float targetDistance;

    private bool isFocused;
    // the distance we were at before focusing, so we can return to it
    private float unfocusedDistance;
    // true while we're tweening back out after unfocusing
    private bool isReturning;
    // the yaw/pitch of the region center, used for deadzone clamping
    private float focusCenterYaw;
    private float focusCenterPitch;

    // tracked region so camera follows it as the earth spins
    private Region focusedRegion;
    private RegionManager focusedRegionManager;

    // shop camera state
    private bool isInShopView;
    private bool shopReturning;
    private float savedShopDistance;
    private float shopLookBlend; // 0 = normal (look at earth), 1 = full shop offset
    private bool wasShopActive;

    // touch state — mirrors ARPlacement's single-finger-rotate + two-finger-pinch pattern
    // so the desktop scene can run on phones too (planned desktop/ar toggle on mobile)
    private bool touchDragging;
    private bool touchStartedOnUI;
    private Vector2 touchStartPos;
    private bool pinchActive;
    private float previousPinchDistance;

    public bool IsFocused => isFocused;
    public bool IsInShopView => isInShopView;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (desktopPlacement != null && desktopPlacement.SpawnedEarth != null)
            InitOrbit();
    }

    void LateUpdate()
    {
        if (desktopPlacement == null || desktopPlacement.SpawnedEarth == null)
            return;

        // don't allow orbit/zoom while paused
        if (PauseMenu.IsPaused)
            return;

        if (currentDistance == 0f)
            InitOrbit();

        // block all user input while overlay is showing or pointer is over UI
        var gm = FindFirstObjectByType<GameManager>();
        bool overlayBlocked = gm != null && (gm.RewardActive || gm.ShopActive || gm.DashboardActive || gm.BannerActive);
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool rewardBlocked = overlayBlocked || PauseMenu.IsPaused || pointerOverUI;

        // shop camera: pull back and offset look so earth slides to the corner
        bool shopNowActive = gm != null && gm.ShopActive;
        if (shopNowActive && !wasShopActive)
        {
            savedShopDistance = currentDistance;
            isInShopView = true;
            shopReturning = false;
        }
        else if (!shopNowActive && wasShopActive && isInShopView)
        {
            isInShopView = false;
            shopReturning = true;
        }
        wasShopActive = shopNowActive;

        // tween distance and look offset for shop
        if (isInShopView || shopReturning)
        {
            float targetBlend = isInShopView ? 1f : 0f;
            float targetDist = isInShopView ? savedShopDistance + shopExtraDistance : savedShopDistance;
            float t = shopSmoothSpeed * Time.deltaTime;

            shopLookBlend = Mathf.Lerp(shopLookBlend, targetBlend, t);
            currentDistance = Mathf.Lerp(currentDistance, targetDist, t);
            ApplyOrbit();

            // done returning — resume normal orbit
            if (shopReturning && Mathf.Abs(shopLookBlend) < 0.005f &&
                Mathf.Abs(currentDistance - savedShopDistance) < 0.05f)
            {
                shopLookBlend = 0f;
                currentDistance = savedShopDistance;
                targetDistance = currentDistance;
                shopReturning = false;
            }
            return;
        }

        if (isFocused)
        {
            // compute how much the region moved since last frame due to earth spin,
            // then shift the camera by the same amount so it rides with the rotation
            if (focusedRegion != null && focusedRegionManager != null)
            {
                Vector3 earthPos = focusedRegionManager.transform.position;
                Vector3 regionCenter = focusedRegionManager.GetRegionWorldCenter(focusedRegion);
                Vector3 dir = (regionCenter - earthPos).normalized;

                float newCenterYaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
                float newCenterPitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;

                // how much the region drifted this frame
                float yawDelta = Mathf.DeltaAngle(focusCenterYaw, newCenterYaw);
                float pitchDelta = newCenterPitch - focusCenterPitch;

                // shift camera and target by the same amount so manual orbit is preserved
                yaw += yawDelta;
                pitch += pitchDelta;
                targetYaw += yawDelta;
                targetPitch += pitchDelta;

                focusCenterYaw = newCenterYaw;
                focusCenterPitch = newCenterPitch;
            }

            // smoothly tween toward the target (only matters during initial snap)
            yaw = Mathf.LerpAngle(yaw, targetYaw, focusSmoothSpeed * Time.deltaTime);
            pitch = Mathf.Lerp(pitch, targetPitch, focusSmoothSpeed * Time.deltaTime);
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, focusSmoothSpeed * Time.deltaTime);

            // let the player orbit slowly while focused, with deadzone limit
            Vector2 focusedDelta = ReadOrbitDrag(out bool focusedDidDrag);
            if (!rewardBlocked && focusedDidDrag
                && TutorialManager.CanPerformAction(TutorialAction.OrbitEarth))
            {
                Vector2 delta = focusedDelta;
                float baseSpeed = orbitSpeed * focusedOrbitMultiplier * SettingsManager.OrbitSensitivity;
                float invertY = SettingsManager.InvertY ? 1f : -1f;

                // how far we currently are from region center
                float dYaw = Mathf.DeltaAngle(yaw, focusCenterYaw);
                float dPitch = pitch - focusCenterPitch;
                float angleDist = Mathf.Sqrt(dYaw * dYaw + dPitch * dPitch);

                // apply the drag at full speed first
                float newYaw = yaw + delta.x * baseSpeed * 0.1f;
                float newPitch = pitch + delta.y * baseSpeed * 0.1f * invertY;
                newPitch = Mathf.Clamp(newPitch, -89f, 89f);

                // check if this move goes further from center or closer
                float newDYaw = Mathf.DeltaAngle(newYaw, focusCenterYaw);
                float newDPitch = newPitch - focusCenterPitch;
                float newDist = Mathf.Sqrt(newDYaw * newDYaw + newDPitch * newDPitch);

                if (newDist > angleDist && angleDist > focusDeadzoneAngle * 0.5f)
                {
                    // moving away from center and past halfway — slow down
                    float edgeFactor = 1f - Mathf.Clamp01((angleDist - focusDeadzoneAngle * 0.5f) / (focusDeadzoneAngle * 0.5f));
                    newYaw = yaw + delta.x * baseSpeed * edgeFactor * 0.1f;
                    newPitch = pitch + delta.y * baseSpeed * edgeFactor * 0.1f * invertY;
                    newPitch = Mathf.Clamp(newPitch, -89f, 89f);

                    newDYaw = Mathf.DeltaAngle(newYaw, focusCenterYaw);
                    newDPitch = newPitch - focusCenterPitch;
                    newDist = Mathf.Sqrt(newDYaw * newDYaw + newDPitch * newDPitch);
                }

                // hard clamp at the edge
                if (newDist > focusDeadzoneAngle)
                {
                    float scale = focusDeadzoneAngle / newDist;
                    newYaw = focusCenterYaw - newDYaw * scale;
                    newPitch = focusCenterPitch + newDPitch * scale;
                }

                yaw = newYaw;
                pitch = newPitch;
                targetYaw = yaw;
                targetPitch = pitch;
            }

            // ESC unfocus is handled by PauseMenu to avoid double-triggering
        }
        else if (isReturning)
        {
            if (!rewardBlocked)
            {
                // let the player orbit while zooming out
                Vector2 returningDelta = ReadOrbitDrag(out bool returningDidDrag);
                if (returningDidDrag
                    && TutorialManager.CanPerformAction(TutorialAction.OrbitEarth))
                {
                    float sens = orbitSpeed * SettingsManager.OrbitSensitivity;
                    float invertY = SettingsManager.InvertY ? 1f : -1f;
                    yaw += returningDelta.x * sens * 0.1f;
                    pitch += returningDelta.y * sens * 0.1f * invertY;
                    pitch = Mathf.Clamp(pitch, -89f, 89f);
                }

                HandleZoom();
            }

            // smoothly pull distance back, keep going even if player is orbiting
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, focusSmoothSpeed * Time.deltaTime);

            if (Mathf.Abs(currentDistance - targetDistance) < 0.05f)
            {
                isReturning = false;
                currentDistance = targetDistance;
            }
        }
        else if (!rewardBlocked)
        {
            HandleOrbit();
            HandleZoom();
        }

        ApplyOrbit();
    }

    void InitOrbit()
    {
        Vector3 offset = mainCamera.transform.position - desktopPlacement.SpawnedEarth.transform.position;
        currentDistance = offset.magnitude;
        yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(Mathf.Clamp(offset.y / currentDistance, -1f, 1f)) * Mathf.Rad2Deg;

        targetYaw = yaw;
        targetPitch = pitch;
        targetDistance = currentDistance;
    }

    void HandleOrbit()
    {
        Vector2 delta = ReadOrbitDrag(out bool didDrag);
        if (!didDrag) return;

        // tutorial blocks orbit unless the current step asks for it
        if (!TutorialManager.CanPerformAction(TutorialAction.OrbitEarth))
            return;

        float sens = orbitSpeed * SettingsManager.OrbitSensitivity;
        float invertY = SettingsManager.InvertY ? 1f : -1f;
        yaw += delta.x * sens * 0.1f;
        pitch += delta.y * sens * 0.1f * invertY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        targetYaw = yaw;
        targetPitch = pitch;
        targetDistance = currentDistance;

        // tell the tutorial the player actually dragged, not just clicked — threshold is inspector-driven on TutorialManager
        if (delta.sqrMagnitude > TutorialManager.OrbitDragThresholdSqr)
            TutorialManager.NotifyAction(TutorialAction.OrbitEarth);
    }

    void HandleZoom()
    {
        float scroll = ReadZoomDelta();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // tutorial blocks zoom unless the current step asks for it
            if (!TutorialManager.CanPerformAction(TutorialAction.ZoomEarth))
                return;

            float sens = zoomSpeed * SettingsManager.ZoomSensitivity;
            currentDistance -= scroll * sens * 0.01f;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            targetDistance = currentDistance;

            TutorialManager.NotifyAction(TutorialAction.ZoomEarth);
        }
    }

    // unified drag reader: one-finger touch first (with UI + threshold gating), mouse-left fallback.
    // returns delta in screen pixels; didDrag = whether the caller should treat this as an orbit move.
    Vector2 ReadOrbitDrag(out bool didDrag)
    {
        didDrag = false;

        int touchCount = Touch.activeTouches.Count;

        // two or more fingers = pinch mode, don't orbit
        if (touchCount >= 2)
            return Vector2.zero;

        if (touchCount == 1)
        {
            var t = Touch.activeTouches[0];

            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                touchStartPos = t.screenPosition;
                touchStartedOnUI = EventSystem.current != null
                    && EventSystem.current.IsPointerOverGameObject(t.touchId);
                touchDragging = false;
                return Vector2.zero;
            }

            if (touchStartedOnUI) return Vector2.zero;

            if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved
                || t.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                if (!touchDragging)
                {
                    // wait until the finger has moved past the threshold so taps don't drift the camera
                    if (Vector2.SqrMagnitude(t.screenPosition - touchStartPos)
                        < touchDragThresholdPx * touchDragThresholdPx)
                        return Vector2.zero;
                    touchDragging = true;
                }
                didDrag = true;
                return t.delta;
            }

            if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended
                || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                touchDragging = false;
                touchStartedOnUI = false;
            }
            return Vector2.zero;
        }

        // no touches — mouse fallback
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.isPressed) return Vector2.zero;
        Vector2 mouseDelta = mouse.delta.ReadValue();
        if (mouseDelta.sqrMagnitude <= 0.0001f) return Vector2.zero;
        didDrag = true;
        return mouseDelta;
    }

    // unified zoom reader: two-finger pinch first, scroll-wheel fallback.
    // returns a signed scalar matching the units HandleZoom's mouse-scroll path consumes.
    float ReadZoomDelta()
    {
        if (Touch.activeTouches.Count >= 2)
        {
            var a = Touch.activeTouches[0];
            var b = Touch.activeTouches[1];
            float dist = Vector2.Distance(a.screenPosition, b.screenPosition);

            if (!pinchActive)
            {
                pinchActive = true;
                previousPinchDistance = dist;
                return 0f;
            }

            float deltaDist = dist - previousPinchDistance;
            previousPinchDistance = dist;
            return deltaDist * pinchZoomMultiplier;
        }

        pinchActive = false;

        var mouse = Mouse.current;
        if (mouse == null) return 0f;
        return mouse.scroll.ReadValue().y;
    }

    void ApplyOrbit()
    {
        Vector3 target = desktopPlacement.SpawnedEarth.transform.position;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);

        mainCamera.transform.position = target + offset;
        mainCamera.transform.LookAt(target);

        // apply shop look offset so earth slides to corner of screen
        if (shopLookBlend > 0.001f)
        {
            mainCamera.transform.rotation *= Quaternion.Euler(
                shopLookOffsetY * shopLookBlend,
                shopLookOffsetX * shopLookBlend,
                0f);
        }
    }

    // called by RegionSelector when a region is right-clicked
    // stores the region so the camera can track it as the earth spins
    public void FocusOnRegion(Region region, RegionManager rm)
    {
        if (desktopPlacement == null || desktopPlacement.SpawnedEarth == null) return;
        if (region == null || rm == null) return;

        // save the distance so we can pull back to it on unfocus
        if (!isFocused)
            unfocusedDistance = currentDistance;

        isFocused = true;
        isReturning = false;
        focusedRegion = region;
        focusedRegionManager = rm;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.regionFocus);

        // compute initial target from the region's current world position
        Vector3 earthPos = rm.transform.position;
        Vector3 regionCenter = rm.GetRegionWorldCenter(region);
        Vector3 dir = (regionCenter - earthPos).normalized;

        focusCenterYaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
        focusCenterPitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        targetYaw = focusCenterYaw;
        targetPitch = focusCenterPitch;
        targetDistance = focusDistance;
    }

    public void Unfocus()
    {
        if (!isFocused) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.regionUnfocus);

        isFocused = false;
        isReturning = true;
        focusedRegion = null;
        focusedRegionManager = null;

        // stay facing the same direction, just pull back to the old distance
        targetDistance = unfocusedDistance;
        targetYaw = yaw;
        targetPitch = pitch;
    }
}
