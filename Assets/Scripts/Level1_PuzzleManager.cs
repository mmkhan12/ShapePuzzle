using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Level1_PuzzleManager : MonoBehaviour
{
    [System.Serializable]
    public class ShapeBatch
    {
        [Tooltip("List of shape pieces for this batch")]
        public List<GameObject> shapePieces;

        [Tooltip("Corresponding slots for this batch (optional)")]
        public List<GameObject> slots;

        [Tooltip("UI Text labels for this batch (optional)")] 
        public List<GameObject> labels;
    }

    [Header("Shape Batches")]
    public List<ShapeBatch> batches = new List<ShapeBatch>();

    [Header("Audio")]
    public AudioClip yaySound;
    private AudioSource audioSource;

    private int currentBatch = 0;
    private int placedCount = 0;

    void Start()
    {
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Hide everything first
        HideAllBatches();
        ActivateBatch(currentBatch);
    }

    private void HideAllBatches()
    {
        foreach (var batch in batches)
        {
            foreach (var shape in batch.shapePieces)
                if (shape != null) shape.SetActive(false);

            foreach (var slot in batch.slots)
                if (slot != null) slot.SetActive(false);

            foreach (var label in batch.labels) 
                if (label != null) label.SetActive(false);
        }
    }

    private void ActivateBatch(int index)
    {
        if (index >= 0 && index < batches.Count)
        {
            foreach (var shape in batches[index].shapePieces)
                if (shape != null) shape.SetActive(true);

            foreach (var slot in batches[index].slots)
                if (slot != null) slot.SetActive(true);

            foreach (var label in batches[index].labels) 
                if (label != null) label.SetActive(true);

            placedCount = 0;
        }
    }

    public void OnShapePlaced()
    {
        placedCount++;

        int totalShapesInBatch = batches[currentBatch].shapePieces.Count;
        if (placedCount >= totalShapesInBatch)
        {
            StartCoroutine(NextBatchRoutine());
        }
    }

    private IEnumerator NextBatchRoutine()
    {
        if (yaySound != null)
        {
            audioSource.PlayOneShot(yaySound);
            yield return new WaitForSeconds(yaySound.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        currentBatch++;

        if (currentBatch < batches.Count)
        {
            HideAllBatches();
            ActivateBatch(currentBatch);
        }
        else
        {
            Debug.Log("🎉 All boards complete for Level 1!");
        }
    }
}
