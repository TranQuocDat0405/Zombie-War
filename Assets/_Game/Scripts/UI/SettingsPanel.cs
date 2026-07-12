using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    /// <summary>
    /// Music + SFX volume sliders backed by GameSettings.
    /// Reused by both the HomeMenu settings popup and the in-game pause menu.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private AudioSource menuMusic;      // optional (HomeMenu music)
        [SerializeField] private float menuMusicBaseVolume = 0.5f;
        [SerializeField] private AudioSource previewSource;  // optional click preview
        [SerializeField] private AudioClip previewClip;

        private float nextPreviewTime;

        private void Awake()
        {
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        private void OnEnable()
        {
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            if (menuMusic != null) menuMusic.volume = menuMusicBaseVolume * GameSettings.MusicVolume;
        }

        private void OnMusicChanged(float value)
        {
            GameSettings.MusicVolume = value;
            if (AudioManager.Instance != null) AudioManager.Instance.RefreshMusicVolume();
            if (menuMusic != null) menuMusic.volume = menuMusicBaseVolume * value;
        }

        private void OnSfxChanged(float value)
        {
            GameSettings.SfxVolume = value;
            // Short preview so the player hears the new level (throttled).
            if (previewSource != null && previewClip != null && Time.unscaledTime >= nextPreviewTime)
            {
                nextPreviewTime = Time.unscaledTime + 0.15f;
                previewSource.PlayOneShot(previewClip, value);
            }
        }
    }
}
