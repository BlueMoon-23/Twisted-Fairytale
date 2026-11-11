using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đòn đánh 1 nhát, chia 3 pha theo thời gian:
///   [0, hitStart)        vung tay  - lướt nhẹ tới trước
///   [hitStart, hitEnd]   hitbox bật - gây damage (mỗi mục tiêu tối đa 1 lần / nhát)
///   (hitEnd, duration)   hồi chiêu  - từ attackCancelTime được nối đòn hoặc dash/dodge huỷ
/// </summary>
public class PlayerAttackState : PlayerState
{
    private Vector2 dir;
    private PlayerAttackData current;
    private HitDealer attackSpawned;
    private bool hasSpawnedAttack;
    private bool isUltimate;
    private readonly PlayerWeapon weapon;

    public PlayerAttackState(PlayerController p, PlayerStateMachine m) : base(p, m)
    {
        weapon = p.GetComponent<PlayerWeapon>();
    }

    // ultimate = true: spawn theo hướng chính xác tới mục tiêu (như PlayerWeapon cũ), không theo 8 hướng
    public void SetAttack(PlayerAttackData data, bool ultimate = false)
    {
        current = data;
        isUltimate = ultimate;
    }

    public override void Enter() => BeginSwing();

    private void BeginSwing()
    {
        base.Enter(); // reset StateTime

        // Có enemy trong tầm: quay về phía nó (làm tròn 8 hướng). Không có: đánh theo hướng đang bấm,
        // đứng yên thì theo hướng đang nhìn. Mỗi nhát combo chọn lại nên vẫn đổi hướng được giữa combo.
        dir = player.Attack.AimForAttack();

        hasSpawnedAttack = false;
        player.PlayAnimation(current.AnimName, restart: true);
        player.currentStateName = "Attack";
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (!hasSpawnedAttack && StateTime >= current.HitStart)
        {
            hasSpawnedAttack = true;
            if (isUltimate) SpawnUltimate();
            else SpawnNormal();
        }
        if (StateTime < current.Duration) return; // đòn chưa xong: bỏ qua mọi input

        if (player.TryEvade()) return;
        var next = player.ConsumeAttackInput();
        if (next != null)
        {
            SetAttack(next); // nhát thường nối combo: tắt cờ ultimate
            BeginSwing();
            return;
        }
        machine.ChangeState(player.GetLocomotionState());
    }

    // Normal / Heavy: theo hướng người chơi (8 hướng), góc lấy từ PlayerAttackData
    private void SpawnNormal()
    {
        Vector2 forward = IsoDirection.ToIso(IsoDirection.ToVector(player.Facing), player.IsoYScale);
        Vector2 spawnPos = player.AttackHitboxCenter + forward * current.SpawnForward;
        attackSpawned = Object.Instantiate(current.Prefab, spawnPos, current.GetRotation(player.Facing), player.transform);
        attackSpawned.Init(player.gameObject, current.Damage, player.attackHitMask,
            current.HitEnd - current.HitStart);
    }

    // Ultimate: hướng chính xác tới mục tiêu, giống đoạn cũ trong PlayerWeapon.PerformUltimate
    private void SpawnUltimate()
    {
        Transform target = player.Attack.CurrentTarget;
        Vector2 worldDir = target != null
            ? (Vector2)target.position - player.Rb.position                                 // có mục tiêu: chĩa thẳng vào nó
            : IsoDirection.ToIso(IsoDirection.ToVector(player.Facing), player.IsoYScale);   // không có: bắn theo hướng đang nhìn

        float angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg - 90f; // -90 vì beam nằm dọc trục Y
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        // Biến cục bộ, không gán vào attackSpawned: ultimate tự sống theo UltimateAOELifespan,
        // Exit() khi hết đòn không được xoá nó (giữ như hành vi cũ của PlayerWeapon)
        HitDealer spawned = Object.Instantiate(current.Prefab, player.AttackHitboxCenter, rot, player.transform);
        spawned.Init(player.gameObject, current.Damage, player.attackHitMask, weapon.CurrentWeaponEquipped.UltimateAOELifespan);

        if (spawned.TryGetComponent(out MovingProjectile projectile))
        {
            // Launch nhận hướng màn hình: đổi hướng world ngược lại qua isoYScale để bay đúng vào mục tiêu
            Vector2 screenDir = new Vector2(worldDir.x, worldDir.y / player.IsoYScale);
            projectile.Launch(screenDir, player.IsoYScale);
        }
    }

    public override void PhysicsUpdate()
    {
        // Đã sát mục tiêu thì đứng lại đánh, không lướt xuyên qua enemy
        if (StateTime < current.HitEnd && player.Attack.ShouldLunge()) player.MoveInDirection(dir, current.LungeSpeed);
        else player.StopMovement();
    }

    public override void Exit()
    {
        if (attackSpawned != null) Object.Destroy(attackSpawned.gameObject);
    }
}