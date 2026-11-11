using UnityEngine;
using VolumetricLines;

// Làm tia Volumetric Line "sống" như particle: độ dày, độ dài, độ trong suốt chạy theo đường cong
// suốt thời gian tồn tại, cộng thêm rung nhẹ cho cảm giác năng lượng.
// Gắn cùng object có VolumetricLineBehavior. Mỗi lần object được bật (spawn) thì chạy lại từ đầu.
[RequireComponent(typeof(VolumetricLineBehavior))]
public class BeamPulse : MonoBehaviour
{
    [Tooltip("Tổng thời gian chạy hết các đường cong (giây). Nên bằng thời gian sống của tia")]
    [SerializeField] private float duration = 0.8f;

    [Header("Độ dày (nhân với độ dày gốc trên component)")]
    [SerializeField]
    private AnimationCurve widthOverLifetime =
        new(new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1.3f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f));

    [Header("Độ dài (nhân với End Pos gốc, tia mọc ra từ Start Pos)")]
    [SerializeField]
    private AnimationCurve lengthOverLifetime =
        new(new Keyframe(0f, 0f), new Keyframe(0.12f, 1f), new Keyframe(1f, 1f));

    [Header("Độ trong suốt (nhân với alpha gốc của Line Color)")]
    [SerializeField]
    private AnimationCurve alphaOverLifetime =
        new(new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f));

    [Header("Rung (0 = tắt)")]
    [Tooltip("Độ dày dao động thêm ± chừng này phần trăm")]
    [SerializeField, Range(0f, 0.5f)] private float flickerAmount = 0.08f;
    [SerializeField] private float flickerSpeed = 30f;

    private VolumetricLineBehavior line;
    private float baseWidth;
    private Vector3 baseStart, baseEnd;
    private Color baseColor;
    private float time;

    private void Awake()
    {
        line = GetComponent<VolumetricLineBehavior>();
        // Lưu giá trị đang chỉnh trên prefab làm mốc 100%
        baseWidth = line.LineWidth;
        baseStart = line.StartPos;
        baseEnd = line.EndPos;
        baseColor = line.LineColor;
    }

    private void OnEnable()
    {
        time = 0f;
        Apply(0f);
    }

    private void Update()
    {
        time += Time.deltaTime;
        Apply(duration > 0f ? Mathf.Clamp01(time / duration) : 1f);
    }

    private void Apply(float t)
    {
        float flicker = 1f + (Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) * 2f - 1f) * flickerAmount;

        line.LineWidth = baseWidth * widthOverLifetime.Evaluate(t) * flicker;
        line.EndPos = Vector3.LerpUnclamped(baseStart, baseEnd, lengthOverLifetime.Evaluate(t));

        Color c = baseColor;
        c.a *= alphaOverLifetime.Evaluate(t);
        line.LineColor = c;
    }
}