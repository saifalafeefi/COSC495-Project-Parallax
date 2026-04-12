using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopPlacement : MonoBehaviour
{
    [SerializeField] private GameObject earthPrefab;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, 3f);

    [Header("Auto Rotation")]
    [Tooltip("how fast the earth spins on its own (degrees per second)")]
    [SerializeField] private float autoRotateSpeed = 5f;

    [Tooltip("seconds to wait after releasing the mouse before spin resumes")]
    [SerializeField] private float resumeDelay = 1f;

    [Tooltip("how many seconds it takes to ramp back up to full spin speed")]
    [SerializeField] private float resumeRampTime = 2f;

    public GameObject SpawnedEarth { get; private set; }

    // tracks how long since the player let go of the mouse
    private float timeSinceRelease;
    private bool playerControlling;
    private DesktopInteraction cachedInteraction;

    void Start()
    {
        if (earthPrefab != null)
        {
            SpawnedEarth = Instantiate(earthPrefab, spawnPosition, earthPrefab.transform.rotation);
        }

        timeSinceRelease = resumeDelay + resumeRampTime;
    }

    void Update()
    {
        if (SpawnedEarth == null) return;

        if (cachedInteraction == null)
            cachedInteraction = FindFirstObjectByType<DesktopInteraction>();

        // when focused on a region, keep spinning at full speed regardless of mouse
        bool focused = cachedInteraction != null && cachedInteraction.IsFocused;

        if (!focused)
        {
            // check if the player is dragging the mouse
            bool mouseDown = Mouse.current != null && Mouse.current.leftButton.isPressed;

            if (mouseDown)
            {
                playerControlling = true;
                timeSinceRelease = 0f;
                return;
            }

            if (playerControlling)
            {
                // player just released the mouse
                playerControlling = false;
                timeSinceRelease = 0f;
            }

            timeSinceRelease += Time.deltaTime;

            // wait for the delay before starting to spin again
            if (timeSinceRelease < resumeDelay) return;
        }

        // gradually ramp up to full speed (instant when focused)
        float rampProgress = focused ? 1f : Mathf.Clamp01((timeSinceRelease - resumeDelay) / resumeRampTime);
        float currentSpeed = autoRotateSpeed * rampProgress * SettingsManager.SpinSpeed;

        float angle = currentSpeed * Time.deltaTime;
        if (Mathf.Abs(angle) > 0.0001f)
            SpawnedEarth.transform.Rotate(0f, angle, 0f, Space.World);
    }
}
