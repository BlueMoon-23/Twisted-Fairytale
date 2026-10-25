using System.Collections.Generic;
using UnityEngine;

public class HitDealer : MonoBehaviour
{
    protected GameObject owner;
    [field: SerializeField] public float damage { get; protected set; }
    protected LayerMask hitMask;
    private HashSet<IDamageable> alreadyHit = new();

    public virtual void Init(GameObject owner, float damage, LayerMask hitMask, float lifetime)
    {
        this.owner = owner; this.damage = damage; this.hitMask = hitMask;
        Destroy(gameObject, lifetime);
    }

    protected virtual void OnTriggerEnter2D(Collider2D col) => TryHit(col);

    /// <summary>
    /// Kiểm tra layer, không tự đánh mình, mỗi mục tiêu chỉ trúng 1 lần, rồi gây damage.
    /// Class con gọi được (vd: AOE quét vùng lúc nổ). Trả về true nếu trúng.
    /// </summary>
    protected bool TryHit(Collider2D col)
    {
        if ((hitMask.value & (1 << col.gameObject.layer)) == 0) return false;
        // owner có thể đã bị huỷ (enemy chết trước khi AOE nổ)
        if (owner != null && col.transform.IsChildOf(owner.transform)) return false;
        var target = col.GetComponentInParent<IDamageable>();
        if (target == null || !alreadyHit.Add(target)) return false;
        OnHit(target);
        return true;
    }

    protected virtual void OnHit(IDamageable target) => target.TakeDamage(damage, owner);
}