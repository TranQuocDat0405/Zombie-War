using System;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Player;

namespace ZombieWar.Weapons
{
    /// <summary>
    /// Auto-fires the equipped gun at the AutoAim target: hitscan rays with
    /// pooled muzzle flash, impact VFX, tracer, recoil and sound. Tracks a
    /// magazine + reserve ammo per weapon with timed reloads.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponData[] weapons;
        [SerializeField] private Transform handSocket;
        [SerializeField] private Recoil bodyRecoil;     // soldier mesh root, kicks with the gun
        [SerializeField] private LayerMask hitMask;      // Zombie + obstacles
        [SerializeField] private float aimHeight = 1.1f; // aim at zombie chest
        [Tooltip("Inside this range a zombie can contain the muzzle, so the shot is resolved directly instead of by a cast that would silently fail.")]
        [SerializeField] private float pointBlankRange = 2.2f;
        [Tooltip("Hold fire until the soldier has turned this close to the target, so rounds never leave his back.")]
        [SerializeField] private float fireFacingAngle = 50f;

        private AutoAim autoAim;
        private PlayerAnimation playerAnimation;
        private Recoil recoil;

        private GameObject[] gunInstances;
        private Transform[] muzzles;
        private ParticleSystem[] muzzleFlashes; // persistent, childed to each muzzle
        private int[] ammo;      // rounds left in magazine, per weapon
        private int[] reserve;   // reserve rounds, per weapon
        private int currentIndex;
        private float nextFireTime;
        private float reloadEndTime;
        private bool isReloading;

        public WeaponData CurrentWeapon =>
            weapons != null && weapons.Length > 0 ? weapons[currentIndex] : null;

        public int CurrentAmmo => ammo != null ? ammo[currentIndex] : 0;
        public int CurrentReserve => reserve != null ? reserve[currentIndex] : 0;
        public bool IsReloading => isReloading;
        public float ReloadProgress => !isReloading || CurrentWeapon == null
            ? 1f
            : Mathf.Clamp01(1f - (reloadEndTime - Time.time) / CurrentWeapon.reloadTime);

        public event Action<WeaponData> OnWeaponChanged;
        public event Action OnAmmoChanged;

        private void Awake()
        {
            autoAim = GetComponent<AutoAim>();
            playerAnimation = GetComponent<PlayerAnimation>();
        }

        private void Start()
        {
            BuildGunInstances();
            ammo = new int[weapons.Length];
            reserve = new int[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
            {
                ammo[i] = weapons[i].magazineSize;
                reserve[i] = weapons[i].reserveAmmo;
            }
            Equip(0);
        }

        private void BuildGunInstances()
        {
            gunInstances = new GameObject[weapons.Length];
            muzzles = new Transform[weapons.Length];
            muzzleFlashes = new ParticleSystem[weapons.Length];

            if (handSocket == null)
            {
                Debug.LogError("WeaponController: handSocket not assigned");
                return;
            }

            recoil = handSocket.GetComponent<Recoil>();
            if (recoil == null) recoil = handSocket.gameObject.AddComponent<Recoil>();

            for (int i = 0; i < weapons.Length; i++)
            {
                var model = Instantiate(weapons[i].gunModelPrefab, handSocket);
                model.name = weapons[i].displayName;
                gunInstances[i] = model;
                var muzzle = model.transform.Find("Muzzle");
                muzzles[i] = muzzle != null ? muzzle : model.transform;

                // The muzzle flash lives permanently on the muzzle and is replayed
                // per shot: always in sync with the tracer, never drifts or leaks.
                if (weapons[i].muzzleFlashPrefab != null)
                {
                    var fx = Instantiate(weapons[i].muzzleFlashPrefab, muzzles[i]);
                    fx.transform.localPosition = Vector3.zero;
                    fx.transform.localRotation = Quaternion.identity;
                    var pooled = fx.GetComponent<PooledAutoRelease>();
                    if (pooled != null) Destroy(pooled); // not pooled in this mode
                    foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = ps.main;
                        main.playOnAwake = false;
                        main.loop = false; // one burst per shot — never keeps flashing on its own
                    }
                    muzzleFlashes[i] = fx.GetComponentInChildren<ParticleSystem>(true);
                }

                model.SetActive(false);
            }
        }

