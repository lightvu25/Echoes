using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public abstract class ShopInteractableBase : MonoBehaviour, IInteractable
{
    private static readonly List<ShopInteractableBase> activeInRange = new();
    private Transform playerRef;

    public void Interact()
    {
        if (!IsClosestActive()) return;
        DoInteract();
    }

    protected abstract void DoInteract();

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerRef = other.transform;
        if (!activeInRange.Contains(this)) activeInRange.Add(this);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        activeInRange.Remove(this);
    }

    private void OnDisable()
    {
        activeInRange.Remove(this);
    }

    // Player only interacts with the nearest overlapping shop object.
    private bool IsClosestActive()
    {
        if (playerRef == null || activeInRange.Count <= 1) return true;

        float myDistance = Vector2.Distance(transform.position, playerRef.position);
        foreach (ShopInteractableBase other in activeInRange)
        {
            if (other == this) continue;
            if (Vector2.Distance(other.transform.position, playerRef.position) < myDistance)
                return false;
        }
        return true;
    }

    protected void PlayClip(AudioClip clip)
    {
        if (clip == null || Camera.main == null) return;
        float volume = SoundManager.Instance != null ? SoundManager.Instance.GetSoundVolumeNormalized() : 1f;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
    }

    private Dictionary<TextMeshPro, Color> originalTextColors = new Dictionary<TextMeshPro, Color>();
    private Dictionary<SpriteRenderer, Color> originalSpriteColors = new Dictionary<SpriteRenderer, Color>();

    protected IEnumerator FlashTextColor(TextMeshPro text, Color flashColor, float duration)
    {
        if (!originalTextColors.ContainsKey(text))
            originalTextColors[text] = text.color;

        text.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (text != null) text.color = originalTextColors[text];
    }

    protected IEnumerator FlashSpriteColor(SpriteRenderer sr, Color flashColor, float duration)
    {
        if (!originalSpriteColors.ContainsKey(sr))
            originalSpriteColors[sr] = sr.color;

        sr.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (sr != null) sr.color = originalSpriteColors[sr];
    }
}
