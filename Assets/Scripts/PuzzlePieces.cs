using UnityEngine;

public class PuzzlePieces : MonoBehaviour
{
    public SlotIdentifier targetSlot;  // Reference to the SlotIdentifier in the inspector
    public float snapRadius = 0.5f;
    public float rotationTolerance = 5f;
    public bool allow180Symmetry = false;

    private Vector3 startPosition;
    private Vector3 offset;
    private bool isPlaced = false;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void OnMouseDown()
    {
        if (!isPlaced)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            offset = transform.position - mouseWorldPos;
        }
    }

    private void OnMouseDrag()
    {
        if (!isPlaced)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            transform.position = mouseWorldPos + offset;
        }
    }

    private void OnMouseUp()
    {
        if (isPlaced) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, snapRadius);
        foreach (var hitCollider in hitColliders)
        {
            var slot = hitCollider.GetComponent<SlotIdentifier>();
            if (slot != null && slot == targetSlot)
            {
                // Check rotation tolerance
                float angleDifference = Quaternion.Angle(transform.rotation, hitCollider.transform.rotation);
                bool isRotationCorrect = angleDifference <= rotationTolerance;

                // Check for 180-degree symmetry if allowed
                if (!isRotationCorrect && allow180Symmetry)
                {
                    float flippedAngle = Quaternion.Angle(
                        transform.rotation,
                        hitCollider.transform.rotation * Quaternion.Euler(0, 0, 180)
                    );
                    if (flippedAngle <= rotationTolerance)
                        isRotationCorrect = true;
                }

                if (isRotationCorrect)
                {
                    // Snap the piece to the slot position
                    transform.position = hitCollider.transform.position;
                    transform.rotation = hitCollider.transform.rotation;
                    isPlaced = true;

                    // Optional: Play a sound or give visual feedback
                    return;
                }
            }
        }

        // If not placed correctly, move back to the start position
        transform.position = startPosition;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
