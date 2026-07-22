using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Chest : MonoBehaviour, IInteractable, IFeedbackProvider
{

    [Header("Persistence")]
    public string persistenceID;

    [Header("Loot")]
    [SerializeField] private LootTable lootTable;
    [Tooltip("Chest tier restricts non-consumable drops below this tier.")]
    [Range(1, 3)] [SerializeField] private int chestTier = 1;

    [Header("Physics Burst")]
    [SerializeField] private float popForceMin = 4f;
    [SerializeField] private float popForceMax = 9f;

    [Header("Animation")]
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private string openTrigger = "Open";

    [Header("Feedback")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);


    private bool isOpened;

    private void Start()
    {
        if (!string.IsNullOrEmpty(persistenceID) && GameDataManager.Instance != null)
        {
            if (GameDataManager.Instance.HasPersistenceKey(persistenceID))
            {
                isOpened = true;
                if (chestAnimator != null && !string.IsNullOrEmpty(openTrigger))
                    chestAnimator.SetTrigger(openTrigger);
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  IFeedbackProvider                                                   //
    // ------------------------------------------------------------------ //

    public Vector3 PromptOffset => promptOffset;

    // ------------------------------------------------------------------ //
    //  IInteractable                                                       //
    // ------------------------------------------------------------------ //

    public void Interact()
    {
        if (isOpened) return;
        isOpened = true;

        if (!string.IsNullOrEmpty(persistenceID) && GameDataManager.Instance != null)
            GameDataManager.Instance.AddPersistenceKey(persistenceID);

        StartCoroutine(OpenSequence());
    }

    // ------------------------------------------------------------------ //
    //  Private                                                             //
    // ------------------------------------------------------------------ //

    private IEnumerator OpenSequence()
    {
        if (chestAnimator != null && !string.IsNullOrEmpty(openTrigger))
            chestAnimator.SetTrigger(openTrigger);

        yield return null;

        if (lootTable == null)
        {
            Debug.LogWarning("[Chest] No LootTable assigned.", this);
            yield break;
        }

        LootBonuses bonuses = LootBonusResolver.Resolve();
        List<DropResult> drops = lootTable.GetDrops(1f, chestTier, bonuses);
        SpawnDrops(drops);
    }

    private void SpawnDrops(List<DropResult> drops)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        foreach (DropResult result in drops)
        {
            if (result.type == LootItemType.Currency)
            {
                GameObject obj = ObjectPoolManager.SpawnObject(result.prefab, origin, Quaternion.identity, ObjectPoolManager.PoolType.Loot);
                if (obj.TryGetComponent(out ResourceDrop resourceDrop))
                {
                    float x = Random.Range(-1f, 1f);
                    float y = Random.Range(popForceMin, popForceMax);
                    resourceDrop.Initialize(result.totalAmount, new Vector2(x, y));
                }
                else
                {
                    ApplyBurst(obj);
                }
            }
            else
            {
                for (int i = 0; i < result.totalAmount; i++)
                {
                    GameObject obj = ObjectPoolManager.SpawnObject(result.prefab, origin, Quaternion.identity, ObjectPoolManager.PoolType.Loot);
                    ApplyBurst(obj);
                }
            }
        }
    }

    private void ApplyBurst(GameObject obj)
    {
        if (!obj.TryGetComponent<Rigidbody2D>(out var rb)) return;
        float x = Random.Range(-1f, 1f);
        float y = Random.Range(popForceMin, popForceMax);
        rb.AddForce(new Vector2(x, y), ForceMode2D.Impulse);
    }
}
