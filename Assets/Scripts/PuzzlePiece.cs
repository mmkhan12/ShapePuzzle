using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public string pieceID;              // e.g. "cross_small", "oval_half1"
    public Vector3 snapOffset;          // Optional offset when snapping
    public float snapDistance = 0.5f;
    public float rotationStep = 45f;
    public float snapAngleTolerance = 10f;
    public float symmetryAngle = 90f;

    private Vector3 startPosition;
    private bool isPlaced = false;
    private PuzzleSlot currentSlot;
    private Vector3 offset;
    private Vector3 originalPosition;

    private float mouseDownTime;
    private Vector3 mouseDownPosition;
    private bool isDragging = false;

    public float clickThresholdTime = 0.2f;
    public float dragThresholdDistance = 0.2f;

    public AudioClip cheerSound;
    public AudioClip rotateSound;
    private AudioSource audioSource;

    
    void Start()
    {
        startPosition = transform.position;

        float targetAngle = 0f;
        float incorrectAngle = targetAngle;
        int maxTries = 10;
        int tries = 0;

        while (Mathf.Abs(Mathf.DeltaAngle(incorrectAngle, targetAngle)) < snapAngleTolerance && tries < maxTries)
        {
            float randomAngle = Random.Range(0f, 360f);
            float roundedAngle = Mathf.Round(randomAngle / rotationStep) * rotationStep;
            incorrectAngle = roundedAngle;
            tries++;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, incorrectAngle);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
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

        if (!isDragging && pressDuration <= clickThresholdTime && moveDistance < dragThresholdDistance)
        {
            transform.Rotate(0f, 0f, rotationStep);
            if (rotateSound != null)
                audioSource.PlayOneShot(rotateSound);
            return;
        }

        if (currentSlot != null)
        {
            float pieceAngle = transform.eulerAngles.z;
            float slotAngle = currentSlot.transform.eulerAngles.z;

            bool rotationMatches = false;
            for (float angleOffset = 0; angleOffset < 360f; angleOffset += symmetryAngle)
            {
                float expectedAngle = (slotAngle + angleOffset) % 360f;
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(pieceAngle, expectedAngle));

                if (angleDiff <= snapAngleTolerance)
                {
                    rotationMatches = true;
                    break;
                }
            }

            if (rotationMatches)
            {
                // Snap
                transform.position = currentSlot.transform.position + snapOffset + new Vector3(0, 0, -0.1f);
                transform.rotation = currentSlot.transform.rotation;
                isPlaced = true;

                if (cheerSound != null)
                    audioSource.PlayOneShot(cheerSound, 0.1f);
                return;
            }
        }

        // Return to original position if not snapped
        transform.position = startPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("TRIGGER ENTER: " + other.name); // ✅ Confirm if the slot is detected
        PuzzleSlot slot = other.GetComponent<PuzzleSlot>();

        if (slot != null)
        {
            Debug.Log("FOUND SLOT: " + slot.name); // ✅ Slot has script attached

            if (IsMatchingSlot(slot))
            {
                Debug.Log("SLOT MATCHED: " + slot.name); // ✅ Valid match
                currentSlot = slot;
            }
            else
            {
                Debug.Log("Slot NOT matched");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        PuzzleSlot slot = other.GetComponent<PuzzleSlot>();
        if (slot != null && slot == currentSlot)
        {
            currentSlot = null;
            Debug.Log("Exited slot: " + slot.name);
        }
    }

    private bool IsMatchingSlot(PuzzleSlot slot)
    {
        if (slot == null || slot.acceptablePieceIDs == null)
        {
            Debug.LogWarning("Slot or acceptablePieceIDs is null");
            return false;
        }

        foreach (string id in slot.acceptablePieceIDs)
        {
            if (pieceID == id)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsPlacedCorrectly()
    {
        return isPlaced;
    }
}
