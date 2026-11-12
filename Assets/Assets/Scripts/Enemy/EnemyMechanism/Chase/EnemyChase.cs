using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy dí theo BaseEnemy.TargetPlayer.
///   Nhìn thẳng thấy player (không tường chắn) -> chạy thẳng tới.
///   Bị chắn -> hỏi PathGrid tìm đường vòng (A*), đi theo từng điểm, định kỳ tìm lại vì player di chuyển.
/// Mọi bước di chuyển đều được chặn bởi vật cản trong PathGrid.ObstacleMask,
/// nên kể cả khi collider của enemy là trigger, enemy vẫn không đi xuyên vật cản.
/// Cần PathGrid trong scene; không có thì chỉ chạy thẳng và không chặn vật cản.
/// </summary>
public class EnemyChase : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [Tooltip("Tới gần player cỡ này thì dừng (để tấn công)")]
    [SerializeField] private float stopDistance = 0.6f;
    [Tooltip("Dừng tấn công trong bao lâu")]
    [SerializeField] private float stopInterval = 1f;
    [Tooltip("Bao lâu tìm lại đường một lần khi đang đi vòng")]
    [SerializeField] private float repathInterval = 0.25f;
    [Tooltip("Tới gần điểm trên đường cỡ này thì chuyển sang điểm kế")]
    [SerializeField] private float waypointTolerance = 0.1f;
    [Tooltip("Phải khớp isoYScale của PlayerMovement để tốc độ trên màn hình giống nhau")]
    [SerializeField] private float isoYScale = 0.5f;
    [Tooltip("Khoảng hở giữ lại để không dính sát vào vật cản")]
    [SerializeField] private float skinWidth = 0.02f;
    [SerializeField] private bool drawPath = true;
    private float pauseUntil; // tới mốc này mới được đi tiếp
    public bool IsMoving { get; private set; }
    public Vector2 MoveDirection { get; private set; }
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
        if (rb != null) rb.linearVelocity = Vector2.zero; // tắt component thì không trượt tiếp
    }

    private void FixedUpdate()
    {
        if (Time.time < pauseUntil) { Stop(); return; }
        PlayerController target = enemy.TargetPlayer;
        if (target == null || target.IsDead || enemy.IsDead) { Stop(); return; }
        Vector2 pos = rb.position;
        Vector2 goal = target.Rb.position;
        if ((goal - pos).sqrMagnitude <= stopDistance * stopDistance) { Stop(); return; }
        PathGrid grid = PathGrid.Instance;
        if (grid == null || grid.HasClearPath(pos, goal))
        {
            ClearPath();
            MoveToward(goal);
            return;
        }
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            pathIndex = 0;
            if (!grid.FindPath(pos, goal, path))
            {
                Stop();
                return;
            }
        }
        else if (!IsDetouring)
        {
            Stop();
            return;
        }
        float tol = waypointTolerance * waypointTolerance;
        while (pathIndex < path.Count && (path[pathIndex] - pos).sqrMagnitude <= tol) pathIndex++;
        if (pathIndex < path.Count) MoveToward(path[pathIndex]);
        else MoveToward(goal);
    }

    private void MoveToward(Vector2 point)
    {
        Vector2 worldDir = point - rb.position;
        if (worldDir.sqrMagnitude < 0.000001f) { Stop(); return; }
        Vector2 screenDir = new Vector2(worldDir.x, worldDir.y / isoYScale).normalized;
        MoveDirection = screenDir;
        Vector2 v = BlockByObstacles(IsoDirection.ToIso(screenDir, isoYScale) * moveSpeed);
        rb.linearVelocity = v;
        IsMoving = v.sqrMagnitude > 0.000001f;
    }

    // Giống PlayerMovement: chạm vật cản thì tiến sát rồi trượt dọc mặt vật cản, kẹt góc thì dừng.
    // Dùng chung obstacleMask và agentRadius với PathGrid để đường tìm được và cách đi khớp nhau.
    private Vector2 BlockByObstacles(Vector2 v)
    {
        PathGrid grid = PathGrid.Instance;
        if (grid == null || grid.ObstacleMask.value == 0) return v;

        for (int i = 0; i < 2; i++)
        {
            if (v.sqrMagnitude < 0.000001f) return Vector2.zero;

            float dist = v.magnitude * Time.fixedDeltaTime + skinWidth;
            RaycastHit2D hit = Physics2D.CircleCast(rb.position, grid.AgentRadius, v.normalized, dist, grid.ObstacleMask);

            // Không chạm gì: đi thoải mái. distance = 0: đang kẹt sẵn trong vật cản, cho đi để thoát ra
            if (hit.collider == null || hit.distance <= 0f) return v;

            Vector2 n = hit.normal;
            float into = -Vector2.Dot(v, n);
            float allowed = Mathf.Max(0f, hit.distance - skinWidth) / Time.fixedDeltaTime;
            if (into > allowed) v += (into - allowed) * n;
        }
        return Vector2.zero;
    }

    private void Stop()
    {
        rb.linearVelocity = Vector2.zero;
        IsMoving = false;
    }
    public IEnumerator DoAttack()
    {
        pauseUntil = Time.time + stopInterval; // FixedUpdate thấy đang tạm dừng thì đứng yên
        Stop();
        yield return new WaitForSeconds(stopInterval);
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