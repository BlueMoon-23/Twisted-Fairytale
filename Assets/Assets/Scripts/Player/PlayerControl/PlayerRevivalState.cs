using UnityEngine;

public class PlayerReviveState : PlayerState
{
    private const float MaxDuration = 3f; // an toàn: thiếu clip hoặc clip đang bật Loop thì cũng không kẹt mãi
    public PlayerReviveState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        player.StopMovement();
        player.PlayAnimation("Revival", restart: true);
        player.currentStateName = "Revive";
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (player.IsAnimationFinished || StateTime >= MaxDuration)
            machine.ChangeState(player.GetLocomotionState());
    }

    public override void PhysicsUpdate() => player.StopMovement();

    public override void Exit() => player.IsInvulnerable = false;
}