using UnityEngine;

public class DesktopPlacement : MonoBehaviour
{
    [SerializeField] private GameObject earthPrefab;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, 3f);

    public GameObject SpawnedEarth { get; private set; }

    void Start()
    {
        if (earthPrefab != null)
        {
            SpawnedEarth = Instantiate(earthPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
