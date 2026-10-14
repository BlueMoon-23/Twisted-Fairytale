using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHP : MonoBehaviour
{
    [SerializeField] private float HP;
    [SerializeField] private float MaxHP;
    private PlayerController player;
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
        if (kb.iKey.isPressed) TakeDamage(10);
        if (kb.kKey.isPressed) player.Revive(Vector2.zero);
    }
    public float GetMaxHP() { return MaxHP; }
    public void TakeDamage(float amount)
    {
        if (player.IsDead || player.IsInvulnerable) return;
        HP -= amount;
        if (HP < 0)
        {
            HP = 0;
            player.Die();
        }
    }
    public void RegainHP(float amount)
    {
        if (player.IsDead || player.IsInvulnerable) return;
        HP += amount;
        if (HP > MaxHP) HP = MaxHP;
    }
}
