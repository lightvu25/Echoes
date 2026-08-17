using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Development helper for granting specific inventory assets during Play Mode.
/// Uses PlayerInventoryCore so Relic effects, Echo runtime data, Equipment logic,
/// inventory events, and UI refreshes follow the same path as normal pickups.
/// </summary>
[AddComponentMenu("Echoes/Debug/Manual Loadout Tester")]
[DisallowMultipleComponent]
public sealed class ManualLoadoutTester : MonoBehaviour
{
    [Header("Items To Test")]
    [SerializeField] private RelicData relicToGive;
    [SerializeField] private EchoData echoToGive;
    [SerializeField] private ToolData equipmentToGive;

    [Header("Grant Behaviour")]
    [Tooltip("Prevents accidentally equipping multiple copies of the same item ID.")]
    [SerializeField] private bool skipIfAlreadyEquipped = true;

    [Tooltip("When no empty unlocked slot exists, remove the first equipped item in that category and grant the selected item.")]
    [SerializeField] private bool replaceFirstItemWhenFull = true;

    [Tooltip("Makes a newly granted Echo the active combat Echo immediately.")]
    [SerializeField] private bool activateGrantedEcho = true;

    [Header("Runtime Controls")]
    [SerializeField] private bool enableHotkeys = true;
    [SerializeField] private KeyCode giveRelicKey = KeyCode.F6;
    [SerializeField] private KeyCode giveEchoKey = KeyCode.F7;
    [SerializeField] private KeyCode giveEquipmentKey = KeyCode.F8;

    [Tooltip("Shows simple buttons in the Game view while playing.")]
    [SerializeField] private bool showRuntimePanel = true;
    [SerializeField] private Vector2 panelPosition = new Vector2(10f, 190f);

    private const float PanelWidth = 330f;
    private Rect panelRect;

    private void Awake()
    {
        panelRect = new Rect(panelPosition.x, panelPosition.y, PanelWidth, 230f);
    }

