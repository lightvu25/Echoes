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

        // Di chuyển 0.4 y trong 0.5s
        mySequence.Insert(0, transform.DOLocalMoveY(0.4f, 0.3f).SetEase(Ease.OutBack).SetRelative(true));

        // Fade In
        mySequence.Insert(0f, sr.DOFade(1f, 0.15f));

        // Fade Out
        mySequence.Append(sr.DOFade(0f, 0.15f));

        mySequence.OnComplete(() => {
            Destroy(gameObject);
        });
    }
}
