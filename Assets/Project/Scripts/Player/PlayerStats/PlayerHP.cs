using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHP : MonoBehaviour, IDamageable, IHealth
{
    [SerializeField] private float HP;
    [SerializeField] private float MaxHP;
    public float CurrentHealth => HP;
    public float MaxHealth => MaxHP;
    public bool IsPlayer => true;
    private PlayerController player;
    public event Action<float, float, bool> HealthChanged;
    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;
        if (kb.kKey.isPressed) player.Revive(Vector2.zero);
    }
    public float GetMaxHP() { return MaxHP; }
    public void TakeDamage(float amount, GameObject source)
    {
        if (source == this.gameObject) return;
        if (player.IsDead || player.IsInvulnerable) return;
        HP -= amount;
        if (HP <= 0f)
        {
            HP = 0f;
            player.Die();
        }
        HealthChanged?.Invoke(HP, MaxHP, true);
    }
    public void RegainHP(float amount)
    {
        if (player.IsDead || player.IsInvulnerable) return;
        HP += amount;
        if (HP > MaxHP) HP = MaxHP;
        HealthChanged?.Invoke(HP, MaxHP, true);
    }
}
