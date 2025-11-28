using UnityEngine;
using System.Collections;

public class Level3_PuzzlePiece : PuzzlePiece
{
    protected override void Start()
    {
        base.Start();
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

        // Rotation click
        if (!isDragging && pressDuration <= clickThresholdTime && moveDistance < dragThresholdDistance)
        {
            transform.Rotate(0f, 0f, rotationStep);
            if (rotateSound != null) audioSource.PlayOneShot(rotateSound, audioVolume);
            return;
        }

        if (currentSlot == null)
        {
            Debug.Log($"[PuzzlePiece] {pieceID}: No current slot.");
            return;
        }

        // -----------------------------
        // FIND CLOSEST SNAP POINT FIRST
        // -----------------------------
        SnapPoint nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var sp in currentSlot.snapPoints)
        {
            float d = Vector3.Distance(transform.position, sp.snapTransform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = sp;
            }
        }

        // Too far → don't snap
        if (nearestDist > snapDistance)
        {
            Debug.Log($"[PuzzlePiece] {pieceID}: No snap point close enough (nearestDist {nearestDist:F2}).");
            snappedPoint = null;
            isPlaced = false;
            return;
        }

        // Now validate that single selected snap point
        SnapPoint snapPoint = nearest;

        // Check ID
        bool idMatches = false;
        foreach (string id in snapPoint.acceptablePieceIDs)
            if (pieceID == id) { idMatches = true; break; }

        if (!idMatches)
        {
            Debug.Log($"[PuzzlePiece] {pieceID}: ID mismatch.");
            return;
        }

        // Check occupancy
        if (snapPoint.isOccupied)
        {
            Debug.Log($"[PuzzlePiece] {pieceID}: SnapPoint already occupied.");
            return;
        }

        // Check rotation
        if (!RotationMatches(currentSlot))
        {
            Debug.Log($"[PuzzlePiece] {pieceID}: Rotation mismatch.");
            return;
        }

        // Test snap transform
        Vector3 targetPos = snapPoint.snapTransform.position + snapOffset + new Vector3(0, 0, -0.1f);
        Quaternion targetRot = Quaternion.Euler(0, 0, Mathf.Round(transform.eulerAngles.z / rotationStep) * rotationStep);

        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        transform.position = targetPos;
        transform.rotation = targetRot;

        // Overlap check
        Collider2D thisCol = GetComponent<Collider2D>();
        if (thisCol == null)
        {
            Debug.LogError($"[PuzzlePiece] {pieceID}: Missing collider.");
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        Collider2D[] results = new Collider2D[10];
        int count = thisCol.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = results[i];
            if (col == null) continue;

            PuzzlePiece other = col.GetComponent<PuzzlePiece>();
            if (other != null && other != this && other.IsPlacedCorrectly)
            {
                Debug.Log($"[PuzzlePiece] {pieceID}: Overlap with {other.pieceID}.");
                transform.position = oldPos;
                transform.rotation = oldRot;
                return;
            }
        }

        // Final snap
        StartCoroutine(SmoothSnap(targetPos, targetRot));

        isPlaced = true;
        snappedPoint = snapPoint;
        snapPoint.isOccupied = true;
        snapPoint.occupyingPiece = this;

        Debug.Log($"[PuzzlePiece] {pieceID}: Snapped to {snapPoint.snapTransform.name}.");

        if (cheerSound != null)
            audioSource.PlayOneShot(cheerSound, audioVolume);
    }

    public new bool IsPlacedCorrectly => isPlaced;
}
