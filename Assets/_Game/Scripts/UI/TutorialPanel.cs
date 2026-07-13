using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    /// <summary>
    /// First-time "How to play" panel: freezes the game until the player
    /// presses GOT IT. Shown only once (PlayerPrefs-backed), Level 1 only.
    /// </summary>
    public class TutorialPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject pauseButton; // hidden while tutorial is open
        [SerializeField] private AudioSource clickSource;
        [SerializeField] private AudioClip clickClip;

        private bool showing;

        private void Start()
        {
            if (GameSettings.HasSeenTutorial)
            {
                if (panel != null) panel.SetActive(false);
                return;
            }

            showing = true;
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
            if (pauseButton != null) pauseButton.SetActive(false);
        }

        public void OnGotItPressed()
        {
            if (!showing) return;
            showing = false;
            GameSettings.HasSeenTutorial = true;
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
            if (pauseButton != null) pauseButton.SetActive(true);
            if (clickSource != null && clickClip != null)
            {
                clickSource.PlayOneShot(clickClip, GameSettings.SfxVolume);
            }
        }
    }
}
