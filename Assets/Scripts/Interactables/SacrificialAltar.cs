using UnityEngine;

[RequireComponent(typeof(InteractableTrigger))]
public class SacrificialAltar : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPanel(UIPanelType.SacrificialFusion);
        }
        else
        {
            Debug.LogWarning("[SacrificialAltar] UIManager Instance is missing!");
        }
    }
}
