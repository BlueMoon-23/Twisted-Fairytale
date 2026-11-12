using TMPro;
using UnityEngine;

// Một con số damage trên Canvas. Gắn lên prefab: Text (TextMeshPro) + CanvasGroup.
// DamagePopupManager lo việc lấy ra, chạy hiệu ứng và trả về pool.
[RequireComponent(typeof(CanvasGroup))]
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    public TextMeshProUGUI Text => text;
    public CanvasGroup Group { get; private set; }
    public RectTransform Rect { get; private set; }

    // Điểm trong thế giới mà con số bám theo, và độ lệch (pixel) do hiệu ứng bay ra
    [HideInInspector] public Vector3 WorldPos;
    [HideInInspector] public Vector2 Offset;

    private void Awake()
    {
        Group = GetComponent<CanvasGroup>();
        Rect = (RectTransform)transform;
        if (text == null) text = GetComponentInChildren<TextMeshProUGUI>();
        Group.blocksRaycasts = false; // không chặn chuột/click của UI khác
    }
}