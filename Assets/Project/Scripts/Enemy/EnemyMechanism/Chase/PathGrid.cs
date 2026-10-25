using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lưới tìm đường 2D cho cả scene (mỗi scene 1 cái).
/// Lúc Awake quét Physics2D để đánh dấu ô nào bị tường chặn.
///
/// Thuật toán: A* = Dijkstra + heuristic (ước lượng khoảng cách còn lại tới đích).
/// Đặt heuristicWeight = 0 là Dijkstra thuần: cùng kết quả nhưng duyệt nhiều ô hơn hẳn.
/// </summary>
[DefaultExecutionOrder(-40)]
public class PathGrid : MonoBehaviour
{
    public static PathGrid Instance { get; private set; }

    [Header("Vùng lưới (world space, tâm = vị trí object này)")]
    [SerializeField] private Vector2 size = new(40f, 40f);
    [Tooltip("Cỡ 1 ô. Nhỏ = đường mượt, chui được khe hẹp, nhưng nhiều ô hơn")]
    [SerializeField] private float cellSize = 0.25f;

    [Header("Vật cản")]
    [Tooltip("Chỉ layer tường/vật cản tĩnh. KHÔNG gồm Player, Enemy")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Bán kính thân enemy: ô cách tường ít hơn khoảng này coi như bị chặn, để enemy không cạ góc tường")]
    [SerializeField] private float agentRadius = 0.2f;

    [Header("Tìm đường")]
    [Tooltip("1 = A* chuẩn. 0 = Dijkstra thuần. >1 = nhanh hơn nhưng đường có thể vòng hơn")]
    [SerializeField] private float heuristicWeight = 1f;
    [Tooltip("Giới hạn số ô duyệt mỗi lần tìm, tránh giật khung hình khi không có đường")]
    [SerializeField] private int maxIterations = 6000;
    [Tooltip("Làm mượt: nhìn trước tối đa chừng này điểm để cắt góc")]
    [SerializeField] private int smoothLookahead = 16;

    [Header("Debug")]
    [SerializeField] private bool drawBlockedCells = true;

    public float AgentRadius => agentRadius;
    public LayerMask ObstacleMask => obstacleMask;

    private int width, height;
    private Vector2 origin; // góc dưới trái
    private bool[] walkable;

    // Dữ liệu A* cấp phát 1 lần. "stamp" thay cho việc xoá mảng mỗi lần tìm.
    private float[] gCost;
    private int[] parent;
    private int[] openStamp, closedStamp;
    private int stamp;
    private readonly MinHeap open = new();
    private readonly List<Vector2> smoothBuffer = new();

    // 4 hướng thẳng trước, 4 hướng chéo sau
    private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };
    private const float DiagCost = 1.41421356f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Dựng lại toàn bộ lưới (đổi kích thước, load map mới).</summary>
    public void Build()
    {
        width = Mathf.Max(1, Mathf.CeilToInt(size.x / cellSize));
        height = Mathf.Max(1, Mathf.CeilToInt(size.y / cellSize));
        origin = (Vector2)transform.position - size * 0.5f;

        int n = width * height;
        walkable = new bool[n];
        gCost = new float[n];
        parent = new int[n];
        openStamp = new int[n];
        closedStamp = new int[n];
        stamp = 0;

        RefreshCells(0, 0, width - 1, height - 1);
    }

    /// <summary>Quét lại 1 vùng khi vật cản thay đổi: cửa mở, tường bị phá, đổi góc nhìn...</summary>
    public void RefreshArea(Bounds worldBounds)
    {
        if (walkable == null) return;
        int pad = Mathf.CeilToInt(Mathf.Max(agentRadius, cellSize * 0.5f) / cellSize) + 1;
        Vector2Int a = WorldToCell(worldBounds.min);
        Vector2Int b = WorldToCell(worldBounds.max);
        RefreshCells(Mathf.Max(0, a.x - pad), Mathf.Max(0, a.y - pad),
                     Mathf.Min(width - 1, b.x + pad), Mathf.Min(height - 1, b.y + pad));
    }

