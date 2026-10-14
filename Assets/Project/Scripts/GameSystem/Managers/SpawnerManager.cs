using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý các Spawner trong scene và nhớ spawn hiện tại.
/// Mỗi scene 1 cái (không DontDestroyOnLoad vì spawner thuộc về scene).
/// </summary>
[DefaultExecutionOrder(-50)] // Awake trước PlayerSpawn để Current sẵn sàng
public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }
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
        if (start == null && spawners.Count > 0)
        {
            start = spawners[0];
            Debug.LogWarning("[SpawnerManager] Không có Spawner nào tick isDefault, dùng tạm " + start.name, start);
        }
        if (start != null) SetCurrent(start);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetCurrent(Spawner spawner)
    {
        if (spawner == null || spawner == Current) return;

        Spawner old = Current;
        Current = spawner;

        if (old != null) old.onDeactivated?.Invoke();
        Current.onActivated?.Invoke();
        CurrentChanged?.Invoke(old, Current);
    }

    /// <summary>Vị trí hồi sinh hiện tại. Không có spawner nào thì trả về fallback.</summary>
    public Vector2 GetSpawnPosition(Vector2 fallback) => Current ? Current.SpawnPosition : fallback;
}