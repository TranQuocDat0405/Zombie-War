using System;
using System.Collections;
using NFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieWar.UI;

namespace ZombieWar
{
    public enum EGameState { NONE, LOADING, HOME, INGAME, RESET }

    /// <summary>
    /// App-level FSM living in the never-unloaded Main scene: boot, home menu,
    /// additive level load/unload, restart. Match-level state (timer, kills,
    /// win/lose) stays in LevelManager inside each Level scene.
    /// </summary>
    public class GameManager : SingletonMono<GameManager>
    {
        [SerializeField] private GameConfig _gameConfig;

        private EGameState _state;
        public EGameState GetGameState() => _state;

        /// <summary>Level being played / about to load (1-based). Set by EnterInGame.</summary>
        public int CurrentLevel { get; private set; } = 1;

        private void Start()
        {
            EnterLoading();
        }

        private void SetGameState(EGameState state)
        {
            if (_state != state)
            {
                _state = state;
                HandleGameStateChanged(_state);
            }
        }

        private void RegisterAndLoadSave()
        {
            SaveManager.I.RegisterSaveData(UserData.I);
            SaveManager.I.RegisterSaveData(SoundManager.I);
            SaveManager.I.Load();
        }

        private void HandleGameStateChanged(EGameState state)
        {
            Debug.Log($"GameState: {state}");
            switch (state)
            {
                case EGameState.LOADING:
                    Application.targetFrameRate = 60;
                    UIManager.I.Open<LoadingPopup>(Define.UIName.LOADING_POPUP)
                        .AssignEvent(LoadingComplete);
                    break;

                case EGameState.HOME:
                    SoundManager.I.StopMusic();
                    SoundManager.I.PlayMusicResource(Define.SoundBG.BGM_MAIN);
                    UIManager.I.Open(Define.UIName.HOME_MENU);
                    break;

                case EGameState.INGAME:
                    SoundManager.I.StopMusic();
                    UIManager.I.Open<LoadingPopup>(Define.UIName.LOADING_POPUP).AssignEvent(null);
                    StartCoroutine(CRLoadScene(Define.SceneName.Level(CurrentLevel), () =>
                    {
                        UIManager.I.Open(Define.UIName.GAMEPLAY_MENU);
                        UIManager.I.Close(Define.UIName.LOADING_POPUP);
                        Core.LevelManager.Instance.Begin();
                    }));
                    break;

                case EGameState.RESET:
                    StartCoroutine(CRUnloadScene(Define.SceneName.Level(CurrentLevel),
                        () => EnterInGame(CurrentLevel)));
                    break;
            }
        }

        private void LoadingComplete()
        {
            RegisterAndLoadSave();
            EnterHome();
        }

        private void EnterLoading() => SetGameState(EGameState.LOADING);

        public void EnterHome()
        {
            // Coming from a match (Home button): unload the level first.
            if (_state == EGameState.INGAME)
            {
                Time.timeScale = 1f;
                UIManager.I.CloseAllInLayer(EUILayer.Popup);
                UIManager.I.Close(Define.UIName.GAMEPLAY_MENU);
                StartCoroutine(CRUnloadScene(Define.SceneName.Level(CurrentLevel),
                    () => SetGameState(EGameState.HOME)));
            }
            else
            {
                SetGameState(EGameState.HOME);
            }
        }

        public void EnterInGame(int level)
        {
            CurrentLevel = level;
            Time.timeScale = 1f;
            SetGameState(EGameState.INGAME);
        }

        /// <summary>Restart the current level (additive unload + reload).</summary>
        public void EnterReset()
        {
            Time.timeScale = 1f;
            UIManager.I.CloseAllInLayer(EUILayer.Popup);
            UIManager.I.Close(Define.UIName.GAMEPLAY_MENU);
            SetGameState(EGameState.RESET);
        }

        /// <summary>Advance from the result popup to the next level.</summary>
        public void EnterNextLevel()
        {
            var next = CurrentLevel + 1;
            Time.timeScale = 1f;
            UIManager.I.CloseAllInLayer(EUILayer.Popup);
            UIManager.I.Close(Define.UIName.GAMEPLAY_MENU);
            StartCoroutine(CRUnloadScene(Define.SceneName.Level(CurrentLevel),
                () => EnterInGame(next)));
        }

        public GameConfig GetGameConfig() => _gameConfig;

        private IEnumerator CRLoadScene(string sceneName, Action callback = null)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            // Active scene = gameplay scene, so lighting and Instantiate default there.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            callback?.Invoke();
        }

        private IEnumerator CRUnloadScene(string sceneName, Action callback = null)
        {
            yield return SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.None);
            callback?.Invoke();
        }
    }
}
