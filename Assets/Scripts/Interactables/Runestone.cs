using UnityEngine;

[RequireComponent(typeof(InteractableTrigger))]
public class Runestone : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPanel(UIPanelType.MindGarden);
        }
        else
        {
            Debug.LogWarning("[Runestone] UIManager Instance is missing!");
        }
    }
}
