using UnityEngine;

public class MenuInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("The UI Panel this object should open when interacted with.")]
    [SerializeField] private UIPanelType targetPanel;

    public void Interact()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPanel(targetPanel);
        }
        else
        {
            Debug.LogWarning("UIManager Instance is missing in the scene!");
        }
    }
}
