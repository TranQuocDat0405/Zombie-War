using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    /// <summary>
    /// In-game pause menu: freezes the game (timeScale 0) and offers
    /// Resume / volume settings / Restart / Home.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private AudioSource clickSource;
        [SerializeField] private AudioClip clickClip;

        private bool paused;
        private float previousTimeScale = 1f;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void OnPauseButtonPressed()
        {
            if (paused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (paused) return;
            // After win/lose the ResultPanel owns the screen — don't stack menus.
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            paused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
            Click();
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = previousTimeScale;
            if (panel != null) panel.SetActive(false);
            Click();
        }

        public void OnRestartPressed()
        {
            Click();
            paused = false;
            SceneLoader.Load(SceneManager.GetActiveScene().name); // loader resets timeScale
        }

        public void OnHomePressed()
        {
            Click();
            paused = false;
            SceneLoader.Load("HomeMenu");
        }

        private void Click()
        {
            if (clickSource != null && clickClip != null)
            {
                clickSource.PlayOneShot(clickClip, GameSettings.SfxVolume);
            }
        }
    }
}
