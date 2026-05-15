using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameBgmType
{
    None,
    Title,
    WorldMap,
    Battle
}

[DisallowMultipleComponent]
public class GameAudioManager : MonoBehaviour
{
    private static GameAudioManager instance;
    public static GameAudioManager Instance => instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Volumes")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float uiVolume = 1f;

    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonHoverSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private bool autoBindButtons = true;

    [Header("BGM")]
    [SerializeField] private AudioClip bootstrapBgm;
    [SerializeField] private AudioClip worldMapBgm;
    [SerializeField] private AudioClip battleBgm;
    [SerializeField] private float bgmFadeSeconds = 1f;

    [Header("Scene Name Matching")]
    [SerializeField] private string[] titleSceneKeywords = { "bootstrap", "title", "lobby", "mainmenu", "menu" };
    [SerializeField] private string[] worldMapSceneKeywords = { "worldmap", "world", "map" };
    [SerializeField] private string[] battleSceneKeywords = { "battle", "combat" };

    [Header("Debug")]
    [SerializeField] private bool logMissingBgmClip = true;

    private Coroutine bgmRoutine;
    private AudioClip currentBgm;
    private GameBgmType currentBgmType = GameBgmType.None;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.AbsorbSettingsFrom(this);
            instance.PlayBgmForScene(SceneManager.GetActiveScene().name);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        PlayBgmForScene(SceneManager.GetActiveScene().name);
        if (autoBindButtons)
            UIButtonSoundEmitter.BindAllButtonsInScene();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBgmForScene(scene.name);
        if (autoBindButtons)
            StartCoroutine(BindButtonsNextFrame());
    }

    private IEnumerator BindButtonsNextFrame()
    {
        yield return null;
        UIButtonSoundEmitter.BindAllButtonsInScene();
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        if (uiSource == null)
            uiSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
        uiSource.playOnAwake = false;
        RefreshVolumes();
    }

    private void RefreshVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = masterVolume * bgmVolume;
        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
        if (uiSource != null)
            uiSource.volume = masterVolume * uiVolume;
    }

    private void AbsorbSettingsFrom(GameAudioManager other)
    {
        if (other == null)
            return;

        if (bootstrapBgm == null && other.bootstrapBgm != null)
            bootstrapBgm = other.bootstrapBgm;
        if (worldMapBgm == null && other.worldMapBgm != null)
            worldMapBgm = other.worldMapBgm;
        if (battleBgm == null && other.battleBgm != null)
            battleBgm = other.battleBgm;
        if (buttonHoverSfx == null && other.buttonHoverSfx != null)
            buttonHoverSfx = other.buttonHoverSfx;
        if (buttonClickSfx == null && other.buttonClickSfx != null)
            buttonClickSfx = other.buttonClickSfx;

        masterVolume = Mathf.Clamp01(other.masterVolume > 0f ? other.masterVolume : masterVolume);
        bgmVolume = Mathf.Clamp01(other.bgmVolume > 0f ? other.bgmVolume : bgmVolume);
        sfxVolume = Mathf.Clamp01(other.sfxVolume > 0f ? other.sfxVolume : sfxVolume);
        uiVolume = Mathf.Clamp01(other.uiVolume > 0f ? other.uiVolume : uiVolume);
        bgmFadeSeconds = Mathf.Max(0f, other.bgmFadeSeconds);
        autoBindButtons = autoBindButtons || other.autoBindButtons;
        RefreshVolumes();
    }

    private void PlayBgmForScene(string sceneName)
    {
        PlayBgm(ResolveBgmType(sceneName));
    }

    public void PlayBgm(GameBgmType type)
    {
        AudioClip clip = ResolveBgm(type);
        if (clip == currentBgm && type == currentBgmType && bgmSource != null && bgmSource.isPlaying)
            return;

        if (clip == null && type != GameBgmType.None && logMissingBgmClip)
            Debug.LogWarning($"[GameAudioManager] {type} BGM clip is not assigned. BGM will be silent.", this);

        if (bgmRoutine != null)
            StopCoroutine(bgmRoutine);
        bgmRoutine = StartCoroutine(SwitchBgmRoutine(type, clip));
    }

    private GameBgmType ResolveBgmType(string sceneName)
    {
        string key = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.ToLowerInvariant();
        if (MatchesAny(key, battleSceneKeywords))
            return GameBgmType.Battle;
        if (MatchesAny(key, worldMapSceneKeywords))
            return GameBgmType.WorldMap;
        if (MatchesAny(key, titleSceneKeywords))
            return GameBgmType.Title;
        return GameBgmType.None;
    }

    private static bool MatchesAny(string key, string[] keywords)
    {
        if (string.IsNullOrEmpty(key) || keywords == null)
            return false;

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (!string.IsNullOrWhiteSpace(keyword) && key.Contains(keyword.Trim().ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private AudioClip ResolveBgm(GameBgmType type)
    {
        switch (type)
        {
            case GameBgmType.Title: return bootstrapBgm;
            case GameBgmType.WorldMap: return worldMapBgm;
            case GameBgmType.Battle: return battleBgm;
            case GameBgmType.None:
            default: return null;
        }
    }

    private IEnumerator SwitchBgmRoutine(GameBgmType nextType, AudioClip next)
    {
        EnsureSources();
        float targetVolume = masterVolume * bgmVolume;
        float fade = Mathf.Max(0f, bgmFadeSeconds);

        if (bgmSource.isPlaying && fade > 0f)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fade));
                yield return null;
            }
        }

        currentBgmType = nextType;
        currentBgm = next;
        bgmSource.clip = next;

        if (next == null)
        {
            bgmSource.Stop();
            bgmSource.volume = targetVolume;
            yield break;
        }

        bgmSource.volume = fade > 0f ? 0f : targetVolume;
        bgmSource.Play();

        if (fade > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, Mathf.Clamp01(elapsed / fade));
                yield return null;
            }
        }

        bgmSource.volume = targetVolume;
    }

    private static GameAudioManager EnsureInstanceForSfx()
    {
        if (instance != null)
            return instance;

        GameObject go = new GameObject("GameAudioManager_Runtime");
        GameAudioManager manager = go.AddComponent<GameAudioManager>();
        manager.EnsureSources();
        return manager;
    }

    public static void PlayTitleBgm()
    {
        if (instance != null)
            instance.PlayBgm(GameBgmType.Title);
    }

    public static void PlayWorldMapBgm()
    {
        if (instance != null)
            instance.PlayBgm(GameBgmType.WorldMap);
    }

    public static void PlayBattleBgm()
    {
        if (instance != null)
            instance.PlayBgm(GameBgmType.Battle);
    }

    public static void StopBgm()
    {
        if (instance != null)
            instance.PlayBgm(GameBgmType.None);
    }

    public static void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        GameAudioManager manager = EnsureInstanceForSfx();
        if (manager == null)
            return;

        manager.EnsureSources();
        manager.sfxSource.PlayOneShot(clip, manager.masterVolume * manager.sfxVolume);
    }

    public static void PlayButtonHover()
    {
        if (instance == null || instance.buttonHoverSfx == null)
            return;
        instance.EnsureSources();
        instance.uiSource.PlayOneShot(instance.buttonHoverSfx, instance.masterVolume * instance.uiVolume);
    }

    public static void PlayButtonClick()
    {
        if (instance == null || instance.buttonClickSfx == null)
            return;
        instance.EnsureSources();
        instance.uiSource.PlayOneShot(instance.buttonClickSfx, instance.masterVolume * instance.uiVolume);
    }
}
