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
        [SerializeField] private GameObject nextLevelButton;
        [SerializeField] private string nextSceneName = "Level2";

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
            if (nextLevelButton != null)
            {
                bool hasNext = won && !string.IsNullOrEmpty(nextSceneName)
                    && Application.CanStreamedLevelBeLoaded(nextSceneName);
                nextLevelButton.SetActive(hasNext);
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
    }
}
