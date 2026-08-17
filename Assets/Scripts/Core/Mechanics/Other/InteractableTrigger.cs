using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class InteractableTrigger : MonoBehaviour, IInteractable
{
    [Header("Interact")]
    [SerializeField] private UnityEvent onInteract;
    
    [Header("Icon Settings (Object Pool)")]
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private Vector3 iconOffset = new Vector3(0, 1.5f, 0);
    
    [Header("Highlight Settings")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [Tooltip("Độ dày của viền khi player lại gần (Ví dụ: 0.02)")]
    [SerializeField] private float outlineThickness = 0.02f;
    
    [ColorUsage(true, true)]
    [SerializeField] private Color outlineColor = Color.white;

    private GameObject currentIcon;
    private bool canInteract = false;

    [Header("Behavior")]
    [Tooltip("0 = infinite")]
    public int maxInteractions = 0;
    public bool canInteractWithF = true;
    
    private int currentInteractions = 0;

    private MaterialPropertyBlock _propBlock;
    private static readonly int ThicknessProp = Shader.PropertyToID("_Thickness");
    private static readonly int OutlineColorProp = Shader.PropertyToID("_OutlineColor");

    private void Awake()
    {
        if (targetSpriteRenderer == null) targetSpriteRenderer = GetComponent<SpriteRenderer>();
        
        // Khởi tạo khối thuộc tính
        _propBlock = new MaterialPropertyBlock();
    }



    public void Interact()
    {
        if (!canInteractWithF) return;
        if (maxInteractions > 0 && currentInteractions >= maxInteractions) return;
        
        currentInteractions++;

        onInteract?.Invoke();
        var interactables = GetComponents<IInteractable>();
        foreach (var interactable in interactables)
        {
            if (ReferenceEquals(interactable, this)) continue;

            // Older prefabs may already invoke the attached interactable through
            // the serialized UnityEvent. Do not invoke the same Interact method
            // again through automatic component discovery.
            if (IsInvokedByPersistentEvent(interactable as Object)) continue;

            interactable.Interact();
        }

        if (maxInteractions > 0 && currentInteractions >= maxInteractions)
        {
            HideFeedback();
            canInteract = false;
        }
    }

    private bool IsInvokedByPersistentEvent(Object target)
    {
        if (target == null || onInteract == null) return false;

        for (int i = 0; i < onInteract.GetPersistentEventCount(); i++)
        {
            if (onInteract.GetPersistentTarget(i) == target &&
                onInteract.GetPersistentMethodName(i) == nameof(IInteractable.Interact))
            {
                return true;
            }
        }

        return false;
    }

    public void ForceComplete()
    {
        if (maxInteractions > 0)
        {
            currentInteractions = maxInteractions;
        }
        else
        {
            // If it had infinite interactions but we want to force it closed, we can just set it to a large number or 1
            maxInteractions = 1;
            currentInteractions = 1;
        }
        canInteract = false;
        HideFeedback();
    }

    private void HideFeedback()
    {
        if (currentIcon != null)
        {
            InteractiveIconAnimation anim = currentIcon.GetComponentInChildren<InteractiveIconAnimation>(true);
            if (anim != null) anim.HideIcon(currentIcon);
            else currentIcon.SetActive(false); 
            
            currentIcon = null;
        }

        if (targetSpriteRenderer != null)
        {
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            targetSpriteRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(ThicknessProp, 0f); // Tắt viền
            targetSpriteRenderer.SetPropertyBlock(_propBlock);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (maxInteractions > 0 && currentInteractions >= maxInteractions) return;

        if (collision.CompareTag("Player"))
        {
            canInteract = true;
            
            if (iconPrefab != null && currentIcon == null)
            {
                currentIcon = ObjectPoolManager.SpawnObject(iconPrefab, transform.position + iconOffset, Quaternion.identity, ObjectPoolManager.PoolType.UI);
                
                InteractiveIconAnimation anim = currentIcon.GetComponentInChildren<InteractiveIconAnimation>(true);
                if (anim != null) anim.ShowIcon();
            }

            if (targetSpriteRenderer != null)
            {
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                targetSpriteRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(ThicknessProp, outlineThickness);
                _propBlock.SetColor(OutlineColorProp, outlineColor);
                targetSpriteRenderer.SetPropertyBlock(_propBlock);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canInteract = false;
            HideFeedback();
        }
    }
}
