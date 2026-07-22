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

    private readonly ItemBaseData[] equippedEchoes = new ItemBaseData[RunData.MAX_SLOTS];
    private readonly ItemBaseData[] equippedRelics = new ItemBaseData[RunData.MAX_SLOTS];
    private readonly ItemBaseData[] equippedTools  = new ItemBaseData[RunData.MAX_SLOTS];

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
    private int activeEchoIndex = 0;
    
    /// <summary>The currently active slot index (0 to 3). Useful for checking combat styles.</summary>
    public int ActiveEchoIndex => activeEchoIndex;

    // ------------------------------------------------------------------ //
    //  Public accessors                                                    //
    // ------------------------------------------------------------------ //

    /// <summary>Read-only view of equipped Elements.</summary>
    public IReadOnlyList<ItemBaseData> EquippedEchoes => equippedEchoes;

    /// <summary>Read-only view of equipped Relics.</summary>
    public IReadOnlyList<ItemBaseData> EquippedRelics => equippedRelics;

    /// <summary>Read-only view of equipped Tools.</summary>
    public IReadOnlyList<ItemBaseData> EquippedTools => equippedTools;

    /// <summary>Element at the active hotbar index, or null if the slot is empty.</summary>
    public EchoData ActiveEcho =>
        activeEchoIndex < RunData.MAX_SLOTS
            ? equippedEchoes[activeEchoIndex] as EchoData
            : null;

    // ------------------------------------------------------------------ //
    //  Slot counts — sourced from RunData with migration guard             //
    // ------------------------------------------------------------------ //

    private static RunData Run => GameSession.Instance?.currentRun;

    /// <summary>Number of unlocked Element slots (strictly 4).</summary>
    public int UnlockedEchoSlots => 4;

    /// <summary>Number of unlocked Relic slots.</summary>
    public int UnlockedRelicSlots => Mathf.Clamp(
        Run != null ? Run.unlockedRelicSlots : 1, 1, RunData.MAX_SLOTS);

    /// <summary>Number of unlocked Tool slots.</summary>
    public int UnlockedEquipmentSlots => Mathf.Clamp(
        Run != null ? Run.unlockedEquipmentSlots : 1, 1, RunData.MAX_SLOTS);

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
            if (Run.unlockedEchoSlots <= 0) Run.unlockedEchoSlots = 1;
            if (Run.unlockedRelicSlots   <= 0) Run.unlockedRelicSlots   = 1;
            if (Run.unlockedEquipmentSlots    <= 0) Run.unlockedEquipmentSlots    = 1;
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

        ItemBaseData[] arr = GetArray(item.Category);
        int limit = GetUnlockedCount(item.Category);

        if (item is EchoData echoData && !echoData.name.Contains("(Clone)"))
        {
            EchoData runtimeInstance = Instantiate(echoData);
            runtimeInstance.InitRuntime();
            item = runtimeInstance;
        }
        else if (item is RelicData relicData && !relicData.name.Contains("(Clone)"))
        {
            RelicData runtimeInstance = Instantiate(relicData);
            runtimeInstance.InitRuntime();
            item = runtimeInstance;
        }

        int emptyIndex = -1;
        for (int i = 0; i < limit; i++)
        {
            if (arr[i] == null)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex >= 0)
        {
            arr[emptyIndex] = item;
            if (item is RelicData r) AddRelicStats(r);
            OnInventoryChanged?.Invoke();
        }
        else
        {
            if (item.Category == ItemCategory.Echo)
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

        ItemBaseData[] arr = GetArray(incoming.Category);
        int index = Array.IndexOf(arr, equipped);
        if (index < 0)
        {
            Debug.LogWarning($"[PlayerInventoryCore] SwapItem: '{equipped.itemName}' not found in {incoming.Category} array.");
            return;
        }

        arr[index] = incoming;
        if (equipped is RelicData rEquipped) RemoveRelicStats(rEquipped);
        if (incoming is RelicData rIncoming) AddRelicStats(rIncoming);
        SpawnDroppedItem(equipped);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Removes an item from the equipped list without dropping it (used for UI consumption like Fusion).
    /// </summary>
    public void RemoveEquippedItem(ItemBaseData item)
    {
        if (item == null) return;
        ItemBaseData[] arr = GetArray(item.Category);
        int index = Array.IndexOf(arr, item);
        if (index >= 0)
        {
            if (arr[index] is RelicData r) RemoveRelicStats(r);
            arr[index] = null;
            OnInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// Removes an item from the equipped list by its ID without dropping it.
    /// Used by self-destructing relics or items.
    /// </summary>
    public void RemoveItemByID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return;

        // Check all arrays
        RemoveByIDFromArray(equippedEchoes, itemID);
        RemoveByIDFromArray(equippedRelics, itemID);
        RemoveByIDFromArray(equippedTools, itemID);
    }

    private void RemoveByIDFromArray(ItemBaseData[] arr, string itemID)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null && arr[i].itemID == itemID)
            {
                if (arr[i] is RelicData r) RemoveRelicStats(r);
                arr[i] = null;
                OnInventoryChanged?.Invoke();
                return;
            }
        }
    }

    /// <summary>
    /// Swaps or moves items within a category array.
    /// </summary>
    public void MoveOrSwapItem(ItemCategory category, int fromIndex, int toIndex)
    {
        ItemBaseData[] arr = GetArray(category);
        if (fromIndex < 0 || fromIndex >= RunData.MAX_SLOTS || toIndex < 0 || toIndex >= RunData.MAX_SLOTS) return;
        
        int limit = GetUnlockedCount(category);
        if (fromIndex >= limit || toIndex >= limit) return;
        
        ItemBaseData temp = arr[fromIndex];
        arr[fromIndex] = arr[toIndex];
        arr[toIndex] = temp;
        
        OnInventoryChanged?.Invoke();
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
            case ItemCategory.Echo:
                Run.unlockedEchoSlots = Mathf.Min(Run.unlockedEchoSlots + 1, RunData.MAX_SLOTS);
                break;
            case ItemCategory.Relic:
                Run.unlockedRelicSlots = Mathf.Min(Run.unlockedRelicSlots + 1, RunData.MAX_SLOTS);
                break;
            case ItemCategory.Tool:
                Run.unlockedEquipmentSlots = Mathf.Min(Run.unlockedEquipmentSlots + 1, RunData.MAX_SLOTS);
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
    public void SetActiveEchoIndex(int index)
    {
        int max = Mathf.Max(0, UnlockedEchoSlots - 1);
        activeEchoIndex = Mathf.Clamp(index, 0, max);
    }

    /// <summary>
    /// Returns the currently active EchoData based on the activeElementIndex.
    /// Used by combat scripts (e.g. AttackHitbox) to determine damage types.
    /// </summary>
    public EchoData GetActiveEcho()
    {
        if (equippedEchoes == null) return null;
        if (activeEchoIndex >= 0 && activeEchoIndex < RunData.MAX_SLOTS)
            return equippedEchoes[activeEchoIndex] as EchoData;
        return null;
    }

    /// <summary>
    /// Returns all active ElementTypes across equipped Elements (for combat use).
    /// </summary>
    public List<EchoType> GetAllActiveEchoTypes()
    {
        var result = new List<EchoType>();
        foreach (var item in equippedEchoes)
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
        GetArray(category);

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
        TrimArray(equippedEchoes, UnlockedEchoSlots);
        TrimArray(equippedRelics,   UnlockedRelicSlots);
        TrimArray(equippedTools,    UnlockedEquipmentSlots);
        OnInventoryChanged?.Invoke();
    }



    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private ItemBaseData[] GetArray(ItemCategory category) => category switch
    {
        ItemCategory.Echo => equippedEchoes,
        ItemCategory.Relic   => equippedRelics,
        _                    => equippedTools
    };

    private int GetUnlockedCount(ItemCategory category) => category switch
    {
        ItemCategory.Echo => UnlockedEchoSlots,
        ItemCategory.Relic   => UnlockedRelicSlots,
        _                    => UnlockedEquipmentSlots
    };

    private void TrimArray(ItemBaseData[] arr, int maxCount)
    {
        for (int i = maxCount; i < arr.Length; i++)
        {
            if (arr[i] != null)
            {
                if (arr[i] is RelicData r) RemoveRelicStats(r);
                SpawnDroppedItem(arr[i]);
                arr[i] = null;
            }
        }
    }

    private void AddRelicStats(RelicData relic)
    {
        if (Run != null)
        {
            Run.bonusVitality += relic.bonusVitality;
            Run.bonusSorcery += relic.bonusSorcery;
            Run.bonusResonance += relic.bonusResonance;

            if (relic.bonusVitality > 0 && healthSystem != null)
            {
                int hpGain = relic.bonusVitality * 10;
                healthSystem.SetMaxHP(healthSystem.MaxHP + hpGain, false);
                healthSystem.Heal(hpGain);
            }
        }
    }

    private void RemoveRelicStats(RelicData relic)
    {
        if (Run != null)
        {
            Run.bonusVitality -= relic.bonusVitality;
            Run.bonusSorcery -= relic.bonusSorcery;
            Run.bonusResonance -= relic.bonusResonance;

            if (relic.bonusVitality > 0 && healthSystem != null)
            {
                int hpLoss = relic.bonusVitality * 10;
                healthSystem.SetMaxHP(Mathf.Max(1, healthSystem.MaxHP - hpLoss), false);
            }
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

        if (dropped.TryGetComponent(out ItemDrop itemDrop))
            itemDrop.Initialize(popForce, item);
        else if (dropped.TryGetComponent(out Rigidbody2D rb))
            rb.AddForce(popForce, ForceMode2D.Impulse);
    }
}
