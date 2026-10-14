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
    public bool HitboxActive { get; private set; }
    /// <summary>Số nhát đã nối liên tiếp (0 = nhát đầu). Sau này dùng để đổi clip combo.</summary>
    public int ComboIndex { get; private set; }

    private Vector2 dir;
    private readonly HashSet<IDamageable> alreadyHit = new();
    private readonly List<Collider2D> overlapResults = new();
    private ContactFilter2D hitFilter;

    public PlayerAttackState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        ComboIndex = 0;
        BeginSwing();
    }

    private void BeginSwing()
    {
        base.Enter(); // reset StateTime

        // Đánh theo hướng đang bấm; đứng yên thì đánh theo hướng đang nhìn.
        // Giữa các nhát combo đổi phím hướng là xoay được đòn tiếp theo.
        player.UpdateFacingFromInput();

        dir = IsoDirection.ToVector(player.Facing);
        alreadyHit.Clear();
        HitboxActive = false;

        hitFilter = new ContactFilter2D { useTriggers = true };
        hitFilter.SetLayerMask(player.attackHitMask);

        player.PlayAnimation("Punch", restart: true);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        HitboxActive = StateTime >= player.attackHitStart && StateTime <= player.attackHitEnd;
        if (HitboxActive) DoHitCheck();

        if (StateTime >= player.attackCancelTime)
        {
            if (player.TryEvade()) return;

            if (player.Input.ConsumeAttack())
            {
                ComboIndex++;
                BeginSwing();
                return;
            }
        }

        if (StateTime >= player.attackDuration)
            machine.ChangeState(player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        if (StateTime < player.attackHitEnd) player.MoveInDirection(dir, player.attackLungeSpeed);
        else player.StopMovement();
    }

    public override void Exit() => HitboxActive = false;

    private void DoHitCheck()
    {
        int count = Physics2D.OverlapCircle(player.AttackHitboxCenter, player.attackRadius, hitFilter, overlapResults);
        for (int i = 0; i < count; i++)
        {
            var col = overlapResults[i];
            if (col.attachedRigidbody == player.Rb) continue; // không tự đánh mình

            var target = col.GetComponentInParent<IDamageable>();
            if (target != null && alreadyHit.Add(target))
                target.TakeDamage(player.attackDamage, player.gameObject);
        }
    }
}