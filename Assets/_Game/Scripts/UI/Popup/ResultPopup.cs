using NFramework;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    /// <summary>Win/Lose overlay (ported from ResultPanel — same layout rules and stinger flow).</summary>
    public class ResultPopup : Popup
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text statsText;
        [SerializeField] private Text unlockText; // "LEVEL 2 UNLOCKED!" — first win only
        [SerializeField] private GameObject nextLevelButton;
        [SerializeField] private RectTransform restartButtonRect;

        public void Show(bool won)
        {
            var level = LevelManager.Instance;

            if (titleText != null)
            {
                titleText.text = won ? "VICTORY!" : "YOU DIED";
                titleText.color = won ? new Color(0.4f, 1f, 0.45f) : new Color(1f, 0.35f, 0.3f);
            }
            if (statsText != null && level != null)
            {
                statsText.text = "Zombies killed: " + level.Kills;
            }
            if (unlockText != null)
            {
                int justUnlocked = level != null ? level.JustUnlockedLevel : 0;
                bool showUnlock = won && justUnlocked > 0;
                unlockText.gameObject.SetActive(showUnlock);
                if (showUnlock) unlockText.text = "LEVEL " + justUnlocked + " UNLOCKED!";
            }

            bool hasNext = won && level != null && level.LevelNumber < UserData.MaxLevel;
            if (nextLevelButton != null) nextLevelButton.SetActive(hasNext);

            // Restart shares a row with Next; with Next hidden it must re-center.
            if (restartButtonRect != null)
            {
                Vector2 p = restartButtonRect.anchoredPosition;
                restartButtonRect.anchoredPosition = new Vector2(hasNext ? -150f : 0f, p.y);
            }

            SoundManager.I.PlaySFXResource(won ? Define.SoundName.WIN : Define.SoundName.LOSE);
        }

        // Dismissing the result screen would strand the match in slow motion —
        // the player must pick Restart / Next / Home.
        public override void HandleOnKeyBack() { }

        public void OnRestartPressed() => GameManager.I.EnterReset();

        public void OnNextLevelPressed() => GameManager.I.EnterNextLevel();

        public void OnHomePressed() => GameManager.I.EnterHome();
    }
}
