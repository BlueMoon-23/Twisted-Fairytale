using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Input cho PC: đọc thẳng phím/chuột.
///   Di chuyển: WASD | Dash: C
///   Chuột trái: bấm nhả nhanh = đòn thường; giữ = heavy
/// </summary>
[DefaultExecutionOrder(-100)] // chạy trước PlayerController để input luôn là của frame hiện tại
public class PlayerInputReader : MonoBehaviour
{
    [Tooltip("Bấm sớm trong khoảng này vẫn được tính (vd: bấm dash khi đang attack)")]
    [SerializeField] private float bufferTime = 0.15f;
    private PlayerController player;
    private PlayerWeapon playerWeapon;
    public Vector2 Move { get; private set; }

    private float attackTime = -1f, heavyAttackTime = -1f, evadeTime = -1f, dashTime = -1f, dodgeTime = -1f, parryTime = -1f;

    [Header("Chuột trái: bấm = đòn thường, giữ = heavy")]
    [Tooltip("Thả tay trước mốc này = đòn thường")]
    [SerializeField] private float tapMaxTime = 0.2f;
    [Tooltip("Giữ tới mốc này = lực tối đa (phạm vi lớn nhất)")]
    [SerializeField] private float fullChargeTime = 1.0f;
    [Tooltip("Bật: đạt lực tối đa thì tự tung đòn, không chờ thả tay")]
    [SerializeField] private bool autoReleaseAtFullCharge = true;

    private float lmbPressTime = -1f; // -1 = không đang giữ
    /// <summary>Đã giữ quá mốc bấm nhanh, đang tích lực heavy.</summary>
    public bool IsChargingHeavy => lmbPressTime >= 0f && Time.time - lmbPressTime >= tapMaxTime;
    private void Awake()
    {
        player = GetComponent<PlayerController>();
        playerWeapon = GetComponent<PlayerWeapon>();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;
        float x = 0f, y = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.sKey.isPressed) y -= 1f;
        if (kb.wKey.isPressed) y += 1f;
        Move = new Vector2(x, y);
        if (kb.cKey.wasPressedThisFrame)
        {
            if (player.currentStateName == "PlayerRunState")
            {
                if (!player.PlayerStamina.IsTired(10)) dashTime = Time.time;
            }
            else
            {
                if (!player.PlayerStamina.IsTired(10)) dodgeTime = Time.time;
            }
        }
        if (kb.fKey.wasPressedThisFrame) parryTime = Time.time;
        if (mouse != null) ReadAttackButton(mouse.leftButton);
        // Change Weapon
        if (kb.digit1Key.wasPressedThisFrame) playerWeapon.DoChangeWeapon(0);
        if (kb.digit2Key.wasPressedThisFrame) playerWeapon.DoChangeWeapon(1);
        if (kb.digit3Key.wasPressedThisFrame) playerWeapon.DoChangeWeapon(2);
        // Ultimate
        if (kb.rKey.wasPressedThisFrame) playerWeapon.PerformUltimate();
    }

    private void ReadAttackButton(ButtonControl lmb)
    {
        if (lmb.wasPressedThisFrame) lmbPressTime = Time.time;
        if (lmbPressTime < 0f) return;
        float held = Time.time - lmbPressTime;
        // !isPressed: chặn trường hợp lỡ mất sự kiện thả (alt-tab giữa chừng)
        bool released = lmb.wasReleasedThisFrame || !lmb.isPressed;

        if (released)
        {
            if (held < tapMaxTime) attackTime = Time.time; // bấm nhanh
            else FireHeavy();                // giữ chưa tới max: lực theo thời gian giữ
            lmbPressTime = -1f;
        }
        else if (autoReleaseAtFullCharge && held >= fullChargeTime)
        {
            FireHeavy();       // giữ tới max: tự tung đòn mạnh nhất
            lmbPressTime = -1f;  // phải thả ra rồi bấm lại mới tích tiếp
        }
    }

    private void FireHeavy()
    {
        heavyAttackTime = Time.time;
    }
    // Consume = lấy và xoá, tránh 1 lần bấm kích hoạt 2 lần
    public bool ConsumeAttack() => Consume(ref attackTime);
    public bool ConsumeHeavyAttack()
    {
        return Consume(ref heavyAttackTime);
    }
    public bool ConsumeDash() => Consume(ref dashTime);
    public bool ConsumeDodge() => Consume(ref dodgeTime);
    public bool ConsumeEvade() => Consume(ref evadeTime);
    public bool ConsumeParry() => Consume(ref parryTime);

    private bool Consume(ref float pressedTime)
    {
        if (pressedTime >= 0f && Time.time - pressedTime <= bufferTime)
        {
            pressedTime = -1f;
            return true;
        }
        return false;
    }
}