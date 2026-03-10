---
description: How to add a new enemy type to the Echoes project
---

## Steps

1. **Create Enemy Data ScriptableObject**
   - Create a new ScriptableObject class extending `EnemyData` in `Assets/Scripts/Enemy/`
   - Create a new asset instance in `Assets/Enemy Data/`
   - Configure stats (health, damage, speed, etc.)

2. **Create Enemy Scripts** in `Assets/Scripts/Enemy/`
   - `[Name]Movement.cs` — Movement and A* pathfinding logic
   - `[Name]Interact.cs` — Combat behavior, state machine, aggression
   - `[Name]Visual.cs` — Sprite animations, flip, visual effects
   - `[Name]Audio.cs` — Sound effects for this enemy type

3. **Set up Combat Integration**
   - Add `HealthSystem` component
   - Implement `IDamageable` interface
   - Add `SpriteColorFlasher` for damage feedback
   - Configure `AttackHitbox` if needed

4. **Create Prefab**
   - Assemble all components on a GameObject
   - Add Rigidbody2D, Collider2D, SpriteRenderer
   - Configure A* Pathfinding seeker
   - Save as prefab in `Assets/Prefabs/`

5. **Add Audio**
   - Import audio clips to `Assets/Audio/`
   - Reference in the enemy Audio component

6. **Test**
   - Place in a scene and verify movement, combat, and visual/audio feedback
   - Test with the existing player combat system
