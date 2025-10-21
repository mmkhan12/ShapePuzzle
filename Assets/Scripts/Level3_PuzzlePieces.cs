using System.Collections.Generic;
using UnityEngine;

public class Level3_PuzzlePieces : MonoBehaviour
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

    private Vector3 offset;
    private float mouseDownTime;
    private Vector3 mouseDownPosition;
    private bool isDragging = false;
    private bool isPlaced = false;
    private Vector3 startPosition;
    private Level3SnapPoint snappedPoint;
    private AudioSource audioSource;
    private float pieceDepthFromCamera;

    private readonly List<Level3_PuzzleSlot> overlappingSlots = new List<Level3_PuzzleSlot>();

    void Start()
    {
        startPosition = transform.position;

        float randomAngle = Random.Range(0f, 360f);
        float roundedAngle = Mathf.Round(randomAngle / rotationStep) * rotationStep;
        transform.rotation = Quaternion.Euler(0f, 0f, roundedAngle);

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        // ✅ Calculate the depth of this piece relative to the camera once
        if (Camera.main != null)
            pieceDepthFromCamera = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
    }

    void Update()
    {
        if (isDragging)
            Debug.Log($"Update Pos: {transform.position}");
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

        // ✅ Calculate proper depth offset ONCE here
        pieceDepthFromCamera = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = pieceDepthFromCamera;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        offset = transform.position - mouseWorldPos;

        Debug.Log($"OnMouseDown - CamZ: {Camera.main.transform.position.z}, PieceZ: {transform.position.z}, Depth: {pieceDepthFromCamera}");
        isDragging = false;
    }

    void OnMouseDrag()
    {
        if (isPlaced || Camera.main == null) return;

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = pieceDepthFromCamera; // ✅ use stored depth value
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3 newPosition = mouseWorldPos + offset;
        newPosition.z = transform.position.z; // keep the same Z layer

        transform.position = newPosition;

        if (!isDragging)
            isDragging = true;

        Debug.Log($"OnMouseDrag - WorldPos: {mouseWorldPos}, NewPos: {newPosition}");
    }

    void OnMouseUp()
    {
        if (Camera.main == null) return;

        float pressDuration = Time.time - mouseDownTime;

        Vector3 worldStart = Camera.main.ScreenToWorldPoint(new Vector3(mouseDownPosition.x, mouseDownPosition.y, pieceDepthFromCamera));
        Vector3 worldEnd = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pieceDepthFromCamera));

        worldStart.z = worldEnd.z = 0f;
        float moveDistance = Vector2.Distance(worldStart, worldEnd);

        // CLICK => rotate
        if (!isDragging && pressDuration <= clickThresholdTime && moveDistance < dragThresholdDistance)
        {
            transform.Rotate(0f, 0f, rotationStep);
            if (rotateSound != null)
                audioSource.PlayOneShot(rotateSound, audioVolume);
            return;
        }

        bool snapped = TrySnapToBestNearbySlot();

        if (!snapped)
        {
            snappedPoint = null;
            isPlaced = false;
        }
    }

    private bool TrySnapToBestNearbySlot()
    {
        Collider2D pieceCollider = GetComponent<Collider2D>();
        if (pieceCollider == null || overlappingSlots.Count == 0)
            return false;

        float bestDistance = float.MaxValue;
        Level3SnapPoint bestSnap = null;
        Level3_PuzzleSlot bestSlot = null;

        foreach (var slot in overlappingSlots)
        {
            if (slot == null || slot.snapPoints == null) continue;

            Collider2D slotCollider = slot.GetComponent<Collider2D>();
            if (slotCollider == null) continue;

            foreach (var snapPoint in slot.snapPoints)
            {
                if (snapPoint == null || snapPoint.isOccupied) continue;

                bool idMatches = false;
                foreach (string id in snapPoint.acceptablePieceIDs)
                {
                    if (pieceID == id) { idMatches = true; break; }
                }
                if (!idMatches) continue;

                if (!IsMostlyInsideSlot(slotCollider, pieceCollider)) continue;
                if (!RotationMatches(slot)) continue;

                float distanceToSnap = Vector3.Distance(transform.position, snapPoint.snapTransform.position);
                if (distanceToSnap > snapDistance * 0.45f) continue;

                if (WouldOverlapPlacedPiece(pieceCollider)) continue;

                if (distanceToSnap < bestDistance)
                {
                    bestDistance = distanceToSnap;
                    bestSnap = snapPoint;
                    bestSlot = slot;
                }
            }
        }

        if (bestSnap != null && bestSlot != null)
        {
            transform.position = bestSnap.snapTransform.position + snapOffset + new Vector3(0, 0, -0.1f);
            transform.rotation = Quaternion.Euler(0, 0,
                Mathf.Round(transform.eulerAngles.z / rotationStep) * rotationStep);

            isPlaced = true;
            snappedPoint = bestSnap;
            bestSnap.isOccupied = true;
            bestSnap.occupyingPiece = this;

            if (cheerSound != null)
                audioSource.PlayOneShot(cheerSound, audioVolume);

            return true;
        }

        return false;
    }

    private bool WouldOverlapPlacedPiece(Collider2D pieceCollider)
    {
        Vector2 center = pieceCollider.bounds.center;
        Vector2 size = pieceCollider.bounds.size * 0.9f;
        float angle = transform.eulerAngles.z;

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, angle);
        foreach (var c in overlaps)
        {
            if (c == null) continue;
            Level3_PuzzlePieces other = c.GetComponent<Level3_PuzzlePieces>();
            if (other != null && other != this && other.IsPlacedCorrectly)
                return true;
        }
        return false;
    }

    private bool IsMostlyInsideSlot(Collider2D slotCollider, Collider2D pieceCollider)
    {
        Bounds b = pieceCollider.bounds;
        Vector2 center = b.center;
        Vector2 half = b.extents;

        Vector2[] samplePoints = new Vector2[9];
        samplePoints[0] = center;
        samplePoints[1] = center + new Vector2(half.x, 0);
        samplePoints[2] = center + new Vector2(-half.x, 0);
        samplePoints[3] = center + new Vector2(0, half.y);
        samplePoints[4] = center + new Vector2(0, -half.y);
        samplePoints[5] = center + new Vector2(half.x * 0.7f, half.y * 0.7f);
        samplePoints[6] = center + new Vector2(-half.x * 0.7f, half.y * 0.7f);
        samplePoints[7] = center + new Vector2(half.x * 0.7f, -half.y * 0.7f);
        samplePoints[8] = center + new Vector2(-half.x * 0.7f, -half.y * 0.7f);

        int insideCount = 0;
        foreach (var p in samplePoints)
        {
            Collider2D hit = Physics2D.OverlapPoint(p);
            if (hit == slotCollider || (hit != null && hit.transform.IsChildOf(slotCollider.transform)))
                insideCount++;
        }

        return insideCount >= 5;
    }

    private bool RotationMatches(Level3_PuzzleSlot slot)
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

    void OnTriggerEnter2D(Collider2D other)
    {
        Level3_PuzzleSlot slot = other.GetComponent<Level3_PuzzleSlot>();
        if (slot != null && !overlappingSlots.Contains(slot))
            overlappingSlots.Add(slot);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Level3_PuzzleSlot slot = other.GetComponent<Level3_PuzzleSlot>();
        if (slot != null && overlappingSlots.Contains(slot))
            overlappingSlots.Remove(slot);
    }

    public bool IsPlacedCorrectly => isPlaced;
}
