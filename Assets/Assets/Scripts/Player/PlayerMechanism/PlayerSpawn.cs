using UnityEngine;

/// <summary>
/// Gắn lên Player. Vào scene -> đặt player ở spawn mặc định.
/// Chết xong (hết animation) -> hồi sinh ở spawn hiện tại.
/// </summary>
public class PlayerSpawn : MonoBehaviour
{
    [Tooltip("Đặt player vào spawn mặc định khi bắt đầu scene")]
    [SerializeField] private bool spawnAtStart = true;

    private PlayerController player;

    private void Awake() => player = GetComponent<PlayerController>();

    private void OnEnable() => player.DeathSequenceFinished += Respawn;
    private void OnDisable() => player.DeathSequenceFinished -= Respawn;

    private void Start()
    {
        if (spawnAtStart && SpawnerManager.Instance != null)
            Teleport(SpawnerManager.Instance.GetSpawnPosition(player.Rb.position));
    }

    public void Respawn()
    {
        Vector2 pos = SpawnerManager.Instance != null
            ? SpawnerManager.Instance.GetSpawnPosition(player.Rb.position)
            : player.Rb.position;

        player.Revive(pos);

        // TODO: đổi theo tên hàm trong class của bạn
        // player.HP.ResetHP();
        // player.Stamina.ResetStamina();
    }

    private void Teleport(Vector2 pos)
    {
        player.Rb.position = pos;
        transform.position = pos;
        player.StopMovement();
    }
}