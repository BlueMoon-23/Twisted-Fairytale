using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý các Spawner trong scene, nhớ checkpoint hiện tại, và hẹn giờ hồi sinh
/// cho mọi IRespawnable (player lẫn enemy).
/// Hẹn giờ ở đây vì manager luôn active: coroutine chạy trên enemy sẽ bị dừng
/// ngay khi enemy tự SetActive(false) lúc chết.
/// Mỗi scene 1 cái (không DontDestroyOnLoad vì spawner thuộc về scene).
/// </summary>
[DefaultExecutionOrder(-50)]
public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }

    /// <summary>Checkpoint hiện tại của những thứ dùng checkpoint (player).</summary>
    public Spawner Current { get; private set; }
    public IReadOnlyList<Spawner> Spawners => spawners;
    /// <summary>(cũ, mới)</summary>
    public event Action<Spawner, Spawner> CurrentChanged;

    private readonly List<Spawner> spawners = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        spawners.AddRange(FindObjectsByType<Spawner>(FindObjectsSortMode.None));

        Spawner start = spawners.Find(s => s.IsDefault);
        if (start == null)
        {
            start = spawners.Find(s => s.IsCheckpoint);
            if (start != null)
                Debug.LogWarning("[SpawnerManager] Không có checkpoint nào tick isDefault, dùng tạm " + start.name, start);
        }
        if (start != null) SetCurrent(start);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetCurrent(Spawner spawner)
    {
        if (spawner == null || spawner == Current || !spawner.IsCheckpoint) return;

        Spawner old = Current;
        Current = spawner;

        if (old != null) old.onDeactivated?.Invoke();
        Current.onActivated?.Invoke();
        CurrentChanged?.Invoke(old, Current);
    }

    /// <summary>
    /// Vị trí hồi sinh của một đối tượng:
    ///   dùng checkpoint -> checkpoint hiện tại
    ///   không dùng      -> HomeSpawner của nó
    ///   không có gì     -> vị trí lúc vào scene
    /// </summary>
    public Vector2 GetSpawnPosition(IRespawnable who)
    {
        if (who.UsesCheckpoint && Current != null) return Current.SpawnPosition;
        if (who.HomeSpawner != null) return who.HomeSpawner.SpawnPosition;
        return who.InitialPosition;
    }

    /// <summary>Giữ lại cho code cũ.</summary>
    public Vector2 GetSpawnPosition(Vector2 fallback) => Current ? Current.SpawnPosition : fallback;

    /// <summary>Hồi sinh sau delay giây. Gọi được cả khi đối tượng đã SetActive(false).</summary>
    public void ScheduleRespawn(IRespawnable who, float delay)
    {
        if (who == null) return;
        StartCoroutine(RespawnAfter(who, delay));
    }

    private IEnumerator RespawnAfter(IRespawnable who, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // Đối tượng bị Destroy trong lúc chờ (vd: đổi scene, boss bị xoá) thì thôi
        if (who is UnityEngine.Object obj && obj == null) yield break;

        who.Respawn(GetSpawnPosition(who));
    }
}