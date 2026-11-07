using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AttackType { Normal, Heavy, Ultimate, Dash }

public class CooldownBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [Header("Tween")]
    [SerializeField] private PlayerWeapon player;
    [SerializeField] private AttackType ChosenType;
    void Start()
    {
        fillImage.fillAmount = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnEnable()
    {
        player.PerformAttack += OnCooldownChanged;
    }
    private void OnDisable()
    {
        player.PerformAttack -= OnCooldownChanged;
    }

    private void OnCooldownChanged(float cooldown, AttackType type)
    {
        if (type != ChosenType) return;

        fillImage.DOKill();
        if (cooldown <= 0f) { fillImage.fillAmount = 0f; return; } // không có cooldown: không cần hiện

        fillImage.fillAmount = 1f;                                  // vừa tung chiêu: đầy ngay
        fillImage.DOFillAmount(0f, cooldown).SetEase(Ease.Linear);  // rút về 0 đúng lúc hồi xong
    }
}
