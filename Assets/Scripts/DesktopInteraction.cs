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

    private float currentDistance;
    private float yaw;
    private float pitch;

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

        if (currentDistance == 0f)
            InitOrbit();

        HandleOrbit();
        HandleZoom();
        ApplyOrbit();
    }

    void InitOrbit()
    {
        Vector3 offset = mainCamera.transform.position - desktopPlacement.SpawnedEarth.transform.position;
        currentDistance = offset.magnitude;
        yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(Mathf.Clamp(offset.y / currentDistance, -1f, 1f)) * Mathf.Rad2Deg;
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
}