    private void RefreshCells(int x0, int y0, int x1, int y1)
    {
        // Không nhỏ hơn nửa ô, nếu không tường mỏng nằm giữa 2 tâm ô sẽ lọt
        float r = Mathf.Max(agentRadius, cellSize * 0.5f);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                walkable[Index(x, y)] = Physics2D.OverlapCircle(CellCenter(x, y), r, obstacleMask) == null;
    }

    // ===== Toạ độ =====
    public Vector2Int WorldToCell(Vector2 p)
        => new(Mathf.FloorToInt((p.x - origin.x) / cellSize), Mathf.FloorToInt((p.y - origin.y) / cellSize));

    public Vector2 CellCenter(int x, int y)
        => origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

    private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
    private int Index(int x, int y) => y * width + x;

    /// <summary>Đi thẳng từ a tới b có bị tường chặn không (tính cả bề ngang thân enemy).</summary>
    public bool HasClearPath(Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        float dist = d.magnitude;
        if (dist < 0.0001f) return true;
        return Physics2D.CircleCast(a, agentRadius, d / dist, dist, obstacleMask).collider == null;
    }

    // ===== A* =====
    /// <summary>
    /// Tìm đường từ `from` tới `to`, ghi các điểm cần đi qua vào `result` (không gồm điểm xuất phát).
    /// Trả về false nếu không có đường hoặc vượt maxIterations.
    /// </summary>
    public bool FindPath(Vector2 from, Vector2 to, List<Vector2> result)
    {
        result.Clear();
        if (walkable == null) return false;

        // Đứng sát tường thì ô của mình có thể bị tính là chặn: lấy ô trống gần nhất
        if (!TryNearestWalkable(from, out Vector2Int s)) return false;
        if (!TryNearestWalkable(to, out Vector2Int g)) return false;

        int start = Index(s.x, s.y);
        int goal = Index(g.x, g.y);
        if (start == goal) { result.Add(CellCenter(g.x, g.y)); return true; }

        stamp++;
        open.Clear();
        gCost[start] = 0f;
        parent[start] = -1;
        openStamp[start] = stamp;
        open.Push(start, Heuristic(s.x, s.y, g.x, g.y));

        int iterations = 0;
        while (open.Count > 0 && iterations++ < maxIterations)
        {
            int cur = open.Pop();
            if (closedStamp[cur] == stamp) continue; // bản cũ còn sót trong heap
            closedStamp[cur] = stamp;

            if (cur == goal)
            {
                BuildPath(goal, result);
                Smooth(from, result);
                return true;
            }

            int cx = cur % width, cy = cur / width;
            for (int d = 0; d < 8; d++)
            {
                int nx = cx + DX[d], ny = cy + DY[d];
                if (!InBounds(nx, ny)) continue;

                int ni = Index(nx, ny);
                if (!walkable[ni] || closedStamp[ni] == stamp) continue;

                bool diagonal = d >= 4;
                // Không cắt góc: đi chéo thì 2 ô kề cũng phải trống
                if (diagonal && (!walkable[Index(nx, cy)] || !walkable[Index(cx, ny)])) continue;

                float newCost = gCost[cur] + (diagonal ? DiagCost : 1f);
                if (openStamp[ni] == stamp && newCost >= gCost[ni]) continue;

                openStamp[ni] = stamp;
                gCost[ni] = newCost;
                parent[ni] = cur;
                open.Push(ni, newCost + heuristicWeight * Heuristic(nx, ny, g.x, g.y));
            }
        }
        return false;
    }

