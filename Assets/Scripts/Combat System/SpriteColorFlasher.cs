using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteColorFlasher : MonoBehaviour
{
    private Coroutine flashRoutine;
    private Color baseColor = Color.white;
    private bool hasBaseColor = false;

    public void FlashColor(SpriteRenderer spriteRend, float duration, Color color) {
        if (!hasBaseColor)
        {
            baseColor = spriteRend.color;
            hasBaseColor = true;
        }
        
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(DoColorFlash(spriteRend, duration, color));
    }

    private IEnumerator DoColorFlash(SpriteRenderer spriteRend, float duration, Color newColor) {
        spriteRend.color = newColor;
        yield return new WaitForSeconds(duration);
        
        if(spriteRend != null) {
            EchoStatusReceiver status = spriteRend.GetComponentInParent<EchoStatusReceiver>();
            if (status != null)
            {
                spriteRend.color = status.CurrentTargetColor;
            }
            else
            {
                spriteRend.color = baseColor;
            }
        }
    }
}
