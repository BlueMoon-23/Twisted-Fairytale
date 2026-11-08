using UnityEngine;

public class MovingProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [Tooltip("An toàn: tự huỷ sau chừng này giây dù HitDealer có huỷ hay không")]
    [SerializeField] private float maxLifetime = 3f;

    [Header("Va chạm")]
    [Tooltip("Chạm layer nào thì dừng và huỷ (Enemy, Wall...). Không gồm layer của người bắn")]
    [SerializeField] private LayerMask stopMask;
    [Tooltip("Bật: xuyên qua mục tiêu, chỉ dừng khi hết giờ")]
    [SerializeField] private bool pierce = false;

    [Header("Hiển thị")]
    [Tooltip("Góc bù nếu sprite gốc không chĩa sang phải (vd: chĩa lên trên thì -90)")]
    [SerializeField] private float spriteAngleOffset = 0f;

    private Vector2 velocity;
    private bool launched;

    /// <summary>Bắn theo 1 trong 8 hướng (vd: player.Facing).</summary>
    public void Launch(Dir8 dir, float isoYScale) => Launch(IsoDirection.ToVector(dir), isoYScale);

    /// <summary>Bắn theo hướng màn hình bất kỳ (vd: hướng chuột sau này).</summary>
    public void Launch(Vector2 screenDir, float isoYScale)
    {
        if (screenDir.sqrMagnitude < 0.0001f) screenDir = Vector2.right;

        // Cùng công thức với PlayerController.MoveInDirection để tên bay đúng hướng nhân vật nhìn
        velocity = IsoDirection.ToIso(screenDir.normalized, isoYScale) * speed;

        // Xoay sprite theo hướng bay THẬT (sau khi ép iso), không phải hướng màn hình
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);

        launched = true;
        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        if (!launched) return;
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!launched) return;
        if ((stopMask.value & (1 << other.gameObject.layer)) == 0) return;
        if (pierce && !IsWall(other)) return;

        // Destroy chờ hết frame, nên OnTriggerEnter2D của HitDealer trong cùng bước vật lý
        // vẫn chạy và gây damage trước khi tên biến mất.
        launched = false;
        Destroy(gameObject);
    }

    // Xuyên mục tiêu nhưng vẫn phải dừng khi gặp tường: coi collider không có Rigidbody là tường.
    private static bool IsWall(Collider2D c) => c.attachedRigidbody == null;
}