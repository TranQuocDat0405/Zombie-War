using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    /// <summary>Win/Lose overlay with restart / next-level buttons.</summary>
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text statsText;
        [SerializeField] private Text unlockText; // "LEVEL 2 UNLOCKED!" — first win only
        [SerializeField] private GameObject nextLevelButton;
        [SerializeField] private RectTransform restartButtonRect;
        [SerializeField] private string nextSceneName = "Level2";
        [SerializeField] private AudioSource stingerSource;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;
        [SerializeField] private float stingerVolumeBoost = 2.6f; // stand out over gunfire/zombie sfx

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Show(bool won)
        {
            gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = won ? "VICTORY!" : "YOU DIED";
                titleText.color = won ? new Color(0.4f, 1f, 0.45f) : new Color(1f, 0.35f, 0.3f);
            }
            if (statsText != null && GameManager.Instance != null)
            {
                statsText.text = "Zombies killed: " + GameManager.Instance.Kills;
            }
            if (unlockText != null)
            {
                int justUnlocked = GameManager.Instance != null ? GameManager.Instance.JustUnlockedLevel : 0;
                bool showUnlock = won && justUnlocked > 0;
                unlockText.gameObject.SetActive(showUnlock);
                if (showUnlock) unlockText.text = "LEVEL " + justUnlocked + " UNLOCKED!";
            }
            bool hasNext = won && nextLevelButton != null && !string.IsNullOrEmpty(nextSceneName)
                && Application.CanStreamedLevelBeLoaded(nextSceneName);
            if (nextLevelButton != null) nextLevelButton.SetActive(hasNext);

            // Restart shares a row with Next; with Next hidden it must re-center.
            if (restartButtonRect != null)
            {
                Vector2 p = restartButtonRect.anchoredPosition;
                restartButtonRect.anchoredPosition = new Vector2(hasNext ? -150f : 0f, p.y);
            }

            if (stingerSource != null)
            {
                var clip = won ? winClip : loseClip;
                if (clip != null) stingerSource.PlayOneShot(clip, GameSettings.SfxVolume * stingerVolumeBoost);
            }
        }

        public void OnRestartPressed()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        public void OnNextLevelPressed()
        {
            if (GameManager.Instance != null) GameManager.Instance.LoadScene(nextSceneName);
        }

        public void OnHomePressed()
        {
            if (SceneLoader.Instance != null) SceneLoader.Load("HomeMenu");
            else SceneManager.LoadScene("HomeMenu");
        }
    }
}
