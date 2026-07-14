using NFramework;
using UnityEngine;

namespace ZombieWar.UI
{
    /// <summary>Home screen (replaces the old MenuController + HomeMenu scene).</summary>
    public class HomeMenu : BaseUIView
    {
        public void OnPlayPressed()
        {
            Click();
            UIManager.I.Open(Define.UIName.LEVEL_SELECT_POPUP);
        }

        public void OnSettingsPressed()
        {
            Click();
            UIManager.I.Open(Define.UIName.SETTINGS_POPUP);
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

        private void Click() => SoundManager.I.PlaySFXResource(Define.SoundName.CLICK_BUTTON);
    }
}
