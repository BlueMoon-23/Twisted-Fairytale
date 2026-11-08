using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyHP : MonoBehaviour, IDamageable
{
    [SerializeField] private float HP;
    [SerializeField] private float MaxHP;
    [SerializeField] private Color HurtColor;
    private BaseEnemy enemy;
    private SpriteRenderer[] EnemySpriteRenderers;
    private Color[] originalColors;
    private Coroutine hurtRoutine;
    private WaitForSeconds hurtWait = new WaitForSeconds(0.15f);
    private void Awake()
    {
        enemy = GetComponent<BaseEnemy>();
        EnemySpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[EnemySpriteRenderers.Length];
        for (int i = 0; i < EnemySpriteRenderers.Length; i++)
        {
            originalColors[i] = EnemySpriteRenderers[i].color;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public float GetMaxHP() { return MaxHP; }
    public void TakeDamage(float amount, GameObject source)
    {
        if (source == this.gameObject) return;
        HP -= amount;
        if (HP <= 0)
        {
            HP = 0;
            enemy.Die();
            return;
        }
        if (hurtRoutine != null) StopCoroutine(hurtRoutine);
        hurtRoutine = StartCoroutine(HurtEffect());
    }
    public void RegainHP(float amount)
    {
        HP += amount;
        if (HP > MaxHP) HP = MaxHP;
    }
    private IEnumerator HurtEffect()
    {
        for (int i = 0; i < EnemySpriteRenderers.Length; i++)
        {
            SpriteRenderer sprite = EnemySpriteRenderers[i];
            sprite.color = new Color(HurtColor.r, HurtColor.g, HurtColor.b, sprite.color.a);
        }

        yield return hurtWait;
        for (int i = 0; i < EnemySpriteRenderers.Length; i++)
        {
            SpriteRenderer sprite = EnemySpriteRenderers[i];
            Color o = originalColors[i];
            sprite.color = new Color(o.r, o.g, o.b, sprite.color.a);
        }
        hurtRoutine = null;
    }
}
