using UnityEngine;

public class PuzzleSlot : MonoBehaviour
{
    [Tooltip("List of acceptable piece IDs (e.g. 'oval1', 'oval2')")]
    public string[] acceptablePieceIDs;

    [HideInInspector]
    public bool isOccupied = false;
}
