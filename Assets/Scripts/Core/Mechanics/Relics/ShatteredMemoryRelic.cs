using UnityEngine;

/// <summary>
/// PoC Relic: Event Listener & Stacking
/// Killing an enemy grants +5% Attack Speed (Stacks up to 5 times). 
/// Resets per room.
/// </summary>
public class ShatteredMemoryRelic : MonoBehaviour, IRelicEffect
{
    private Animator playerAnimator;
    private int currentStacks = 0;
    private const int MAX_STACKS = 5;
    private const float SPEED_PER_STACK = 0.05f;

    public void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID)
    {
        playerAnimator = GetComponentInChildren<Animator>();

        eventBus.OnEnemyKilled += HandleEnemyKilled;
        eventBus.OnRoomCleared += HandleRoomCleared;
    }

    public void OnUnequip(PlayerEventBus eventBus)
    {
        eventBus.OnEnemyKilled -= HandleEnemyKilled;
        eventBus.OnRoomCleared -= HandleRoomCleared;
        
        ResetStacks();
    }

    private void HandleEnemyKilled()
    {
        if (currentStacks < MAX_STACKS)
        {
            currentStacks++;
            ApplyAttackSpeed();
        }
    }

    private void HandleRoomCleared()
    {
        ResetStacks();
    }

    private void ApplyAttackSpeed()
    {
        if (playerAnimator != null)
        {
            // By default, Animator speed is 1.0f
            // We increase it by 5% per stack
            float totalSpeedMultiplier = 1.0f + (currentStacks * SPEED_PER_STACK);
            playerAnimator.speed = totalSpeedMultiplier;
        }
    }

    private void ResetStacks()
    {
        currentStacks = 0;
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1.0f; // Restore default speed
        }
    }
}
