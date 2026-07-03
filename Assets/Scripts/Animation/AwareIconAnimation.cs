using DG.Tweening;
using UnityEngine;

public class AwareIconAnimation : MonoBehaviour
{
    private Sequence mySequence;

    private void OnEnable()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        Color c = sr.color;
        c.a = 0;
        sr.color = c;

        mySequence?.Kill();
        
        mySequence = DOTween.Sequence();

        mySequence.Insert(0, transform.DOLocalMoveY(0.1f, 0.3f).SetEase(Ease.OutBack).SetRelative(true));
        mySequence.Insert(0f, sr.DOFade(1f, 0.15f));
        mySequence.Append(sr.DOFade(0f, 0.15f));
        mySequence.SetLink(gameObject);

        mySequence.OnComplete(() => {
            if (gameObject.activeInHierarchy)
            {
                ObjectPoolManager.ReturnObjectToPool(gameObject);
            }
        });
    }

    private void OnDisable()
    {
        mySequence?.Kill();
    }
}