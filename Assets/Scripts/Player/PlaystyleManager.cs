using System;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class PlaystyleManager : MonoBehaviour
{
    // Events removed as unlocks are now permanently managed by PlayerInventoryCore

    [Header("Default Playstyles")]
    [SerializeField] private PlaystyleData meleePlaystyle;
    [SerializeField] private PlaystyleData midRangePlaystyle;
    [SerializeField] private PlaystyleData longRangePlaystyle;
    [SerializeField] private PlaystyleData magicPlaystyle;

    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        // Unlocks are now managed by PlayerInventoryCore directly.
    }

    private void OnDestroy()
    {
    }

    public bool IsPlaystyleUnlocked(PlaystyleType type)
    {
        if (PlayerInventoryCore.Instance == null) return false;

        int index = type switch
        {
            PlaystyleType.Melee => 0,
            PlaystyleType.MidRange => 1,
            PlaystyleType.LongRange => 2,
            PlaystyleType.Magic => 3,
            _ => -1
        };

        if (index == -1) return false;
        
        bool isSlotUnlocked = PlayerInventoryCore.Instance.IsSlotUnlocked(ItemCategory.Echo, index);
        if (!isSlotUnlocked) return false;

        // The slot is unlocked in the inventory.
        // We do not require an Echo to be equipped to allow the attack.
        return true;
    }

    public PlaystyleData GetPlaystyleData(PlaystyleType type)
    {
        return type switch
        {
            PlaystyleType.Melee => meleePlaystyle,
            PlaystyleType.MidRange => midRangePlaystyle,
            PlaystyleType.LongRange => longRangePlaystyle,
            PlaystyleType.Magic => magicPlaystyle,
            _ => null
        };
    }

    public bool CanUseMagic()
    {
        if (!IsPlaystyleUnlocked(PlaystyleType.Magic)) return false;

        PlayerInventoryCore inventory = PlayerInventoryCore.Instance;
        const int magicEchoSlot = 3;
        return inventory != null &&
               inventory.EquippedEchoes != null &&
               inventory.EquippedEchoes.Count > magicEchoSlot &&
               inventory.EquippedEchoes[magicEchoSlot] is EchoData;
    }

    // HandleSlotsChanged and lastKnownSlots removed because max HP drops no longer lock styles.
}
