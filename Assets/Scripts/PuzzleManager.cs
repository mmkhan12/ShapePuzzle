using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [Header("Assign all puzzle pieces here")]
    public PuzzlePiece[] pieces; // Drag all your puzzle pieces into this array in Inspector

    void Update()
    {
        if (AllPiecesPlaced())
        {
            Debug.Log("Puzzle Completed!");
            // TODO: Add cheer sound, confetti, animation, or load next level
        }
    }

    bool AllPiecesPlaced()
    {
        foreach (var piece in pieces)
        {
            if (!piece.IsPlacedCorrectly) // ✅ works with property version
                return false; // If one piece isn’t placed, puzzle isn’t done
        }
        return true; // Only true if all pieces are placed
    }
}
