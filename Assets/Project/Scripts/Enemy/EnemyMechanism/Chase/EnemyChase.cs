using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy dí theo BaseEnemy.TargetPlayer.
///   Nhìn thẳng thấy player (không tường chắn) -> chạy thẳng tới.
///   Bị chắn -> hỏi PathGrid tìm đường vòng (A*), đi theo từng điểm, định kỳ tìm lại vì player di chuyển.
/// Cần PathGrid trong scene; không có thì chỉ chạy thẳng.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BaseEnemy))]
public class EnemyChase : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [Tooltip("Tới gần player cỡ này thì dừng (để tấn công)")]
    [SerializeField] private float stopDistance = 0.6f;
    [Tooltip("Bao lâu tìm lại đường một lần khi đang đi vòng")]
    [SerializeField] private float repathInterval = 0.25f;
    [Tooltip("Tới gần điểm trên đường cỡ này thì chuyển sang điểm kế")]
    [SerializeField] private float waypointTolerance = 0.1f;
    [Tooltip("Phải khớp isoYScale của PlayerController để tốc độ trên màn hình giống nhau")]
    [SerializeField] private float isoYScale = 0.5f;
    [SerializeField] private bool drawPath = true;

    public bool IsMoving { get; private set; }
    /// <summary>Hướng màn hình đang đi, dùng cho Facing/animation: IsoDirection.FromVector(MoveDirection).</summary>
    public Vector2 MoveDirection { get; private set; }
    /// <summary>true khi đang đi đường vòng (bị chắn), false khi chạy thẳng hoặc đứng yên.</summary>
    public bool IsDetouring => pathIndex < path.Count;

    private Rigidbody2D rb;
    private BaseEnemy enemy;
    private readonly List<Vector2> path = new();
    private int pathIndex;
    private float nextRepathTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemy = GetComponent<BaseEnemy>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void OnDisable()
    {
        ClearPath();
        IsMoving = false;
    }

    private void FixedUpdate()
    {
        PlayerController target = enemy.TargetPlayer;
        if (target == null || target.IsDead || enemy.IsDead) { Stop(); return; }

        Vector2 pos = rb.position;
        Vector2 goal = target.Rb.position;

        if ((goal - pos).sqrMagnitude <= stopDistance * stopDistance) { Stop(); return; }

        PathGrid grid = PathGrid.Instance;

        // 1. Không có gì chắn: chạy thẳng, bỏ đường vòng cũ
        if (grid == null || grid.HasClearPath(pos, goal))
        {
            ClearPath();
            MoveToward(goal);
            return;
        }

        // 2. Bị chắn: tìm đường vòng (giới hạn tần suất vì A* tốn hơn raycast)
        if (!IsDetouring || Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            pathIndex = 0;
            if (!grid.FindPath(pos, goal, path))
            {
                Stop(); // không có đường (player ở chỗ kín): đứng chờ, lần sau tìm lại
                return;
            }
        }

        // Bỏ qua các điểm đã tới
        float tol = waypointTolerance * waypointTolerance;
        while (pathIndex < path.Count && (path[pathIndex] - pos).sqrMagnitude <= tol) pathIndex++;

        if (pathIndex < path.Count) MoveToward(path[pathIndex]);
        else MoveToward(goal);
    }

    private void MoveToward(Vector2 point)
    {
        Vector2 worldDir = point - rb.position;
        if (worldDir.sqrMagnitude < 0.000001f) { Stop(); return; }

        // Đổi sang hướng màn hình rồi ép iso lại, giống PlayerController.MoveInDirection:
        // giữ đúng hướng tới điểm đích, còn tốc độ đi dọc trên màn hình khớp với player.
        Vector2 screenDir = new Vector2(worldDir.x, worldDir.y / isoYScale).normalized;
        MoveDirection = screenDir;
        rb.linearVelocity = IsoDirection.ToIso(screenDir, isoYScale) * moveSpeed;
        IsMoving = true;
    }

    private void Stop()
    {
        rb.linearVelocity = Vector2.zero;
        IsMoving = false;
    }

    private void ClearPath()
    {
        path.Clear();
        pathIndex = 0;
    }

    private void OnDrawGizmos()
    {
        if (!drawPath || !Application.isPlaying || !IsDetouring) return;
        Gizmos.color = Color.yellow;
        Vector3 prev = rb.position;
        for (int i = pathIndex; i < path.Count; i++)
        {
            Gizmos.DrawLine(prev, path[i]);
            Gizmos.DrawWireSphere(path[i], 0.06f);
            prev = path[i];
        }
    }
}