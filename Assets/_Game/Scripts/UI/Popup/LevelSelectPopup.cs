using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    /// <summary>Level chooser with the Level-2 lock (ported from MenuController).</summary>
    public class LevelSelectPopup : Popup
    {
        [SerializeField] private Button level2Button;
        [SerializeField] private TMPro.TextMeshProUGUI level2Label;
        [SerializeField] private GameObject level2LockIcon; // crossed chains while locked

        public override void OnOpen()
        {
            base.OnOpen();
            RefreshLevelLocks();
        }

        private void RefreshLevelLocks()
        {
            bool level2Open = UserData.I.IsLevelUnlocked(2);
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
                level2Label.color = level2Open ? Color.white : new Color(0.62f, 0.64f, 0.68f, 0.55f);
            }
            if (level2LockIcon != null) level2LockIcon.SetActive(!level2Open);
        }

        public void PlayLevel1() => PlayLevel(1);

        public void PlayLevel2()
        {
            if (!UserData.I.IsLevelUnlocked(2)) return; // guard against sneaky taps
            PlayLevel(2);
        }

        private void PlayLevel(int level)
        {
            SoundManager.I.PlaySFXResource(Define.SoundName.CLICK_BUTTON);
            CloseSelf();
            UIManager.I.Close(Define.UIName.HOME_MENU);
            GameManager.I.EnterInGame(level);
        }
    }
}
