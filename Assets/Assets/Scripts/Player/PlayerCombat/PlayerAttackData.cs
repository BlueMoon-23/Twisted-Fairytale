
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AttackDirection
{
    public Dir8 direction;
    public Vector3 rotation;
}

[CreateAssetMenu(menuName = "Combat/AttackData")]
public class PlayerAttackData : ScriptableObject
{
    public string AnimName = "Punch";
    public float Duration = 0.4f;
    public float HitStart = 0.12f;
    public float HitEnd = 0.22f;
    public float LungeSpeed = 1.5f;
    public float Damage = 1;
    public float SpawnForward = 0f;
    public HitDealer Prefab;
    public AttackDirection[] PrefabDirections;
    [Tooltip("Đẩy đòn đánh ra xa thêm theo hướng nhìn (đơn vị world), cộng thêm vào attackReach")]
    [NonSerialized] private Dictionary<Dir8, Quaternion> lookup; // không lưu vào asset, dựng lại khi cần
    public Quaternion GetRotation(Dir8 direction)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(direction, out Quaternion rot) ? rot : Quaternion.identity;
    }
    private void BuildLookup()
    {
        lookup = new Dictionary<Dir8, Quaternion>();
        if (PrefabDirections == null) return;
        foreach (AttackDirection d in PrefabDirections)
            lookup[d.direction] = Quaternion.Euler(d.rotation); // đổi góc Euler (độ) sang Quaternion lúc dựng
    }
    // Sửa mảng trên Inspector (kể cả lúc đang Play) thì bỏ bản cũ để lần sau dựng lại
    private void OnValidate() => lookup = null;
}