using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIAudioHandler : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
    [Header("UI Audio Clips")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    
    [Range(0f, 1f)] 
    public float volume = 1f;
    
    public bool useRandomPitch = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound(hoverSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySound(clickSound);
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlaySound(hoverSound);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(clip, volume, useRandomPitch);
        }
    }
}