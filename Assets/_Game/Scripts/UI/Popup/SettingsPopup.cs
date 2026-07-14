using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    /// <summary>
    /// Music/SFX sliders bound straight to NFramework SoundManager (which persists
    /// them itself). Shared by the home menu and the pause popup.
    /// </summary>
    public class SettingsPopup : Popup
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private float nextPreviewTime;

        protected override void Awake()
        {
            base.Awake();
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        public override void OnOpen()
        {
            base.OnOpen();
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(SoundManager.I.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SoundManager.I.SFXVolume);
        }

        private void OnMusicChanged(float value)
        {
            SoundManager.I.MusicVolume = value; // hits the mixer + persists automatically
        }

        private void OnSfxChanged(float value)
        {
            SoundManager.I.SFXVolume = value;
            // Short preview so the player hears the new level (throttled).
            if (Time.unscaledTime >= nextPreviewTime)
            {
                nextPreviewTime = Time.unscaledTime + 0.15f;
                SoundManager.I.PlaySFXResource(Define.SoundName.CLICK_BUTTON);
            }
        }
    }
}
