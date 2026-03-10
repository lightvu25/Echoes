---
description: How to add a new player ability to the Echoes project
---

## Steps

1. **Plan the Ability**
   - Define input binding (which button/key)
   - Define behavior (dash, special attack, shield, etc.)
   - Identify interactions with existing systems

2. **Update Input Actions** (if needed)
   - Open `Assets/InputActions.inputactions` in Unity
   - Add new action mapping
   - Regenerate `InputActions.cs` if using code generation

3. **Update GameInput**
   - Add event subscription for the new input action in `Assets/Scripts/GameInput.cs`
   - Expose C# event for the new ability

4. **Create Ability Script**
   - Create new MonoBehaviour in `Assets/Scripts/Player/` (e.g., `PlayerDash.cs`)
   - Subscribe to `GameInput` events
   - Implement ability logic with cooldowns, state checks, etc.

5. **Add Visual Feedback**
   - Add DOTween animations for ability effects
   - Update `PlayerVisual` if needed
   - Integrate with `GameFeelManager` for screen shake/hit stop

6. **Add Audio**
   - Add ability sound effects to `Assets/Audio/`
   - Trigger via `PlayerAudio` or `SoundManager`

7. **Update PlayerData** (if needed)
   - Add ability-related stats to `PlayerData` ScriptableObject
   - Configure in `Assets/Player Data/`

8. **Test**
   - Verify input responsiveness
   - Test interactions with enemies and environment
   - Check performance with Profiler
