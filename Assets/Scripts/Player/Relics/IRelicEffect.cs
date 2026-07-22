using UnityEngine;

/// <summary>
/// The core interface for all Relic logic components.
/// Relics are added as MonoBehaviours to the Player and subscribe to the PlayerEventBus.
/// </summary>
public interface IRelicEffect
{
    /// <summary>
    /// Called when the relic is equipped.
    /// Use this to subscribe to events on the PlayerEventBus and cache references.
    /// </summary>
    /// <param name="eventBus">The central event bus for the player.</param>
    /// <param name="relicManager">The manager controlling this relic component.</param>
    /// <param name="itemID">The unique string ID of the relic data item.</param>
    void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID);

    /// <summary>
    /// Called when the relic is unequipped or consumed.
    /// Use this to clean up all event subscriptions and revert any base stat modifications.
    /// </summary>
    /// <param name="eventBus">The central event bus for the player.</param>
    void OnUnequip(PlayerEventBus eventBus);
}
