using DG.Tweening;
using UnityEngine;

public class AwareIconAnimation : MonoBehaviour
{
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        Color c = sr.color;
        c.a = 0;
        sr.color = c;

        Sequence mySequence = DOTween.Sequence();

        mySequence.Insert(0, transform.DOLocalMoveY(0.4f, 0.5f).SetEase(Ease.OutBack).SetRelative(true));

        // Fade in
        mySequence.Insert(0.1f, sr.DOFade(1f, 0.15f));


        mySequence.AppendInterval(0.1f);

        // Fade out
        mySequence.Append(sr.DOFade(0f, 0.15f));

        mySequence.OnComplete(() => {
            Destroy(gameObject);
        });
    }
}