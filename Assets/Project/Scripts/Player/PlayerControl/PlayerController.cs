using System;
using UnityEngine;

/// <summary>
/// "Context" của FSM: giữ component, thông số, và các hàm dùng chung cho mọi state.
/// Bản thân class này KHÔNG chứa logic chuyển state - việc đó thuộc về từng state.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // General stats
    [Header("Movement")]
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
    public PlayerAttackData normalAttack;
    public PlayerAttackData heavyAttack;
    [Header("Ultimate (nút R)")]
    public PlayerAttackData ultimateAttack;
    public float attackReach = 0.35f;
    public LayerMask attackHitMask;
    [Header("Parry (F)")]
    [Tooltip("Khoảng đỡ được đòn tính từ đầu parry")]
    public float parryWindow = 0.2f;
    [Tooltip("Tổng thời gian parry nếu đỡ trượt (gồm cả hồi chiêu)")]
    public float parryDuration = 0.45f;
    [Tooltip("Thời gian khựng lại sau khi đỡ thành công, trước khi điều khiển lại")]
    public float parrySuccessRecovery = 0.1f;
    // Die
    [Header("Die")]
    public float deathSequenceTime = 1.5f;
    // Isometric directions
    [Header("Isometric")]
    [Tooltip("0.5 cho isometric tile 2:1. 1 = top-down thường")]
    [SerializeField] private float isoYScale = 0.5f;
    [Tooltip("Ép hướng di chuyển về đúng 8 hướng để khớp animation")]
    [SerializeField] private bool snapTo8Directions = true;
    [SerializeField] private float inputDeadzone = 0.2f;
    [Header("Facing")]
    [SerializeField] private Dir8 startFacing = Dir8.S;
    [Tooltip("Thả 2 phím chéo không cùng lúc vẫn giữ hướng chéo")]
    [SerializeField] private float diagonalReleaseGrace = 0.08f;
    // Animation
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Debug (read only)")]
    [field: SerializeField] public string currentStateName { get; set; }

    public Rigidbody2D Rb { get; private set; }
    public PlayerInputReader Input { get; private set; }
    public Animator Animator => animator;
    public PlayerStateMachine Machine { get; private set; }
    public Dir8 Facing { get; private set; }

    // ===== States =====
    public PlayerIdleState IdleState { get; private set; }
    public PlayerRunState RunState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    public PlayerDodgeState DodgeState { get; private set; }
    public PlayerParryState ParryState { get; private set; }
    public PlayerDieState DieState { get; private set; }

    // ===== Stats =====
    public PlayerHP PlayerHP { get; private set; }
    public PlayerStamina PlayerStamina { get; private set; }

    public event Action Died;
    public event Action DeathSequenceFinished;
    public bool IsDead => Machine.Current == DieState;

    private Dir8 lastDiagonal;
    private float lastDiagonalTime = float.NegativeInfinity;

    // Tham số blend tree (Float) trong Animator
    private static readonly int FaceXParam = Animator.StringToHash("FaceX");
    private static readonly int FaceYParam = Animator.StringToHash("FaceY");
    private int currentStateHash;

    private Camera cam;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = 0f;
        Rb.freezeRotation = true;
        Input = GetComponent<PlayerInputReader>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        cam = Camera.main;

        Machine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, Machine);
        RunState = new PlayerRunState(this, Machine);
        AttackState = new PlayerAttackState(this, Machine);
        DashState = new PlayerDashState(this, Machine);
        DodgeState = new PlayerDodgeState(this, Machine);
        ParryState = new PlayerParryState(this, Machine);
        DieState = new PlayerDieState(this, Machine);
        PlayerHP = GetComponent<PlayerHP>();
        PlayerStamina = GetComponent<PlayerStamina>();
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
        UpdateAnimatorFacing();
    }

    private void LateUpdate()
    {
        if (cam == null) return;
        Vector3 newPosition = transform.position;
        newPosition.z = -10f; // Ép camera lùi lại Z = -10 để nhìn thấy nhân vật
        cam.transform.position = newPosition;
    }

    private void FixedUpdate() => Machine.Current.PhysicsUpdate();

    // ===== Input helpers =====
    public Vector2 RawMove => Input.Move;
    public bool HasMoveInput => RawMove.sqrMagnitude > inputDeadzone * inputDeadzone;

    /// <summary>State locomotion phù hợp với input hiện tại. Action state (attack/dash) gọi khi kết thúc.</summary>
    public PlayerState GetLocomotionState()
    {
        if (!HasMoveInput) return IdleState;
        return RunState;
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

    /// <summary>Đòn ứng với nút vừa bấm (heavy ưu tiên), null nếu không bấm gì.</summary>
    public PlayerAttackData ConsumeAttackInput()
    {
        if (Input.ConsumeHeavyAttack()) return heavyAttack;
        if (Input.ConsumeAttack()) return normalAttack;
        return null;
    }

    /// <summary>Gọi từ Idle/Walk/Run: có bấm đánh thì vào AttackState với đúng đòn.</summary>
    public bool TryAttack()
    {
        var attack = ConsumeAttackInput();
        if (attack == null) return false;
        AttackState.SetAttack(attack);
        Machine.ChangeState(AttackState);
        return true;
    }

    // ===== Die =====
    public void Die()
    {
        if (IsDead) return;
        Machine.ChangeState(DieState);
    }

    public void Revive(Vector2 position)
    {
        if (!IsDead) return;
        Rb.position = position;
        transform.position = position;
        Facing = startFacing;
        Machine.ChangeState(IdleState);
        PlayerHP.RegainHP(PlayerHP.GetMaxHP());
    }

    public void RaiseDied() => Died?.Invoke();
    public void RaiseDeathSequenceFinished() => DeathSequenceFinished?.Invoke();

    // ===== Animation (Blend Tree 8 hướng) =====
    /// <summary>
    /// Chuyển sang blend tree của hành động ("Idle", "Walk", "Run", "Punch"...).
    /// Hướng do blend tree tự chọn qua FaceX/FaceY nên không cần truyền.
    /// Gọi mỗi frame cũng được: state không đổi thì bỏ qua.
    /// </summary>
    public void PlayAnimation(string stateName, bool restart = false)
    {
        int hash = Animator.StringToHash(stateName);
        if (hash == currentStateHash && !restart) return;

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning($"[PlayerController] Animator chưa có state '{stateName}'", this);
            return;
        }

        UpdateAnimatorFacing(); // set hướng trước khi Play để frame đầu đúng clip
        animator.Play(hash, 0, 0f);
        currentStateHash = hash;
    }

    /// <summary>Cho clip không loop (attack, parry, die...): đã chạy hết chưa.</summary>
    public bool IsAnimationFinished
    {
        get
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == currentStateHash && info.normalizedTime >= 1f;
        }
    }

    /// <summary>Gửi hướng hiện tại cho blend tree.</summary>
    private void UpdateAnimatorFacing()
    {
        Vector2 dir = IsoDirection.ToVector(Facing);
        animator.SetFloat(FaceXParam, dir.x);
        animator.SetFloat(FaceYParam, dir.y);
    }
}