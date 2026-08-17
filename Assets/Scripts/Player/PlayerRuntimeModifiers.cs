using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerRuntimeModifiers : MonoBehaviour
{
    private readonly Dictionary<object, float> movementSpeed = new Dictionary<object, float>();
    private readonly Dictionary<object, float> attackSpeed = new Dictionary<object, float>();
    private PlayerMovement playerMovement;
    private PlayerAttack playerAttack;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerAttack = GetComponent<PlayerAttack>();
        Apply();
    }

    private void Update()
    {
        if (HasDestroyedSource(movementSpeed) || HasDestroyedSource(attackSpeed)) Apply();
    }

    public void SetMovementSpeed(object source, float multiplier)
    {
        SetModifier(movementSpeed, source, multiplier);
        Apply();
    }

    public void SetAttackSpeed(object source, float multiplier)
    {
        SetModifier(attackSpeed, source, multiplier);
        Apply();
    }

    public void RemoveSource(object source)
    {
        if (source == null) return;
        movementSpeed.Remove(source);
        attackSpeed.Remove(source);
        Apply();
    }

    private static void SetModifier(Dictionary<object, float> modifiers, object source, float multiplier)
    {
        if (source == null) return;
        if (Mathf.Approximately(multiplier, 1f)) modifiers.Remove(source);
        else modifiers[source] = Mathf.Max(0f, multiplier);
    }

    private void Apply()
    {
        PruneDestroyedSources(movementSpeed);
        PruneDestroyedSources(attackSpeed);

        float moveProduct = 1f;
        foreach (float multiplier in movementSpeed.Values) moveProduct *= multiplier;
        if (playerMovement != null) playerMovement.SetBuffSpeedMultiplier(moveProduct);

        float attackProduct = 1f;
        foreach (float multiplier in attackSpeed.Values) attackProduct *= multiplier;
        if (playerAttack != null) playerAttack.SetAttackSpeedMultiplier(attackProduct);
    }

    private static void PruneDestroyedSources(Dictionary<object, float> modifiers)
    {
        if (modifiers.Count == 0) return;
        List<object> stale = null;
        foreach (object source in modifiers.Keys)
        {
            if (source is Object unityObject && unityObject == null)
            {
                stale ??= new List<object>();
                stale.Add(source);
            }
        }

        if (stale == null) return;
        foreach (object source in stale) modifiers.Remove(source);
    }

    private static bool HasDestroyedSource(Dictionary<object, float> modifiers)
    {
        foreach (object source in modifiers.Keys)
        {
            if (source is Object unityObject && unityObject == null) return true;
        }
        return false;
    }
}
