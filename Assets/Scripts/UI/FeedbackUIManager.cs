using UnityEngine;
using DG.Tweening;
using TMPro;

public class FeedbackUIManager : MonoBehaviour
{
    public static FeedbackUIManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject interactPromptPrefab;
    [SerializeField] private GameObject screenTextPrefab;

    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float floatOffset = 0.2f;

    private GameObject currentInteractPrompt;
    private IFeedbackProvider currentInteractProvider;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowInteractPrompt(IFeedbackProvider provider)
    {
        if (provider == null || interactPromptPrefab == null) return;
        if (currentInteractProvider == provider && currentInteractPrompt != null) return;

        HideInteractPrompt();
        
        currentInteractPrompt = ObjectPoolManager.SpawnObject(
            interactPromptPrefab, 
            provider.transform.position + provider.PromptOffset, 
            Quaternion.identity, 
            ObjectPoolManager.PoolType.UI
        );
        
        currentInteractProvider = provider;
        AnimatePrompt(currentInteractPrompt, provider);
    }

    public void HideInteractPrompt()
    {
        if (currentInteractPrompt != null)
        {
            GameObject promptToHide = currentInteractPrompt;
            
            currentInteractPrompt = null;
            currentInteractProvider = null;

            promptToHide.transform.DOKill(); 
            
            promptToHide.transform.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    if (promptToHide != null && promptToHide.activeSelf)
                    {
                        ObjectPoolManager.ReturnObjectToPool(promptToHide);
                    }
                });
        }
    }

    private void AnimatePrompt(GameObject prompt, IFeedbackProvider provider)
    {
        if (prompt == null) return;

        prompt.SetActive(true);
        prompt.transform.SetParent(provider.transform);
        prompt.transform.localPosition = provider.PromptOffset;
        
        Vector3 pScale = provider.transform.lossyScale;
        Vector3 targetScale = new Vector3(
            pScale.x != 0 ? 1f / Mathf.Abs(pScale.x) : 1f,
            pScale.y != 0 ? 1f / Mathf.Abs(pScale.y) : 1f,
            pScale.z != 0 ? 1f / Mathf.Abs(pScale.z) : 1f
        );

        prompt.transform.DOKill();
        prompt.transform.localScale = Vector3.zero;
        prompt.transform.DOScale(targetScale, animationDuration).SetEase(Ease.OutBack);
        
        float startY = provider.PromptOffset.y;
        prompt.transform.DOLocalMoveY(startY + floatOffset, 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    public void ShowScreenText(string message)
    {
        if (screenTextPrefab != null)
        {
            GameObject txtObj = ObjectPoolManager.SpawnObject(
                screenTextPrefab, 
                transform.position, 
                Quaternion.identity, 
                ObjectPoolManager.PoolType.UI
            );
            
            TMP_Text tmp = txtObj.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = message;
                tmp.alpha = 1f;
                txtObj.transform.localScale = Vector3.zero;
                txtObj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnComplete(() => {
                    tmp.DOFade(0f, 1f).SetDelay(1.5f).OnComplete(() => ObjectPoolManager.ReturnObjectToPool(txtObj));
                });
            }
        }
    }
}