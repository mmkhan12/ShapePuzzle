using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Piece Settings")]
    public string pieceID;
    public Vector3 snapOffset;
    public float snapDistance = 0.3f;
    public float rotationStep = 45f;
    public float snapAngleTolerance = 2f;
    public float symmetryAngle = 360f;

    [Header("Drag Settings")]
    public float clickThresholdTime = 0.2f;
    public float dragThresholdDistance = 0.2f;

    [Header("Audio Settings")]
    public AudioClip cheerSound;
    public AudioClip rotateSound;
    [Range(0f, 1f)] public float audioVolume = 0.3f;

    private Vector3 startPosition;
    private bool isPlaced = false;
    private PuzzleSlot currentSlot;
    private Vector3 offset;
    private float mouseDownTime;
    private Vector3 mouseDownPosition;
    private bool isDragging = false;
    public SnapPoint snappedPoint;
    private AudioSource audioSource;

    void Start()
    {
        startPosition = transform.position;
        float randomAngle = Random.Range(0f, 360f);
        float roundedAngle = Mathf.Round(randomAngle / rotationStep) * rotationStep;
        transform.rotation = Quaternion.Euler(0f, 0f, roundedAngle);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnMouseDown()
    {
        if (Camera.main == null) return;

        if (isPlaced && snappedPoint != null)
        {
            snappedPoint.isOccupied = false;
            snappedPoint.occupyingPiece = null;
            snappedPoint = null;
            isPlaced = false;
        }

        mouseDownTime = Time.time;
        mouseDownPosition = Input.mousePosition;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        offset = transform.position - mouseWorldPos;
        isDragging = false;
    }

    void OnMouseDrag()
    {
        if (isPlaced || Camera.main == null) return;

        Vector3 currentMousePos = Input.mousePosition;
        Vector3 worldStart = Camera.main.ScreenToWorldPoint(mouseDownPosition);
        Vector3 worldCurrent = Camera.main.ScreenToWorldPoint(currentMousePos);
        worldStart.z = worldCurrent.z = 0f;

        float distance = Vector2.Distance(worldStart, worldCurrent);
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
        if (Camera.main == null) return;

        float pressDuration = Time.time - mouseDownTime;
        Vector3 worldStart = Camera.main.ScreenToWorldPoint(mouseDownPosition);
        Vector3 worldEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldStart.z = worldEnd.z = 0f;

        float moveDistance = Vector2.Distance(worldStart, worldEnd);

        if (!isDragging && pressDuration <= clickThresholdTime && moveDistance < dragThresholdDistance)
        {
            transform.Rotate(0f, 0f, rotationStep);
            if (rotateSound != null)
                audioSource.PlayOneShot(rotateSound, audioVolume);
            return;
        }

        bool snapped = false;

        if (currentSlot == null)
        {
            Debug.Log($"[PuzzlePiece] {pieceID}: No current slot to snap to.");
        }
        else
        {
            foreach (var snapPoint in currentSlot.snapPoints)
            {
                // ID match?
                bool idMatches = false;
                foreach (string id in snapPoint.acceptablePieceIDs)
                {
                    if (pieceID == id)
                    {
                        idMatches = true;
                        break;
                    }
                }
                if (!idMatches)
                {
                    Debug.Log($"[PuzzlePiece] {pieceID}: SnapPoint rejects ID {pieceID}.");
                    continue;
                }
                if (snapPoint.isOccupied)
                {
                    Debug.Log($"[PuzzlePiece] {pieceID}: SnapPoint is already occupied.");
                    continue;
                }

                float distanceToSlot = Vector3.Distance(transform.position, snapPoint.snapTransform.position);
                if (distanceToSlot > snapDistance)
                {
                    Debug.Log($"[PuzzlePiece] {pieceID}: Too far from snap (dist {distanceToSlot:F3} > snapDistance {snapDistance}).");
                    continue;
                }

                if (!RotationMatches(currentSlot))
                {
                    Debug.Log($"[PuzzlePiece] {pieceID}: Rotation mismatch (angle tolerance).");
                    continue;
                }

                // Save old
                Vector3 oldPos = transform.position;
                Quaternion oldRot = transform.rotation;

                Vector3 testPosition = snapPoint.snapTransform.position + snapOffset + new Vector3(0, 0, -0.1f);
                Quaternion testRotation = Quaternion.Euler(0, 0, Mathf.Round(transform.eulerAngles.z / rotationStep) * rotationStep);

                transform.position = testPosition;
                transform.rotation = testRotation;

                Collider2D thisCollider = GetComponent<Collider2D>();
                if (thisCollider == null)
                {
                    Debug.LogError($"[PuzzlePiece] {pieceID}: No Collider2D on piece!");
                    transform.position = oldPos;
                    transform.rotation = oldRot;
                    continue;
                }

                // Overlap check with other pieces
                Collider2D[] overlaps = Physics2D.OverlapBoxAll(thisCollider.bounds.center, thisCollider.bounds.size, transform.eulerAngles.z);
                bool overlapFound = false;
                foreach (var col in overlaps)
                {
                    PuzzlePiece other = col.GetComponent<PuzzlePiece>();
                    if (other != null && other != this && other.IsPlacedCorrectly)
                    {
                        // allow if it's another snap point in same slot
                        if (other.snappedPoint != null &&
                            other.snappedPoint != snapPoint &&
                            currentSlot != null &&
                            other.snappedPoint.snapTransform.parent == currentSlot.transform)
                        {
                            // skip
                        }
                        else
                        {
                            overlapFound = true;
                            Debug.Log($"[PuzzlePiece] {pieceID}: Overlap with other piece {other.pieceID}.");
                            break;
                        }
                    }
                }

                if (overlapFound)
                {
                    transform.position = oldPos;
                    transform.rotation = oldRot;
                    continue;
                }

               
                // If we reach here, snap!
                transform.position = testPosition;
                transform.rotation = testRotation;
                isPlaced = true;
                snappedPoint = snapPoint;
                snapPoint.isOccupied = true;
                snapPoint.occupyingPiece = this;
                snapped = true;
                Debug.Log($"[PuzzlePiece] {pieceID}: Snapped successfully to {snapPoint.snapTransform.name}.");
                if (cheerSound != null)
                    audioSource.PlayOneShot(cheerSound, audioVolume);
                break;
            }
        }

        if (!snapped)
        {
            //transform.position = startPosition;
            snappedPoint = null;
            isPlaced = false;
        }
    }

    private bool RotationMatches(PuzzleSlot slot)
    {
        float pieceAngle = transform.eulerAngles.z;
        float slotAngle = slot.transform.eulerAngles.z;
        for (float angleOffset = 0f; angleOffset < 360f; angleOffset += symmetryAngle)
        {
            float expected = (slotAngle + angleOffset) % 360f;
            float diff = Mathf.Abs(Mathf.DeltaAngle(pieceAngle, expected));
            if (diff <= snapAngleTolerance)
                return true;
        }
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PuzzleSlot slot = other.GetComponent<PuzzleSlot>();
        if (slot != null)
        {
            currentSlot = slot;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PuzzleSlot slot = other.GetComponent<PuzzleSlot>();
        if (slot != null && slot == currentSlot)
        {
            currentSlot = null;
        }
    }

    public bool IsPlacedCorrectly => isPlaced;
}
