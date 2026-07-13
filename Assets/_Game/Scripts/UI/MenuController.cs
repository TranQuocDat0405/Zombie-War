using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private Button level2Button;
        [SerializeField] private TMPro.TextMeshProUGUI level2Label; // "LEVEL 2 - ..." title line
        [SerializeField] private GameObject level2LockIcon; // crossed chains over the locked button

        private void Start()
        {
            if (levelSelectPopup != null) levelSelectPopup.SetActive(false);
            if (settingsPopup != null) settingsPopup.SetActive(false);
            RefreshLevelLocks();
        }

        /// <summary>Applies the persisted unlock state to the level-select buttons.</summary>
        private void RefreshLevelLocks()
        {
            bool level2Open = GameSettings.IsLevelUnlocked(2);
            if (level2Button != null)
            {
                level2Button.interactable = level2Open;
                var img = level2Button.GetComponent<Image>();
                if (img != null)
                {
                    img.color = level2Open
                        ? new Color(0.85f, 0.9f, 1f, 0.95f)
                        : new Color(0.4f, 0.42f, 0.48f, 0.55f); // dimmed while locked
                }
            }
            if (level2Label != null)
            {
                // Locked: the title dims so the chains read as the foreground element.
                level2Label.color = level2Open ? Color.white : new Color(0.62f, 0.64f, 0.68f, 0.55f);
            }
            if (level2LockIcon != null) level2LockIcon.SetActive(!level2Open);
        }

        public void OnPlayPressed()
        {
            Click();
            if (settingsPopup != null) settingsPopup.SetActive(false);
            RefreshLevelLocks();
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
            if (!GameSettings.IsLevelUnlocked(2)) return; // guard against sneaky taps
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
