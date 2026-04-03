using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("name of the gameplay scene to load")]
    [SerializeField] private string gameSceneName = "DesktopScene";

    [Header("Earth Spin")]
    [Tooltip("optional — assign the earth object in the menu scene to auto-spin it")]
    [SerializeField] private GameObject earthObject;
    [SerializeField] private float spinSpeed = 8f;

    void Update()
    {
        // spin the decorative earth
        if (earthObject != null)
        {
            float angle = spinSpeed * Time.deltaTime;
            if (Mathf.Abs(angle) > 0.0001f)
                earthObject.transform.Rotate(0f, angle, 0f, Space.World);
        }
    }

    // wire to Start Game button OnClick in Inspector
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // wire to Quit button OnClick in Inspector
    public void QuitGame()
    {
        Debug.Log("[MainMenu] quit");
        Application.Quit();

        // stop play mode in editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
