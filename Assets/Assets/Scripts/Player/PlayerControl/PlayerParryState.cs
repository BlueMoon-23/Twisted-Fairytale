using UnityEngine;

/// <summary>
/// Đỡ đòn (F). Đứng yên, trong parryWindow đầu tiên mọi đòn trúng bị đỡ.
///   - Đỡ trượt: chịu hết parryDuration (bị phạt vì bấm bừa).
///   - Đỡ thành công: khựng ngắn parrySuccessRecovery rồi điều khiển lại ngay,
///     được phép dash/dodge/attack phản công liền.
/// Phần làm choáng enemy xử lý qua event PlayerController.ParrySucceeded.
/// </summary>
public class PlayerParryState : PlayerState
{
    public bool InParryWindow => !succeeded && StateTime <= player.parryWindow;

    private bool succeeded;
    private float successTime;

    public PlayerParryState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        succeeded = false;

        player.UpdateFacingFromInput(); // cùng quy tắc hướng với attack
        player.StopMovement();
        player.PlayAnimation("Interact", restart: true);
        player.currentStateName = "Parry";
    }

    /// <summary>PlayerController.TakeDamage gọi khi đỡ trúng.</summary>
    public void OnParrySuccess()
    {
        succeeded = true;
        successTime = StateTime;
        player.PlayAnimation("ParrySuccess", restart: true);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (succeeded)
        {
            if (StateTime - successTime < player.parrySuccessRecovery) return;

            // Hết khựng: cho phản công ngay
            if (player.TryEvade()) return;
            if (player.Input.ConsumeAttack()) { machine.ChangeState(player.AttackState); return; }
            machine.ChangeState(player.GetLocomotionState());
            return;
        }

        if (StateTime >= player.parryDuration)
            machine.ChangeState(player.GetLocomotionState());
    }

    public override void PhysicsUpdate() => player.StopMovement();
}