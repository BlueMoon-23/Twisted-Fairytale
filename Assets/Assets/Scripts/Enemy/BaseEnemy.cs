using UnityEngine;

public class BaseEnemy : MonoBehaviour, IRespawnable
{
    public PlayerController TargetPlayer { get; set; }
    public LayerMask attackHitMask;

    [Header("Respawn")]
    [SerializeField] private float respawnCooldown = 5f;
    [Tooltip("Spawner riêng của enemy này (bỏ tick isCheckpoint). Trống = hồi sinh tại vị trí đặt ban đầu")]
    [SerializeField] private Spawner homeSpawner;

    // ===== IRespawnable =====
    public bool IsDead { get; protected set; }
    public bool UsesCheckpoint => false;       // enemy không bao giờ đổi checkpoint
    public Spawner HomeSpawner => homeSpawner;
    public Vector2 InitialPosition { get; private set; }

    protected virtual void Awake()
    {
        InitialPosition = transform.position;
    }

    public virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // Hẹn giờ ở SpawnerManager: coroutine chạy trên chính enemy sẽ chết theo SetActive(false)
        if (SpawnerManager.Instance != null)
            SpawnerManager.Instance.ScheduleRespawn(this, respawnCooldown);
        else
            Debug.LogWarning("[BaseEnemy] Scene không có SpawnerManager, enemy sẽ không hồi sinh", this);

        gameObject.SetActive(false);
    }

    public virtual void Respawn(Vector2 position)
    {
        if (!IsDead) return;

        transform.position = position;
        IsDead = false;
        gameObject.SetActive(true);

        if (TryGetComponent(out EnemyHP hp)) hp.RegainHP(hp.GetMaxHP());
    }
}