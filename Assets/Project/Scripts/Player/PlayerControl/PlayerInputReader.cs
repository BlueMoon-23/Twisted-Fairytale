using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input cho PC: đọc thẳng phím/chuột.
///   Di chuyển: WASD | Chạy: giữ Left Shift | Dash: C | Parry: F | Attack: chuột trái
/// </summary>
[DefaultExecutionOrder(-100)] // chạy trước PlayerController để input luôn là của frame hiện tại
public class PlayerInputReader : MonoBehaviour
{
    [Tooltip("Bấm sớm trong khoảng này vẫn được tính (vd: bấm dash khi đang attack)")]
    [SerializeField] private float bufferTime = 0.15f;
    private PlayerController player;
    public Vector2 Move { get; private set; }
    public bool RunHeld { get; private set; }

    private float attackTime = -1f, heavyAttackTime = -1f, evadeTime = -1f, dashTime = -1f, dodgeTime = -1f, parryTime = -1f;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
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
        RunHeld = kb.leftShiftKey.isPressed;
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
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) attackTime = Time.time;
        if (kb.ctrlKey.wasPressedThisFrame) heavyAttackTime = Time.time;
    }

    // Consume = lấy và xoá, tránh 1 lần bấm kích hoạt 2 lần
    public bool ConsumeAttack() => Consume(ref attackTime);
    public bool ConsumeHeavyAttack() => Consume(ref heavyAttackTime);
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