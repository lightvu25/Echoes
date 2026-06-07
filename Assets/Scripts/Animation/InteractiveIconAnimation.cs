using UnityEngine;
using DG.Tweening;

public class InteractiveIconAnimation : MonoBehaviour
{
    [Header("Cài đặt Animation")]
    public float fadeDuration = 0.3f;
    public float moveDistance = 0.4f;

    private SpriteRenderer sr;
    private float originalY;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        
        originalY = transform.localPosition.y;

        Color c = sr.color;
        c.a = 0f;
        sr.color = c;
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            gameObject.SetActive(true);
            
            sr.DOKill();
            transform.DOKill();
            transform.localPosition = new Vector3(transform.localPosition.x, originalY, transform.localPosition.z);

            Sequence enterSequence = DOTween.Sequence();
            
            enterSequence.Insert(0f, transform.DOLocalMoveY(originalY + moveDistance, fadeDuration).SetEase(Ease.OutBack));
            enterSequence.Insert(0f, sr.DOFade(1f, fadeDuration));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            sr.DOKill();
            transform.DOKill();

            Sequence exitSequence = DOTween.Sequence();

            exitSequence.Insert(0f, transform.DOLocalMoveY(originalY, fadeDuration).SetEase(Ease.InBack));
            exitSequence.Insert(0f, sr.DOFade(0f, fadeDuration));

            exitSequence.OnComplete(() => gameObject.SetActive(false));
        }
    }
}
