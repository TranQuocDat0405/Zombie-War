using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZombieWar.Core
{
    public enum GameState { Playing, Won, Lost }

    /// <summary>
    /// Per-level state machine: survival timer, kill counter, win/lose flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private float levelDuration = 180f;

        public GameState State { get; private set; } = GameState.Playing;
        public float TimeRemaining { get; private set; }
        public float TimeElapsed => levelDuration - TimeRemaining;
        public int Kills { get; private set; }

        /// <summary>Level number newly unlocked by this win (0 = nothing new).</summary>
        public int JustUnlockedLevel { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnKillsChanged;

        private void Awake()
        {
            Instance = this;
            TimeRemaining = levelDuration;
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;

            // Ragdoll bones must not shove the player or live zombies around.
            int player = LayerMask.NameToLayer("Player");
            int zombie = LayerMask.NameToLayer("Zombie");
            int ragdoll = LayerMask.NameToLayer("ZombieRagdoll");
            if (ragdoll >= 0)
            {
                if (player >= 0) Physics.IgnoreLayerCollision(player, ragdoll, true);
                if (zombie >= 0) Physics.IgnoreLayerCollision(zombie, ragdoll, true);
            }
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                SetState(GameState.Won);
            }
        }

        public void RegisterKill()
        {
            Kills++;
            OnKillsChanged?.Invoke(Kills);
        }

        public void PlayerDied()
        {
            if (State == GameState.Playing) SetState(GameState.Lost);
        }

        private void SetState(GameState state)
        {
            State = state;

            if (state == GameState.Won)
            {
                // "Level1" -> unlock level 2; persists via PlayerPrefs.
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName.StartsWith("Level") &&
                    int.TryParse(sceneName.Substring(5), out int levelNumber))
                {
                    if (GameSettings.UnlockLevel(levelNumber + 1))
                    {
                        JustUnlockedLevel = levelNumber + 1;
                    }
                }
            }

            OnStateChanged?.Invoke(state);
            if (state != GameState.Playing) Time.timeScale = 0.4f;
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            string current = SceneManager.GetActiveScene().name;
            if (SceneLoader.Instance != null) SceneLoader.Load(current);
            else SceneManager.LoadScene(current);
        }

        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            if (SceneLoader.Instance != null) SceneLoader.Load(sceneName);
            else SceneManager.LoadScene(sceneName);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
