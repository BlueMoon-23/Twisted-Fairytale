using UnityEngine;

public class PlayerDodgeState : PlayerState
{
    private Vector2 dir;

    public PlayerDodgeState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        player.UpdateFacingFromInput();
        dir = IsoDirection.ToVector(player.Facing);
        player.IsInvulnerable = true;
        player.PlayAnimation("Dodge", restart: true);
        player.PlayerStamina.ConsumeStamina(10);
        player.currentStateName = "Dodge";
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (StateTime >= player.dodgeIFrames)
            player.IsInvulnerable = false;

        if (StateTime >= player.dodgeDuration)
            machine.ChangeState(player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        float t = Mathf.Clamp01(StateTime / player.dodgeDuration);
        player.MoveInDirection(-1 * dir, player.dodgeSpeed * Mathf.Lerp(1f, 0.4f, t));
    }

    public override void Exit()
    {
        player.IsInvulnerable = false;
        player.StartEvadeCooldown();
    }
}
