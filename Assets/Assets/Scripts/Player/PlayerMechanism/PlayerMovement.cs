using UnityEngine;

[DefaultExecutionOrder(-90)]
public class PlayerMovement : MonoBehaviour
{
    [Header("Isometric")]
    [Tooltip("0.5 cho isometric tile 2:1. 1 = top-down thường")]
    [SerializeField] private float isoYScale = 0.5f;
    [Tooltip("Ép hướng di chuyển về đúng 8 hướng để khớp animation")]
    [SerializeField] private bool snapTo8Directions = true;

    [Header("Facing")]
    [SerializeField] private Dir8 startFacing = Dir8.S;
    [Tooltip("Thả 2 phím chéo không cùng lúc vẫn giữ hướng chéo")]
    [SerializeField] private float diagonalReleaseGrace = 0.08f;

    [Header("Vật cản")]
    [Tooltip("Layer player không được đi xuyên qua (Obstacle, Wall...)")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Bán kính 'bàn chân' tính từ pivot (pivot phải đặt ở chân)")]
    [SerializeField] private float moveRadius = 0.2f;
    [Tooltip("Khoảng hở giữ lại để không dính sát vào tường")]
    [SerializeField] private float skinWidth = 0.02f;

    public Rigidbody2D Rb { get; private set; }
    public Dir8 Facing { get; private set; }
    public float IsoYScale => isoYScale; // projectile dùng để bay đúng tỉ lệ iso với nhân vật

    private Dir8 lastDiagonal;
    private float lastDiagonalTime = float.NegativeInfinity;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = 0f;
        Rb.freezeRotation = true;
        Facing = startFacing;
    }

    // ===== Facing =====
    public void UpdateFacing(Vector2 move)
    {
        Facing = IsoDirection.FromVector(move);
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

    public void ResetFacing() => Facing = startFacing;

    // ===== Movement =====
    public void MoveInFacingOrInput(Vector2 rawMove, float speed)
    {
        Vector2 dir = snapTo8Directions ? IsoDirection.ToVector(Facing) : rawMove.normalized;
        SetVelocity(IsoDirection.ToIso(dir, isoYScale) * speed);
    }

    /// <summary>Dash/knockback dùng: di chuyển theo 1 hướng màn hình bất kỳ.</summary>
    public void MoveInDirection(Vector2 screenDir, float speed)
        => SetVelocity(IsoDirection.ToIso(screenDir.normalized, isoYScale) * speed);

    // Unity 6: linearVelocity. Unity 2022 trở xuống: đổi thành Rb.velocity
    public void SetVelocity(Vector2 v) => Rb.linearVelocity = BlockByObstacles(v);
    public void StopMovement() => Rb.linearVelocity = Vector2.zero;

    // Dịch chuyển tức thì (hồi sinh, cổng dịch chuyển...): không bị vật cản chặn
    public void Teleport(Vector2 position)
    {
        Rb.position = position;
        transform.position = position;
        StopMovement();
    }

    // Đi thẳng nếu trống; chạm vật cản thì tiến sát tới rồi trượt dọc mặt tường; kẹt góc thì dừng
    private Vector2 BlockByObstacles(Vector2 v)
    {
        if (obstacleMask.value == 0) return v;

        for (int i = 0; i < 2; i++) // lần 2 kiểm tra hướng trượt có đâm vào mặt tường khác không
        {
            if (v.sqrMagnitude < 0.000001f) return Vector2.zero;

            float dist = v.magnitude * Time.fixedDeltaTime + skinWidth;
            RaycastHit2D hit = Physics2D.CircleCast(Rb.position, moveRadius, v.normalized, dist, obstacleMask);

            // Không chạm gì: đi thoải mái. distance = 0 nghĩa là đang kẹt sẵn bên trong vật cản: cho đi để thoát ra
            if (hit.collider == null || hit.distance <= 0f) return v;

            Vector2 n = hit.normal;
            float into = -Vector2.Dot(v, n);                                                // tốc độ đang đâm vào mặt vật cản
            float allowed = Mathf.Max(0f, hit.distance - skinWidth) / Time.fixedDeltaTime;  // còn được tiến bao nhiêu
            if (into > allowed) v += (into - allowed) * n;                                  // bỏ phần thừa, giữ phần trượt dọc
        }
        return Vector2.zero; // vẫn bị chặn sau khi trượt: góc trong, đứng yên
    }

    // Vòng "bàn chân" dùng để dò vật cản, để kiểm tra moveRadius có khớp nhân vật không
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, moveRadius);
    }
}