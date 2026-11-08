using System;

// Thứ gì có máu để hiển thị: PlayerHP, EnemyHP, boss...
// HealthBar chỉ biết interface này nên dùng chung cho mọi loại.
public interface IHealth
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsPlayer { get; }
    // (máu hiện tại, máu tối đa). Bắn mỗi khi máu thay đổi: trúng đòn, hồi máu, hồi sinh.
    event Action<float, float, bool> HealthChanged;
}