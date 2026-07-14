using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    /// <summary>
    /// First-run "HOW TO PLAY" (ported from TutorialPanel). Whether it shows is
    /// decided by LevelManager.Begin(); this popup only handles GOT IT.
    /// </summary>
    public class TutorialPopup : Popup
    {
        public void OnGotItPressed()
        {
            UserData.I.HasSeenTutorial = true;
            Time.timeScale = 1f;
            CloseSelf();
            LevelManager.Instance.Begin();
        }

        // The player must acknowledge the tutorial — back key doesn't dismiss it.
        public override void HandleOnKeyBack() { }
    }
}
