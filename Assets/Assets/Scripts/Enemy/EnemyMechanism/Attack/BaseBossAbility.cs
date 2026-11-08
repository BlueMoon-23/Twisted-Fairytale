using System.Collections;
using UnityEngine;

public class BaseBossAbility : MonoBehaviour
{
    [SerializeField] private HitDealer Prefab;
    [SerializeField] private float Cooldown;
    private BaseEnemy enemy;
    private void Awake()
    {
        enemy = GetComponent<BaseEnemy>();
    }
    void OnEnable()
    {
        StartCoroutine(PerformAbility());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private IEnumerator PerformAbility()
    {
        while (true)
        {
            if (enemy.TargetPlayer != null)
            {
                HitDealer attackSpawned = Object.Instantiate(Prefab, enemy.TargetPlayer.AttackHitboxCenter, Quaternion.identity);
                attackSpawned.Init(enemy.gameObject, Prefab.damage, enemy.attackHitMask, 1f);
                yield return new WaitForSeconds(Cooldown);
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}
