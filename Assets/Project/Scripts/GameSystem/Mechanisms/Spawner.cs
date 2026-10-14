using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Điểm hồi sinh (checkpoint). Player chạm vào collider -> thành spawn hiện tại.
/// Cần 1 Collider2D tick "Is Trigger" trên cùng object.
/// </summary>
public class Spawner : MonoBehaviour
{
    [Tooltip("Spawn mặc định khi vào scene (chỉ nên tick 1 cái)")]
    [SerializeField] private bool isDefault;
    [Tooltip("Vị trí hồi sinh cụ thể. Để trống = vị trí của chính object này")]
    [SerializeField] private Transform spawnPoint;

    [Header("Feedback (tuỳ chọn)")]
    public UnityEvent onActivated;    // bật lửa, đổi sprite, phát âm thanh...
    public UnityEvent onDeactivated;

    public bool IsDefault => isDefault;
    public Vector2 SpawnPosition => spawnPoint ? spawnPoint.position : transform.position;

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == null) return;
        if (!other.attachedRigidbody.TryGetComponent(out PlayerController player)) return;
        if (player.IsDead) return;

        SpawnerManager.Instance?.SetCurrent(this);
    }

    private void OnDrawGizmos()
    {
        bool current = Application.isPlaying && SpawnerManager.Instance?.Current == this;
        Gizmos.color = current ? Color.green : (isDefault ? Color.cyan : Color.yellow);
        Gizmos.DrawWireSphere(SpawnPosition, 0.2f);
    }
}