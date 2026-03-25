using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopInteraction : MonoBehaviour
{
    [SerializeField] private DesktopPlacement desktopPlacement;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float orbitSpeed = 5f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 10f;

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

    public bool IsFocused => isFocused;

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

        // don't allow orbit/zoom while paused or reward popup is showing
        if (PauseMenu.IsPaused)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null && gm.RewardActive)
            return;

        if (currentDistance == 0f)
            InitOrbit();

        if (isFocused)
        {
            // smoothly tween toward the region
            yaw = Mathf.LerpAngle(yaw, targetYaw, focusSmoothSpeed * Time.deltaTime);
            pitch = Mathf.Lerp(pitch, targetPitch, focusSmoothSpeed * Time.deltaTime);
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, focusSmoothSpeed * Time.deltaTime);

            // let the player orbit slowly while focused, with deadzone limit
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                float baseSpeed = orbitSpeed * focusedOrbitMultiplier;

                // how far we currently are from region center
                float dYaw = Mathf.DeltaAngle(yaw, focusCenterYaw);
                float dPitch = pitch - focusCenterPitch;
                float angleDist = Mathf.Sqrt(dYaw * dYaw + dPitch * dPitch);

                // apply the drag at full speed first
                float newYaw = yaw + delta.x * baseSpeed * 0.1f;
                float newPitch = pitch - delta.y * baseSpeed * 0.1f;
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
                    newPitch = pitch - delta.y * baseSpeed * edgeFactor * 0.1f;
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
            // let the player orbit while zooming out
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * orbitSpeed * 0.1f;
                pitch -= delta.y * orbitSpeed * 0.1f;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
            }

            HandleZoom();

            // smoothly pull distance back, keep going even if player is orbiting
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, focusSmoothSpeed * Time.deltaTime);

            if (Mathf.Abs(currentDistance - targetDistance) < 0.05f)
            {
                isReturning = false;
                currentDistance = targetDistance;
            }
        }
        else
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
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.isPressed)
            return;

        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * orbitSpeed * 0.1f;
        pitch -= delta.y * orbitSpeed * 0.1f;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        targetYaw = yaw;
        targetPitch = pitch;
        targetDistance = currentDistance;
    }

    void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed * 0.01f;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            targetDistance = currentDistance;
        }
    }

    void ApplyOrbit()
    {
        Vector3 target = desktopPlacement.SpawnedEarth.transform.position;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);

        mainCamera.transform.position = target + offset;
        mainCamera.transform.LookAt(target);
    }

    // called by RegionSelector when a region is right-clicked
    // dir should be the direction from earth center to the region surface
    public void FocusOnDirection(Vector3 dir)
    {
        if (desktopPlacement == null || desktopPlacement.SpawnedEarth == null) return;

        // save the distance so we can pull back to it on unfocus
        if (!isFocused)
            unfocusedDistance = currentDistance;

        isFocused = true;
        isReturning = false;

        // the orbit offset is Euler(pitch,yaw) * (0,0,-dist), so we need to
        // flip the direction to place the camera on the region's side of the earth
        focusCenterYaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
        focusCenterPitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        targetYaw = focusCenterYaw;
        targetPitch = focusCenterPitch;
        targetDistance = focusDistance;
    }

    public void Unfocus()
    {
        if (!isFocused) return;

        isFocused = false;
        isReturning = true;

        // stay facing the same direction, just pull back to the old distance
        targetDistance = unfocusedDistance;
        targetYaw = yaw;
        targetPitch = pitch;
    }
}
