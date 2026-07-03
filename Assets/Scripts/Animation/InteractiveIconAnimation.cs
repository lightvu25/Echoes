using UnityEngine;
using DG.Tweening;

public class InteractiveIconAnimation : MonoBehaviour
{
    [Header("Cài đặt Animation")]
    public float fadeDuration = 0.3f;
    public float moveDistance = 0.4f;

    private SpriteRenderer sr;
    private float originalY;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "UI";
            sr.sortingOrder = 100;
            originalY = transform.localPosition.y;
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }
        gameObject.SetActive(false);
    }

    public void ShowIcon()
    {
        gameObject.SetActive(true);
        sr.DOKill();
        transform.DOKill();
        
        transform.localPosition = new Vector3(transform.localPosition.x, originalY, transform.localPosition.z);

        Sequence enterSequence = DOTween.Sequence();
        enterSequence.Insert(0f, transform.DOLocalMoveY(originalY + moveDistance, fadeDuration).SetEase(Ease.OutBack));
        enterSequence.Insert(0f, sr.DOFade(1f, fadeDuration));
    }

    public void HideIcon(GameObject objectToPool = null)
    {
        sr.DOKill();
        transform.DOKill();

        Sequence exitSequence = DOTween.Sequence();
        exitSequence.Insert(0f, transform.DOLocalMoveY(originalY, fadeDuration).SetEase(Ease.InBack));
        exitSequence.Insert(0f, sr.DOFade(0f, fadeDuration));
        exitSequence.SetLink(gameObject);

        exitSequence.OnComplete(() => {
            ObjectPoolManager.ReturnObjectToPool(objectToPool != null ? objectToPool : gameObject);
        });
    }
}