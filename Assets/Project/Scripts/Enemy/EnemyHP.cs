using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyHP : MonoBehaviour, IDamageable
{
    [SerializeField] private float HP;
    [SerializeField] private float MaxHP;
    private BaseEnemy enemy;
    private void Awake()
    {
        enemy = GetComponent<BaseEnemy>();
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
        if (HP < 0)
        {
            HP = 0;
            enemy.Die();
        }
    }
    public void RegainHP(float amount)
    {
        HP += amount;
        if (HP > MaxHP) HP = MaxHP;
    }
}
