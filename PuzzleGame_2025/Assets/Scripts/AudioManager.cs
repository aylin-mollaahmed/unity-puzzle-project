using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Clips")]
    public AudioClip bgm;
    public AudioClip click;
    public AudioClip place;
    public AudioClip win;

    [Header("Volumes")]
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    // PlayerPrefs keys
    private const string PREF_BGM_VOL = "BGM_VOL";
    private const string PREF_SFX_VOL = "SFX_VOL";
    private const string PREF_MUTE_BGM = "MUTE_BGM";
    private const string PREF_MUTE_SFX = "MUTE_SFX";

    private bool muteBgm = false;
    private bool muteSfx = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        LoadAudioSettings();
        ApplyVolumes();
    }

    private void Start()
    {
        PlayBGM();
    }

    // ---------- Play methods ----------
    public void PlayBGM()
    {
        if (bgm == null) return;

        if (bgmSource.clip != bgm)
            bgmSource.clip = bgm;

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }

    public void PlayClick() => PlaySFX(click);
    public void PlayPlace() => PlaySFX(place);
    public void PlayWin() => PlaySFX(win);

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (sfxSource == null) return;

        // ако SFX е mute-нат, не пускаме
        if (muteSfx) return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ---------- Getters ----------
    public float GetBgmVolume() => bgmVolume;
    public float GetSfxVolume() => sfxVolume;
    public bool GetMuteBgm() => muteBgm;
    public bool GetMuteSfx() => muteSfx;

    // ---------- Setters ----------
    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        SaveAudioSettings();
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        SaveAudioSettings();
    }

    public void SetMuteBgm(bool mute)
    {
        muteBgm = mute;
        ApplyVolumes();
        SaveAudioSettings();
    }

    public void SetMuteSfx(bool mute)
    {
        muteSfx = mute;
        ApplyVolumes();
        SaveAudioSettings();
    }

    // ---------- Internal ----------
    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.mute = muteBgm;
            bgmSource.volume = muteBgm ? 0f : bgmVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.mute = muteSfx;
            sfxSource.volume = muteSfx ? 0f : sfxVolume;
        }
    }

    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat(PREF_BGM_VOL, bgmVolume);
        PlayerPrefs.SetFloat(PREF_SFX_VOL, sfxVolume);
        PlayerPrefs.SetInt(PREF_MUTE_BGM, muteBgm ? 1 : 0);
        PlayerPrefs.SetInt(PREF_MUTE_SFX, muteSfx ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadAudioSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOL, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, sfxVolume);
        muteBgm = PlayerPrefs.GetInt(PREF_MUTE_BGM, 0) == 1;
        muteSfx = PlayerPrefs.GetInt(PREF_MUTE_SFX, 0) == 1;
    }
}
