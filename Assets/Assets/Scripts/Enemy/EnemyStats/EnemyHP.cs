using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyHP : MonoBehaviour, IDamageable, IHealth
{
    [SerializeField] private float HP;
    [SerializeField] private float MaxHP;
    [SerializeField] private Color HurtColor;
    [SerializeField] private GameObject HitEffect;
    private BaseEnemy enemy;
    private SpriteRenderer[] EnemySpriteRenderers;
    private Color[] originalColors;
    private Coroutine hurtRoutine;
    private WaitForSeconds hurtWait = new WaitForSeconds(0.15f);
    private Collider2D hitEffectArea;
    public event Action<float, float, bool> HealthChanged;
    // DamagePopupManager nghe sự kiện này 1 lần cho mọi enemy
    public static event Action<float, Vector3> AnyDamaged;
    private void Awake()
    {
        enemy = GetComponent<BaseEnemy>();
        EnemySpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[EnemySpriteRenderers.Length];
        for (int i = 0; i < EnemySpriteRenderers.Length; i++)
        {
            originalColors[i] = EnemySpriteRenderers[i].color;
        }

        hitEffectArea = GetComponent<Collider2D>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public float CurrentHealth => HP;
    public float MaxHealth => MaxHP;
    public bool IsPlayer => false;
    public void TakeDamage(float amount, GameObject source)
    {
        if (source == this.gameObject) return;
        HP -= amount;
        AnyDamaged?.Invoke(amount, RandomPointInArea());
        if (HP <= 0)
        {
            HP = 0;
            enemy.Die();
            return;
        }
        HealthChanged?.Invoke(HP, MaxHP, false);
        if (hurtRoutine != null) StopCoroutine(hurtRoutine);
        hurtRoutine = StartCoroutine(HurtEffect());
    }
    public void RegainHP(float amount)
    {
        HP += amount;
        if (HP > MaxHP) HP = MaxHP;
        HealthChanged?.Invoke(HP, MaxHP, false);
    }
    private IEnumerator HurtEffect()
    {
        for (int i = 0; i < EnemySpriteRenderers.Length; i++)
        {
            SpriteRenderer sprite = EnemySpriteRenderers[i];
            sprite.color = new Color(HurtColor.r, HurtColor.g, HurtColor.b, sprite.color.a);
        }
        GameObject newEffect = Instantiate(HitEffect, RandomPointInArea(), Quaternion.identity);
        yield return hurtWait;
        for (int i = 0; i < EnemySpriteRenderers.Length; i++)
        {
            SpriteRenderer sprite = EnemySpriteRenderers[i];
            Color o = originalColors[i];
            sprite.color = new Color(o.r, o.g, o.b, sprite.color.a);
        }
        Destroy(newEffect);
        hurtRoutine = null;
    }
    // Điểm ngẫu nhiên trong hình của hitEffectArea. Tính từ thông số collider nên chạy được cả khi collider bị tắt
    private Vector3 RandomPointInArea()
    {
        if (hitEffectArea == null) return transform.position;
        Transform t = hitEffectArea.transform;

        switch (hitEffectArea)
        {
            case BoxCollider2D box:
                return t.TransformPoint(box.offset + new Vector2(
                    UnityEngine.Random.Range(-0.5f, 0.5f) * box.size.x,
                    UnityEngine.Random.Range(-0.5f, 0.5f) * box.size.y));

            case CircleCollider2D circle:
                return t.TransformPoint(circle.offset + UnityEngine.Random.insideUnitCircle * circle.radius);

            case PolygonCollider2D poly:
                Vector2[] pts = poly.points;
                Vector2 min = pts[0], max = pts[0];
                foreach (Vector2 p in pts) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                for (int i = 0; i < 30; i++) // thử ngẫu nhiên trong khung bao, lấy điểm rơi vào trong đa giác
                {
                    Vector2 p = new(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y));
                    if (InsidePolygon(p, pts)) return t.TransformPoint(poly.offset + p);
                }
                return t.TransformPoint(poly.offset + (min + max) * 0.5f);
            case CapsuleCollider2D capsule:
                {
                    bool vertical = capsule.direction == CapsuleDirection2D.Vertical;
                    float r = (vertical ? capsule.size.x : capsule.size.y) * 0.5f;                       // bán kính đầu tròn
                    float half = Mathf.Max(0f, (vertical ? capsule.size.y : capsule.size.x) * 0.5f - r); // nửa đoạn thẳng giữa

                    for (int i = 0; i < 30; i++) // thử ngẫu nhiên trong khung bao, lấy điểm rơi vào trong viên thuốc
                    {
                        Vector2 p = new(UnityEngine.Random.Range(-0.5f, 0.5f) * capsule.size.x, UnityEngine.Random.Range(-0.5f, 0.5f) * capsule.size.y);
                        Vector2 nearest = vertical ? new Vector2(0f, Mathf.Clamp(p.y, -half, half))
                                                   : new Vector2(Mathf.Clamp(p.x, -half, half), 0f);
                        if ((p - nearest).sqrMagnitude <= r * r) return t.TransformPoint(capsule.offset + p);
                    }
                    return t.TransformPoint(capsule.offset);
                }
        }
        return t.position;
    }

    private static bool InsidePolygon(Vector2 p, Vector2[] pts)
    {
        bool inside = false;
        for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
        {
            if ((pts[i].y > p.y) != (pts[j].y > p.y) &&
                p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                inside = !inside;
        }
        return inside;
    }
}
