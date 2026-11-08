using UnityEngine;

// Tuỳ chọn, gắn lên enemy (object gốc hoặc con) khi vị trí/cỡ marker tự tính chưa đẹp:
// Enemy không có component này thì PlayerAttack tự đặt marker ở đáy giữa hình.
public class TargetAnchor : MonoBehaviour
{
    [Tooltip("Điểm đặt marker (tạo 1 Empty con đặt dưới thân). Trống = tự tính từ đáy hình")]
    public Transform point;
    [Tooltip("Bề ngang marker (world). 0 = tự tính theo bề ngang hình")]
    public float markerWidth = 0f;

    private void OnDrawGizmosSelected()
    {
        if (point == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, markerWidth > 0f ? markerWidth * 0.5f : 0.3f);
    }
}