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
        if (Time.time < ultimateReadyTime || player.ultimateAttack == null) return;

        // Chỉ tung ultimate khi đang đứng hoặc chạy: không cắt ngang dash, dodge, lúc chết...
        var cur = player.Machine.Current;
        if (cur != player.IdleState && cur != player.RunState) return;

        ultimateReadyTime = Time.time + CurrentWeaponEquipped.UltimateCooldown;
        player.AttackState.SetAttack(player.ultimateAttack, ultimate: true);
        player.Machine.ChangeState(player.AttackState);
        PerformAttack?.Invoke(CurrentWeaponEquipped.UltimateCooldown, AttackType.Ultimate);
    }
}
