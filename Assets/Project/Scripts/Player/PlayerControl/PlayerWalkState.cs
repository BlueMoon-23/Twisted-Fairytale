using UnityEngine;

public class PlayerWalkState : PlayerGroundedState
{
    public PlayerWalkState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        player.UpdateFacingFromInput();
        player.PlayAnimation("Walk");
        player.currentStateName = "Walk";
    }

    protected override void OnStateUpdate()
    {
        player.UpdateFacingFromInput();
        player.PlayAnimation("Walk"); // tự bỏ qua nếu clip không đổi
    }

    public override void PhysicsUpdate() => player.MoveInFacingOrInput(player.walkSpeed);
}