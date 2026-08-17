using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HealthSystem), typeof(PlayerMovement))]
public class PlayerBuffManager : MonoBehaviour
{
    private HealthSystem healthSystem;
    private PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;
    private PlayerRuntimeModifiers runtimeModifiers;

    private Coroutine adrenalineCoroutine;
    private Coroutine voidAegisCoroutine;
    private Color spriteBaseColor = Color.white;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        playerMovement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // Assumes SpriteRenderer is on player or child
        if (spriteRenderer != null) spriteBaseColor = spriteRenderer.color;
        runtimeModifiers = GetComponent<PlayerRuntimeModifiers>();
        if (runtimeModifiers == null) runtimeModifiers = gameObject.AddComponent<PlayerRuntimeModifiers>();
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
        runtimeModifiers?.SetMovementSpeed(this, speedMultiplier);
        runtimeModifiers?.SetAttackSpeed(this, speedMultiplier);

        yield return new WaitForSeconds(duration);

        runtimeModifiers?.RemoveSource(this);
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
        if (healthSystem != null) healthSystem.SetInvincible(this, true);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(spriteBaseColor.r, spriteBaseColor.g, spriteBaseColor.b, 0.5f);
        }

        yield return new WaitForSeconds(duration);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = spriteBaseColor;
        }

        if (healthSystem != null) healthSystem.SetInvincible(this, false);
        voidAegisCoroutine = null;
    }

    private void OnDisable()
    {
        runtimeModifiers?.RemoveSource(this);
        if (healthSystem != null) healthSystem.SetInvincible(this, false);
        if (spriteRenderer != null)
            spriteRenderer.color = spriteBaseColor;
    }
}
