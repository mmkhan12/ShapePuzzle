using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;


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
    // Controls what counts as a click vs drag
    public float clickThresholdTime = 0.2f;
    public float dragThresholdDistance = 0.2f;

    [Header("Audio Settings")]
    public AudioClip cheerSound;
    public AudioClip rotateSound;
    [Range(0f, 1f)] public float audioVolume = 0.3f;

    protected Vector3 startPosition;
    protected bool isPlaced = false;
    protected PuzzleSlot currentSlot;
    protected Vector3 offset;
    protected float mouseDownTime;
    protected Vector3 mouseDownPosition;
    protected bool isDragging = false;
    public SnapPoint snappedPoint;
    [SerializeField] protected AudioSource audioSource;

    protected virtual void Start()
    {
        // Gives piece a random initial orientation but snapped to nearest rotation step
        float randomAngle = Random.Range(0f, 360f);
        float roundedAngle = Mathf.Round(randomAngle / rotationStep) * rotationStep;
        transform.rotation = Quaternion.Euler(0f, 0f, roundedAngle);

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnMouseDown()
    {
        if (Camera.main == null) return;

        // If piece already placed, free its snap point so it can be picked up again
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

        // Treat short click as rotation
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
                // Check piece ID
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

                // Check if snap point free
                if (snapPoint.isOccupied)
                {
                    Debug.Log($"[PuzzlePiece] {pieceID}: SnapPoint is already occupied.");
                    continue;
                }

                // Distance check
                float distanceToSlot = Vector3.Distance(transform.position, snapPoint.snapTransform.position);
                if (distanceToSlot > snapDistance)
                {
                    Debug.Log($"[PuzzlePiece] {pieceID}: Too far from snap (dist {distanceToSlot:F3} > snapDistance {snapDistance}).");
                    continue;
                }

                // Rotation match
                if (!RotationMatches(currentSlot))
                {
                    Debug.Log($"[PuzzlePiece] {pieceID}: Rotation mismatch (angle tolerance).");
                    continue;
                }

                // Tentatively snap
                Vector3 oldPos = transform.position;
                Quaternion oldRot = transform.rotation;

                Vector3 testPosition = snapPoint.snapTransform.position + snapOffset + new Vector3(0, 0, -0.1f);
                Quaternion testRotation = Quaternion.Euler(0, 0, Mathf.Round(transform.eulerAngles.z / rotationStep) * rotationStep);

                transform.position = testPosition;
                transform.rotation = testRotation;

                // Collision check
                // Overlap check with other pieces
                Collider2D thisCollider = GetComponent<Collider2D>();
                if (thisCollider == null)
                {
                    Debug.LogError($"[PuzzlePiece] {pieceID}: No Collider2D on piece!");
                    transform.position = oldPos;
                    transform.rotation = oldRot;
                    continue;
                }

                bool overlapFound = false;

                // Get all colliders overlapping the actual shape
                ContactFilter2D filter = new ContactFilter2D();
                filter.NoFilter(); // we want all collisions
                Collider2D[] results = new Collider2D[10]; // stores any overlaps
                int count = thisCollider.Overlap(filter, results);

                for (int i = 0; i < count; i++)
                {
                    Collider2D col = results[i];
                    if (col == null) continue;

                    PuzzlePiece other = col.GetComponent<PuzzlePiece>();
                    if (other != null && other != this && other.IsPlacedCorrectly)
                    {
                        bool sameSlotDifferentPoint = (
                            other.snappedPoint != null &&
                            other.snappedPoint != snapPoint &&
                            currentSlot != null &&
                            other.snappedPoint.snapTransform.parent == currentSlot.transform
                        );

                        if (sameSlotDifferentPoint)
                        {
                            // true collider overlap check (Polygon vs Polygon)
                            ColliderDistance2D distanceInfo = thisCollider.Distance(col);
                            // Allow tiny edge contact tolerance (e.g. 0.02 units)
                            float overlapTolerance = 0.07f;

                            // Check if colliders overlap significantly
                            if (distanceInfo.isOverlapped && distanceInfo.distance < -overlapTolerance)
                            {
                                overlapFound = true;
                                Debug.Log($"[PuzzlePiece] {pieceID}: Significant overlap with {other.pieceID} inside same slot (distance {distanceInfo.distance:F4}).");
                                break;
                            }

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


                // Successful snap
                StartCoroutine(SmoothSnap(testPosition, testRotation));

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
            snappedPoint = null;
            isPlaced = false;
        }
    }

    protected IEnumerator SmoothSnap(Vector3 targetPosition, Quaternion targetRotation)
    {
        float duration = 0.25f; // adjust this for faster/slower snap
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            transform.rotation = Quaternion.Lerp(startRot, targetRotation, t);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }


    protected virtual bool RotationMatches(PuzzleSlot slot)
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
            currentSlot = slot;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PuzzleSlot slot = other.GetComponent<PuzzleSlot>();
        if (slot != null && slot == currentSlot)
            currentSlot = null;
    }

    public bool IsPlacedCorrectly => isPlaced;
}
