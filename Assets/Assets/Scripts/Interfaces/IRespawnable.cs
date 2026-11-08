using UnityEngine;

/// <summary>
/// Thứ gì chết rồi hồi sinh được tại một Spawner: player, enemy, NPC...
/// SpawnerManager chỉ làm việc qua interface này, không cần biết đó là ai.
/// </summary>
public interface IRespawnable
{
    bool IsDead { get; }
    /// <summary>
    /// true: player: có đổi checkpoint
    /// false: enemy: không bao giờ đổi checkpoint, luôn hồi sinh tại HomeSpawner của riêng nó.
    /// </summary>
    bool UsesCheckpoint { get; }
    /// <summary>Spawner riêng (cho enemy). null = hồi sinh tại InitialPosition.</summary>
    Spawner HomeSpawner { get; }
    /// <summary>Vị trí lúc vào scene, dùng khi không có spawner nào phù hợp.</summary>
    Vector2 InitialPosition { get; }
    /// <summary>Đưa về vị trí, hồi máu, bật lại. Chỉ có tác dụng khi đang chết.</summary>
    void Respawn(Vector2 position);
}