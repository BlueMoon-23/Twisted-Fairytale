using UnityEngine;

/// <summary>Thứ gì nhận được đòn đánh (enemy, player, thùng gỗ...) thì implement interface này.</summary>
public interface IDamageable
{
    void TakeDamage(int amount, GameObject source);
}
