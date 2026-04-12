using UnityEngine;

// central settings manager — loads from PlayerPrefs, exposes static properties
// other scripts read from here instead of hardcoded values
public static class SettingsManager
{
    // --- keys ---
    private const string KEY_FPS = "TargetFPS";
    private const string KEY_RESOLUTION_W = "ResolutionW";
    private const string KEY_RESOLUTION_H = "ResolutionH";
    private const string KEY_FULLSCREEN = "FullscreenMode";
    private const string KEY_ORBIT_SENS = "OrbitSensitivity";
    private const string KEY_ZOOM_SENS = "ZoomSensitivity";
    private const string KEY_INVERT_Y = "InvertY";
    private const string KEY_MASTER_VOL = "MasterVolume";
    private const string KEY_MUSIC_VOL = "MusicVolume";
    private const string KEY_SFX_VOL = "SFXVolume";
    private const string KEY_SPIN_SPEED = "SpinSpeed";
    private const string KEY_DEAL_SPEED = "DealSpeed";

    // --- defaults ---
    public const int DEFAULT_FPS = 60;
    public const float DEFAULT_ORBIT_SENS = 1f;
    public const float DEFAULT_ZOOM_SENS = 1f;
    public const bool DEFAULT_INVERT_Y = false;
    public const float DEFAULT_MASTER_VOL = 1f;
    public const float DEFAULT_MUSIC_VOL = 1f;
    public const float DEFAULT_SFX_VOL = 1f;
    public const float DEFAULT_SPIN_SPEED = 1f;
    public const float DEFAULT_DEAL_SPEED = 1f;
    public const int DEFAULT_FULLSCREEN = 1; // FullScreenWindow

    // --- properties ---

    public static int TargetFPS
    {
        get => PlayerPrefs.GetInt(KEY_FPS, DEFAULT_FPS);
        set { PlayerPrefs.SetInt(KEY_FPS, value); PlayerPrefs.Save(); Application.targetFrameRate = value; }
    }

    // resolution stored as width/height pair
    public static int ResolutionW
    {
        get => PlayerPrefs.GetInt(KEY_RESOLUTION_W, Screen.currentResolution.width);
        set { PlayerPrefs.SetInt(KEY_RESOLUTION_W, value); PlayerPrefs.Save(); }
    }

    public static int ResolutionH
    {
        get => PlayerPrefs.GetInt(KEY_RESOLUTION_H, Screen.currentResolution.height);
        set { PlayerPrefs.SetInt(KEY_RESOLUTION_H, value); PlayerPrefs.Save(); }
    }

    // 0 = ExclusiveFullScreen, 1 = FullScreenWindow, 2 = MaximizedWindow, 3 = Windowed
    public static int FullscreenMode
    {
        get => PlayerPrefs.GetInt(KEY_FULLSCREEN, DEFAULT_FULLSCREEN);
        set { PlayerPrefs.SetInt(KEY_FULLSCREEN, value); PlayerPrefs.Save(); }
    }

    // multiplier: 0.25 – 3.0
    public static float OrbitSensitivity
    {
        get => PlayerPrefs.GetFloat(KEY_ORBIT_SENS, DEFAULT_ORBIT_SENS);
        set { PlayerPrefs.SetFloat(KEY_ORBIT_SENS, Mathf.Clamp(value, 0.25f, 3f)); PlayerPrefs.Save(); }
    }

    // multiplier: 0.25 – 3.0
    public static float ZoomSensitivity
    {
        get => PlayerPrefs.GetFloat(KEY_ZOOM_SENS, DEFAULT_ZOOM_SENS);
        set { PlayerPrefs.SetFloat(KEY_ZOOM_SENS, Mathf.Clamp(value, 0.25f, 3f)); PlayerPrefs.Save(); }
    }

    public static bool InvertY
    {
        get => PlayerPrefs.GetInt(KEY_INVERT_Y, DEFAULT_INVERT_Y ? 1 : 0) == 1;
        set { PlayerPrefs.SetInt(KEY_INVERT_Y, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // volume: 0 – 1
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KEY_MASTER_VOL, DEFAULT_MASTER_VOL);
        set { PlayerPrefs.SetFloat(KEY_MASTER_VOL, Mathf.Clamp01(value)); PlayerPrefs.Save(); AudioListener.volume = value; }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KEY_MUSIC_VOL, DEFAULT_MUSIC_VOL);
        set { PlayerPrefs.SetFloat(KEY_MUSIC_VOL, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat(KEY_SFX_VOL, DEFAULT_SFX_VOL);
        set { PlayerPrefs.SetFloat(KEY_SFX_VOL, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    // multiplier: 0.0 – 3.0 (0 = no spin)
    public static float SpinSpeed
    {
        get => PlayerPrefs.GetFloat(KEY_SPIN_SPEED, DEFAULT_SPIN_SPEED);
        set { PlayerPrefs.SetFloat(KEY_SPIN_SPEED, Mathf.Clamp(value, 0f, 3f)); PlayerPrefs.Save(); }
    }

    // multiplier: 0.5 – 2.0 (affects deal animation duration inversely)
    public static float DealSpeed
    {
        get => PlayerPrefs.GetFloat(KEY_DEAL_SPEED, DEFAULT_DEAL_SPEED);
        set { PlayerPrefs.SetFloat(KEY_DEAL_SPEED, Mathf.Clamp(value, 0.5f, 2f)); PlayerPrefs.Save(); }
    }

    // apply all runtime settings (call on scene load)
    public static void ApplyAll()
    {
        Application.targetFrameRate = TargetFPS;
        AudioListener.volume = MasterVolume;

        // apply resolution + fullscreen (desktop only)
        #if !UNITY_ANDROID && !UNITY_IOS
        int w = ResolutionW;
        int h = ResolutionH;
        FullScreenMode mode = (FullScreenMode)FullscreenMode;
        Screen.SetResolution(w, h, mode);
        #endif
    }

    // reset everything to defaults
    public static void ResetToDefaults()
    {
        TargetFPS = DEFAULT_FPS;
        OrbitSensitivity = DEFAULT_ORBIT_SENS;
        ZoomSensitivity = DEFAULT_ZOOM_SENS;
        InvertY = DEFAULT_INVERT_Y;
        MasterVolume = DEFAULT_MASTER_VOL;
        MusicVolume = DEFAULT_MUSIC_VOL;
        SFXVolume = DEFAULT_SFX_VOL;
        SpinSpeed = DEFAULT_SPIN_SPEED;
        DealSpeed = DEFAULT_DEAL_SPEED;
        FullscreenMode = DEFAULT_FULLSCREEN;
        ResolutionW = Screen.currentResolution.width;
        ResolutionH = Screen.currentResolution.height;
        ApplyAll();
    }
}
