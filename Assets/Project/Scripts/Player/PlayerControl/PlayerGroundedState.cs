using UnityEngine;

/// <summary>
/// Super-state cho mọi trạng thái "tự do điều khiển" (Idle/Walk/Run).
/// Chỗ DUY NHẤT xử lý chuyển sang action (attack/dash/parry) -> thêm state mới chỉ sửa ở đây.
/// </summary>
public abstract class PlayerGroundedState : PlayerState
{
    protected PlayerGroundedState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 1. Action có độ ưu tiên cao hơn locomotion (bật dần khi có state):
        if (player.Input.ConsumeDash())   { machine.ChangeState(player.DashState);   return; }
        if (player.Input.ConsumeParry())  { machine.ChangeState(player.ParryState);  return; }
        if (player.Input.ConsumeAttack()) { machine.ChangeState(player.AttackState); return; }

        // 2. Locomotion: Idle <-> Walk <-> Run
        PlayerState desired = player.GetLocomotionState();
        if (desired != this)
        {
            machine.ChangeState(desired);
            return;
        }

        OnStateUpdate();
    }

    /// <summary>Logic riêng của state con, chỉ chạy khi KHÔNG có chuyển state frame này.</summary>
    protected virtual void OnStateUpdate() { }
}