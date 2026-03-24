# Core Architecture & Coding Patterns

## 1. Clean, Consistent, and Reusable Code
* **Single Responsibility Principle (SRP):** Scripts must do exactly one thing. Do not write a monolithic `PlayerController` that handles movement, taking damage, shooting, and inventory. Split these into discrete, focused scripts (`PlayerMovement`, `Health`, `WeaponController`, `Inventory`).
* **Component-Based Modularity:** Build generic, reusable components instead of highly specific ones. Instead of writing `PlayerHealth` and `EnemyHealth`, create a single `Damageable` component that can be attached to the player, enemies, bosses, and destructible environment props.
* **Strict Naming Conventions:** Adhere completely to standard C# and Unity naming conventions to keep the codebase legible:
    * `PascalCase` for Classes, Structs, and Methods (e.g., `public void TakeDamage()`).
    * `camelCase` for local variables and parameters (e.g., `int damageAmount`).
    * `_camelCase` for private and protected fields (e.g., `private int _currentHealth;`).
    * `I` prefix for Interfaces (e.g., `IDamageable`).
* **DRY (Don't Repeat Yourself):** If the same logic appears in two different scripts, extract it into a shared utility class, a base class, or an interface. 

## 2. Data Management (ScriptableObjects)
* **Rule:** Never hardcode gameplay stats (health, damage, speed, drop rates) into MonoBehaviour scripts.
* **Implementation:** Use `ScriptableObjects` as data containers for all enemy types, weapon stats, loot tables, and room configurations. This allows for rapid balancing of Project Echoes without needing to recompile the C# code, and keeps memory usage low since multiple enemies share one data instance.

## 3. Event-Driven Architecture
* **Rule:** Strictly decouple game logic from UI, audio, and visual effects. 
* **Implementation:** Use C# `Action` or `UnityEvent` to broadcast state changes. For example, a `Health` script should never reference the health bar UI directly. Instead, it invokes a `public event Action<int> OnHealthChanged`. The UI Manager, Audio Manager, and Particle Manager should listen for that event and react accordingly.

## 4. State Machines for AI and Logic
* **Rule:** Avoid messy `if/else` or `switch` statements inside `Update()` for managing complex states (like enemy behaviors or player animations).
* **Implementation:** Implement the State Pattern or a Finite State Machine (FSM). Each state (e.g., `IdleState`, `ChaseState`, `AttackState`) should be its own class that handles its specific logic and transitions cleanly.

## 5. Performance, Memory, & Object Pooling
* **Rule:** Never use `Instantiate()` or `Destroy()` during active gameplay for frequently used objects. This causes garbage collection spikes and micro-stutters.
* **Implementation:** Use a strict Object Pool pattern for all projectiles, enemy spawns, floating damage text, and particle effects. Pre-warm these pools during scene load or room transitions.
* **Unity Methods:** Never use `GetComponent<>`, `FindObjectOfType<>`, or `GameObject.Find()` inside an `Update()`, `FixedUpdate()`, or `LateUpdate()` loop. Cache these references in `Awake()` or `Start()`. Compare tags using `CompareTag("TagName")` rather than `tag == "TagName"`.

## 6. Custom Tick Rates for AI and Game Logic
* **Rule:** Never run expensive calculations—such as A* pathfinding, line-of-sight checks, or damage-over-time (DOT) effects—every single frame inside an `Update()` loop. 
* **Implementation:** Use a Custom Tick System or Coroutines to throttle logic execution to a fixed rate (e.g., 5 to 10 ticks per second). 
    * *Example:* An enemy should only recalculate its path to the player every 0.2 seconds, not every frame. 
    * *Example:* Poison damage should tick exactly once per second.
* **Benefit:** This frees up the CPU for rendering and physics, keeping the game performant even with high entity counts.