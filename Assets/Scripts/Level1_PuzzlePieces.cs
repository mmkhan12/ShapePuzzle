using UnityEngine;

public class ShapePiece : MonoBehaviour
{
    [Header("Slot & Snapping")]
    [SerializeField] private Transform correctSlot;
    [SerializeField] private float snapDistance = 0.5f;
    [SerializeField] private float rotationStep = 45f;

    [Header("Symmetry Settings")]
    [SerializeField] private bool isSymmetrical = false;
    [SerializeField] private float customSymmetryAngle = 360f; // e.g., 180 or 90 for symmetrical shapes

    [Header("Audio")]
    public AudioClip cheerSound;
    public AudioClip rotateSound;
    private AudioSource audioSource;

    private Vector3 startPosition;
    private bool isDragging = false;
    private bool placedCorrectly = false;

    private float mouseDownTime;
    private Vector3 mouseDownPosition;

    private float clickThresholdTime = 0.2f; // Max time for tap
    private float dragThresholdDistance = 0.2f; // Min movement to count as drag

    void Start()
    {
        startPosition = transform.position;

        float targetAngle = correctSlot.eulerAngles.z;
        float incorrectAngle = targetAngle;

        int maxTries = 10;
        int tries = 0;

        // Randomize start rotation, avoiding snapping into correct orientation
        while (Mathf.Abs(Mathf.DeltaAngle(incorrectAngle, targetAngle)) < 10f && tries < maxTries)
        {
            float randomAngle = Random.Range(0f, 360f);
            float roundedAngle = Mathf.Round(randomAngle / rotationStep) * rotationStep;
            incorrectAngle = roundedAngle;
            tries++;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, incorrectAngle);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.volume = 1f;
        }
    }

    void OnMouseDown()
    {
        if (!placedCorrectly)
        {
            mouseDownTime = Time.time;
            mouseDownPosition = Input.mousePosition;
        }
    }

    void OnMouseDrag()
    {
        if (!placedCorrectly)
        {
            Vector3 currentMousePos = Input.mousePosition;
            float distance = Vector3.Distance(mouseDownPosition, currentMousePos);

            if (distance > dragThresholdDistance)
            {
                isDragging = true;

                Vector3 mousePos = Input.mousePosition;
                mousePos.z = 10f; // Distance from camera
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                transform.position = new Vector3(worldPos.x, worldPos.y, startPosition.z);
            }
        }
    }

    void OnMouseUp()
    {
        if (!placedCorrectly)
        {
            float pressDuration = Time.time - mouseDownTime;
            float moveDistance = Vector3.Distance(mouseDownPosition, Input.mousePosition);

            // Tap to rotate
            if (!isDragging && pressDuration <= clickThresholdTime && moveDistance < dragThresholdDistance)
            {
                transform.Rotate(0f, 0f, rotationStep);

                if (rotateSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(rotateSound, 0.3f);
                }
            }

            isDragging = false;

            Vector2 shapePos = new Vector2(transform.position.x, transform.position.y);
            Vector2 slotPos = new Vector2(correctSlot.position.x, correctSlot.position.y);
            float distance = Vector2.Distance(shapePos, slotPos);

            float currentAngle = transform.eulerAngles.z;
            float targetAngle = correctSlot.eulerAngles.z;

            bool isRotationCorrect = false;

            if (isSymmetrical)
            {
                for (float a = 0; a < 360f; a += customSymmetryAngle)
                {
                    float expectedAngle = (targetAngle + a) % 360f;
                    float difference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, expectedAngle));
                    if (difference < 10f)
                    {
                        isRotationCorrect = true;
                        break;
                    }
                }
            }
            else
            {
                float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
                isRotationCorrect = angleDifference < 10f;
            }

            if (distance <= snapDistance && isRotationCorrect)
            {
                transform.position = new Vector3(correctSlot.position.x, correctSlot.position.y, startPosition.z);
                // Don't snap rotation — must be already correct
                placedCorrectly = true;

                if (cheerSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(cheerSound, 0.3f);
                }
            }
        }
    }
}
