using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DroppedMemoryItem : MonoBehaviour
{
    [Tooltip("The echo data this dropped item represents.")]
    public EchoData itemData;
    
    [Tooltip("UI Prompt to show when the player is in range (e.g., 'Press J to swap').")]
    public GameObject promptUI;

    private bool playerInRange = false;

    private void Start()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.J))
        {
            SwapWithPlayer();
        }
    }

    private void SwapWithPlayer()
    {
        if (PlayerInventoryCore.Instance == null || itemData == null) return;
        
        // When swapping, the player needs to drop an existing item.
        // The inventory core handles dropping if we trigger a swap.
        // Wait, the prompt says: "DroppedMemoryItem.cs must handle OnTriggerEnter2D to show a "J" key prompt UI, waiting for input to swap."
        // And "If full, instantiate the new DroppedMemoryItem prefab in the 2D world."
        
        // TryEquip will trigger a swap overlay or drop the equipped item if the inventory handles it.
        // We will just call TryEquip on the inventory.
        PlayerInventoryCore.Instance.TryEquip(itemData);
        
        // Destroy the dropped item in the world since the player is taking it
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }
}
