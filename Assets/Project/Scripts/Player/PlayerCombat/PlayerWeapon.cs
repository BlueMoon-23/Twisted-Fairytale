using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public PlayerWeaponData[] WeaponEquipped;
    public PlayerWeaponData CurrentWeaponEquipped { get; private set; }
    public PlayerController player { get; private set; }
    private float ultimateReadyTime = -1f;
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
            HitDealer attackSpawned = Object.Instantiate(player.ultimateAttack.Prefab, player.AttackHitboxCenter, Quaternion.identity, player.transform);
            attackSpawned.Init(player.gameObject, player.ultimateAttack.Damage, player.attackHitMask, CurrentWeaponEquipped.UltimateAOELifespan);
            ultimateReadyTime = Time.time + CurrentWeaponEquipped.UltimateCooldown;
            if (attackSpawned.TryGetComponent(out MovingProjectile projectile))
            {
                projectile.Launch(player.Facing, 0.5f);
            }
        }
    }
}
