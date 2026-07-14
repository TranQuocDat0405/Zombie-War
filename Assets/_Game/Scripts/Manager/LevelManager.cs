using System;
using NFramework;
using UnityEngine;
using ZombieWar.UI;

namespace ZombieWar.Core
{
    public enum GameState { Playing, Won, Lost }

    /// <summary>
    /// Per-level state machine: survival timer, kill counter, win/lose flow.
    /// Lives inside each Level scene (loaded additively under Main); app-level
    /// flow (loading, home, restart) belongs to GameManager.
    /// </summary>
    public class LevelManager : SingletonMono<LevelManager>
    {
        [Tooltip("1-based level number — MUST match the scene (Level1 = 1, Level2 = 2). Replaces scene-name parsing for unlocks.")]
        [SerializeField] private int levelNumber = 1;

        [Header("Match context (wired per Level scene)")]
        // The HUD lives in a Resources prefab and cannot serialize references into
        // this scene, so it resolves everything through LevelManager.I instead.
        [SerializeField] private Player.PlayerHealth playerHealth;
        [SerializeField] private Weapons.WeaponController weaponController;
        [SerializeField] private Weapons.BombThrower bombThrower;

        public int LevelNumber => levelNumber;
        public Player.PlayerHealth PlayerHealth => playerHealth;
        public Weapons.WeaponController WeaponController => weaponController;
        public Weapons.BombThrower BombThrower => bombThrower;

        public GameState State { get; private set; } = GameState.Playing;
        public float TimeRemaining { get; private set; }
        public float TimeElapsed => LevelDuration - TimeRemaining;
        public int Kills { get; private set; }

        /// <summary>Level number newly unlocked by this win (0 = nothing new).</summary>
        public int JustUnlockedLevel { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnKillsChanged;

        private bool _begun;

        private float LevelDuration => GameManager.I.GetGameConfig().levelDuration;

        protected override void Awake()
        {
            base.Awake();
            TimeRemaining = GameManager.IsSingletonAlive ? LevelDuration : 180f;

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

        /// <summary>
        /// Called by GameManager once the scene is loaded and the HUD is open.
        /// First visit to Level 1 pauses behind the tutorial; GOT IT re-enters here.
        /// </summary>
        public void Begin()
        {
            if (levelNumber == 1 && !UserData.I.HasSeenTutorial)
            {
                Time.timeScale = 0f;
                UIManager.I.Open(Define.UIName.TUTORIAL_POPUP);
                return; // TutorialPopup.OnGotItPressed calls Begin() again
            }
            _begun = true;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (!_begun || State != GameState.Playing) return;

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
                if (UserData.I.UnlockLevel(levelNumber + 1))
                {
                    JustUnlockedLevel = levelNumber + 1;
                }
            }

            OnStateChanged?.Invoke(state);
            if (state != GameState.Playing)
            {
                Time.timeScale = GameManager.I.GetGameConfig().endSlowMotionScale;
                UIManager.I.Open<ResultPopup>(Define.UIName.RESULT_POPUP,
                    p => p.Show(state == GameState.Won));
            }
        }
    }
}
