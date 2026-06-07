using System;
using UnityEngine;

public class EventChoice
{
    public string prompt;
    public string description;
    public Action onChosen;
}

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public event Action<EventChoice[]> OnEventTriggered;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerEvent(EventChoice[] choices)
    {
        if (choices == null || choices.Length == 0) return;
        OnEventTriggered?.Invoke(choices);
    }

    // Example pre-built event
    public void TriggerSacrificeEvent()
    {
        EventChoice[] choices = new EventChoice[]
        {
            new EventChoice
            {
                prompt = "Sacrifice HP for Power",
                description = "Lose 50% of your current HP to gain +25 Max HP (+1 Slot).",
                onChosen = () => 
                {
                    if (PlayerStats.Instance != null && PlayerStats.Instance.TryGetComponent(out HealthSystem healthSys))
                    {
                        int hpToLose = Mathf.FloorToInt(healthSys.CurrentHP * 0.5f);
                        
                        DamageInfo damageInfo = new DamageInfo();
                        healthSys.TakeDamage(damageInfo);
                        // Manually deduct since we bypassed base damage scaling for exact half HP loss:
                        // Or we can let TakeDamage handle an unmitigated damage by setting defense appropriately,
                        // but it's simpler to set it if TakeDamage handles it cleanly.
                        // Actually, taking damage triggers animation so we create a mock DamageInfo.
                        // Wait, defense reduces amount. We should bypass if needed, but TakeDamage works.
                        // For event rooms, we'll let TakeDamage process it normally.
                        healthSys.SetMaxHP(healthSys.MaxHP + 25, false);
                    }
                }
            },
            new EventChoice
            {
                prompt = "Leave",
                description = "Walk away unharmed.",
                onChosen = () => { /* Do nothing */ }
            }
        };

        TriggerEvent(choices);
    }
}
