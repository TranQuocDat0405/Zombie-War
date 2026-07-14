using NFramework;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Core;
using ZombieWar.Player;
using ZombieWar.Weapons;

namespace ZombieWar.UI
{
    /// <summary>
    /// In-match HUD as a UIManager view (ported from HUDController). Lives in a
    /// Resources prefab, so all gameplay references are resolved through
    /// LevelManager.Instance in OnOpen — GameManager guarantees the level scene is
    /// loaded before this view opens.
    /// </summary>
    public class GamePlayMenu : BaseUIView
    {
        [Header("Widgets")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Text timerText;
        [SerializeField] private Text killsText;
        [SerializeField] private Text weaponLabel;
        [SerializeField] private Text ammoText;
        [SerializeField] private Image bombCooldownFill;
        [SerializeField] private Text bombCountText;
        [SerializeField] private DamageFlashUI damageFlash;
        [SerializeField] private Joystick joystick;

        /// <summary>PlayerController (level scene) resolves its input source here.</summary>
        public Joystick Joystick => joystick;

        private PlayerHealth playerHealth;
        private WeaponController weaponController;
        private BombThrower bombThrower;
        private LevelManager level;

        public override void OnOpen()
        {
            base.OnOpen();

            level = LevelManager.Instance;
            playerHealth = level != null ? level.PlayerHealth : null;
            weaponController = level != null ? level.WeaponController : null;
            bombThrower = level != null ? level.BombThrower : null;

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += OnHealthChanged;
                playerHealth.OnDamaged += OnPlayerDamaged;
                OnHealthChanged(playerHealth.Health, playerHealth.MaxHealth);
            }
            if (weaponController != null)
            {
                weaponController.OnWeaponChanged += OnWeaponChanged;
                weaponController.OnAmmoChanged += OnAmmoChanged;
                if (weaponController.CurrentWeapon != null) OnWeaponChanged(weaponController.CurrentWeapon);
                OnAmmoChanged();
            }
            if (level != null)
            {
                level.OnKillsChanged += OnKillsChanged;
                OnKillsChanged(level.Kills);
            }
            if (bombThrower != null)
            {
                bombThrower.OnBombCountChanged += OnBombCountChanged;
                OnBombCountChanged();
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= OnHealthChanged;
                playerHealth.OnDamaged -= OnPlayerDamaged;
            }
            if (weaponController != null)
            {
                weaponController.OnWeaponChanged -= OnWeaponChanged;
                weaponController.OnAmmoChanged -= OnAmmoChanged;
            }
            if (level != null) level.OnKillsChanged -= OnKillsChanged;
            if (bombThrower != null) bombThrower.OnBombCountChanged -= OnBombCountChanged;

            playerHealth = null;
            weaponController = null;
            bombThrower = null;
            level = null;
        }

        private void Update()
        {
            if (level != null && timerText != null)
            {
                float t = Mathf.Max(0f, level.TimeRemaining);
                timerText.text = string.Format("{0:00}:{1:00}", (int)(t / 60f), (int)(t % 60f));
            }
            if (bombThrower != null && bombCooldownFill != null)
            {
                bombCooldownFill.fillAmount = 1f - bombThrower.CooldownRemaining / bombThrower.Cooldown;
            }
            if (weaponController != null && ammoText != null && weaponController.IsReloading)
            {
                ammoText.text = "RELOAD " + Mathf.RoundToInt(weaponController.ReloadProgress * 100f) + "%";
                ammoText.color = new Color(1f, 0.6f, 0.2f);
            }
        }

        private void OnAmmoChanged()
        {
            if (ammoText == null || weaponController == null) return;
            if (weaponController.IsReloading) return; // Update() shows reload progress
            ammoText.text = weaponController.CurrentAmmo + " / " + weaponController.CurrentReserve;
            ammoText.color = weaponController.CurrentAmmo == 0 && weaponController.CurrentReserve == 0
                ? new Color(1f, 0.3f, 0.25f)
                : Color.white;
        }

        private void OnHealthChanged(float health, float max)
        {
            if (healthFill == null) return;
            // 9-sliced fill: resize via anchors so the rounded caps never stretch.
            float pct = Mathf.Clamp01(health / max);
            var rt = healthFill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Lerp(0.03f, 1f, pct), 1f);
            healthFill.enabled = pct > 0.001f;
        }

        private void OnPlayerDamaged()
        {
            if (damageFlash != null) damageFlash.Flash();
        }

        private void OnKillsChanged(int kills)
        {
            if (killsText != null) killsText.text = kills.ToString();
        }

        private void OnBombCountChanged()
        {
            if (bombCountText == null || bombThrower == null) return;
            bombCountText.text = "x" + bombThrower.BombCount;
            bombCountText.color = bombThrower.BombCount > 0 ? Color.white : new Color(1f, 0.35f, 0.3f);
        }

        private void OnWeaponChanged(WeaponData weapon)
        {
            if (weaponLabel != null) weaponLabel.text = weapon.displayName.ToUpper();
        }

        // UI button hooks (wired on the prefab)
        public void OnSwitchWeaponPressed()
        {
            if (weaponController != null) weaponController.NextWeapon();
        }

        public void OnBombPressed()
        {
            if (bombThrower != null) bombThrower.ThrowBomb();
        }

        public void OnPausePressed()
        {
            // After win/lose the ResultPopup owns the screen — don't stack menus.
            if (level != null && level.State != GameState.Playing) return;
            SoundManager.I.PlaySFXResource(Define.SoundName.CLICK_BUTTON);
            UIManager.I.Open(Define.UIName.PAUSE_POPUP);
        }
    }
}
