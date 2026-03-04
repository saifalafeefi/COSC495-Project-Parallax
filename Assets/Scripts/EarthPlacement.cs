using UnityEngine;
using UnityEngine.XR.ARFoundation;

using TMPro;

public class EarthPlacement : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private GameObject earthPrefab;
    [SerializeField] private TMP_Text coachingText;

    public GameObject SpawnedEarth { get; private set; }

    void OnEnable()
    {
        trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        UpdateCoachingText("Point your camera at the reference image");
    }

    void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // Spawn Earth on the first newly detected image
        foreach (var trackedImage in args.added)
        {
            if (SpawnedEarth == null)
            {
                SpawnedEarth = Instantiate(earthPrefab, trackedImage.transform);
                SpawnedEarth.transform.localPosition = Vector3.zero;
                UpdateCoachingText(null);
            }
        }

        // Keep Earth visible only while the image is actively tracked
        foreach (var trackedImage in args.updated)
        {
            if (SpawnedEarth != null && SpawnedEarth.transform.parent == trackedImage.transform)
            {
                SpawnedEarth.SetActive(trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking);
            }
        }
    }

    void UpdateCoachingText(string message)
    {
        if (coachingText == null)
            return;

        if (message == null)
        {
            coachingText.gameObject.SetActive(false);
        }
        else
        {
            coachingText.gameObject.SetActive(true);
            coachingText.text = message;
        }
    }
}
