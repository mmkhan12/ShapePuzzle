using UnityEngine;

public class PuzzleSlot : MonoBehaviour
{
    [Header("Slot Settings")]
    [Tooltip("Which pieces are allowed in this slot (IDs must match PuzzlePiece.pieceID)")]
    public string[] acceptablePieceIDs;

    [Tooltip("Where pieces can snap to (set child transforms in Inspector)")]
    public Transform[] snapPoints;

    [HideInInspector]
    public bool isOccupied = false;

    private void OnDrawGizmos()
    {
        // ✅ Draw snap point positions in Scene view for clarity
        if (snapPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform snap in snapPoints)
            {
                if (snap != null)
                {
                    Gizmos.DrawSphere(snap.position, 0.05f);
                }
            }
        }
    }
}
