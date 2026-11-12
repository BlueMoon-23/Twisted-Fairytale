using UnityEngine;

/// <summary>
/// Gắn lên BiteRange, không gắn lên Enemy
/// </summary>
public class Bite : MonoBehaviour
{
    [SerializeField] private float BiteDamage;
    [Tooltip("Cắn xong chờ bao lâu mới cắn tiếp nếu player vẫn đứng trong tầm")]
    [SerializeField] private float BiteCooldown = 1f;

    private BaseEnemy enemy;
    private float nextBiteTime;

    private void Awake()
    {
        enemy = GetComponentInParent<BaseEnemy>();
    }

    // Enter: cắn ngay khi player vừa bước vào tầm
    private void OnTriggerEnter2D(Collider2D collision) => TryBite(collision);

    // Stay: player đứng yên trong tầm thì cứ hết cooldown là cắn tiếp
    private void OnTriggerStay2D(Collider2D collision) => TryBite(collision);

    private void TryBite(Collider2D collision)
    {
        if (Time.time < nextBiteTime || enemy.IsDead) return;

        // Collider chạm vào có thể là con của Player (hitbox, body...): lấy Player qua Rigidbody2D
        if (collision.attachedRigidbody == null ||
            !collision.attachedRigidbody.TryGetComponent(out PlayerController player)) return;
        if (player != enemy.TargetPlayer || player.IsDead) return;
        if (!player.TryGetComponent(out IDamageable target)) return;

        nextBiteTime = Time.time + BiteCooldown; // player có nhiều collider: chặn cắn nhiều lần trong 1 lượt
        enemy.StopToAttack();
        target.TakeDamage(BiteDamage, enemy.gameObject);
    }
}