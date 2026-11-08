using System.Collections.Generic;
using UnityEngine;

// Tự chọn mục tiêu cho đòn đánh của player (soft lock).
//   Chọn enemy gần nhất trong tầm, mọi hướng (360°), không phụ thuộc hướng đang bấm.
//   Đã khoá một enemy thì giữ qua các nhát combo, trừ khi nó chết, ra quá xa hoặc bị che.
// Hướng đánh luôn làm tròn về 8 hướng (Dir8) để khớp animation, hitbox và cách lướt isometric.
[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Tự chọn mục tiêu")]
    [SerializeField] private bool autoTarget = true;
    [Tooltip("Layer hurtbox của enemy. KHÔNG gồm Detection")]
    [SerializeField] private LayerMask targetMask;
    [Tooltip("Tầm chọn mục tiêu, đo trên màn hình (đã bù tỉ lệ iso)")]
    [SerializeField] private float lockRange = 3f;
    [Tooltip("Đang khoá thì giữ mục tiêu tới khi nó ra xa hơn lockRange × hệ số này")]
    [SerializeField] private float keepRangeMultiplier = 1.3f;
    [Tooltip("Thôi đánh quá chừng này giây thì bỏ khoá, lần sau chọn lại từ đầu")]
    [SerializeField] private float lockMemory = 1.5f;
    [Tooltip("Không khoá enemy bị vật cản che (Obstacle, Wall...)")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Mục tiêu gần hơn khoảng này thì không lướt tới nữa, tránh lướt xuyên qua enemy")]
    [SerializeField] private float lungeStopDistance = 0.6f;

    [Header("Hiển thị (tuỳ chọn)")]
    [Tooltip("Object đánh dấu mục tiêu, vd: vòng tròn dưới chân enemy. Không làm con của Player")]
    [SerializeField] private Transform targetMarker;
    [Tooltip("Tự đổi cỡ marker theo bề ngang hình của enemy")]
    [SerializeField] private bool autoMarkerSize = true;
    [Tooltip("Marker rộng bằng chừng này lần bề ngang hình enemy")]
    [SerializeField] private float markerWidthFactor = 0.6f;
    [SerializeField] private float markerMinWidth = 0.5f;
    [SerializeField] private float markerMaxWidth = 4f;

    public Transform CurrentTarget { get; private set; }

    private PlayerController player;
    private readonly List<Collider2D> hits = new();
    private ContactFilter2D filter;
    private float lastAimTime = float.NegativeInfinity;
    private Transform preview;

    // Vị trí/cỡ marker tính 1 lần khi đổi mục tiêu, rồi bám theo mục tiêu.
    // Không tính lại mỗi frame vì cánh, tay chân đang animation làm khung hình co giãn -> marker rung.
    private Transform markerTarget;
    private Vector2 markerOffset;
    private float markerWidth;
    private readonly List<SpriteRenderer> rendererBuffer = new();

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        filter = new ContactFilter2D { useTriggers = true }; // hurtbox enemy thường là trigger
    }

    private void Update()
    {
        // Chỉ để hiện marker cho người chơi biết sẽ đánh vào ai. Lúc ra đòn vẫn chọn lại.
        preview = autoTarget && !player.IsDead ? PickTarget() : null;
    }

    private void LateUpdate() => UpdateMarker(preview); // sau khi enemy đã di chuyển trong frame

    // Gọi lúc bắt đầu mỗi nhát đánh: xoay player về phía mục tiêu (làm tròn 8 hướng),
    // trả về hướng đánh trên màn hình để lướt và đặt hitbox.
    public Vector2 AimForAttack()
    {
        CurrentTarget = autoTarget ? PickTarget() : null;
        lastAimTime = Time.time;

        if (CurrentTarget != null)
            player.SetFacing(IsoDirection.FromVector(ScreenDelta(CurrentTarget.position)));
        else
            player.UpdateFacingFromInput(); // như cũ: hướng đang bấm, đứng yên thì hướng đang nhìn

        return IsoDirection.ToVector(player.Facing);
    }

    // Đòn có được lướt tới không: đã sát mục tiêu thì đứng lại đánh
    public bool ShouldLunge()
        => CurrentTarget == null || ScreenDelta(CurrentTarget.position).magnitude > lungeStopDistance;

    private Transform PickTarget()
    {
        Vector2 origin = player.Rb.position;

        // 1. Giữ mục tiêu đang khoá nếu còn hợp lệ (combo không bị nhảy mục tiêu)
        if (CurrentTarget != null && Time.time - lastAimTime <= lockMemory &&
            IsValid(CurrentTarget, origin, lockRange * keepRangeMultiplier))
            return CurrentTarget;

        // 2. Chọn mới: gần nhất trong tầm, mọi hướng.
        //    Hình tròn world bán kính lockRange đã bao trọn vùng elip trên màn hình.
        filter.SetLayerMask(targetMask);
        hits.Clear();
        Physics2D.OverlapCircle(origin, lockRange, filter, hits);

        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (Collider2D col in hits)
        {
            Transform t = GetTargetRoot(col);
            if (t == null || t == best) continue; // 1 enemy có thể có nhiều collider
            if (!IsValid(t, origin, lockRange)) continue;

            float d = ScreenDelta(t.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }

    private bool IsValid(Transform t, Vector2 origin, float range)
    {
        if (t == null || !t.gameObject.activeInHierarchy) return false;
        if (t.TryGetComponent(out BaseEnemy enemy) && enemy.IsDead) return false;
        if (ScreenDelta(t.position).magnitude > range) return false;

        if (obstacleMask.value != 0 && Physics2D.Linecast(origin, t.position, obstacleMask).collider != null)
            return false;

        return true;
    }

    // Đổi collider trúng được thành "con enemy" để so sánh: ưu tiên BaseEnemy, không thì thứ nhận damage
    private Transform GetTargetRoot(Collider2D col)
    {
        if (col.transform.IsChildOf(transform)) return null; // không tự khoá mình
        BaseEnemy enemy = col.GetComponentInParent<BaseEnemy>();
        if (enemy != null) return enemy.transform;
        return col.GetComponentInParent<IDamageable>() is Component c ? c.transform : null;
    }

    // Chênh lệch vị trí đổi sang "hướng màn hình": ngược lại với IsoDirection.ToIso
    private Vector2 ScreenDelta(Vector2 worldPos)
    {
        Vector2 d = worldPos - player.Rb.position;
        return new Vector2(d.x, d.y / player.IsoYScale);
    }

    private void UpdateMarker(Transform target)
    {
        if (targetMarker == null) return;
        bool show = target != null;
        if (targetMarker.gameObject.activeSelf != show) targetMarker.gameObject.SetActive(show);
        if (!show) { markerTarget = null; return; }

        if (target != markerTarget) MeasureMarker(target);
        targetMarker.position = (Vector2)target.position + markerOffset;

        if (autoMarkerSize && targetMarker.TryGetComponent(out SpriteRenderer sr) && sr.sprite != null)
        {
            float spriteWidth = sr.sprite.bounds.size.x; // cỡ gốc của sprite marker khi scale = 1
            if (spriteWidth > 0.0001f)
            {
                float s = markerWidth / spriteWidth;
                targetMarker.localScale = new Vector3(s, s * player.IsoYScale, 1f); // ép dẹt cho nằm trên sàn
            }
        }
    }

    // Tìm chỗ đặt marker cho 1 enemy:
    //   có TargetAnchor (đặt tay) -> dùng điểm và cỡ trong đó
    //   không có -> đáy giữa của toàn bộ hình (mọi SpriteRenderer của enemy), không phụ thuộc pivot
    private void MeasureMarker(Transform target)
    {
        markerTarget = target;
        markerOffset = Vector2.zero;
        markerWidth = markerMinWidth;

        TargetAnchor anchor = target.GetComponentInChildren<TargetAnchor>();
        bool hasPoint = anchor != null && anchor.point != null;
        bool hasWidth = anchor != null && anchor.markerWidth > 0f;
        if (hasPoint) markerOffset = (Vector2)(anchor.point.position - target.position);
        if (hasWidth) markerWidth = anchor.markerWidth;
        if (hasPoint && hasWidth) return;

        rendererBuffer.Clear();
        target.GetComponentsInChildren(false, rendererBuffer);

        bool any = false;
        Bounds b = default;
        foreach (SpriteRenderer r in rendererBuffer)
        {
            if (!r.enabled || r.sprite == null || r.transform == targetMarker) continue;
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!any) return; // enemy không có hình: đặt ở pivot

        if (!hasPoint) markerOffset = new Vector2(b.center.x, b.min.y) - (Vector2)target.position;
        if (!hasWidth) markerWidth = Mathf.Clamp(b.size.x * markerWidthFactor, markerMinWidth, markerMaxWidth);
    }

    // Vùng chọn mục tiêu là hình elip trên sàn (tròn trên màn hình theo cách tính iso)
    private void OnDrawGizmosSelected()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        float k = movement != null ? movement.IsoYScale : 0.5f;
        Vector3 c = transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 prev = c + new Vector3(lockRange, 0f, 0f);
        for (int i = 1; i <= 40; i++)
        {
            float a = i / 40f * Mathf.PI * 2f;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * lockRange, Mathf.Sin(a) * lockRange * k, 0f);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        if (Application.isPlaying && CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(c, CurrentTarget.position);
        }
    }
}