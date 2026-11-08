using UnityEngine;

/// <summary>
/// Chết: state cuối, không tự chuyển sang state nào khác.
/// Chỉ thoát ra khi gọi PlayerController.Revive().
///
/// Trình tự:
///   Enter        -> dừng lại, tắt va chạm, chơi animation Die, bắn event Died
///   sau deathSequenceTime -> bắn event DeathSequenceFinished (hiện màn Game Over, load checkpoint...)
/// </summary>
public class PlayerDieState : PlayerState
{
    private bool sequenceFinished;

    public PlayerDieState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        sequenceFinished = false;

        player.StopMovement();
        player.IsInvulnerable = true;   // chặn mọi damage tiếp theo
        player.Rb.simulated = false;    // tắt va chạm: enemy đi xuyên qua, không bị đẩy xác

        player.PlayAnimation("Die", restart: true);
        player.RaiseDied();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Không đọc input, không chuyển state: chết là chết.
        if (!sequenceFinished && StateTime >= player.deathSequenceTime)
        {
            sequenceFinished = true;
            player.RaiseDeathSequenceFinished();
        }
    }

    public override void Exit()
    {
        // Chỉ chạy khi Revive()
        player.Rb.simulated = true;
        player.IsInvulnerable = false;
    }
}