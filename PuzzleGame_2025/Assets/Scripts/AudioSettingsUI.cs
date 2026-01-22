using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Toggles")]
    public Toggle muteMusicToggle;
    public Toggle muteSfxToggle;

    private void Start()
    {
        // 1) Попълваме UI със стойностите от AudioManager
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetBgmVolume());

            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSfxVolume());

            if (muteMusicToggle != null)
                muteMusicToggle.SetIsOnWithoutNotify(AudioManager.Instance.GetMuteBgm());

            if (muteSfxToggle != null)
                muteSfxToggle.SetIsOnWithoutNotify(AudioManager.Instance.GetMuteSfx());
        }

        // 2) Връзваме UI -> AudioManager
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetBgmVolume(v));

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));

        if (muteMusicToggle != null)
            muteMusicToggle.onValueChanged.AddListener(isOn => AudioManager.Instance?.SetMuteBgm(isOn));

        if (muteSfxToggle != null)
            muteSfxToggle.onValueChanged.AddListener(isOn => AudioManager.Instance?.SetMuteSfx(isOn));
    }
}
