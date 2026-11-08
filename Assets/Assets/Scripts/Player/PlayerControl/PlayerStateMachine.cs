using System;
using UnityEngine;


public class PlayerStateMachine
{
    public PlayerState Current { get; private set; }
    public PlayerState Previous { get; private set; }

    /// <summary>(from, to) - tiện cho debug, SFX, VFX.</summary>
    public event Action<PlayerState, PlayerState> StateChanged;

    public void Initialize(PlayerState start)
    {
        Current = start;
        Current.Enter();
    }

    public void ChangeState(PlayerState next)
    {
        if (next == null || next == Current) return;

        Current.Exit();
        Previous = Current;
        Current = next;
        Current.Enter();

        StateChanged?.Invoke(Previous, Current);
    }
}