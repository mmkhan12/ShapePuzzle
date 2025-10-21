using UnityEngine;

[System.Serializable]
public class Level3SnapPoint
{
    [Tooltip("The transform (yellow dot) where the piece should snap")]
    public Transform snapTransform;

    [Tooltip("IDs of puzzle pieces allowed to snap here")]
    public string[] acceptablePieceIDs;

    [HideInInspector]
    public bool isOccupied = false;

    [HideInInspector]
    public Level3_PuzzlePieces occupyingPiece;
}

public class Level3_PuzzleSlot : MonoBehaviour
{
    [Header("Slot Settings")]
    [Tooltip("Each snap point + which pieces it accepts")]
    public Level3SnapPoint[] snapPoints;

    [HideInInspector]
    public bool isOccupied = false;

    private void OnDrawGizmos()
    {
        if (snapPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var snap in snapPoints)
            {
                if (snap != null && snap.snapTransform != null)
                {
                    Gizmos.DrawSphere(snap.snapTransform.position, 0.05f);
                }
            }
        }
    }
}

