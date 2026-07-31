using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 10;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color criticalColor = new Color(1f, 0.8f, 0.2f, 1f); // Yellow
    [SerializeField] private Color trueDamageColor = new Color(0.2f, 0.8f, 1f, 1f); // Blue

    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>();

        if (textMesh == null)
        {
            Debug.LogError($"[DamagePopup] No TextMeshPro found on {gameObject.name} or its children!");
            return;
        }

        textMesh.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        textMesh.sortingOrder   = sortingOrder;
    }

    public void Setup(int damageAmount, bool isCritical = false, bool isTrueDamage = false)
    {
        if (textMesh == null) return;
        transform.localScale = isCritical ? Vector3.one * 1.5f : Vector3.one;

        textMesh.SetText(damageAmount.ToString());
        
        if (isTrueDamage)
        {
            textColor = trueDamageColor;
        }
        else if (isCritical)
        {
            textColor = criticalColor;
        }
        else
        {
            textColor = normalColor;
        }
        
        textMesh.color = textColor;

        disappearTimer = 1f;
        moveVector     = new Vector3(Random.Range(-1f, 1f), 3f, 0f) * 5f;
    }

    private void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        if (disappearTimer > 0.5f)
        {
            transform.localScale += Vector3.one * 1f * Time.deltaTime;
        }
        else
        {
            transform.localScale -= Vector3.one * 1f * Time.deltaTime;
        }

        disappearTimer -= Time.deltaTime;

        if (disappearTimer < 0)
        {
            textColor.a -= 3f * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a <= 0f)
            {
                ObjectPoolManager.ReturnObjectToPool(gameObject);
            }
        }
    }
}
