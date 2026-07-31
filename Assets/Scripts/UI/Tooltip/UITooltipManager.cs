using UnityEngine;
using TMPro;

/// <summary>
/// Universal UI Tooltip Manager.
/// Follows the mouse cursor, clamps to screen boundaries, and populates data decoupled from any specific UI slot.
/// Place this script on a Tooltip GameObject inside a high-order Canvas.
/// </summary>
public class UITooltipManager : MonoBehaviour
{
    public static UITooltipManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private UIPanelAnimator panelAnimator;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private int frameShown = -1;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (panelAnimator == null) panelAnimator = GetComponent<UIPanelAnimator>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Instantly hide
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        // If the tooltip is visible and the user clicks anywhere, hide it.
        // We check frameShown to prevent hiding it in the exact same frame it was shown
        // (in case the UI event system processes the click before or after this Update).
        if (IsTooltipVisible() && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)))
        {
            if (Time.frameCount != frameShown)
            {
                HideTooltip();
            }
        }
    }

    private bool IsTooltipVisible()
    {
        if (panelAnimator != null) return panelAnimator.IsShowing;
        return canvasGroup != null && canvasGroup.alpha > 0f;
    }

    /// <summary>
    /// Populates the tooltip UI and fades it in.
    /// </summary>
    /// <param name="data">The item data to display.</param>
    public void ShowTooltip(ItemBaseData data)
    {
        if (data == null) return;

        // Populate Texts
        if (titleText != null) titleText.text = data.itemName;
        if (categoryText != null) categoryText.text = data.Category.ToString();
        if (descriptionText != null) 
        {
            descriptionText.text = data.description;
            if (data is RelicData relic && !string.IsNullOrEmpty(relic.FactionBonus))
            {
                descriptionText.text += $"\n\n<color=#FFD700><b>{relic.FactionBonus}</b></color>";
            }
        }
        // Populate Icon
        if (iconImage != null)
        {
            if (data.itemIcon != null)
            {
                iconImage.sprite = data.itemIcon;
                iconImage.SetNativeSize();
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        // Force an immediate layout update so the RectTransform size is correct before positioning
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        if (panelAnimator != null)
        {
            panelAnimator.Show();
            // Critical override: UIPanelAnimator forces raycasts ON, but Tooltips must NEVER block raycasts!
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            // Fallback show
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false; 
            canvasGroup.interactable = false;
        }

        frameShown = Time.frameCount;
    }

    /// <summary>
    /// Instantly or smoothly hides the tooltip.
    /// </summary>
    public void HideTooltip()
    {
        if (panelAnimator != null && panelAnimator.IsShowing)
        {
            panelAnimator.Hide();
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }
}
