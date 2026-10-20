using UnityEngine;

[CreateAssetMenu(menuName = "Combat/AttackData")]
public class PlayerAttackData : ScriptableObject
{
    public string AnimName = "Punch";
    public float Duration = 0.4f;
    public float HitStart = 0.12f;
    public float HitEnd = 0.22f;
    public float LungeSpeed = 1.5f;
    public int Damage = 1;
    public HitDealer Prefab;
}