# Mask Weaver Core Systems Update Log

## Files Modified/Created

| File Path | Action | Description |
|-----------|--------|-------------|
| `Assets/Scripts/Combat System/HealthSystem.cs` | Modified | Added Slot Threshold logic, `TakeDamage` audio hooks, and Event hooks. |
| `Assets/Scripts/Audio/EntityAudioManager.cs` | Created | Handles localized audio playback, pooling, and global off-loading for death SFX. |
| `Assets/Scripts/Manager/System/SoundManager.cs` | Modified | Expanded to manage global music, evolution sounds, and UI volume settings. |
| `Assets/Scripts/UI/PausedUI.cs` | Modified | Integrated Audio Sliders directly tied into the global `SoundManager`. |
| `Assets/Scripts/UI/FeedbackUI.cs` | Created | Added dynamic UI Tooltips for items, mechanics, and environment interactions. |
| `Assets/Scripts/Mind/MindNode.cs` | Created | Core logic for the Mind World graph, handling node connections, risk/reward branching, and challenge tracking. |
| `Assets/Scripts/Player/PlayerRelicManager.cs` | Created | Manages Relic logic, equipping, effects, and synergies across the player's build. |
| `Assets/Scripts/Manager/Gameplay/EchoEffectManager.cs` | Created | Tracks active Echoes and dispatches their unique combat modifier logics. |
| `Assets/Scripts/Environment/Interactables/CrimsonFlower.cs` | Created | Map-based healing resource for refilling Healing Flasks on interaction. |
| `README.md` | Modified | This exact summary file. |


## New & Updated Systems Overview

### 1. Audio System Integration
- **EntityAudioManager**: Enemies and players now use a robust `EntityAudioManager` that handles local SFX like footsteps, attacks, and roars via Animation Events.
- **Global & Lifecycle Resilience**: Implemented a `PlaySoundGlobal` method so critical sounds (like Enemy Death) don't get cut off when the enemy GameObject is rapidly disabled and returned to the Object Pool Manager.
- **Global Evolution Sound**: Hooked the `EvolutionManager` directly into `SoundManager` to blast a global sound when enemies tier-up based on kill counts.

### 2. Mind World Graph Setup
The Mind World is now driven by a fully interactive Node-based graph (`MindNode.cs`).
- **Path Selection**: Players can interact with Nodes to see upcoming paths via the UI.
- **Risk & Reward**: Each Node can carry a `MindNodeModifierData` that applies buffs or curses to the run if the path is accepted.
- **Challenges**: Integrated Challenge Nodes (e.g., No-hit rooms requiring 30 flawless kills, or Speedrun doors requiring completion within 120 seconds).
- **Branching Logic**: Selecting a node can physically cut or connect visual branches (`MindNodeConnection`), dynamically altering the layout of the Mind World for that run.

### 3. Combat, Relics, and Echoes
- **Relic Logic**: Added a suite of modular relics (e.g., `CompensatingSawRelic`, `DyingAmuletRelic`, `CondemnedRingRelic`) that hook into the `PlayerRelicManager` to provide persistent passive effects and run-altering stat changes.
- **Echo Logic**: Extended the Echo system. Different Echo elements are now handled via `IEchoModifier` classes (e.g., `BlazeModifier`, `VoidModifier`, `CryoStasisModifier`) and are tracked globally by the `EchoEffectManager`.
- **Healing Flask & Crimson Flowers**: Added the Healing Flask item, allowing players to heal on demand. The flask charges are naturally refilled by finding and interacting with `CrimsonFlower` objects spawned natively inside the combat maps.

### 4. UI & QoL Improvements
- **UI Tooltips**: Added robust tooltips (`FeedbackUI`) to explain mechanics, item stats, and interactables dynamically as the player explores.
- **Menu/Paused UI Rework**: The Pause Menu is fully functional, featuring non-blocking interactive Sliders (Music/Sound) that modify the `AudioMixer` properly. Fixed TextMeshPro raycast blocking issues to ensure smooth UI dragging on timescale 0.
- **Item Logic**: Expanded item dropping/picking up systems, tying into `DroppedMemoryItem`, resources, and the overall Object Pool.
