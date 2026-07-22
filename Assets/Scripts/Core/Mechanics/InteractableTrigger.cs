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
        onInteract?.Invoke();
        var interactables = GetComponents<IInteractable>();
        foreach (var interactable in interactables)
        {
            if (!ReferenceEquals(interactable, this))
            {
                interactable.Interact();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
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
            
            if (currentIcon != null)
            {
                InteractiveIconAnimation anim = currentIcon.GetComponentInChildren<InteractiveIconAnimation>(true);
                if (anim != null) anim.HideIcon(currentIcon);
                else currentIcon.SetActive(false); 
                
                currentIcon = null;
            }

            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(ThicknessProp, 0f); // Tắt viền
                targetSpriteRenderer.SetPropertyBlock(_propBlock);
            }
        }
    }
}