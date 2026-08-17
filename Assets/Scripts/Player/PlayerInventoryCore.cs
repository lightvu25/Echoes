using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central inventory manager for Project Echoes.
/// Manages three independent slot lists (Echoes, Relics, Items) whose
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

    [Header("Run Persistence")]
    [Tooltip("Catalog used to restore saved item IDs without Resources lookups or runtime asset-path searches.")]
    [SerializeField] private RunItemCatalog itemCatalog;

    // ------------------------------------------------------------------ //
    //  Drop physics settings                                               //
    // ------------------------------------------------------------------ //

    [Header("Drop Settings")]
    public float popForceMin      = 5f;
    public float popForceMax      = 10f;
    public float sidewaysForceMod = 1.5f;



    // ------------------------------------------------------------------ //
    //  Private state                                                       //
    // ------------------------------------------------------------------ //

    private HealthSystem healthSystem;
    private PlaystyleManager playstyleManager;
    private bool hasRestoredRunInventory;

    /// <summary>Index of the currently active Echo slot (used by HotbarController).</summary>
    private int activeEchoIndex = 0;
    
    /// <summary>The currently active slot index (0 to 3). Useful for checking combat styles.</summary>
    public int ActiveEchoIndex => activeEchoIndex;

    // ------------------------------------------------------------------ //
    //  Public accessors                                                    //
    // ------------------------------------------------------------------ //

    /// <summary>Read-only view of equipped Echoes.</summary>
    public IReadOnlyList<ItemBaseData> EquippedEchoes => equippedEchoes;

    /// <summary>Read-only view of equipped Relics.</summary>
    public IReadOnlyList<ItemBaseData> EquippedRelics => equippedRelics;

    /// <summary>Read-only view of equipped Tools.</summary>
    public IReadOnlyList<ItemBaseData> EquippedTools => equippedTools;

    /// <summary>Echo at the active hotbar index, or null if the slot is empty.</summary>
    public EchoData ActiveEcho =>
        activeEchoIndex < RunData.MAX_SLOTS
            ? equippedEchoes[activeEchoIndex] as EchoData
            : null;

    // ------------------------------------------------------------------ //
    //  Slot counts & Unlock state                                          //
    // ------------------------------------------------------------------ //

    public static RunData Run => GameSession.Instance?.currentRun;

    public int AvailableUnlockPoints => Run != null ? Run.availableUnlockPoints : 0;

    public bool IsSlotUnlocked(ItemCategory category, int index)
    {
        if (Run == null) return index == 0; // Default fallback: slot 0 is unlocked
        return category switch
        {
            ItemCategory.Echo => Run.unlockedEchoIndices.Contains(index),
            ItemCategory.Relic => Run.unlockedRelicIndices.Contains(index),
            _ => Run.unlockedToolIndices.Contains(index),
        };
    }

    public int GetUnlockedCount(ItemCategory category)
    {
        if (Run == null) return 1;
        return category switch
        {
            ItemCategory.Echo => Run.unlockedEchoIndices.Count,
            ItemCategory.Relic => Run.unlockedRelicIndices.Count,
            _ => Run.unlockedToolIndices.Count,
        };
    }

    public int UnlockedEchoSlots => GetUnlockedCount(ItemCategory.Echo);
    public int UnlockedRelicSlots => GetUnlockedCount(ItemCategory.Relic);
    public int UnlockedEquipmentSlots => GetUnlockedCount(ItemCategory.Tool);

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
        playstyleManager = GetComponent<PlaystyleManager>();

        TryRestoreRunInventory();
    }

    private void Start()
    {
        if (!hasRestoredRunInventory)
            TryRestoreRunInventory();

        if (healthSystem != null)
        {
            healthSystem.OnMaxHPGained       += HandleMaxHPGained;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnMaxHPGained       -= HandleMaxHPGained;
        }

        if (Instance == this)
            Instance = null;
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
        for (int i = 0; i < RunData.MAX_SLOTS; i++)
        {
            if (IsSlotUnlocked(item.Category, i) && arr[i] == null)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex >= 0)
        {
            arr[emptyIndex] = item;
            if (item is RelicData r) AddRelicStats(r);
            PersistInventoryState();
            OnInventoryChanged?.Invoke();
        }
        else
        {
            OnSwapRequired?.Invoke(item, item.Category);
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
        PersistInventoryState();
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
            PersistInventoryState();
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
                PersistInventoryState();
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
        
        if (!IsSlotUnlocked(category, fromIndex) || !IsSlotUnlocked(category, toIndex)) return;
        
        ItemBaseData temp = arr[fromIndex];
        arr[fromIndex] = arr[toIndex];
        arr[toIndex] = temp;

        PersistInventoryState();
        OnInventoryChanged?.Invoke();
    }

    // ------------------------------------------------------------------ //
    //  Slot unlock (called by UI after player makes a choice)             //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Unlocks a specific slot index and consumes 1 available point.
    /// </summary>
    public void UnlockSlot(ItemCategory category, int index)
    {
        if (Run == null || Run.availableUnlockPoints <= 0) return;
        if (IsSlotUnlocked(category, index)) return;

        switch (category)
        {
            case ItemCategory.Echo:
                Run.unlockedEchoIndices.Add(index);
                break;
            case ItemCategory.Relic:
                Run.unlockedRelicIndices.Add(index);
                break;
            case ItemCategory.Tool:
                Run.unlockedToolIndices.Add(index);
                break;
        }

        Run.availableUnlockPoints--;
        GameSession.Instance?.SaveCurrentRun();
        OnInventoryChanged?.Invoke();
    }

    // ------------------------------------------------------------------ //
    //  Hotbar (called by HotbarController)                                 //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Sets the active hotbar index for Echoes.
    /// Clamps to the current number of unlocked Echo slots.
    /// </summary>
    /// <param name="index">0-based slot index.</param>
    public void SetActiveEchoIndex(int index)
    {
        if (IsSlotUnlocked(ItemCategory.Echo, index))
        {
            if (activeEchoIndex != index)
            {
                activeEchoIndex = index;
                PersistInventoryState();
                OnInventoryChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Returns the currently active EchoData based on the activeEchoIndex.
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
    /// Returns all active EchoTypes across equipped Echoes (for combat use).
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
        if (Run != null)
        {
            Run.availableUnlockPoints++;
            GameSession.Instance?.SaveCurrentRun();
            OnInventoryChanged?.Invoke();
        }
        OnSlotUnlockRequired?.Invoke(); // Kept for any listeners that still want this (e.g., sound effects)
    }

    private void RestoreEquippedItemsFromRun()
    {
        RunData run = Run;
        EnsureSlotList(ref run.equippedEchoIds);
        EnsureSlotList(ref run.equippedRelicIds);
        EnsureSlotList(ref run.equippedToolIds);

        if (itemCatalog == null)
        {
            if (HasSavedItems(run.equippedEchoIds) || HasSavedItems(run.equippedRelicIds) || HasSavedItems(run.equippedToolIds))
                Debug.LogError("[PlayerInventoryCore] A saved loadout exists, but no RunItemCatalog is assigned.", this);
            return;
        }

        RestoreCategory(equippedEchoes, run.equippedEchoIds, ItemCategory.Echo);
        RestoreCategory(equippedRelics, run.equippedRelicIds, ItemCategory.Relic);
        RestoreCategory(equippedTools, run.equippedToolIds, ItemCategory.Tool);
        activeEchoIndex = Mathf.Clamp(run.activeEchoIndex, 0, RunData.MAX_SLOTS - 1);
    }

    private void TryRestoreRunInventory()
    {
        RunData run = Run;
        if (hasRestoredRunInventory || run == null) return;

        // Migrate old integer slot counts before restoring saved slot contents.
        if (run.unlockedEchoSlots > 1 && run.unlockedEchoIndices.Count == 1)
        {
            for (int i = 1; i < run.unlockedEchoSlots; i++)
                if (!run.unlockedEchoIndices.Contains(i)) run.unlockedEchoIndices.Add(i);
        }

        if (run.unlockedRelicSlots > 1 && run.unlockedRelicIndices.Count == 1)
        {
            for (int i = 1; i < run.unlockedRelicSlots; i++)
                if (!run.unlockedRelicIndices.Contains(i)) run.unlockedRelicIndices.Add(i);
        }

        if (run.unlockedEquipmentSlots > 1 && run.unlockedToolIndices.Count == 1)
        {
            for (int i = 1; i < run.unlockedEquipmentSlots; i++)
                if (!run.unlockedToolIndices.Contains(i)) run.unlockedToolIndices.Add(i);
        }

        RestoreEquippedItemsFromRun();
        hasRestoredRunInventory = true;
    }

    private void RestoreCategory(ItemBaseData[] destination, List<string> savedIds, ItemCategory expectedCategory)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            string itemId = savedIds[i];
            if (string.IsNullOrWhiteSpace(itemId)) continue;

            if (!itemCatalog.TryGetItem(itemId, out ItemBaseData source))
            {
                Debug.LogWarning($"[PlayerInventoryCore] Saved item '{itemId}' is missing from the run item catalog.", this);
                continue;
            }

            if (source.Category != expectedCategory)
            {
                Debug.LogWarning($"[PlayerInventoryCore] Saved item '{itemId}' belongs to {source.Category}, not {expectedCategory}.", source);
                continue;
            }

            destination[i] = CreateRuntimeItem(source);
        }
    }

    private static ItemBaseData CreateRuntimeItem(ItemBaseData source)
    {
        if (source is EchoData echoData)
        {
            EchoData runtimeEcho = Instantiate(echoData);
            runtimeEcho.InitRuntime();
            return runtimeEcho;
        }

        if (source is RelicData relicData)
        {
            RelicData runtimeRelic = Instantiate(relicData);
            runtimeRelic.InitRuntime();
            return runtimeRelic;
        }

        return source;
    }

    private void PersistInventoryState()
    {
        RunData run = Run;
        if (run == null) return;

        WriteSlotIds(equippedEchoes, ref run.equippedEchoIds);
        WriteSlotIds(equippedRelics, ref run.equippedRelicIds);
        WriteSlotIds(equippedTools, ref run.equippedToolIds);
        run.activeEchoIndex = activeEchoIndex;

        run.currentRelics ??= new List<string>();
        run.currentRelics.Clear();
        for (int i = 0; i < equippedRelics.Length; i++)
        {
            if (equippedRelics[i] != null && !string.IsNullOrWhiteSpace(equippedRelics[i].itemID))
                run.currentRelics.Add(equippedRelics[i].itemID);
        }

        GameSession.Instance?.SaveCurrentRun();
    }

    private static void WriteSlotIds(ItemBaseData[] source, ref List<string> destination)
    {
        EnsureSlotList(ref destination);
        for (int i = 0; i < RunData.MAX_SLOTS; i++)
            destination[i] = source[i] != null ? source[i].itemID : string.Empty;
    }

    private static void EnsureSlotList(ref List<string> slots)
    {
        slots ??= new List<string>(RunData.MAX_SLOTS);
        while (slots.Count < RunData.MAX_SLOTS) slots.Add(string.Empty);
        if (slots.Count > RunData.MAX_SLOTS)
            slots.RemoveRange(RunData.MAX_SLOTS, slots.Count - RunData.MAX_SLOTS);
    }

    private static bool HasSavedItems(List<string> ids)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(ids[i])) return true;
        }

        return false;
    }

    // HandlePlaystyleLocked removed because playstyles are permanently unlocked.



    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private ItemBaseData[] GetArray(ItemCategory category) => category switch
    {
        ItemCategory.Echo => equippedEchoes,
        ItemCategory.Relic   => equippedRelics,
        _                    => equippedTools
    };

    // Removed GetUnlockedCount(ItemCategory category) helper since it is now public above

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
            // Vitality is applied through StatBonusSystem so shrine and relic stacks
            // share the same Harmonic Decay pool (run.bonusVitality is the counter).
            if (relic.bonusVitality > 0)
                StatBonusSystem.ApplyVitalityBonus(Run, healthSystem, relic.bonusVitality);

            Run.bonusSorcery += relic.bonusSorcery;
            Run.bonusResonance += relic.bonusResonance;
        }
    }

    private void RemoveRelicStats(RelicData relic)
    {
        if (Run != null)
        {
            // Reverse the Vitality HP gain using the same Harmonic Decay curve.
            if (relic.bonusVitality > 0)
                StatBonusSystem.RemoveVitalityBonus(Run, healthSystem, relic.bonusVitality);

            Run.bonusSorcery -= relic.bonusSorcery;
            Run.bonusResonance -= relic.bonusResonance;
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
