using System;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class PlaystyleManager : MonoBehaviour
{
    public event Action<PlaystyleType> OnPlaystyleUnlocked;
    public event Action<PlaystyleType> OnPlaystyleLocked;

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

    public bool IsPlaystyleUnlocked(PlaystyleType type)
    {
        // Bypass the slot requirement so you can test all weapons immediately!
        return true;
        
        /*
        if (healthSystem == null) return false;

        // Slot 0 (1 slot) = Melee
        // Slot 1 (2 slots) = MidRange
        // Slot 2 (3 slots) = LongRange
        // Slot 3 (4 slots) = Magic
        int unlockedSlots = healthSystem.UnlockedSlots;

        return type switch
        {
            PlaystyleType.Melee => unlockedSlots >= 1,
            PlaystyleType.MidRange => unlockedSlots >= 2,
            PlaystyleType.LongRange => unlockedSlots >= 3,
            PlaystyleType.Magic => unlockedSlots >= 4,
            _ => false
        };
        */
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
        
        // Check if an Echo is equipped
        return PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.GetActiveEcho() != null;
    }

    private void HandleSlotsDecreased(int newSlots)
    {
        // When taking damage, slots decrease, making higher-tier playstyles locked.
        if (newSlots < 4) OnPlaystyleLocked?.Invoke(PlaystyleType.Magic);
        if (newSlots < 3) OnPlaystyleLocked?.Invoke(PlaystyleType.LongRange);
        if (newSlots < 2) OnPlaystyleLocked?.Invoke(PlaystyleType.MidRange);
        if (newSlots < 1) OnPlaystyleLocked?.Invoke(PlaystyleType.Melee);
    }
}