    // Khoảng cách octile: đúng chi phí đi 8 hướng khi không có vật cản
    private static float Heuristic(int x0, int y0, int x1, int y1)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        return dx + dy + (DiagCost - 2f) * Mathf.Min(dx, dy);
    }

    private void BuildPath(int goal, List<Vector2> result)
    {
        for (int i = goal; parent[i] != -1; i = parent[i])
            result.Add(CellCenter(i % width, i / width));
        result.Reverse();
    }

    // String pulling: từ mỗi điểm, nhảy thẳng tới điểm xa nhất còn nhìn thấy,
    // để enemy đi chéo tự nhiên thay vì bò theo bậc thang của lưới.
    private void Smooth(Vector2 from, List<Vector2> pts)
    {
        if (pts.Count <= 1) return;
        smoothBuffer.Clear();

        Vector2 anchor = from;
        int i = 0;
        while (i < pts.Count)
        {
            int far = i;
            int last = Mathf.Min(pts.Count - 1, i + smoothLookahead);
            for (int j = last; j > i; j--)
            {
                if (HasClearPath(anchor, pts[j])) { far = j; break; }
            }
            smoothBuffer.Add(pts[far]);
            anchor = pts[far];
            i = far + 1;
        }

        pts.Clear();
        pts.AddRange(smoothBuffer);
    }

    // Đứng sát tường thì ô dưới chân có thể bị tính là chặn (do agentRadius): tìm ô trống gần nhất.
    // Bắt buộc ô đó phải nhìn thẳng thấy được từ vị trí thật, nếu không sẽ "nhảy" xuyên tường
    // sang vùng trống bên kia (ngoài sàn) và A* không bao giờ tìm ra đường.
    private bool TryNearestWalkable(Vector2 worldPos, out Vector2Int result)
    {
        Vector2Int c = WorldToCell(worldPos);
        c.x = Mathf.Clamp(c.x, 0, width - 1);
        c.y = Mathf.Clamp(c.y, 0, height - 1);
        result = c;
        if (walkable[Index(c.x, c.y)]) return true;

        float bestDist = float.MaxValue;
        bool found = false;
        for (int r = 1; r <= 6; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // chỉ xét viền vòng r
                    int x = c.x + dx, y = c.y + dy;
                    if (!InBounds(x, y) || !walkable[Index(x, y)]) continue;

                    Vector2 center = CellCenter(x, y);
                    float d = (center - worldPos).sqrMagnitude;
                    if (d >= bestDist) continue;

                    // Linecast mảnh (không dùng CircleCast: đang đứng sát tường thì CircleCast luôn chạm)
                    if (Physics2D.Linecast(worldPos, center, obstacleMask).collider != null) continue;

                    bestDist = d;
                    result = new Vector2Int(x, y);
                    found = true;
                }
        return found;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, size);

        if (!drawBlockedCells || walkable == null) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Vector3 cube = new(cellSize * 0.9f, cellSize * 0.9f, 0f);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!walkable[Index(x, y)]) Gizmos.DrawCube(CellCenter(x, y), cube);
    }

    // Binary heap nhỏ nhất. Unity dùng .NET Standard 2.1 nên chưa có PriorityQueue.
    // Cho phép trùng phần tử (lazy deletion): bản cũ bị bỏ qua nhờ closedStamp.
    private class MinHeap
    {
        private readonly List<int> items = new();
        private readonly List<float> keys = new();
        public int Count => items.Count;

        public void Clear() { items.Clear(); keys.Clear(); }

        public void Push(int item, float key)
        {
            items.Add(item); keys.Add(key);
            int i = items.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (keys[p] <= keys[i]) break;
                Swap(i, p); i = p;
            }
        }

        public int Pop()
        {
            int top = items[0];
            int last = items.Count - 1;
            items[0] = items[last]; keys[0] = keys[last];
            items.RemoveAt(last); keys.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < items.Count && keys[l] < keys[m]) m = l;
                if (r < items.Count && keys[r] < keys[m]) m = r;
                if (m == i) break;
                Swap(i, m); i = m;
            }
            return top;
        }

        private void Swap(int a, int b)
        {
            (items[a], items[b]) = (items[b], items[a]);
            (keys[a], keys[b]) = (keys[b], keys[a]);
        }
    }
}