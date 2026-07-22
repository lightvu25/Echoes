using UnityEngine;
using TMPro;

public class ShopRerollButton : ShopInteractableBase
{
    [SerializeField] private ShopRoomManager roomManager;
    [Header("Price Popup")]
    [SerializeField] private GameObject priceTextPrefab;
    [SerializeField] private Vector3 priceTextOffset = new Vector3(0f, -1.25f, 0f);
    private TextMeshPro costText;
    private Coroutine popupCoroutine;
    [SerializeField] private AudioClip rerollSFX;
    [SerializeField] private AudioClip errorSFX;

    [SerializeField] private int baseCost = 10;
    [SerializeField] private int costIncrease = 10;

    private int currentCost;

    private void Start()
    {
        currentCost = baseCost;
        
        if (costText == null && priceTextPrefab != null)
        {
            GameObject obj = Instantiate(priceTextPrefab, transform);
            obj.transform.localPosition = priceTextOffset;
            costText = obj.GetComponent<TextMeshPro>();
            if (costText == null) costText = obj.GetComponentInChildren<TextMeshPro>();
        }

        if (costText != null) 
        {
            costText.sortingLayerID = SortingLayer.NameToID("UI");
            costText.sortingOrder = 10;
            costText.gameObject.SetActive(false);
        }

        UpdateText();
    }

    protected override void DoInteract()
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.SpendGold(currentCost))
        {
            PlayClip(rerollSFX);
            if (roomManager != null) roomManager.RerollShop();
            currentCost += costIncrease;
            UpdateText();
        }
        else
        {
            PlayClip(errorSFX);
            if (costText != null) StartCoroutine(FlashTextColor(costText, Color.red, 0.2f));
        }
    }

    private void UpdateText()
    {
        if (costText != null) costText.text = currentCost.ToString();
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        
        if (other.CompareTag("Player") && costText != null)
        {
            if (popupCoroutine != null) StopCoroutine(popupCoroutine);
            popupCoroutine = StartCoroutine(AnimatePopup(true));
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
        if (other.CompareTag("Player") && costText != null)
        {
            if (popupCoroutine != null) StopCoroutine(popupCoroutine);
            
            if (gameObject.activeInHierarchy)
            {
                popupCoroutine = StartCoroutine(AnimatePopup(false));
            }
        }
    }

    private System.Collections.IEnumerator AnimatePopup(bool show)
    {
        if (show)
        {
            costText.gameObject.SetActive(true);
            costText.transform.localScale = Vector3.zero;
        }

        Vector3 targetScale = show ? Vector3.one : Vector3.zero;
        float speed = 10f;

        while (Vector3.Distance(costText.transform.localScale, targetScale) > 0.01f)
        {
            costText.transform.localScale = Vector3.Lerp(costText.transform.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }

        costText.transform.localScale = targetScale;
        
        if (!show)
        {
            costText.gameObject.SetActive(false);
        }
    }
}
