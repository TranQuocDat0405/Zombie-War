using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    /// <summary>
    /// Pause overlay: freezes the match, offers Resume/Restart/Home plus the same
    /// in-place volume sliders the old pause panel had.
    /// </summary>
    public class PausePopup : Popup
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private float _previousTimeScale = 1f;
        private float _nextPreviewTime;

        protected override void Awake()
        {
            base.Awake();
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(v => SoundManager.I.MusicVolume = v);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        private void OnSfxChanged(float value)
        {
            SoundManager.I.SFXVolume = value;
            if (Time.unscaledTime >= _nextPreviewTime)
            {
                _nextPreviewTime = Time.unscaledTime + 0.15f;
                SoundManager.I.PlaySFXResource(Define.SoundName.CLICK_BUTTON);
            }
        }

        public override void OnOpen()
        {
            base.OnOpen();
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(SoundManager.I.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SoundManager.I.SFXVolume);
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        public override void OnClose()
        {
            base.OnClose();
            Time.timeScale = _previousTimeScale; // Resume == CloseSelf
        }

        public void OnResumePressed() => CloseSelf();

        public void OnRestartPressed()
        {
            CloseSelf();
            GameManager.I.EnterReset();
        }

        public void OnHomePressed()
        {
            CloseSelf();
            GameManager.I.EnterHome();
        }
    }
}
