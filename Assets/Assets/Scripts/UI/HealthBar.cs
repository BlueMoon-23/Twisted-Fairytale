using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thanh máu: đăng ký nghe IHealth.HealthChanged, chạy mượt bằng DOTween.
//   Player (IsPlayer = true) : Image trên HUD góc màn hình, đổi fillAmount.
//   Enemy  (IsPlayer = false): object trong scene đặt ở mục tiêu (vd: SpriteRenderer), đổi scale X.
public class HealthBar : MonoBehaviour
{
    [Tooltip("Object có PlayerHP/EnemyHP. Trống = tự tìm ở các object cha (thanh máu gắn trên enemy)")]
    [SerializeField] private GameObject target;

    [Header("Player: Image trên HUD")]
    [Tooltip("Image Type = Filled, Fill Method = Horizontal")]
    [SerializeField] private Image fillImage;
    [Tooltip("Thanh trắng, đặt NGAY TRƯỚC fillImage trong Hierarchy để vẽ phía sau. Cũng Filled, Horizontal")]
    [SerializeField] private Image whiteImage;
    [SerializeField] private TextMeshProUGUI HPText;

    [Header("Enemy: object trong scene")]
    [Tooltip("Object phần máu. Pivot đặt ở mép TRÁI để mất máu thì co về bên trái")]
    [SerializeField] private Transform fillObject;
    [Tooltip("Thanh trắng, pivot ở mép TRÁI, Order in Layer thấp hơn fillObject để vẽ phía sau")]
    [SerializeField] private Transform whiteObject;

    [Header("Tween")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    [Header("Thanh trắng")]
    [Tooltip("Mất máu xong chờ chừng này giây thanh trắng mới tụt. Trúng đòn liên tiếp thì chờ lại từ đầu")]
    [SerializeField] private float whiteDelay = 0.15f;
    [SerializeField] private float whiteDuration = 0.3f;
    [SerializeField] private Ease whiteEase = Ease.InQuad;

    private IHealth health;
    private float fullScaleX = 1f;      // scale X lúc đầy máu, lấy theo cỡ đã chỉnh trong scene
    private float whiteFullScaleX = 1f;
    private float lastRatio = 1f;

    private void Awake()
    {
        health = target != null ? target.GetComponent<IHealth>() : GetComponentInParent<IHealth>();
        if (health == null)
            Debug.LogWarning("[HealthBar] Không tìm thấy IHealth. Gán Target hoặc đặt thanh máu làm con của nhân vật.", this);

        if (fillObject != null) fullScaleX = fillObject.localScale.x;
        if (whiteObject != null) whiteFullScaleX = whiteObject.localScale.x;
    }

    // Đăng ký khi bật, huỷ khi tắt: enemy chết bị SetActive(false) thì tự huỷ, hồi sinh thì tự đăng ký lại
    private void OnEnable()
    {
        if (health == null) return;
        health.HealthChanged += OnHealthChanged;
        Apply(Ratio(health.CurrentHealth, health.MaxHealth), instant: true); // lấy giá trị hiện tại, không tween
    }

    private void OnDisable()
    {
        if (health != null) health.HealthChanged -= OnHealthChanged;
        if (fillImage != null) fillImage.DOKill();
        if (whiteImage != null) whiteImage.DOKill();
        if (fillObject != null) fillObject.DOKill();
        if (whiteObject != null) whiteObject.DOKill();
    }

    private void OnHealthChanged(float current, float max, bool isPlayer)
        => Apply(Ratio(current, max), instant: false);

    private static float Ratio(float current, float max) => max > 0f ? Mathf.Clamp01(current / max) : 0f;

    private void Apply(float ratio, bool instant)
    {
        bool losing = ratio < lastRatio;
        lastRatio = ratio;

        // Thanh thật: luôn chạy ngay
        float mainTime = instant ? 0f : duration;
        // Thanh trắng: mất máu thì chờ rồi mới tụt, hồi máu (hoặc instant) thì nhảy ngay
        float whiteTime = instant || !losing ? 0f : whiteDuration;
        float whiteWait = instant || !losing ? 0f : whiteDelay;

        if (health.IsPlayer)
        {
            TweenFill(fillImage, ratio, mainTime, 0f, ease);
            TweenFill(whiteImage, ratio, whiteTime, whiteWait, whiteEase);
            if (HPText != null) HPText.text = $"Health: {health.CurrentHealth:0} / {health.MaxHealth:0}";
        }
        else
        {
            TweenScaleX(fillObject, fullScaleX * ratio, mainTime, 0f, ease);
            TweenScaleX(whiteObject, whiteFullScaleX * ratio, whiteTime, whiteWait, whiteEase);
        }
    }

    // DOKill trước: trúng nhiều đòn liên tiếp thì bỏ tween cũ, chạy từ chỗ đang dở tới giá trị mới.
    // Với thanh trắng, việc này cũng làm thời gian chờ đếm lại từ đầu, nên nó đứng yên suốt chuỗi combo.
    private static void TweenFill(Image img, float value, float time, float delay, Ease e)
    {
        if (img == null) return;
        img.DOKill();
        if (time <= 0f) { img.fillAmount = value; return; }
        img.DOFillAmount(value, time).SetDelay(delay).SetEase(e);
    }

    private static void TweenScaleX(Transform t, float x, float time, float delay, Ease e)
    {
        if (t == null) return;
        t.DOKill();
        if (time <= 0f)
        {
            Vector3 s = t.localScale;
            s.x = x;
            t.localScale = s;
            return;
        }
        t.DOScaleX(x, time).SetDelay(delay).SetEase(e);
    }
}