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
            lastKnownSlots = healthSystem.UnlockedSlots;
            healthSystem.OnSlotsChanged += HandleSlotsChanged;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnSlotsChanged -= HandleSlotsChanged;
        }
    }

    public bool IsPlaystyleUnlocked(PlaystyleType type)
    {
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

    private int lastKnownSlots = 4;

    private void HandleSlotsChanged(int newSlots)
    {
        // Check what got locked (lost health)
        if (lastKnownSlots >= 4 && newSlots < 4) OnPlaystyleLocked?.Invoke(PlaystyleType.Magic);
        if (lastKnownSlots >= 3 && newSlots < 3) OnPlaystyleLocked?.Invoke(PlaystyleType.LongRange);
        if (lastKnownSlots >= 2 && newSlots < 2) OnPlaystyleLocked?.Invoke(PlaystyleType.MidRange);
        if (lastKnownSlots >= 1 && newSlots < 1) OnPlaystyleLocked?.Invoke(PlaystyleType.Melee);

        // Check what got unlocked (gained/healed health)
        if (lastKnownSlots < 4 && newSlots >= 4) OnPlaystyleUnlocked?.Invoke(PlaystyleType.Magic);
        if (lastKnownSlots < 3 && newSlots >= 3) OnPlaystyleUnlocked?.Invoke(PlaystyleType.LongRange);
        if (lastKnownSlots < 2 && newSlots >= 2) OnPlaystyleUnlocked?.Invoke(PlaystyleType.MidRange);
        if (lastKnownSlots < 1 && newSlots >= 1) OnPlaystyleUnlocked?.Invoke(PlaystyleType.Melee);

        lastKnownSlots = newSlots;
    }
}
