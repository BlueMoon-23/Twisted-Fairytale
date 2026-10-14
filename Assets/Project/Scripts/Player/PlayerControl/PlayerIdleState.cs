using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        player.StopMovement();
        player.ResolveFacingOnStop();
        player.PlayAnimation("Idle");
    }

    public override void PhysicsUpdate() => player.StopMovement();
}