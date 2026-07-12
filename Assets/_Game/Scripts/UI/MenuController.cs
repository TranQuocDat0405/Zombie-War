using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    /// <summary>HomeMenu flow: Play -> level select popup, Settings popup, Exit.</summary>
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private GameObject levelSelectPopup;
        [SerializeField] private GameObject settingsPopup;
        [SerializeField] private AudioSource clickSource;
        [SerializeField] private AudioClip clickClip;

        private void Start()
        {
            if (levelSelectPopup != null) levelSelectPopup.SetActive(false);
            if (settingsPopup != null) settingsPopup.SetActive(false);
        }

        public void OnPlayPressed()
        {
            Click();
            if (settingsPopup != null) settingsPopup.SetActive(false);
            if (levelSelectPopup != null) levelSelectPopup.SetActive(true);
        }

        public void OnSettingsPressed()
        {
            Click();
            if (levelSelectPopup != null) levelSelectPopup.SetActive(false);
            if (settingsPopup != null) settingsPopup.SetActive(true);
        }

        public void OnClosePopups()
        {
            Click();
            if (levelSelectPopup != null) levelSelectPopup.SetActive(false);
            if (settingsPopup != null) settingsPopup.SetActive(false);
        }

        public void PlayLevel1()
        {
            Click();
            SceneLoader.Load("Level1");
        }

        public void PlayLevel2()
        {
            Click();
            SceneLoader.Load("Level2");
        }

        public void OnExitPressed()
        {
            Click();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
