using System;
using UnityEngine;

public class PuzzleObserver : MonoBehaviour
{
    [field: SerializeField] public PuzzleData puzzle { get; private set; }
    public int CurrentAmount { get; private set; }
    public event Action OnCompleted;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnEnable()
    {
        PuzzleObject.AnyCollected += OnCollectionChanged;
    }
    private void OnDisable()
    {
        PuzzleObject.AnyCollected -= OnCollectionChanged;
    }
    private void OnCollectionChanged(string ObjectWord)
    {
        if (puzzle.CollectingKeyword == ObjectWord)
        {
            CurrentAmount += 1;
            if (CurrentAmount == puzzle.CollectingAmount)
            {
                OnCompleted?.Invoke();
            }
        }
    }
}
