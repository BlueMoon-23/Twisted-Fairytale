using UnityEngine;

/// <summary>
/// Quy ước nhiệm vụ của puzzle
/// </summary>
[CreateAssetMenu(menuName = "Puzzle/PuzzleData")]
public class PuzzleData : ScriptableObject
{
    public string CollectingKeyword; // nhiệm vụ nhặt item sẽ cần keyword để so
    public int CollectingAmount; // số lượng item cần nhặt
}
