using UnityEngine;

public class PlayerRunState : PlayerGroundedState
{
    public PlayerRunState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        player.UpdateFacingFromInput();
        player.PlayAnimation("Run");
    }

    protected override void OnStateUpdate()
    {
        player.UpdateFacingFromInput();
        player.PlayAnimation("Run");
    }

    public override void PhysicsUpdate() => player.MoveInFacingOrInput(player.runSpeed);
}