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
    public PlayerAttackState(PlayerController p, PlayerStateMachine m) : base(p, m) { }
    public void SetAttack(PlayerAttackData data) => current = data;
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
            Vector2 forward = IsoDirection.ToIso(IsoDirection.ToVector(player.Facing), player.IsoYScale);
            Vector2 spawnPos = player.AttackHitboxCenter + forward * current.SpawnForward;
            attackSpawned = Object.Instantiate(current.Prefab, spawnPos, current.GetRotation(player.Facing), player.transform);
            attackSpawned.Init(player.gameObject, current.Damage, player.attackHitMask,
            current.HitEnd - current.HitStart);
        }
        if (StateTime < current.Duration) return; // đòn chưa xong: bỏ qua mọi input

        if (player.TryEvade()) return;
        var next = player.ConsumeAttackInput();
        if (next != null)
        {
            current = next;
            BeginSwing();
            return;
        }
        machine.ChangeState(player.GetLocomotionState());
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