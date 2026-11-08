using UnityEngine;

[CreateAssetMenu(menuName = "Combat/WeaponData")]
public class PlayerWeaponData : ScriptableObject
{
    public PlayerAttackData NormalAttack;
    public PlayerAttackData HeavyAttack;
    public PlayerAttackData Ultimate;
    public float UltimateCooldown;
    public float UltimateAOELifespan;
}