        public void NextWeapon()
        {
            if (weapons.Length == 0) return;
            if (isReloading)
            {
                isReloading = false; // switching cancels the reload in progress
                if (playerAnimation != null) playerAnimation.CancelReload();
            }
            Equip((currentIndex + 1) % weapons.Length);
        }

        /// <summary>Ammo pickup: adds the given number of magazines to every weapon's reserve.</summary>
        public void AddAmmoMagazines(int magazines)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                reserve[i] += weapons[i].magazineSize * magazines;
            }
            OnAmmoChanged?.Invoke();
        }

        private void Equip(int index)
        {
            currentIndex = index;
            for (int i = 0; i < gunInstances.Length; i++)
            {
                if (gunInstances[i] != null) gunInstances[i].SetActive(i == index);
            }
            if (playerAnimation != null)
            {
                playerAnimation.SetShootSpeed(weapons[index].shootAnimSpeed);
            }
            OnWeaponChanged?.Invoke(weapons[index]);
            OnAmmoChanged?.Invoke();

            if (ammo != null && ammo[currentIndex] == 0) TryStartReload();
        }

        private void TryStartReload()
        {
            if (isReloading) return;
            var weapon = CurrentWeapon;
            if (weapon == null || reserve[currentIndex] <= 0) return;
            if (ammo[currentIndex] >= weapon.magazineSize) return;

            isReloading = true;
            reloadEndTime = Time.time + weapon.reloadTime;
            if (playerAnimation != null) playerAnimation.PlayReload(weapon.reloadTime);
        }

        private void FinishReload()
        {
            isReloading = false;
            var weapon = CurrentWeapon;
            int need = weapon.magazineSize - ammo[currentIndex];
            int take = Mathf.Min(need, reserve[currentIndex]);
            ammo[currentIndex] += take;
            reserve[currentIndex] -= take;
            OnAmmoChanged?.Invoke();
        }

        private void Update()
        {
            var weapon = CurrentWeapon;
            if (weapon == null || autoAim == null) return;
            if (LevelManager.Instance != null && LevelManager.Instance.State != GameState.Playing) return;

            if (isReloading)
            {
                if (Time.time >= reloadEndTime) FinishReload();
                else return;
            }

            var target = autoAim.CurrentTarget;
            if (target == null) return;

            Vector3 aimPoint = target.position + Vector3.up * aimHeight;
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > weapon.range) return;

            if (Time.time < nextFireTime) return;

            // Wait for the soldier to come round. Firing the instant AutoAim latched a
            // zombie behind him sent rounds flying out of his back, because the shot
            // direction is muzzle->target regardless of which way he is facing.
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f &&
                Vector3.Angle(transform.forward, toTarget) > fireFacingAngle)
            {
                return;
            }

            // Bullets in the air already carry enough damage to finish this one —
            // don't waste another round on a corpse-to-be, wait for AutoAim to move on.
            var targetDamageable = target.GetComponent<IDamageable>();
            if (targetDamageable != null &&
                targetDamageable.PendingDamage >= GetRemainingHealth(targetDamageable))
            {
                return;
            }

            if (ammo[currentIndex] <= 0)
            {
                TryStartReload();
                return;
            }

            nextFireTime = Time.time + 1f / weapon.fireRate;
            Fire(weapon, aimPoint, targetDamageable, dist);
        }

        private static float GetRemainingHealth(IDamageable damageable)
        {
            var zombie = damageable as ZombieWar.Zombie.ZombieHealth;
            return zombie != null ? zombie.Health : float.MaxValue;
        }

        private void Fire(WeaponData weapon, Vector3 aimPoint, IDamageable target, float dist)
        {
            ammo[currentIndex]--;

            // No lead needed: the guided round re-aims at the chest every frame.
            Vector3 muzzlePos = muzzles[currentIndex].position;
            Vector3 baseDir = (aimPoint - muzzlePos).normalized;

            if (dist <= pointBlankRange && target != null && !target.IsDead)
            {
                FirePointBlank(weapon, aimPoint, target, muzzlePos, baseDir);
            }
            else
            {
                FireProjectiles(weapon, target, muzzlePos, baseDir);
            }

            var flash = muzzleFlashes[currentIndex];
            if (flash != null)
            {
                flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                flash.Play(true); // same frame the bullet leaves the barrel
            }
            if (weapon.fireClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxRandomPitch(weapon.fireClip, muzzlePos, weapon.fireVolume);
            }
            if (recoil != null) recoil.Kick(weapon.recoilKick);
            if (bodyRecoil != null) bodyRecoil.Kick(weapon.recoilKick * 4f); // whole-body jerk, clearly visible
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(weapon.cameraShake);
            if (playerAnimation != null) playerAnimation.NotifyFiring();

            OnAmmoChanged?.Invoke();
            if (ammo[currentIndex] <= 0) TryStartReload();
        }

        /// <summary>
        /// A zombie this close can physically contain the muzzle — its capsule has a
        /// 0.35m radius and the barrel sits 0.77m out from the player's centre — and a
        /// cast that starts inside a collider never reports a hit. There is no cast
        /// that can be trusted here, so resolve the shot directly: it simply cannot
        /// miss. The bullet is still drawn leaving the barrel, but at this range it
        /// crosses the gap in about four frames, so nobody can tell it isn't the one
        /// doing the damage.
        /// </summary>
        private void FirePointBlank(WeaponData weapon, Vector3 aimPoint, IDamageable target,
                                    Vector3 muzzlePos, Vector3 baseDir)
        {
            float total = weapon.damage * weapon.pelletCount;
            target.TakeDamage(total, aimPoint, baseDir);

            if (weapon.impactPrefab != null)
            {
                ObjectPool.Spawn(weapon.impactPrefab, aimPoint, Quaternion.LookRotation(-baseDir));
            }

            if (weapon.bulletPrefab == null) return;

            // The full cone, drawn — the shotgun's fan must read at point-blank too.
            float flightDistance = Vector3.Distance(muzzlePos, aimPoint);
            for (int i = 0; i < weapon.pelletCount; i++)
            {
                Vector3 dir = i == 0 ? baseDir : ApplySpread(weapon, baseDir);
                var go = ObjectPool.Spawn(weapon.bulletPrefab, muzzlePos, Quaternion.LookRotation(dir));
                go.GetComponent<Bullet>().LaunchCosmetic(dir, weapon.bulletSpeed, flightDistance);
            }
        }

        private void FireProjectiles(WeaponData weapon, IDamageable target,
                                     Vector3 muzzlePos, Vector3 baseDir)
        {
            if (weapon.bulletPrefab == null) return;

            for (int i = 0; i < weapon.pelletCount; i++)
            {
                // Centre pellet is guided and cannot miss; the rest fan out straight
                // and hit whatever crosses them — that IS the shotgun cone, and it is
                // what mows down crowds. Only the guaranteed pellet reserves damage,
                // so the ammo ledger stays exact.
                bool guided = i == 0;
                Vector3 dir = guided ? baseDir : ApplySpread(weapon, baseDir);

                if (guided && target != null) target.ReservePending(weapon.damage);

                var go = ObjectPool.Spawn(weapon.bulletPrefab, muzzlePos, Quaternion.LookRotation(dir));
                go.GetComponent<Bullet>().Launch(
                    dir, weapon.bulletSpeed, weapon.damage, weapon.range,
                    hitMask, weapon.impactPrefab, guided ? target : null, guided ? weapon.damage : 0f);
            }
        }

        private static Vector3 ApplySpread(WeaponData weapon, Vector3 dir)
        {
            if (weapon.spreadAngle <= 0.01f) return dir;
            float yaw = UnityEngine.Random.Range(-weapon.spreadAngle, weapon.spreadAngle);
            float pitch = UnityEngine.Random.Range(-weapon.spreadAngle, weapon.spreadAngle) * 0.25f;
            return Quaternion.Euler(pitch, yaw, 0f) * dir;
        }

    }
}
