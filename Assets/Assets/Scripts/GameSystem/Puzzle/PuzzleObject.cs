using System;
using UnityEngine;

public class PuzzleObject : MonoBehaviour
{
    [SerializeField] private string Keyword;
    public static event Action<string> AnyCollected; // 1 sự kiện chung cho mọi PuzzleObject
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            AnyCollected?.Invoke(Keyword);
            Destroy(this.gameObject);
        }
    }
}
