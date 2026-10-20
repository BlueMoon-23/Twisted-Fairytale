using System.Collections.Generic;
using UnityEngine;

public class HitDealer : MonoBehaviour
{
    protected GameObject owner;
    protected int damage;
    protected LayerMask hitMask;
    private HashSet<IDamageable> alreadyHit = new();

    public virtual void Init(GameObject owner, int damage, LayerMask hitMask, float lifetime)
    {
        this.owner = owner; this.damage = damage; this.hitMask = hitMask;
        Destroy(gameObject, lifetime);
    }

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if ((hitMask.value & (1 << col.gameObject.layer)) == 0) return;
        if (col.transform.IsChildOf(owner.transform)) return; // không tự đánh mình
        var target = col.GetComponentInParent<IDamageable>();
        if (target != null && alreadyHit.Add(target)) OnHit(target);
    }

    protected virtual void OnHit(IDamageable target) => target.TakeDamage(damage, owner);
}