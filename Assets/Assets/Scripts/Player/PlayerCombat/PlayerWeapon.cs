using System;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public PlayerWeaponData[] WeaponEquipped;
    public PlayerWeaponData CurrentWeaponEquipped { get; private set; }
    public PlayerController player { get; private set; }
    private float ultimateReadyTime = -1f;

    // Phát sự kiện cho cooldownbar
    public Action<float, AttackType> PerformAttack;
    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }
    private void Start()
    {
        DoChangeWeapon(0);
    }
    public void ChangeWeapon(PlayerWeaponData weaponData)
    {
        player.normalAttack = weaponData.NormalAttack;
        player.heavyAttack = weaponData.HeavyAttack;
        player.ultimateAttack = weaponData.Ultimate;
        CurrentWeaponEquipped = weaponData;
    }
    public void DoChangeWeapon(int index)
    {
        if (WeaponEquipped.Length <= index) return;
        ChangeWeapon(WeaponEquipped[index]);
    }
    public void PerformUltimate()
    {
        if (Time.time >= ultimateReadyTime)
        {
            Transform target = player.Attack.CurrentTarget;
            Vector2 dir = target != null
                ? (Vector2)target.position - player.Rb.position                                 // có mục tiêu: chĩa thẳng vào nó
                : IsoDirection.ToIso(IsoDirection.ToVector(player.Facing), player.IsoYScale);   // không có: bắn theo hướng đang nhìn

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // -90 vì beam nằm dọc trục Y
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            HitDealer attackSpawned = Instantiate(player.ultimateAttack.Prefab, player.AttackHitboxCenter, rot, player.transform);
            attackSpawned.Init(player.gameObject, player.ultimateAttack.Damage, player.attackHitMask, CurrentWeaponEquipped.UltimateAOELifespan);
            ultimateReadyTime = Time.time + CurrentWeaponEquipped.UltimateCooldown;
            if (attackSpawned.TryGetComponent(out MovingProjectile projectile))
            {
                projectile.Launch(player.Attack.AimForAttack(), 0.5f);
            }
            PerformAttack?.Invoke(CurrentWeaponEquipped.UltimateCooldown, AttackType.Ultimate);
        }
    }
}
