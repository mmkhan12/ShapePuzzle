using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public string pieceID;
    public Vector3 snapOffset;
    public float snapDistance = 0.3f;
    public float rotationStep = 45f;
    public float snapAngleTolerance = 2f;
    public float symmetryAngle = 90f;  // ✅ Allows multiple correct angles

    private Vector3 startPosition;
    private bool isPlaced = false;
    private PuzzleSlot currentSlot;
    private Vector3 offset;

    private float mouseDownTime;
    private Vector3 mouseDownPosition;
    private bool isDragging = false;

    public float clickThresholdTime = 0.2f;
    public float dragThresholdDistance = 0.2f;

    void Start()
    {
        startPosition = transform.position;

        // ✅ Randomize start rotation
        float randomAngle = Random.Range(0f, 360f);
        float roundedAngle = Mathf.Round(randomAngle / rotationStep) * rotationStep;
        transform.rotation = Quaternion.Euler(0f, 0f, roundedAngle);
    }

    void OnMouseDown()
    {
        if (isPlaced) return;
        mouseDownTime = Time.time;
        mouseDownPosition = Input.mousePosition;
        offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        isDragging = false;
    }

    void OnMouseDrag()
    {
        if (isPlaced) return;

        Vector3 currentMousePos = Input.mousePosition;
        float distance = Vector3.Distance(mouseDownPosition, currentMousePos);

        if (distance > dragThresholdDistance)
        {
            isDragging = true;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
            mousePos.z = 0f;
            transform.position = mousePos;
        }
    }

    void OnMouseUp()
    {
        if (isPlaced) return;

        float pressDuration = Time.time - mouseDownTime;
        float moveDistance = Vector3.Distance(mouseDownPosition, Input.mousePosition);

        // ✅ Click = rotate
        if (!isDragging && pressDuration <= clickThresholdTime && moveDistance < dragThresholdDistance)
        {
            transform.Rotate(0f, 0f, rotationStep);
            return;
        }

        // ✅ Try to snap into slot
        if (currentSlot != null)
        {
            foreach (Transform snapPoint in currentSlot.snapPoints)
            {
                float distanceToSlot = Vector3.Distance(transform.position, snapPoint.position);
                if (distanceToSlot <= snapDistance && RotationMatches(currentSlot))
                {
                    transform.position = snapPoint.position + snapOffset + new Vector3(0, 0, -0.1f);
                    transform.rotation = Quaternion.Euler(0, 0, Mathf.Round(transform.eulerAngles.z / rotationStep) * rotationStep);
                    isPlaced = true;
                    return;
                }
            }
        }

        // Reset if failed
        transform.position = startPosition;
    }

    private bool RotationMatches(PuzzleSlot slot)
    {
        float pieceAngle = transform.eulerAngles.z;
        float slotAngle = slot.transform.eulerAngles.z;

        for (float angleOffset = 0; angleOffset < 360f; angleOffset += symmetryAngle)
        {
            float expectedAngle = (slotAngle + angleOffset) % 360f;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(pieceAngle, expectedAngle));

            if (angleDiff <= snapAngleTolerance)
                return true;
        }

        return false;
    }

    void OnTriggerEnter2D(Collider2D other)   // ✅ 2D trigger
    {
        PuzzleSlot slot = other.GetComponent<PuzzleSlot>();
        if (slot != null && IsMatchingSlot(slot))
        {
            currentSlot = slot;
        }
        Debug.Log("Piece entered trigger with: " + other.name);
    }

    void OnTriggerExit2D(Collider2D other)   // ✅ 2D trigger
    {
        PuzzleSlot slot = other.GetComponent<PuzzleSlot>();
        if (slot != null && slot == currentSlot)
        {
            currentSlot = null;
        }
    }

    private bool IsMatchingSlot(PuzzleSlot slot)
    {
        foreach (string id in slot.acceptablePieceIDs)
        {
            if (pieceID == id) return true;
        }
        return false;
    }

    public bool IsPlacedCorrectly => isPlaced;
}
