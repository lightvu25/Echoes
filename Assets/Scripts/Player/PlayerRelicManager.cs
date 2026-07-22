using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the lifecycle of Relic logic components.
/// Listens to PlayerInventoryCore and dynamically adds/removes IRelicEffect MonoBehaviours.
/// </summary>
[RequireComponent(typeof(PlayerEventBus))]
public class PlayerRelicManager : MonoBehaviour
{
    private PlayerEventBus eventBus;
    private Dictionary<string, IRelicEffect> activeRelics = new Dictionary<string, IRelicEffect>();

    // --- Static Registry ---
    // Maps RelicData.itemID to the concrete MonoBehaviour Type that implements the logic.
    private static readonly Dictionary<string, Type> RelicTypeRegistry = new Dictionary<string, Type>
    {
        // Migrated Old Relics
        { "IRON_RING", typeof(IronRingRelic) },
        { "COMPENSATING_SAW", typeof(CompensatingSawRelic) },
        { "DYING_AMULET", typeof(DyingAmuletRelic) },

        // New PoC Relics
        { "CONDEMNED_RING", typeof(CondemnedRingRelic) },
        { "SHATTERED_MEMORY", typeof(ShatteredMemoryRelic) },
        { "STALACTITE_HEART", typeof(StalactiteHeartRelic) }
    };

    private void Awake()
    {
        eventBus = GetComponent<PlayerEventBus>();
    }

    private void Start()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged += SyncRelics;
            SyncRelics(); // Initial sync
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged -= SyncRelics;
        }

        // Cleanup all active relics
        foreach (var relic in activeRelics.Values)
        {
            relic.OnUnequip(eventBus);
        }
        activeRelics.Clear();
    }

    /// <summary>
    /// Synchronizes attached components with the equipped relics in PlayerInventoryCore.
    /// </summary>
    private void SyncRelics()
    {
        if (PlayerInventoryCore.Instance == null) return;

        var equippedRelicsData = PlayerInventoryCore.Instance.EquippedRelics;
        HashSet<string> currentlyEquippedIDs = new HashSet<string>();

        // 1. Identify equipped relic IDs
        foreach (var item in equippedRelicsData)
        {
            if (item is RelicData relic && !string.IsNullOrEmpty(relic.itemID))
            {
                currentlyEquippedIDs.Add(relic.itemID);
                
                // Equip if not already active
                if (!activeRelics.ContainsKey(relic.itemID))
                {
                    EquipRelic(relic.itemID);
                }
            }
        }

        // 2. Identify and remove unequipped relics
        List<string> idsToRemove = new List<string>();
        foreach (var activeID in activeRelics.Keys)
        {
            if (!currentlyEquippedIDs.Contains(activeID))
            {
                idsToRemove.Add(activeID);
            }
        }

        foreach (var id in idsToRemove)
        {
            UnequipRelic(id);
        }
    }

    private void EquipRelic(string itemID)
    {
        if (RelicTypeRegistry.TryGetValue(itemID, out Type relicType))
        {
            // Add the component
            Component comp = gameObject.AddComponent(relicType);
            if (comp is IRelicEffect relicEffect)
            {
                activeRelics[itemID] = relicEffect;
                relicEffect.OnEquip(eventBus, this, itemID);
                Debug.Log($"[PlayerRelicManager] Equipped Relic: {itemID}");
            }
            else
            {
                Debug.LogError($"[PlayerRelicManager] Component {relicType.Name} does not implement IRelicEffect!");
                Destroy(comp);
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerRelicManager] No logic script registered for relic ID: {itemID}");
        }
    }

    private void UnequipRelic(string itemID)
    {
        if (activeRelics.TryGetValue(itemID, out IRelicEffect relicEffect))
        {
            relicEffect.OnUnequip(eventBus);
            activeRelics.Remove(itemID);

            // Destroy the MonoBehaviour component
            if (relicEffect is MonoBehaviour mb)
            {
                Destroy(mb);
            }
            Debug.Log($"[PlayerRelicManager] Unequipped Relic: {itemID}");
        }
    }
}
