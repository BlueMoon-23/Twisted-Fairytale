using UnityEngine;

public class PlayerDashState : PlayerState
{
    private Vector2 dir;
    public PlayerDashState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        player.UpdateFacingFromInput();
        dir = IsoDirection.ToVector(player.Facing);
        player.PlayAnimation("Jump", restart: true);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        // Hết dash: còn giữ Shift + hướng thì chạy tiếp luôn
        if (StateTime >= player.dashDuration)
            machine.ChangeState(player.GetLocomotionState());
    }
    public override void PhysicsUpdate() => player.MoveInDirection(dir, player.dashSpeed);

    public override void Exit() => player.StartEvadeCooldown();
}
