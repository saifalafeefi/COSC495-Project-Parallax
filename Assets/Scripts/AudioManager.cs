using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// centralized audio manager — singleton on a GameObject in MainMenuScene
// survives scene loads via DontDestroyOnLoad
// all clips assigned in Inspector, other scripts call PlaySFX / PlayMusic
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─── UI General ─────────────────────────────────────────────
    [Header("UI General")]
    public AudioClip buttonClick;
    public AudioClip buttonHover;
    public AudioClip panelOpen;
    public AudioClip panelClose;
    public AudioClip sliderTick;

    // ─── Cards ──────────────────────────────────────────────────
    [Header("Cards")]
    public AudioClip cardDeal;
    public AudioClip cardHover;
    public AudioClip cardSelect;
    public AudioClip cardDeselect;
    public AudioClip cardPlay;
    public AudioClip cardReject;

    // ─── Region ─────────────────────────────────────────────────
    [Header("Region")]
    public AudioClip regionHover;
    public AudioClip regionSelect;
    public AudioClip regionDeselect;
    public AudioClip regionFocus;
    public AudioClip regionUnfocus;

    // ─── Shop ───────────────────────────────────────────────────
    [Header("Shop")]
    public AudioClip shopOpen;
    public AudioClip shopClose;
    public AudioClip shopPurchase;
    public AudioClip shopPurchaseFail;
    public AudioClip shopkeeperGreet;

    // ─── Reward ─────────────────────────────────────────────────
    [Header("Reward")]
    public AudioClip rewardPopup;
    public AudioClip rewardClaim;
    public AudioClip rewardSkip;

    // ─── Dashboard ──────────────────────────────────────────────
    [Header("Dashboard")]
    public AudioClip dashboardOpen;
    public AudioClip dashboardClose;
    public AudioClip dashboardSort;

    // ─── Events & Banners ───────────────────────────────────────
    [Header("Events & Banners")]
    public AudioClip bannerSlideIn;
    public AudioClip bannerSlideOut;
    public AudioClip eventPositive;
    public AudioClip eventNegative;

    // ─── Game Flow ──────────────────────────────────────────────
    [Header("Game Flow")]
    public AudioClip roundStart;
    public AudioClip roundEnd;
    public AudioClip gameOverLoss;
    public AudioClip gameOverVictory;
    public AudioClip skipRound;
    public AudioClip skipPenalty;

    // ─── Status ─────────────────────────────────────────────────
    [Header("Status")]
    public AudioClip regionStressed;
    public AudioClip regionCrisis;
    public AudioClip focusWarning;
    public AudioClip focusPunishment;
    public AudioClip neglectPenalty;

    // ─── Ambient / Feedback ─────────────────────────────────────
    [Header("Ambient / Feedback")]
    public AudioClip capitalGain;
    public AudioClip fundsGain;
    public AudioClip scoreReveal;

    // ─── Music ──────────────────────────────────────────────────
    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip gameOverMusic;

    [Header("Music Settings")]
    [Tooltip("how long music crossfades take in seconds")]
    [SerializeField] private float crossfadeDuration = 1f;

    // audio sources created in Awake — no Inspector wiring needed
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource sfxSource;
    private AudioSource activeMusicSource;
    private bool isCrossfading;

    void Awake()
    {
        // singleton — destroy duplicate when returning to MainMenuScene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // create audio sources programmatically
        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceA.loop = true;
        musicSourceA.playOnAwake = false;

        musicSourceB = gameObject.AddComponent<AudioSource>();
        musicSourceB.loop = true;
        musicSourceB.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        activeMusicSource = musicSourceA;

        // start menu music immediately
        if (menuMusic != null)
        {
            activeMusicSource.clip = menuMusic;
            activeMusicSource.volume = SettingsManager.MusicVolume;
            activeMusicSource.Play();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string name = scene.name;
        if (name.Contains("MainMenu"))
            PlayMusic(menuMusic);
        else if (name.Contains("Desktop") || name.Contains("AR"))
            PlayMusic(gameplayMusic);
    }

    void Update()
    {
        // keep music volume in sync with settings slider
        if (!isCrossfading)
        {
            float musicVol = SettingsManager.MusicVolume;
            if (activeMusicSource != null)
                activeMusicSource.volume = musicVol;
        }
    }

    // ─── Public API ─────────────────────────────────────────────

    /// <summary>
    /// play a one-shot sfx clip — overlapping is fine
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, SettingsManager.SFXVolume);
    }

    /// <summary>
    /// play a one-shot sfx clip with custom volume scale (0-1, multiplied by SFX setting)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volumeScale * SettingsManager.SFXVolume);
    }

    /// <summary>
    /// crossfade to a new music track — skips if same clip is already playing
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        // don't restart the same track
        if (activeMusicSource.clip == clip && activeMusicSource.isPlaying)
            return;

        // cancel any in-progress crossfade
        if (isCrossfading)
        {
            StopAllCoroutines();
            isCrossfading = false;
            // snap: silence the non-active source
            AudioSource inactive = (activeMusicSource == musicSourceA) ? musicSourceB : musicSourceA;
            inactive.Stop();
            inactive.volume = 0f;
        }

        StartCoroutine(CrossfadeMusic(clip));
    }

    /// <summary>
    /// stop all music immediately
    /// </summary>
    public void StopMusic()
    {
        StopAllCoroutines();
        isCrossfading = false;
        musicSourceA.Stop();
        musicSourceB.Stop();
    }

    /// <summary>
    /// fade out current music over duration seconds — after fade, source is stopped
    /// </summary>
    public void FadeOutMusic(float duration = -1f)
    {
        if (duration < 0f) duration = crossfadeDuration;
        StopAllCoroutines();
        isCrossfading = false;
        StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        isCrossfading = true;
        float startVolume = activeMusicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            activeMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        activeMusicSource.Stop();
        activeMusicSource.volume = 0f;
        isCrossfading = false;
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        isCrossfading = true;

        AudioSource oldSource = activeMusicSource;
        AudioSource newSource = (activeMusicSource == musicSourceA) ? musicSourceB : musicSourceA;

        newSource.clip = newClip;
        newSource.volume = 0f;
        newSource.Play();

        float musicVol = SettingsManager.MusicVolume;
        float startVolume = oldSource.volume;
        float elapsed = 0f;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);

            // read live volume so slider changes during crossfade feel responsive
            musicVol = SettingsManager.MusicVolume;
            oldSource.volume = Mathf.Lerp(startVolume, 0f, t);
            newSource.volume = Mathf.Lerp(0f, musicVol, t);
            yield return null;
        }

        oldSource.Stop();
        oldSource.volume = 0f;
        newSource.volume = SettingsManager.MusicVolume;
        activeMusicSource = newSource;
        isCrossfading = false;
    }
}
