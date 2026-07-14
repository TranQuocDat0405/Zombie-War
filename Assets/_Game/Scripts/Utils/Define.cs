namespace ZombieWar
{
    /// <summary>
    /// Every string identifier in one place. A renamed prefab or scene fails at
    /// compile-time autocomplete instead of silently at runtime.
    /// </summary>
    public static class Define
    {
        public static class UIName
        {
            public const string HOME_MENU          = "HomeMenu";
            public const string GAMEPLAY_MENU      = "GamePlayMenu";
            public const string LOADING_POPUP      = "LoadingPopup";
            public const string LEVEL_SELECT_POPUP = "LevelSelectPopup";
            public const string SETTINGS_POPUP     = "SettingsPopup";
            public const string PAUSE_POPUP        = "PausePopup";
            public const string RESULT_POPUP       = "ResultPopup";
            public const string TUTORIAL_POPUP     = "TutorialPopup";
        }

        public static class SceneName
        {
            public const string MAIN = "Main";
            public const string LEVEL_PREFIX = "Level"; // Level1, Level2
            public static string Level(int number) => LEVEL_PREFIX + number;
        }

        public static class SoundName
        {
            // Paths under Resources/; clips live in Assets/_Game/Resources/Audio/Sfx/
            public const string CLICK_BUTTON = "Audio/Sfx/click_button";
            public const string OPEN_POPUP   = "Audio/Sfx/open_popup";
            public const string WIN          = "Audio/Sfx/win";
            public const string LOSE         = "Audio/Sfx/lose";
        }

        public static class SoundBG
        {
            public const string BGM_MAIN = "Audio/Bgm/BGM_main";
        }
    }
}
