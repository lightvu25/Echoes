# Mask Weaver Core Systems Update Log

## Files Modified/Created

| File Path | Action | Description |
|-----------|--------|-------------|
| `Assets/Scripts/Combat System/HealthSystem.cs` | Modified | Added Slot Threshold logic (`MaxSlots`, `UnlockedSlots`) and `OnSlotsChanged` event. |
| `Assets/Scripts/Player/MemoryInventorySystem.cs` | Modified | Swapped raw HP for `UnlockedSlots` when dropping items & checking capacity. |
| `Assets/Scripts/Player/PlayerStats.cs` | Modified | Added `SpendGold` and `SpendMemoryFragments` methods. |
| `Assets/Scripts/Data/ProfileData.cs` | Modified | Added `bankedMems` and `bonusStartingMaxHP` fields. |
| `Assets/Scripts/Player/PlayerInteract.cs` | Modified | Added detection and usage of `StatueInteractable` via `GameInput` / Input logic. |
| `Assets/Scripts/Data/BuffData.cs` | Created | New ScriptableObject holding info for buffs, icons, and banish costs. |
| `Assets/Scripts/Core/Managers/BuffSelectionManager.cs` | Created | Handles rolling offers, selecting, and banishing buffs with Mems. |
| `Assets/Scripts/Core/Mechanics/StatueInteractable.cs` | Created | Handles banking and withdrawing Mems to persistent Storage. |
| `Assets/Scripts/Core/Managers/ShopManager.cs` | Created | Allows spending coins for Max HP or Healing upgrades. |
| `Assets/Scripts/Core/Managers/EventManager.cs` | Created | Handles text-based choice events (e.g. Sacrifice 50% HP for an upgrade). |
| `Assets/Scripts/Core/Mechanics/WeaponVFXController.cs` | Created | Listens to inventory to swap elemental weapon trail and particle effects. |
| `README_Update_Log.md` | Created | This exact summary file. |


## Health Threshold Math Setup

The "Health-as-Inventory" mechanic dynamically limits inventory size based on the player's current HP threshold.
This prevents players from keeping items when they drop below fractional health thresholds, unlocking the items back out into the world.

`UnlockedSlots = CeilToInt(currentHP / hpPerSlot)` where `hpPerSlot = maxHP / maxSlots`

**Example Scenario (100 Max HP, 4 Slots):**
- 1 Slot = 25 HP.
- `currentHP = 100` -> `UnlockedSlots` = 4.
- `currentHP = 76` -> `UnlockedSlots` = 4. (The 4th slot remains unlocked as you retain enough HP).
- `currentHP = 74` -> `UnlockedSlots` = 3. (You lost the fractional 25 HP boundary; the outermost item drops out of inventory).
- Only when healed back to 76+ do you regain the 4th slot.

## Global C# Events Added

The entire system remains tightly decoupled via C# Actions/Events. You can wire UI or specific Sound/VFX controllers up to any of these:

* `HealthSystem.OnSlotsChanged(int currentUnlockedSlots)` - Fired when taking damage/healing passes a slot threshold boundary.
* `BuffSelectionManager.OnOfferReady(BuffData[] buffs)` - Fired when 3 random buffs are rolled (hook UI to spawn panels).
* `BuffSelectionManager.OnBuffSelected(BuffData buff)` - Fire when player picks a buff.
* `BuffSelectionManager.OnBuffBanished(string buffID)` - Fire when player bans a buff.
* `StatueInteractable.OnStatueOpened(int currentMems, int bankedMems)` - Hook up the Statue UI panel to show persistent savings.
* `ShopManager.OnShopOpened(ShopItem[] items)` - Display dynamic item costs and labels.
* `EventManager.OnEventTriggered(EventChoice[] choices)` - Draw text-driven dialog UI from `choices` and map their `onChosen` actions. 
