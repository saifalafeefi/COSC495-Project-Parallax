using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARPlacement : MonoBehaviour
{
    [Header("Earth Setup")]
    [SerializeField] private GameObject earthPrefab;
    [Tooltip("distance in front of the camera to spawn the earth")]
    [SerializeField] private float spawnDistance = 2f;
    [Tooltip("vertical offset from camera center (negative = below eye level)")]
    [SerializeField] private float spawnVerticalOffset = -0.3f;

    [Header("Auto Rotation")]
    [Tooltip("how fast the earth spins on its own (degrees per second)")]
    [SerializeField] private float autoRotateSpeed = 5f;
    [Tooltip("seconds to wait after releasing touch before spin resumes")]
    [SerializeField] private float resumeDelay = 1f;
    [Tooltip("how many seconds it takes to ramp back up to full spin speed")]
    [SerializeField] private float resumeRampTime = 2f;

    [Header("Touch Controls")]
    [SerializeField] private float rotationSpeed = 0.3f;
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 1.5f;

    [Header("Region Focus")]
    [Tooltip("how smooth the earth rotates to show the focused region")]
    [SerializeField, Range(0.5f, 20f)]
    private float focusSmoothSpeed = 4f;

    public GameObject SpawnedEarth { get; private set; }
    public bool IsFocused => isFocused;

    // auto-spin state
    private float timeSinceRelease;
    private bool playerTouching;

    // focus state
    private bool isFocused;
    private Region focusedRegion;
    private RegionManager focusedRegionManager;

    // pinch state
    private float previousPinchDistance;

    // track earth's current rotation for focus tracking
    private Quaternion lastEarthRotation;

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
        if (earthPrefab == null) return;

        // spawn earth in front of the camera at a fixed world position
        Camera cam = Camera.main;
        Vector3 spawnPos;
        if (cam != null)
        {
            spawnPos = cam.transform.position + cam.transform.forward * spawnDistance;
            spawnPos.y += spawnVerticalOffset;
        }
        else
        {
            spawnPos = new Vector3(0f, spawnVerticalOffset, spawnDistance);
        }

        SpawnedEarth = Instantiate(earthPrefab, spawnPos, earthPrefab.transform.rotation);
        timeSinceRelease = resumeDelay + resumeRampTime;
        lastEarthRotation = SpawnedEarth.transform.rotation;
    }

    void Update()
    {
        if (SpawnedEarth == null) return;

        // block input while paused
        bool paused = PauseMenu.IsPaused;

        // check overlays
        var gm = FindFirstObjectByType<GameManager>();
        bool overlayBlocked = gm != null && (gm.RewardActive || gm.ShopActive || gm.DashboardActive || gm.BannerActive);
        bool inputBlocked = paused || overlayBlocked;

        // handle touch input
        if (!inputBlocked)
        {
            int touchCount = Touch.activeTouches.Count;
            if (touchCount == 1)
            {
                HandleRotation();
            }
            else if (touchCount == 2)
            {
                HandlePinchScale();
            }

            if (touchCount > 0)
            {
                playerTouching = true;
                timeSinceRelease = 0f;
            }
            else if (playerTouching)
            {
                playerTouching = false;
                timeSinceRelease = 0f;
            }
        }

        if (!playerTouching)
            timeSinceRelease += Time.deltaTime;

        // focus tracking: rotate earth so focused region stays facing camera
        if (isFocused && focusedRegion != null && focusedRegionManager != null)
        {
            TrackFocusedRegion();
        }

        // auto-spin (skip during touch, ramp up after release)
        HandleAutoSpin();

        lastEarthRotation = SpawnedEarth.transform.rotation;
    }

    void HandleRotation()
    {
        var touch = Touch.activeTouches[0];
        if (touch.phase != UnityEngine.InputSystem.TouchPhase.Moved) return;

        float deltaX = touch.delta.x;
        float deltaY = touch.delta.y;

        // rotate the earth itself (not the camera)
        SpawnedEarth.transform.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);
        SpawnedEarth.transform.Rotate(Camera.main.transform.right, deltaY * rotationSpeed, Space.World);
    }

    void HandlePinchScale()
    {
        var touch0 = Touch.activeTouches[0];
        var touch1 = Touch.activeTouches[1];

        float currentDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);

        if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            touch1.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            previousPinchDistance = currentDistance;
            return;
        }

        if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
            touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            float scaleFactor = currentDistance / previousPinchDistance;
            Vector3 newScale = SpawnedEarth.transform.localScale * scaleFactor;

            float clampedUniform = Mathf.Clamp(newScale.x, minScale, maxScale);
            SpawnedEarth.transform.localScale = Vector3.one * clampedUniform;

            previousPinchDistance = currentDistance;
        }
    }

    void HandleAutoSpin()
    {
        if (playerTouching) return;

        // when focused, keep spinning at full speed
        bool focused = isFocused;

        if (!focused && timeSinceRelease < resumeDelay) return;

        float rampProgress = focused ? 1f : Mathf.Clamp01((timeSinceRelease - resumeDelay) / resumeRampTime);
        float currentSpeed = autoRotateSpeed * rampProgress;

        float angle = currentSpeed * Time.deltaTime;
        if (Mathf.Abs(angle) > 0.0001f)
            SpawnedEarth.transform.Rotate(0f, angle, 0f, Space.World);
    }

    void TrackFocusedRegion()
    {
        // figure out where the region is and rotate the earth so it faces the camera
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 earthPos = SpawnedEarth.transform.position;
        Vector3 regionCenter = focusedRegionManager.GetRegionWorldCenter(focusedRegion);
        Vector3 regionDir = (regionCenter - earthPos).normalized;

        // direction from earth to camera
        Vector3 cameraDir = (cam.transform.position - earthPos).normalized;

        // we want regionDir to point toward cameraDir
        // compute the rotation needed and apply a fraction of it for smooth tracking
        Quaternion needed = Quaternion.FromToRotation(regionDir, cameraDir);

        // slerp toward the target so it doesn't snap instantly
        Quaternion smoothed = Quaternion.Slerp(Quaternion.identity, needed, focusSmoothSpeed * Time.deltaTime);
        SpawnedEarth.transform.rotation = smoothed * SpawnedEarth.transform.rotation;
    }

    // called by RegionSelector when a region is selected
    public void FocusOnRegion(Region region, RegionManager rm)
    {
        if (SpawnedEarth == null || region == null || rm == null) return;

        isFocused = true;
        focusedRegion = region;
        focusedRegionManager = rm;
    }

    public void Unfocus()
    {
        if (!isFocused) return;

        isFocused = false;
        focusedRegion = null;
        focusedRegionManager = null;
    }
}
