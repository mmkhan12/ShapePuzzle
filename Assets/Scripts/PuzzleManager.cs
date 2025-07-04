using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public PuzzlePiece[] pieces; // Drag all your puzzle pieces here in the Inspector

    void Update()
    {
        if (AllPiecesPlaced())
        {
            Debug.Log("Puzzle Completed!");
            // Add sound, animation, or next level logic here
        }
    }

    bool AllPiecesPlaced()
    {
        foreach (var piece in pieces)
        {
            if (!piece.IsPlacedCorrectly())
                return false;
        }
        return true;
    }
}
