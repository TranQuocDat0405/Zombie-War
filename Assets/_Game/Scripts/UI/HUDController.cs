using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Core;
using ZombieWar.Player;
using ZombieWar.Weapons;

namespace ZombieWar.UI
{
    /// <summary>Wires the HUD widgets to gameplay events.</summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private BombThrower bombThrower;

        [Header("Widgets")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Text timerText;
        [SerializeField] private Text killsText;
        [SerializeField] private Text weaponLabel;
        [SerializeField] private Text ammoText;
        [SerializeField] private Image bombCooldownFill;
        [SerializeField] private Text bombCountText;
        [SerializeField] private DamageFlashUI damageFlash;
        [SerializeField] private ResultPanel resultPanel;

        private void Start()
        {
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnKillsChanged += OnKillsChanged;
                GameManager.Instance.OnStateChanged += OnStateChanged;
                OnKillsChanged(GameManager.Instance.Kills);
            }
            if (bombThrower != null)
            {
                bombThrower.OnBombCountChanged += OnBombCountChanged;
                OnBombCountChanged();
            }
        }

        private void OnDestroy()
        {
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnKillsChanged -= OnKillsChanged;
                GameManager.Instance.OnStateChanged -= OnStateChanged;
            }
            if (bombThrower != null) bombThrower.OnBombCountChanged -= OnBombCountChanged;
        }

        private void OnBombCountChanged()
        {
            if (bombCountText == null || bombThrower == null) return;
            bombCountText.text = "x" + bombThrower.BombCount;
            bombCountText.color = bombThrower.BombCount > 0 ? Color.white : new Color(1f, 0.35f, 0.3f);
        }

        private void Update()
        {
            if (GameManager.Instance != null && timerText != null)
            {
                float t = Mathf.Max(0f, GameManager.Instance.TimeRemaining);
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
            if (healthFill != null) healthFill.fillAmount = health / max;
        }

        private void OnPlayerDamaged()
        {
            if (damageFlash != null) damageFlash.Flash();
        }

        private void OnKillsChanged(int kills)
        {
            if (killsText != null) killsText.text = kills.ToString();
        }

        private void OnWeaponChanged(WeaponData weapon)
        {
            if (weaponLabel != null) weaponLabel.text = weapon.displayName.ToUpper();
        }

        private void OnStateChanged(GameState state)
        {
            if (resultPanel != null && state != GameState.Playing)
            {
                resultPanel.Show(state == GameState.Won);
            }
        }

        // UI button hooks
        public void OnSwitchWeaponPressed()
        {
            if (weaponController != null) weaponController.NextWeapon();
        }

        public void OnBombPressed()
        {
            if (bombThrower != null) bombThrower.ThrowBomb();
        }
    }
}
