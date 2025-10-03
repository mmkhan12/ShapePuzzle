using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Piece Settings")]
    public string pieceID; // e.g. "star1", "oval_half2"
    public Vector3 snapOffset; // Optional offset when snapping
    public float snapDistance = 0.3f; // Distance threshold for snapping
    public float rotationStep = 45f; // How much the piece rotates per click
    public float snapAngleTolerance = 2f; // Angle tolerance for snapping
    public float symmetryAngle = 360f; // 360 = only exact match, 180 = 2 valid, 90 = 4 valid

    [Header("Drag Settings")]
    public float clickThresholdTime = 0.2f;
    public float dragThresholdDistance = 0.2f; // Measured in world units

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

    // Track which snap point we’re snapped to
    private SnapPoint snappedPoint;

    private AudioSource audioSource;

    void Start()
    {
        startPosition = transform.position;

        // Randomize start rotation
        float randomAngle = Random.Range(0f, 360f);
        float roundedAngle = Mathf.Round(randomAngle / rotationStep) * rotationStep;
        transform.rotation = Quaternion.Euler(0f, 0f, roundedAngle);

        // Set up audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnMouseDown()
    {
        if (Camera.main == null) return;

        // If already placed, free the snap point
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

        // Convert to world distance
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

        // Use world space for drag check
        Vector3 worldStart = Camera.main.ScreenToWorldPoint(mouseDownPosition);
        Vector3 worldEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldStart.z = worldEnd.z = 0f;
        float moveDistance = Vector2.Distance(worldStart, worldEnd);

        // ✅ Click = rotate
        if (!isDragging && pressDuration <= clickThresholdTime && moveDistance < dragThresholdDistance)
        {
            transform.Rotate(0f, 0f, rotationStep);

            if (rotateSound != null)
                audioSource.PlayOneShot(rotateSound, audioVolume);

            return;
        }

        // ✅ Try snapping to slot
        bool snapped = false;

        if (currentSlot != null)
        {
            foreach (var snapPoint in currentSlot.snapPoints)
            {
                // Check if this piece ID is valid for this snap point
                bool idMatches = false;
                foreach (string id in snapPoint.acceptablePieceIDs)
                {
                    if (pieceID == id)
                    {
                        idMatches = true;
                        break;
                    }
                }

                // Skip if not allowed or already occupied
                if (!idMatches || snapPoint.isOccupied) continue;

                // Distance check
                float distanceToSlot = Vector3.Distance(transform.position, snapPoint.snapTransform.position);

                if (distanceToSlot <= snapDistance && RotationMatches(currentSlot))
                {
                    // --- NEW OVERLAP CHECK ---
                    Collider2D thisCollider = GetComponent<Collider2D>();

                    // Save old transform
                    Vector3 oldPos = transform.position;
                    Quaternion oldRot = transform.rotation;

                    // Test transform at snap position
                    Vector3 testPosition = snapPoint.snapTransform.position + snapOffset + new Vector3(0, 0, -0.1f);
                    Quaternion testRotation = Quaternion.Euler(0, 0,
                        Mathf.Round(transform.eulerAngles.z / rotationStep) * rotationStep);

                    transform.position = testPosition;
                    transform.rotation = testRotation;

                    bool overlapFound = false;
                    Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                        thisCollider.bounds.center,
                        thisCollider.bounds.size,
                        transform.eulerAngles.z
                    );

                    foreach (var col in overlaps)
                    {
                        PuzzlePiece otherPiece = col.GetComponent<PuzzlePiece>();
                        if (otherPiece != null && otherPiece != this && otherPiece.IsPlacedCorrectly)
                        {
                            // ✅ Allow if snapped to a *different* snap point in the same slot
                            if (otherPiece.snappedPoint != null &&
                                otherPiece.snappedPoint != snapPoint &&
                                currentSlot != null &&
                                otherPiece.snappedPoint.snapTransform.parent == currentSlot.transform)
                            {
                                continue; // skip rejection
                            }

                            // ❌ Otherwise block snapping
                            overlapFound = true;
                            break;
                        }
                    }

                    // Restore original transform
                    transform.position = oldPos;
                    transform.rotation = oldRot;

                    // If overlap → skip snapping
                    if (overlapFound) continue;

                    // --- SNAP INTO PLACE ---
                    transform.position = testPosition;
                    transform.rotation = testRotation;

                    // Mark as placed
                    isPlaced = true;
                    snappedPoint = snapPoint;
                    snapPoint.isOccupied = true;
                    snapPoint.occupyingPiece = this;
                    snapped = true;

                    if (cheerSound != null)
                        audioSource.PlayOneShot(cheerSound, audioVolume);

                    break;
                }
            }
        }

        // Reset if not snapped
        if (!snapped)
        {
            transform.position = startPosition;
            snappedPoint = null;
            isPlaced = false;
        }
    }

    private bool RotationMatches(PuzzleSlot slot)
    {
        float pieceAngle = transform.eulerAngles.z;
        float slotAngle = slot.transform.eulerAngles.z;

        // Allow symmetry (90, 180, etc.)
        for (float angleOffset = 0; angleOffset < 360f; angleOffset += symmetryAngle)
        {
            float expectedAngle = (slotAngle + angleOffset) % 360f;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(pieceAngle, expectedAngle));

            if (angleDiff <= snapAngleTolerance)
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
