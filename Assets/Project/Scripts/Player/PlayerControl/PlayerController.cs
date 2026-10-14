using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "Context" của FSM: giữ component, thông số, và các hàm dùng chung cho mọi state.
/// Bản thân class này KHÔNG chứa logic chuyển state - việc đó thuộc về từng state.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // General stats
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    // Dash / Dodge
    [Header("Dash (C khi đang Run)")]
    public float dashSpeed = 9f;
    public float dashDuration = 0.5f;
    [Header("Dodge (C khi Idle/Walk)")]
    public float dodgeSpeed = 6f;
    public float dodgeDuration = 0.3f;
    [Tooltip("Khoảng bất tử tính từ đầu dodge")]
    public float dodgeIFrames = 0.25f;
    [Header("Dash/Dodge chung")]
    [SerializeField] private float evadeCooldown = 0.3f;
    public bool IsInvulnerable { get; set; }
    public bool CanEvade => Time.time >= evadeReadyTime;
    private float evadeReadyTime;
    // Attack / Parry 
    [Header("Attack (chuột trái)")]
    [Tooltip("Tổng thời gian đòn đánh. Nên = độ dài clip Attack")]
    public float attackDuration = 0.4f;
    [Tooltip("Hitbox bật trong khoảng [start, end] (giây tính từ đầu đòn)")]
    public float attackHitStart = 0.12f;
    public float attackHitEnd = 0.22f;
    [Tooltip("Sau mốc này được nối đòn (bấm chuột tiếp) hoặc huỷ bằng dash/dodge")]
    public float attackCancelTime = 0.25f;
    [Tooltip("Lướt tới trước trong lúc vung đòn")]
    public float attackLungeSpeed = 1.5f;
    public float attackReach = 0.35f;
    public float attackRadius = 0.3f;
    public int attackDamage = 1;
    public LayerMask attackHitMask;
    [Header("Parry (F)")]
    [Tooltip("Khoảng đỡ được đòn tính từ đầu parry")]
    public float parryWindow = 0.2f;
    [Tooltip("Tổng thời gian parry nếu đỡ trượt (gồm cả hồi chiêu)")]
    public float parryDuration = 0.45f;
    [Tooltip("Thời gian khựng lại sau khi đỡ thành công, trước khi điều khiển lại")]
    public float parrySuccessRecovery = 0.1f;
    // Isometric directions
    [Tooltip("0.5 cho isometric tile 2:1. 1 = top-down thường")]
    [SerializeField] private float isoYScale = 0.5f;
    [Tooltip("Ép hướng di chuyển về đúng 8 hướng để khớp animation (quan trọng với gamepad)")]
    [SerializeField] private bool snapTo8Directions = true;
    [SerializeField] private float inputDeadzone = 0.2f;
    [Header("Facing")]
    [SerializeField] private Dir8 startFacing = Dir8.S;
    [Tooltip("Thả 2 phím chéo không cùng lúc vẫn giữ hướng chéo")]
    [SerializeField] private float diagonalReleaseGrace = 0.08f;
    // Animation
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Bật nếu clip *_Side trong art gốc quay mặt sang PHẢI")]
    [SerializeField] private bool artFacesRight = true;

    [Header("Debug (read only)")]
    [SerializeField] private string currentStateName;

    public Rigidbody2D Rb { get; private set; }
    public PlayerInputReader Input { get; private set; }
    public Animator Animator => animator;
    public PlayerStateMachine Machine { get; private set; }
    public Dir8 Facing { get; private set; }

    // ===== States =====
    public PlayerIdleState IdleState { get; private set; }
    public PlayerWalkState WalkState { get; private set; }
    public PlayerRunState RunState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    public PlayerDodgeState DodgeState { get; private set; }
    public PlayerParryState ParryState { get; private set; }

    private Dir8 lastDiagonal;
    private float lastDiagonalTime = float.NegativeInfinity;

    // Cache hash theo tên gốc: "Walk" -> [Walk_Down, Walk_Down_Side, Walk_Side, Walk_Up_Side, Walk_Up]
    private readonly Dictionary<string, int[]> animHashes = new();
    private int currentAnimHash;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = 0f;
        Rb.freezeRotation = true;
        Input = GetComponent<PlayerInputReader>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Machine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, Machine);
        WalkState = new PlayerWalkState(this, Machine);
        RunState = new PlayerRunState(this, Machine);
        AttackState = new PlayerAttackState(this, Machine);
        DashState = new PlayerDashState(this, Machine);
        DodgeState = new PlayerDodgeState(this, Machine);
        ParryState = new PlayerParryState(this, Machine);
    }

    private void Start()
    {
        Facing = startFacing;
        Machine.Initialize(IdleState);
    }

    private void Update()
    {
        Machine.Current.LogicUpdate();
        currentStateName = Machine.Current.ToString();
    }
    private void LateUpdate()
    {
        Vector3 newPosition = transform.position;
        newPosition.z = -10f; // Ép camera lùi lại Z = -10 để nhìn thấy nhân vật
        Camera.main.transform.position = newPosition;
    }
    private void FixedUpdate() => Machine.Current.PhysicsUpdate();

    // ===== Input helpers =====
    public Vector2 RawMove => Input.Move;
    public bool HasMoveInput => RawMove.sqrMagnitude > inputDeadzone * inputDeadzone;

    /// <summary>State locomotion phù hợp với input hiện tại. Action state (attack/dash) gọi khi kết thúc.</summary>
    public PlayerState GetLocomotionState()
    {
        if (!HasMoveInput) return IdleState;
        return Input.RunHeld ? RunState : WalkState;
    }

    // ===== Facing =====
    public void UpdateFacingFromInput()
    {
        if (!HasMoveInput) return;
        Facing = IsoDirection.FromVector(RawMove);
        if (IsoDirection.IsDiagonal(Facing))
        {
            lastDiagonal = Facing;
            lastDiagonalTime = Time.time;
        }
    }

    /// <summary>Gọi khi dừng lại: nếu vừa thả phím chéo lệch nhịp thì khôi phục hướng chéo.</summary>
    public void ResolveFacingOnStop()
    {
        if (!IsoDirection.IsDiagonal(Facing) && Time.time - lastDiagonalTime <= diagonalReleaseGrace)
            Facing = lastDiagonal;
    }

    /// <summary>Cho sau này: attack hướng theo chuột, bị đánh bật lùi...</summary>
    public void SetFacing(Dir8 dir) => Facing = dir;

    // ===== Movement =====
    public void MoveInFacingOrInput(float speed)
    {
        Vector2 dir = snapTo8Directions ? IsoDirection.ToVector(Facing) : RawMove.normalized;
        SetVelocity(IsoDirection.ToIso(dir, isoYScale) * speed);
    }

    /// <summary>Dash/knockback dùng: di chuyển theo 1 hướng màn hình bất kỳ.</summary>
    public void MoveInDirection(Vector2 screenDir, float speed)
        => SetVelocity(IsoDirection.ToIso(screenDir.normalized, isoYScale) * speed);

    // Unity 6: linearVelocity. Unity 2022 trở xuống: đổi thành Rb.velocity
    public void SetVelocity(Vector2 v) => Rb.linearVelocity = v;
    public void StopMovement() => Rb.linearVelocity = Vector2.zero;

    // ===== Dash/Dodge =====
    public bool TryEvade()
    {
        if (!CanEvade || !Input.ConsumeEvade()) return false;
        bool running = GetLocomotionState() == RunState;
        Machine.ChangeState(running ? DashState : DodgeState);
        return true;
    }
    public void StartEvadeCooldown() => evadeReadyTime = Time.time + evadeCooldown;

    // ===== Attack/Parry =====
    public Vector2 AttackHitboxCenter
    => Rb.position + IsoDirection.ToIso(IsoDirection.ToVector(Facing), isoYScale) * attackReach;
    
    // ===== Animation =====
    /// <summary>
    /// Chơi clip "{baseName}_{hướng}" theo Facing và lật sprite.
    /// Gọi mỗi frame cũng được: clip không đổi thì bỏ qua.
    /// Đổi hướng giữa cùng loại (Walk_Side -> Walk_Up_Side) vẫn giữ nhịp bước chân.
    /// </summary>
    public void PlayAnimation(string baseName, bool restart = false)
    {
        var (index, flip) = IsoDirection.ToAnim(Facing);
        spriteRenderer.flipX = artFacesRight ? flip : !flip;

        int[] hashes = GetAnimHashes(baseName);
        int hash = hashes[index];
        if (hash == currentAnimHash && !restart) return;

        float startTime = 0f;
        if (!restart && System.Array.IndexOf(hashes, currentAnimHash) >= 0)
            startTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;

        animator.Play(hash, 0, startTime);
        currentAnimHash = hash;
    }

    /// <summary>Cho clip không loop (attack, parry...): đã chạy hết chưa.</summary>
    public bool IsAnimationFinished
    {
        get
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == currentAnimHash && info.normalizedTime >= 1f;
        }
    }

    private int[] GetAnimHashes(string baseName)
    {
        if (animHashes.TryGetValue(baseName, out var hashes)) return hashes;

        hashes = new int[IsoDirection.AnimSuffixes.Length];
        for (int i = 0; i < hashes.Length; i++)
        {
            string stateName = $"{baseName}_{IsoDirection.AnimSuffixes[i]}";
            hashes[i] = Animator.StringToHash(stateName);
#if UNITY_EDITOR
            if (!animator.HasState(0, hashes[i]))
                Debug.LogWarning($"[PlayerController] Animator thiếu state '{stateName}'", this);
#endif
        }
        animHashes[baseName] = hashes;
        return hashes;
    }
}