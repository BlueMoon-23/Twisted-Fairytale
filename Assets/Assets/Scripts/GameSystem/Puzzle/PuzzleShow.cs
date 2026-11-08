using TMPro;
using UnityEngine;

public class PuzzleShow : MonoBehaviour
{
    [SerializeField] private GameObject PuzzleMissionUI;
    [SerializeField] private TextMeshProUGUI PuzzleText;
    [SerializeField] private PuzzleObserver Observer;
    [SerializeField] private string Description;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnEnable()
    {
        Observer.OnCompleted += OnPuzzleCompleted;
    }
    private void OnDisable()
    {
        Observer.OnCompleted -= OnPuzzleCompleted;
    }
    private void OnPuzzleCompleted()
    {
        gameObject.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            PuzzleMissionUI?.SetActive(true);
            PuzzleText.text = Description
    .Replace("{collected}", Observer.CurrentAmount.ToString())
    .Replace("{amount}", Observer.puzzle.CollectingAmount.ToString());
        }
    }
}
