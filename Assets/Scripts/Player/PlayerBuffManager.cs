using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HealthSystem), typeof(PlayerMovement))]
public class PlayerBuffManager : MonoBehaviour
{
    private HealthSystem healthSystem;
    private PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;

    private Coroutine adrenalineCoroutine;
    private Coroutine voidAegisCoroutine;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        playerMovement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // Assumes SpriteRenderer is on player or child
    }

    public void ActivateAdrenaline(float duration, float speedMultiplier)
    {
        if (adrenalineCoroutine != null)
        {
            StopCoroutine(adrenalineCoroutine);
        }
        adrenalineCoroutine = StartCoroutine(AdrenalineRoutine(duration, speedMultiplier));
    }

    private IEnumerator AdrenalineRoutine(float duration, float speedMultiplier)
    {
        if (playerMovement != null) playerMovement.SetBuffSpeedMultiplier(speedMultiplier);

        // Optionally hook into PlayerAttack to boost attack speed if supported

        yield return new WaitForSeconds(duration);

        if (playerMovement != null) playerMovement.SetBuffSpeedMultiplier(1f);
        adrenalineCoroutine = null;
    }

    public void ActivateVoidAegis(float duration)
    {
        if (voidAegisCoroutine != null)
        {
            StopCoroutine(voidAegisCoroutine);
            // Ensure previous visual reset if needed, though we just override it
        }
        voidAegisCoroutine = StartCoroutine(VoidAegisRoutine(duration));
    }

    private IEnumerator VoidAegisRoutine(float duration)
    {
        if (healthSystem != null) healthSystem.SetInvincible(true);

        Color originalColor = Color.white;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
        }

        yield return new WaitForSeconds(duration);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (healthSystem != null) healthSystem.SetInvincible(false);
        voidAegisCoroutine = null;
    }
}
