using System.Collections;
using UnityEngine;

/// <summary>
/// Stamina được xài cho heavy attack, dash / dodge
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private float Stamina;
    [SerializeField] private float MaxStamina;
    [Tooltip("Tốc độ hồi stamina mỗi cooldown")]
    [SerializeField] private float RegainSpeed;
    [SerializeField] private float RegainCooldown;
    [SerializeField] private float regenDelay = 0.6f; // chờ sau lần dùng gần nhất

    private float lastUseTime = float.NegativeInfinity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(RegainLoop());
    }

    // Update is called once per frame
    void Update()
    {

    }
    public bool IsTired (float amount)
    {
        if (Time.time - lastUseTime < regenDelay) return true;
        return Stamina - amount < 0;
    }
    public void ConsumeStamina(float amount)
    {
        if (IsTired(amount)) return;
        Stamina -= amount;
        lastUseTime = Time.time;
        if (Stamina < 0) Stamina = 0;
    }
    public void RegainStamina(float amount)
    {
        Stamina += amount;
        if (Stamina >= MaxStamina) Stamina = MaxStamina;
    }
    private IEnumerator RegainLoop()
    {
        while (Stamina <= MaxStamina)
        {
            RegainStamina(RegainSpeed);
            yield return new WaitForSeconds(RegainCooldown);
        }
    }

    public bool TryConsume(float amount)
    {
        if (Stamina < amount) return false;
        Stamina -= amount;
        lastUseTime = Time.time;
        return true;
    }
}
