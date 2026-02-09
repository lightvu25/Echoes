using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        // Fallback: check children if not on this object
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshPro>();
        }
        if (textMesh == null)
        {
            Debug.LogError($"[DamagePopup] No TextMeshPro found on {gameObject.name} or its children!");
        }
    }

    public void Setup(int damageAmount)
    {
        textMesh.SetText(damageAmount.ToString());
        textColor = textMesh.color;
        disappearTimer = 1f;
        
        // Randomize movement slightly
        moveVector = new Vector3(Random.Range(-1f, 1f), 3f, 0f) * 5f;
    }

    private void Update()
    {
        float moveYSpeed = 3f;
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        if (disappearTimer > 0.5f)
        {
            // First half of lifetime: scale up
            float increaseScaleAmount = 1f;
            transform.localScale += Vector3.one * increaseScaleAmount * Time.deltaTime;
        }
        else
        {
            // Second half: scale down
            float decreaseScaleAmount = 1f;
            transform.localScale -= Vector3.one * decreaseScaleAmount * Time.deltaTime;
        }

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // Start fading out
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
