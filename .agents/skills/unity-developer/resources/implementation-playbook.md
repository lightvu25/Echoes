# Echoes Implementation Playbook

Reference implementations and patterns for the Echoes project.

---

## 1. Adding a New Enemy Type

Follow the existing component-based pattern:

```
Assets/Scripts/Enemy/
├── NewEnemyMovement.cs    # Movement/pathfinding (extend EnemyMovement or create new)
├── NewEnemyInteract.cs    # Combat behavior, aggression, state machine
├── NewEnemyVisual.cs      # Sprite animations, flip, effects
├── NewEnemyAudio.cs       # Sound effects for this enemy
```

### Steps
1. Create a `ScriptableObject` in `Assets/Enemy Data/` for stats
2. Create the component scripts following existing patterns
3. Implement `IDamageable` for damage reception
4. Use `HealthSystem` component for HP management
5. Set up A* pathfinding for movement
6. Create prefab in `Assets/Prefabs/`
7. Add audio clips and reference them in the Audio component

### Example ScriptableObject
```csharp
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy Data/New Enemy")]
public class NewEnemyData : EnemyData
{
    [Header("Unique Properties")]
    public float specialAttackCooldown = 3f;
    public float specialAttackRange = 2f;
}
```

---

## 2. Adding a New Player Ability

Follow the Player component pattern:

### Steps
1. Create new component (e.g., `PlayerDash.cs`) in `Assets/Scripts/Player/`
2. Reference `GameInput` for input binding
3. Add DOTween animations for visual feedback
4. Integrate with `GameFeelManager` for screen effects
5. Play audio via `PlayerAudio`
6. Update `PlayerData` ScriptableObject if needed

### Example Structure
```csharp
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private PlayerMovement playerMovement;
    
    private GameInput gameInput;
    
    private void Start()
    {
        gameInput = FindFirstObjectByType<GameInput>();
        // Subscribe to input events
    }
    
    private void PerformDash()
    {
        // Dash logic
        // Call GameFeelManager for screen effects
        // Play audio via PlayerAudio
    }
}
```

---

## 3. Extending the Combat System

### Adding a New Damage Type
1. Extend `DamageInfo` with new fields
2. Update `DamageCalculator` with new formulas
3. Add visual feedback in `SpriteColorFlasher`
4. Update `DamagePopup` for styled display

### Adding Status Effects
1. Create `StatusEffect` base class
2. Create specific effects (burn, freeze, poison, etc.)
3. Add `StatusEffectManager` component to entities
4. Integrate with `HealthSystem` for DoT/HoT
5. Add visual indicators via entity Visual components

---

## 4. UI Panel Pattern

### Steps
1. Create script in `Assets/Scripts/UI/`
2. Create Canvas prefab with UI elements
3. Wire up with `GameManager` for show/hide
4. Add DOTween animations for transitions

### Example
```csharp
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    
    public void Show()
    {
        canvasGroup.DOFade(1f, 0.3f);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    public void Hide()
    {
        canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        });
    }
}
```

---

## 5. Manager Singleton Pattern

```csharp
public class NewManager : MonoBehaviour
{
    public static NewManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

---

## 6. Performance Checklist

- [ ] Use object pooling for frequently spawned objects (projectiles, particles, enemies)
- [ ] Minimize `GetComponent<>()` calls — cache references in `Awake()`/`Start()`
- [ ] Use `CompareTag()` instead of `tag ==`
- [ ] Use `Physics2D.OverlapCircleNonAlloc()` to avoid GC allocations
- [ ] Use DOTween `SetAutoKill(true)` and `.Kill()` on `OnDestroy()`
- [ ] Use sprite atlases for batching draw calls
- [ ] Limit active 2D lights for mobile targets
- [ ] Profile with Unity Profiler before and after changes
- [ ] Use `[SerializeField]` private fields instead of public fields
- [ ] Avoid `Find()` methods in `Update()` — cache at start
