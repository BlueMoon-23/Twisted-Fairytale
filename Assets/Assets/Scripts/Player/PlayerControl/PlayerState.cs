using UnityEngine;

public abstract class PlayerState
{
    protected readonly PlayerController player;
    protected readonly PlayerStateMachine machine;

    /// <summary>Thời gian (giây) đã ở trong state hiện tại. Dùng cho dash duration, attack window...</summary>
    protected float StateTime { get; private set; }

    protected PlayerState(PlayerController player, PlayerStateMachine machine)
    {
        this.player = player;
        this.machine = machine;
    }

    public virtual void Enter() => StateTime = 0f;
    public virtual void Exit() { }

    /// <summary>Gọi trong Update: đọc input, quyết định chuyển state, chọn animation.</summary>
    public virtual void LogicUpdate() => StateTime += Time.deltaTime;

    /// <summary>Gọi trong FixedUpdate: chỉ xử lý vật lý (velocity).</summary>
    public virtual void PhysicsUpdate() { }

    public override string ToString() => GetType().Name;
}

