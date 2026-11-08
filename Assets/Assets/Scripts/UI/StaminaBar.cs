using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [Tooltip("Thanh trắng, đặt NGAY TRƯỚC fillImage trong Hierarchy để vẽ phía sau. Cũng Filled, Horizontal")]
    [SerializeField] private Image whiteImage;
    [SerializeField] private TextMeshProUGUI StaminaText;
    [Header("Tween")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    [Header("Thanh trắng")]
    [Tooltip("Mất máu xong chờ chừng này giây thanh trắng mới tụt. Trúng đòn liên tiếp thì chờ lại từ đầu")]
    [SerializeField] private float whiteDelay = 0.15f;
    [SerializeField] private float whiteDuration = 0.3f;
    [SerializeField] private Ease whiteEase = Ease.InQuad;

    [SerializeField] private PlayerStamina player;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnEnable()
    {
        player.StaminaChanged += OnStaminaChanged;
    }
    private void OnDisable()
    {
        player.StaminaChanged -= OnStaminaChanged;
    }

    private void OnStaminaChanged(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // Thanh thật: chạy ngay tới giá trị mới
        fillImage.DOKill();
        fillImage.DOFillAmount(ratio, duration).SetEase(ease);

        // Thanh trắng: hồi stamina thì nhảy lên ngay, tiêu stamina thì chờ rồi mới tụt
        if (whiteImage != null)
        {
            whiteImage.DOKill();
            if (ratio >= whiteImage.fillAmount) whiteImage.fillAmount = ratio;
            else whiteImage.DOFillAmount(ratio, whiteDuration).SetDelay(whiteDelay).SetEase(whiteEase);
        }

        if (StaminaText != null) StaminaText.text = $"Stamina: {current:0} / {max:0}";
    }
}
