using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Điểm hồi sinh. Hai vai trò:
///   isCheckpoint = true : checkpoint của player. Thứ dùng checkpoint chạm vào -> thành spawn hiện tại.
///                         Cần 1 Collider2D tick "Is Trigger" trên cùng object.
///   isCheckpoint = false: chỉ là điểm spawn riêng cho enemy (gán vào BaseEnemy.homeSpawner).
///                         Không cần collider, player đi qua không bị đổi checkpoint.
/// </summary>
public class Spawner : MonoBehaviour
{
    [Tooltip("Bật: player chạm vào sẽ thành checkpoint. Tắt: chỉ là điểm spawn của enemy")]
    [SerializeField] private bool isCheckpoint = true;
    [Tooltip("Checkpoint mặc định khi vào scene (chỉ nên tick 1 cái)")]
    [SerializeField] private bool isDefault;
    [Tooltip("Vị trí hồi sinh cụ thể. Để trống = vị trí của chính object này")]
    [SerializeField] private Transform spawnPoint;

    [Header("Feedback (tuỳ chọn, chỉ dùng cho checkpoint)")]
    public UnityEvent onActivated;    // bật lửa, đổi sprite, phát âm thanh...
    public UnityEvent onDeactivated;

    public bool IsCheckpoint => isCheckpoint;
    public bool IsDefault => isCheckpoint && isDefault;
    public Vector2 SpawnPosition => spawnPoint ? spawnPoint.position : transform.position;

    private void Reset()
    {
        if (TryGetComponent(out Collider2D col)) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCheckpoint) return;

        var who = other.GetComponentInParent<IRespawnable>();
        if (who == null || !who.UsesCheckpoint || who.IsDead) return; // enemy đi qua: bỏ qua

        SpawnerManager.Instance?.SetCurrent(this);
    }

    private void OnDrawGizmos()
    {
        if (!isCheckpoint)
        {
            Gizmos.color = Color.magenta; // điểm spawn của enemy
        }
        else
        {
            bool current = Application.isPlaying && SpawnerManager.Instance != null && SpawnerManager.Instance.Current == this;
            Gizmos.color = current ? Color.green : (isDefault ? Color.cyan : Color.yellow);
        }
        Gizmos.DrawWireSphere(SpawnPosition, 0.2f);
    }
}