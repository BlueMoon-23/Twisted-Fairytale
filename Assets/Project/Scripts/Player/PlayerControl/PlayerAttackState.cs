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

        // Đánh theo hướng đang bấm; đứng yên thì đánh theo hướng đang nhìn.
        // Giữa các nhát combo đổi phím hướng là xoay được đòn tiếp theo.
        player.UpdateFacingFromInput();

        dir = IsoDirection.ToVector(player.Facing);
        hasSpawnedAttack = false;
        player.PlayAnimation("Punch", restart: true);
        player.currentStateName = "Attack";
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (!hasSpawnedAttack && StateTime >= current.HitStart)
        {
            hasSpawnedAttack = true;
            attackSpawned = Object.Instantiate(current.Prefab, player.AttackHitboxCenter, Quaternion.identity, player.transform);
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
        if (StateTime < current.HitEnd) player.MoveInDirection(dir, current.LungeSpeed);
        else player.StopMovement();
    }
    public override void Exit()
    {
        if (attackSpawned != null) Object.Destroy(attackSpawned.gameObject);
    }
}