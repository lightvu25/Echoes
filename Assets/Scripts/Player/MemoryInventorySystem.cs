using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(HealthSystem))]
public class MemoryInventorySystem : MonoBehaviour
{
    public static MemoryInventorySystem Instance { get; private set; }

    [Header("Inventory State")]
    public List<MemoryItemData> activeSlots = new List<MemoryItemData>();
    
    public int coreSlotCount = 1;

    [Header("Drop Settings")]
    [SerializeField] private float popForceMin = 5f;
    [SerializeField] private float popForceMax = 10f;
    [SerializeField] private float sidewaysForceMod = 1.5f;

    public event Action<IReadOnlyList<MemoryItemData>> OnInventoryChanged;

    private HealthSystem healthSystem;

    public int UnlockedSlots => healthSystem != null ? healthSystem.UnlockedSlots : 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        if (healthSystem != null)
        {
            healthSystem.OnUnlockedSlotsDecreased += HandleSlotsDecreased;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnUnlockedSlotsDecreased -= HandleSlotsDecreased;
        }
    }

    private void HandleSlotsDecreased(int newUnlockedSlots)
    {
        while (activeSlots.Count > newUnlockedSlots && activeSlots.Count > coreSlotCount)
        {
            DropOutermostItem();
        }
    }
    
    private void DropOutermostItem()
    {
        if (activeSlots.Count == 0) return;

        int outermostIndex = activeSlots.Count - 1;
        MemoryItemData itemToDrop = activeSlots[outermostIndex];
        
        activeSlots.RemoveAt(outermostIndex);
        
        OnInventoryChanged?.Invoke(activeSlots);

        if (itemToDrop == null || itemToDrop.dropPrefab == null) return;

        Vector3 spawnPos = transform.position + (Vector3.up * 0.5f);
        
        GameObject droppedItem = ObjectPoolManager.SpawnObject(itemToDrop.dropPrefab, spawnPos, Quaternion.identity, ObjectPoolManager.PoolType.Loot);
        float dirX = Random.value > 0.5f ? 1f : -1f;
        float randomX = dirX * Random.Range(3f, 5f) * sidewaysForceMod; 
        float randomY = Random.Range(popForceMin, popForceMax); 
        Vector2 popForce = new Vector2(randomX, randomY);

        if (droppedItem.TryGetComponent(out Collectible collectible))
        {
            collectible.Initialize(1, popForce);
        }
        else if (droppedItem.TryGetComponent(out Rigidbody2D rb))
        {
            rb.AddForce(popForce, ForceMode2D.Impulse);
        }
    }

    public bool TryAddMemoryItem(MemoryItemData item)
    {
        if (item == null || healthSystem == null) return false;

        if (activeSlots.Count >= UnlockedSlots)
        {
            return false;
        }

        activeSlots.Add(item);
        OnInventoryChanged?.Invoke(activeSlots);
        return true;
    }

    public List<ElementType> GetAllActiveElements()
    {
        List<ElementType> elements = new List<ElementType>();
        foreach (var item in activeSlots)
        {
            if (item != null && item.elementType != ElementType.None)
            {
                elements.Add(item.elementType);
            }
        }
        return elements;
    }
}
