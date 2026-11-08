using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitOverTimeDealer : HitDealer
{
    [SerializeField, Min(1)] private int tickCount = 5;
    [SerializeField, Min(0f)] private float tickInterval = 0.2f;

    private Collider2D hitbox;
    private readonly List<Collider2D> overlaps = new();
    private readonly HashSet<IDamageable> hitThisTick = new();
    private Coroutine tickRoutine;

    protected virtual void Awake() => hitbox = GetComponent<Collider2D>();

    /// <summary>Dùng số tick và khoảng cách tick đang đặt trong Inspector.</summary>
    public override void Init(GameObject owner, float damage, LayerMask hitMask, float lifetime)
    {
        base.Init(owner, damage, hitMask, lifetime);
        if (tickRoutine != null) StopCoroutine(tickRoutine);
        tickRoutine = StartCoroutine(TickLoop());
    }

    /// <summary>Đặt số tick bằng code, lifetime tự tính để đủ thời gian cho mọi tick.</summary>
    public void Init(GameObject owner, float damage, LayerMask hitMask, int tickCount, float tickInterval)
    {
        this.tickCount = Mathf.Max(1, tickCount);
        this.tickInterval = Mathf.Max(0f, tickInterval);
        float lifetime = this.tickInterval * (this.tickCount - 1) + Time.fixedDeltaTime * 2f;
        Init(owner, damage, hitMask, lifetime);
    }

    // Không gây damage khi vừa chạm vào, damage chỉ đến từ tick.
    protected override void OnTriggerEnter2D(Collider2D col) { }

    private IEnumerator TickLoop()
    {
        // Chờ physics cập nhật vị trí hitbox xong rồi mới quét tick đầu tiên.
        yield return new WaitForFixedUpdate();

        var wait = new WaitForSeconds(tickInterval);
        for (int i = 0; i < tickCount; i++)
        {
            DealTick();
            if (i < tickCount - 1) yield return wait;
        }
        tickRoutine = null;
    }

    private void DealTick()
    {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(hitMask);

        overlaps.Clear();
        hitbox.Overlap(filter, overlaps);

        // Một mục tiêu có nhiều collider vẫn chỉ ăn 1 lần mỗi tick.
        hitThisTick.Clear();
        foreach (var col in overlaps)
        {
            var target = GetTarget(col);
            if (target != null && hitThisTick.Add(target)) OnHit(target);
        }
    }
}