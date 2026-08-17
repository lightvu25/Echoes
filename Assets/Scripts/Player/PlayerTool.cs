using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthSystem), typeof(PlayerBuffManager), typeof(EquipmentRuntimeRegistry))]
public class PlayerTool : MonoBehaviour
{
    public class ToolState
    {
        public int CurrentUses;
        public float NextRechargeTime;
        public float CooldownDuration;
        public int MaxUses;
    }

    private readonly Dictionary<string, ToolState> toolStates = new Dictionary<string, ToolState>();
    private PlayerBuffManager buffManager;
    private EquipmentRuntimeRegistry runtimeRegistry;
    private bool inputSubscribed;

    public event System.Action OnConsume;

    private void Awake()
    {
        buffManager = GetComponent<PlayerBuffManager>();
        runtimeRegistry = GetComponent<EquipmentRuntimeRegistry>();
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void Start()
    {
        // GameInput may initialize after this component's OnEnable.
        SubscribeToInput();
    }

    private void Update()
    {
        if (PlayerInventoryCore.Instance == null) return;
        
        // Recharge equipped tools over time
        foreach (var item in PlayerInventoryCore.Instance.EquippedTools)
        {
            if (item is ToolData tool && !string.IsNullOrWhiteSpace(tool.itemID))
            {
                ToolState state = GetOrCreateState(tool);
                if (state.CurrentUses < state.MaxUses)
                {
                    if (Time.time >= state.NextRechargeTime)
                    {
                        state.CurrentUses++;
                        if (state.CurrentUses < state.MaxUses)
                        {
                            state.NextRechargeTime = Time.time + state.CooldownDuration;
                        }
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
    }

    private void SubscribeToInput()
    {
        if (inputSubscribed || GameInput.Instance == null) return;
        GameInput.Instance.OnToolKeyPressed += HandleToolInput;
        inputSubscribed = true;
    }

    private void UnsubscribeFromInput()
    {
        if (!inputSubscribed) return;
        if (GameInput.Instance != null) GameInput.Instance.OnToolKeyPressed -= HandleToolInput;
        inputSubscribed = false;
    }

    private ToolState GetOrCreateState(ToolData tool)
    {
        if (!toolStates.TryGetValue(tool.itemID, out ToolState state))
        {
            state = new ToolState 
            { 
                CurrentUses = Mathf.Max(1, tool.maxUses), 
                MaxUses = Mathf.Max(1, tool.maxUses),
                CooldownDuration = tool.cooldown
            };
            toolStates[tool.itemID] = state;
        }
        else
        {
            // Sync properties in case they were updated externally
            state.MaxUses = Mathf.Max(1, tool.maxUses);
            state.CooldownDuration = tool.cooldown;
        }
        return state;
    }

    private void HandleToolInput(int slotIndex)
    {
        if (slotIndex < 0) return;
        if (PlayerInventoryCore.Instance == null) return;

        IReadOnlyList<ItemBaseData> equippedTools = PlayerInventoryCore.Instance.EquippedTools;
        if (slotIndex >= equippedTools.Count) return;

        ToolData activeTool = equippedTools[slotIndex] as ToolData;
        if (activeTool == null) return;
        
        string toolId = activeTool.itemID;
        if (string.IsNullOrWhiteSpace(toolId)) return;
        
        ToolState state = GetOrCreateState(activeTool);
        if (state.CurrentUses <= 0) return; // Out of charges, waiting for cooldown

        EquipmentUseContext context = new EquipmentUseContext(gameObject, buffManager, () => OnConsume?.Invoke());
        if (runtimeRegistry != null && runtimeRegistry.TryExecute(activeTool, context))
        {
            if (state.CurrentUses == state.MaxUses) 
            {
                // Start the cooldown timer for the first consumed charge
                state.NextRechargeTime = Time.time + state.CooldownDuration;
            }
            state.CurrentUses--;
        }
    }

    public float GetRemainingCooldown(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId) || !toolStates.TryGetValue(toolId, out ToolState state)) return 0f;
        if (state.CurrentUses >= state.MaxUses) return 0f;
        return Mathf.Max(0f, state.NextRechargeTime - Time.time);
    }

    public float GetTotalCooldown(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId) || !toolStates.TryGetValue(toolId, out ToolState state)) return 1f;
        return Mathf.Max(0.1f, state.CooldownDuration);
    }
    
    public int GetCurrentUses(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId) || !toolStates.TryGetValue(toolId, out ToolState state)) return 0;
        return state.CurrentUses;
    }
}
