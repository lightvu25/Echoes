using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central inventory manager for Project Echoes.
/// Manages three independent slot lists (Elements, Relics, Items) whose
/// capacities are persisted in <see cref="RunData"/> and unlocked by gaining Max HP.
///
/// Design contract
/// ---------------
/// • This class owns ALL inventory state and business rules.
/// • It has ZERO knowledge of UI classes — it communicates outward via events only.
/// • UI classes subscribe to events and call back into this class to mutate state.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class PlayerInventoryCore : MonoBehaviour
{
    public static PlayerInventoryCore Instance { get; private set; }

    // ------------------------------------------------------------------ //
    //  Events (UI subscribes to these)                                     //
    // ------------------------------------------------------------------ //

    /// <summary>Fired whenever any category's contents or slot count change.</summary>
    public event Action OnInventoryChanged;

    /// <summary>
    /// Fired when the player gains Max HP and must choose which category to unlock.
    /// UI should pause the game and show the unlock panel.
    /// </summary>
    public event Action OnSlotUnlockRequired;

    /// <summary>
    /// Fired when the player tries to equip an item into a full category.
    /// UI should show the inline swap overlay.
    /// </summary>
    public event Action<ItemBaseData, ItemCategory> OnSwapRequired;

    // ------------------------------------------------------------------ //
    //  Equipped lists (read-only externally)                               //
    // ------------------------------------------------------------------ //

    private readonly List<ItemBaseData> equippedElements = new();
    private readonly List<ItemBaseData> equippedRelics   = new();
    private readonly List<ItemBaseData> equippedItems    = new();

    // ------------------------------------------------------------------ //
    //  Drop physics settings                                               //
    // ------------------------------------------------------------------ //

    [Header("Drop Settings")]
    [SerializeField] private float popForceMin      = 5f;
    [SerializeField] private float popForceMax      = 10f;
    [SerializeField] private float sidewaysForceMod = 1.5f;



    // ------------------------------------------------------------------ //
    //  Private state                                                       //
    // ------------------------------------------------------------------ //

    private HealthSystem healthSystem;

    /// <summary>Index of the currently active Element slot (used by HotbarController).</summary>
    private int activeElementIndex = 0;

    // ------------------------------------------------------------------ //
    //  Public accessors                                                    //
    // ------------------------------------------------------------------ //

    /// <summary>Read-only view of equipped Elements.</summary>
    public IReadOnlyList<ItemBaseData> EquippedElements => equippedElements;

    /// <summary>Read-only view of equipped Relics.</summary>
    public IReadOnlyList<ItemBaseData> EquippedRelics => equippedRelics;

    /// <summary>Read-only view of equipped Items (consumables).</summary>
    public IReadOnlyList<ItemBaseData> EquippedItems => equippedItems;

    /// <summary>Element at the active hotbar index, or null if the slot is empty.</summary>
    public EchoData ActiveElement =>
        activeElementIndex < equippedElements.Count
            ? equippedElements[activeElementIndex] as EchoData
            : null;

    // ------------------------------------------------------------------ //
    //  Slot counts — sourced from RunData with migration guard             //
    // ------------------------------------------------------------------ //

    private static RunData Run => GameSession.Instance?.currentRun;

    /// <summary>Number of unlocked Element slots (strictly 4).</summary>
    public int UnlockedElementSlots => 4;

    /// <summary>Number of unlocked Relic slots.</summary>
    public int UnlockedRelicSlots => Mathf.Clamp(
        Run != null ? Run.unlockedRelicSlots : 1, 1, RunData.MAX_SLOTS);

    /// <summary>Number of unlocked Item slots.</summary>
    public int UnlockedItemSlots => Mathf.Clamp(
        Run != null ? Run.unlockedItemSlots : 1, 1, RunData.MAX_SLOTS);

    // ------------------------------------------------------------------ //
    //  Unity lifecycle                                                     //
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        healthSystem = GetComponent<HealthSystem>();

        // Migration guard: if an old save has 0 for any slot count, reset to 1.
        if (Run != null)
        {
            if (Run.unlockedElementSlots <= 0) Run.unlockedElementSlots = 1;
            if (Run.unlockedRelicSlots   <= 0) Run.unlockedRelicSlots   = 1;
            if (Run.unlockedItemSlots    <= 0) Run.unlockedItemSlots    = 1;
        }
    }

    private void Start()
    {
        if (healthSystem != null)
        {
            healthSystem.OnMaxHPGained       += HandleMaxHPGained;
            healthSystem.OnUnlockedSlotsDecreased += HandleSlotsDecreased;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnMaxHPGained       -= HandleMaxHPGained;
            healthSystem.OnUnlockedSlotsDecreased -= HandleSlotsDecreased;
        }
    }

    // ------------------------------------------------------------------ //
    //  Equip / Swap                                                        //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Attempts to equip <paramref name="item"/> into the appropriate category slot.
    /// If a free slot exists, the item is equipped immediately.
    /// If all slots are full, <see cref="OnSwapRequired"/> is raised for the UI to handle.
    /// </summary>
    /// <param name="item">The item to equip. Must not be null.</param>
    public void TryEquip(ItemBaseData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[PlayerInventoryCore] TryEquip called with null item.");
            return;
        }

        List<ItemBaseData> list  = GetList(item.Category);
        int                limit = GetUnlockedCount(item.Category);

        if (item is EchoData echoData)
        {
            EchoData runtimeInstance = Instantiate(echoData);
            runtimeInstance.InitRuntime();
            item = runtimeInstance;
        }

        if (list.Count < limit)
        {
            list.Add(item);
            OnInventoryChanged?.Invoke();
            

        }
        else
        {
            if (item.Category == ItemCategory.Element)
            {
                SpawnDroppedItem(item);
            }
            else
            {
                OnSwapRequired?.Invoke(item, item.Category);
            }
        }
    }

    /// <summary>
    /// Drops <paramref name="equipped"/> from the inventory (spawning its prefab) and
    /// equips <paramref name="incoming"/> in its place.
    /// </summary>
    /// <param name="equipped">Currently equipped item to remove.</param>
    /// <param name="incoming">New item to equip.</param>
    public void SwapItem(ItemBaseData equipped, ItemBaseData incoming)
    {
        if (equipped == null || incoming == null) return;

        List<ItemBaseData> list = GetList(incoming.Category);
        int index = list.IndexOf(equipped);
        if (index < 0)
        {
            Debug.LogWarning($"[PlayerInventoryCore] SwapItem: '{equipped.itemName}' not found in {incoming.Category} list.");
            return;
        }

        list[index] = incoming;
        SpawnDroppedItem(equipped);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Removes an item from the equipped list without dropping it (used for UI consumption like Fusion).
    /// </summary>
    public void RemoveEquippedItem(ItemBaseData item)
    {
        if (item == null) return;
        List<ItemBaseData> list = GetList(item.Category);
        if (list.Remove(item))
        {
            OnInventoryChanged?.Invoke();
        }
    }

    // ------------------------------------------------------------------ //
    //  Slot unlock (called by UI after player makes a choice)             //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Increments the unlocked slot count for the given <paramref name="category"/> by 1,
    /// up to <see cref="RunData.MAX_SLOTS"/>.
    /// </summary>
    /// <param name="category">The category whose slot count to increase.</param>
    public void UnlockSlot(ItemCategory category)
    {
        if (Run == null) return;

        switch (category)
        {
            case ItemCategory.Element:
                Run.unlockedElementSlots = Mathf.Min(Run.unlockedElementSlots + 1, RunData.MAX_SLOTS);
                break;
            case ItemCategory.Relic:
                Run.unlockedRelicSlots = Mathf.Min(Run.unlockedRelicSlots + 1, RunData.MAX_SLOTS);
                break;
            case ItemCategory.Item:
                Run.unlockedItemSlots = Mathf.Min(Run.unlockedItemSlots + 1, RunData.MAX_SLOTS);
                break;
        }

        GameSession.Instance?.SaveCurrentRun();
        OnInventoryChanged?.Invoke();
    }

    // ------------------------------------------------------------------ //
    //  Hotbar (called by HotbarController)                                 //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Sets the active hotbar index for Elements.
    /// Clamps to the current number of unlocked Element slots.
    /// </summary>
    /// <param name="index">0-based slot index.</param>
    public void SetActiveElementIndex(int index)
    {
        int max = Mathf.Max(0, UnlockedElementSlots - 1);
        activeElementIndex = Mathf.Clamp(index, 0, max);
    }

    /// <summary>
    /// Returns the currently active EchoData based on the activeElementIndex.
    /// Used by combat scripts (e.g. AttackHitbox) to determine damage types.
    /// </summary>
    public EchoData GetActiveElement()
    {
        if (equippedElements == null || equippedElements.Count == 0) return null;
        if (activeElementIndex >= 0 && activeElementIndex < equippedElements.Count)
            return equippedElements[activeElementIndex] as EchoData;
        return null;
    }

    /// <summary>
    /// Returns all active ElementTypes across equipped Elements (for combat use).
    /// </summary>
    public List<EchoType> GetAllActiveElementTypes()
    {
        var result = new List<EchoType>();
        foreach (var item in equippedElements)
        {
            if (item is EchoData ed && ed.echoType != EchoType.None)
                result.Add(ed.echoType);
        }
        return result;
    }

    /// <summary>
    /// Returns a read-only view of the equipped list for the given <paramref name="category"/>.
    /// Used by <see cref="SwapUI"/> to populate the swap overlay without exposing internal lists.
    /// </summary>
    /// <param name="category">The inventory category to look up.</param>
    public IReadOnlyList<ItemBaseData> GetEquippedList(ItemCategory category) =>
        GetList(category);

    // ------------------------------------------------------------------ //
    //  Event handlers                                                      //
    // ------------------------------------------------------------------ //

    private void HandleMaxHPGained()
    {
        OnSlotUnlockRequired?.Invoke();
    }

    /// <summary>
    /// When the player loses a slot (takes damage that crosses a threshold),
    /// drop excess items until all lists fit within their new capacities.
    /// </summary>
    private void HandleSlotsDecreased(int _)
    {
        TrimList(equippedElements, UnlockedElementSlots);
        TrimList(equippedRelics,   UnlockedRelicSlots);
        TrimList(equippedItems,    UnlockedItemSlots);
        OnInventoryChanged?.Invoke();
    }



    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private List<ItemBaseData> GetList(ItemCategory category) => category switch
    {
        ItemCategory.Element => equippedElements,
        ItemCategory.Relic   => equippedRelics,
        _                    => equippedItems
    };

    private int GetUnlockedCount(ItemCategory category) => category switch
    {
        ItemCategory.Element => UnlockedElementSlots,
        ItemCategory.Relic   => UnlockedRelicSlots,
        _                    => UnlockedItemSlots
    };

    private void TrimList(List<ItemBaseData> list, int maxCount)
    {
        while (list.Count > maxCount)
        {
            int last = list.Count - 1;
            SpawnDroppedItem(list[last]);
            list.RemoveAt(last);
        }
    }

    private void SpawnDroppedItem(ItemBaseData item)
    {
        if (item == null || item.dropPrefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject dropped = ObjectPoolManager.SpawnObject(
            item.dropPrefab, spawnPos, Quaternion.identity, ObjectPoolManager.PoolType.Loot);

        float  dirX      = UnityEngine.Random.value > 0.5f ? 1f : -1f;
        float  randomX   = dirX * UnityEngine.Random.Range(3f, 5f) * sidewaysForceMod;
        float  randomY   = UnityEngine.Random.Range(popForceMin, popForceMax);
        Vector2 popForce = new Vector2(randomX, randomY);

        if (dropped.TryGetComponent(out Collectible collectible))
            collectible.Initialize(1, popForce);
        else if (dropped.TryGetComponent(out Rigidbody2D rb))
            rb.AddForce(popForce, ForceMode2D.Impulse);
    }
}
