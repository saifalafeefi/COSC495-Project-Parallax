using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class EarthInteraction : MonoBehaviour
{
    [SerializeField] private EarthPlacement earthPlacement;
    [SerializeField] private float rotationSpeed = 0.3f;
    [SerializeField] private float minScale = 0.4f;
    [SerializeField] private float maxScale = 1.2f;

    private float previousPinchDistance;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        if (earthPlacement == null || earthPlacement.SpawnedEarth == null)
            return;

        GameObject earth = earthPlacement.SpawnedEarth;

        if (Touch.activeTouches.Count == 1)
        {
            HandleRotation(earth);
        }
        else if (Touch.activeTouches.Count == 2)
        {
            HandlePinchScale(earth);
        }
    }

    void HandleRotation(GameObject earth)
    {
        var touch = Touch.activeTouches[0];

        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            float deltaX = touch.delta.x;
            earth.transform.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);
        }
    }

    void HandlePinchScale(GameObject earth)
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
            Vector3 newScale = earth.transform.localScale * scaleFactor;

            float clampedUniform = Mathf.Clamp(newScale.x, minScale, maxScale);
            earth.transform.localScale = Vector3.one * clampedUniform;

            previousPinchDistance = currentDistance;
        }
    }
}