    private void OnEnable()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        enabled = false;
#endif
    }

    private void Update()
    {
        if (!enableHotkeys) return;

        if (Input.GetKeyDown(giveRelicKey)) GiveSelectedRelic();
        if (Input.GetKeyDown(giveEchoKey)) GiveSelectedEcho();
        if (Input.GetKeyDown(giveEquipmentKey)) GiveSelectedEquipment();
    }

    [ContextMenu("Give Selected Relic")]
    public void GiveSelectedRelic()
    {
        GiveItem(relicToGive, "Relic");
    }

    [ContextMenu("Give Selected Echo")]
    public void GiveSelectedEcho()
    {
        GiveItem(echoToGive, "Echo");
    }

    [ContextMenu("Give Selected Equipment")]
    public void GiveSelectedEquipment()
    {
        GiveItem(equipmentToGive, "Equipment");
    }

    [ContextMenu("Give All Selected Items")]
    public void GiveAllSelectedItems()
    {
        GiveItem(relicToGive, "Relic");
        GiveItem(echoToGive, "Echo");
        GiveItem(equipmentToGive, "Equipment");
    }

    [ContextMenu("Remove All Selected Items")]
    public void RemoveAllSelectedItems()
    {
        RemoveMatchingItems(relicToGive, "Relic");
        RemoveMatchingItems(echoToGive, "Echo");
        RemoveMatchingItems(equipmentToGive, "Equipment");
    }

    private void GiveItem(ItemBaseData item, string label)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[ManualLoadoutTester] Enter Play Mode before granting {label} items.", this);
            return;
        }

        if (item == null)
        {
            Debug.LogWarning($"[ManualLoadoutTester] No {label} asset is assigned.", this);
            return;
        }

        PlayerInventoryCore inventory = PlayerInventoryCore.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[ManualLoadoutTester] PlayerInventoryCore is not ready yet.", this);
            return;
        }

        if (skipIfAlreadyEquipped && ContainsItem(inventory, item))
        {
            Debug.Log($"[ManualLoadoutTester] {label} '{DisplayName(item)}' is already equipped.", item);
            return;
        }

        if (!HasEmptyUnlockedSlot(inventory, item.Category))
        {
            if (!replaceFirstItemWhenFull)
            {
                Debug.LogWarning(
                    $"[ManualLoadoutTester] No empty unlocked {label} slot. Enable replacement or remove an item first.",
                    this);
                return;
            }

            ItemBaseData itemToReplace = FindFirstEquippedUnlockedItem(inventory, item.Category);
            if (itemToReplace == null)
            {
                Debug.LogWarning($"[ManualLoadoutTester] Could not find an unlocked {label} slot to replace.", this);
                return;
            }

            inventory.RemoveEquippedItem(itemToReplace);
            Debug.Log(
                $"[ManualLoadoutTester] Removed '{DisplayName(itemToReplace)}' to test '{DisplayName(item)}'.",
                this);
        }

        inventory.TryEquip(item);

        if (ContainsItem(inventory, item))
        {
            if (activateGrantedEcho && item.Category == ItemCategory.Echo)
                ActivateMatchingEcho(inventory, item);

            Debug.Log($"[ManualLoadoutTester] Granted {label}: {DisplayName(item)} ({item.itemID}).", item);
        }
        else
            Debug.LogWarning($"[ManualLoadoutTester] {label} '{DisplayName(item)}' was not equipped.", item);
    }

    private void RemoveMatchingItems(ItemBaseData selectedItem, string label)
    {
        if (!Application.isPlaying || selectedItem == null || PlayerInventoryCore.Instance == null) return;

        PlayerInventoryCore inventory = PlayerInventoryCore.Instance;
        IReadOnlyList<ItemBaseData> equipped = inventory.GetEquippedList(selectedItem.Category);
        List<ItemBaseData> matches = new List<ItemBaseData>();

        for (int i = 0; i < equipped.Count; i++)
        {
            if (IsSameItem(equipped[i], selectedItem)) matches.Add(equipped[i]);
        }

        foreach (ItemBaseData match in matches)
            inventory.RemoveEquippedItem(match);

        if (matches.Count > 0)
            Debug.Log($"[ManualLoadoutTester] Removed {matches.Count} matching {label} item(s).", this);
    }

    private static bool ContainsItem(PlayerInventoryCore inventory, ItemBaseData selectedItem)
    {
        IReadOnlyList<ItemBaseData> equipped = inventory.GetEquippedList(selectedItem.Category);
        for (int i = 0; i < equipped.Count; i++)
        {
            if (IsSameItem(equipped[i], selectedItem)) return true;
        }
        return false;
    }

    private static bool HasEmptyUnlockedSlot(PlayerInventoryCore inventory, ItemCategory category)
    {
        IReadOnlyList<ItemBaseData> equipped = inventory.GetEquippedList(category);
        for (int i = 0; i < equipped.Count; i++)
        {
            if (inventory.IsSlotUnlocked(category, i) && equipped[i] == null) return true;
        }
        return false;
    }

    private static void ActivateMatchingEcho(PlayerInventoryCore inventory, ItemBaseData selectedItem)
    {
        IReadOnlyList<ItemBaseData> equipped = inventory.GetEquippedList(ItemCategory.Echo);
        for (int i = 0; i < equipped.Count; i++)
        {
            if (!IsSameItem(equipped[i], selectedItem)) continue;
            inventory.SetActiveEchoIndex(i);
            return;
        }
    }

    private static ItemBaseData FindFirstEquippedUnlockedItem(PlayerInventoryCore inventory, ItemCategory category)
    {
        IReadOnlyList<ItemBaseData> equipped = inventory.GetEquippedList(category);
        for (int i = 0; i < equipped.Count; i++)
        {
            if (inventory.IsSlotUnlocked(category, i) && equipped[i] != null) return equipped[i];
        }
        return null;
    }

    private static bool IsSameItem(ItemBaseData equipped, ItemBaseData selected)
    {
        if (equipped == null || selected == null || equipped.Category != selected.Category) return false;
        if (!string.IsNullOrWhiteSpace(selected.itemID)) return equipped.itemID == selected.itemID;
        return equipped == selected || equipped.itemName == selected.itemName;
    }

    private static string DisplayName(ItemBaseData item)
    {
        if (item == null) return "None";
        return string.IsNullOrWhiteSpace(item.itemName) ? item.name : item.itemName;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !showRuntimePanel) return;
        panelRect = GUI.Window(GetInstanceID(), panelRect, DrawRuntimePanel, "Manual Loadout Tester");
    }

    private void DrawRuntimePanel(int windowId)
    {
        GUILayout.Label($"Relic: {DisplayName(relicToGive)}");
        if (GUILayout.Button($"Give Relic ({giveRelicKey})")) GiveSelectedRelic();

        GUILayout.Label($"Echo: {DisplayName(echoToGive)}");
        if (GUILayout.Button($"Give Echo ({giveEchoKey})")) GiveSelectedEcho();

        GUILayout.Label($"Equipment: {DisplayName(equipmentToGive)}");
        if (GUILayout.Button($"Give Equipment ({giveEquipmentKey})")) GiveSelectedEquipment();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Give All")) GiveAllSelectedItems();
        if (GUILayout.Button("Remove Selected")) RemoveAllSelectedItems();
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0f, 0f, PanelWidth, 24f));
    }
}
