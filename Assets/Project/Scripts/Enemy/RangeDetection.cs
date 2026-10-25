using UnityEngine;

public class RangeDetection : MonoBehaviour
{
    private BaseEnemy enemy;
    private void Awake()
    {
        enemy = GetComponentInParent<BaseEnemy>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            enemy.TargetPlayer = player;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            enemy.TargetPlayer = null;
        }
    }
}
