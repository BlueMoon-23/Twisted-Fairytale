using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

// Hiện số damage nhảy từ trong thân enemy ra: bật to lên, bay lên lệch sang một bên, rồi mờ dần.
// Đặt trên Canvas (Screen Space - Overlay). Nghe EnemyHP.AnyDamaged nên chỉ cần 1 cái cho cả scene.
public class DamagePopupManager : MonoBehaviour
{
    [SerializeField] private DamagePopup prefab;
    [Tooltip("Trống = Camera.main")]
    [SerializeField] private Camera cam;

    [Header("Bay ra (pixel ở độ phân giải gốc của Canvas)")]
    [SerializeField] private float riseHeight = 70f;
    [SerializeField] private float spread = 45f;
    [SerializeField] private float flyTime = 0.55f;

    [Header("Bật to")]
    [SerializeField] private float popScale = 1.4f;
    [SerializeField] private float popTime = 0.1f;

    [Header("Mờ dần")]
    [SerializeField] private float fadeDelay = 0.35f;
    [SerializeField] private float fadeTime = 0.25f;

    [Header("Màu")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("Damage từ mức này trở lên: đổi màu, to và bay cao hơn")]
    [SerializeField] private float bigHitThreshold = 20f;
    [SerializeField] private Color bigHitColor = new(1f, 0.8f, 0.2f);

    private ObjectPool<DamagePopup> pool;
    private readonly List<DamagePopup> active = new();
    private Canvas canvas;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        canvas = GetComponentInParent<Canvas>();
        pool = new ObjectPool<DamagePopup>(
            () => Instantiate(prefab, transform),
            p => p.gameObject.SetActive(true),
            p => p.gameObject.SetActive(false),
            p => Destroy(p.gameObject),
            collectionCheck: false, defaultCapacity: 20, maxSize: 100);
    }

    private void OnEnable() => EnemyHP.AnyDamaged += Show;

    private void OnDisable()
    {
        EnemyHP.AnyDamaged -= Show;
        foreach (DamagePopup p in active) DOTween.Kill(p);
        active.Clear();
    }

    private void Show(float amount, Vector3 worldPos)
    {
        DamagePopup p = pool.Get();
        active.Add(p);
        p.transform.SetAsLastSibling(); // số mới vẽ đè lên số cũ

        bool big = amount >= bigHitThreshold;
        p.Text.text = Mathf.CeilToInt(amount).ToString();
        p.Text.color = big ? bigHitColor : normalColor;

        p.WorldPos = worldPos;
        p.Offset = Vector2.zero;
        p.Group.alpha = 1f;
        p.Rect.localScale = Vector3.zero;
        Place(p);

        float boost = big ? 1.3f : 1f;
        Vector2 target = new(Random.Range(-spread, spread), riseHeight * boost);

        DOTween.Kill(p);
        Sequence seq = DOTween.Sequence().SetId(p);
        seq.Append(p.Rect.DOScale(popScale * boost, popTime).SetEase(Ease.OutBack)); // bật ra từ con số 0
        seq.Append(p.Rect.DOScale(boost, popTime));                                  // thu về cỡ thường
        seq.Insert(0f, DOTween.To(() => p.Offset, v => p.Offset = v, target, flyTime).SetEase(Ease.OutCubic));
        seq.Insert(fadeDelay, p.Group.DOFade(0f, fadeTime));
        seq.OnComplete(() =>
        {
            active.Remove(p);
            pool.Release(p);
        });
    }

    // Bám theo điểm trong thế giới mỗi frame, để camera chạy theo player thì số vẫn dính vào chỗ enemy bị đánh
    private void LateUpdate()
    {
        foreach (DamagePopup p in active) Place(p);
    }

    private void Place(DamagePopup p)
    {
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector3 screen = cam.WorldToScreenPoint(p.WorldPos);
        p.Rect.position = screen + (Vector3)(p.Offset * scale);
    }
}