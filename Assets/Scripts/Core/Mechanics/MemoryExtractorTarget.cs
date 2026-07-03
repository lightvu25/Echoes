using UnityEngine;

public class MemoryExtractorTarget : MonoBehaviour, IExtractable, IFeedbackProvider
{
    [SerializeField] private EchoData memoryToExtract;
    [SerializeField] private int extractionUses = 1;
    [SerializeField] private GameObject extractVFXPrefab;
    [SerializeField] private bool destroyOnDepletion = true;
    
    [Header("Feedback")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 1.5f, 0);

    public Vector3 PromptOffset => promptOffset;

    public bool IsAvailable => extractionUses > 0 && memoryToExtract != null;

    public void Extract()
    {
        if (!IsAvailable) return;

        ExtractMemory();
    }

    public void ExtractMemory()
    {
        if (!IsAvailable) return;

        // Add to inventory
        if (PlayerInventoryCore.Instance != null)
        {
            // TryEquip handles inventory full checks and swap UI internally
            PlayerInventoryCore.Instance.TryEquip(memoryToExtract);
        }

        if (extractVFXPrefab != null)
            Instantiate(extractVFXPrefab, transform.position, Quaternion.identity);

        extractionUses--;

        if (extractionUses <= 0)
        {
            if (destroyOnDepletion)
            {
                Destroy(gameObject);
            }
            else
            {
                
                if (FeedbackUI.Instance != null)
                    FeedbackUI.Instance.HideInteractPrompt();
            }
        }
    }
}