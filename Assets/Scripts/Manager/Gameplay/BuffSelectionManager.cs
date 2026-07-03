using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BuffSelectionManager : MonoBehaviour
{
    public static BuffSelectionManager Instance { get; private set; }

    [SerializeField] private List<BuffData> allBuffPool = new List<BuffData>();

    private List<string> bannedBuffs = new List<string>();
    private BuffData[] currentOffer = new BuffData[3];

    public event Action<BuffData[]> OnOfferReady;
    public event Action<BuffData> OnBuffSelected;
    public event Action<string> OnBuffBanished;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RollNewOffer()
    {
        List<BuffData> availableBuffs = new List<BuffData>();
        
        foreach (var buff in allBuffPool)
        {
            if (buff != null && !bannedBuffs.Contains(buff.buffID))
            {
                availableBuffs.Add(buff);
            }
        }

        // Try to pick up to 3 unique buffs
        int offerCount = Mathf.Min(3, availableBuffs.Count);
        currentOffer = new BuffData[offerCount];

        for (int i = 0; i < offerCount; i++)
        {
            int randomIndex = Random.Range(0, availableBuffs.Count);
            currentOffer[i] = availableBuffs[randomIndex];
            availableBuffs.RemoveAt(randomIndex);
        }

        OnOfferReady?.Invoke(currentOffer);
    }

    public void SelectBuff(int index)
    {
        if (index >= 0 && index < currentOffer.Length && currentOffer[index] != null)
        {
            BuffData selected = currentOffer[index];
            OnBuffSelected?.Invoke(selected);
        }
    }

    public void BanishBuff(int index)
    {
        if (index >= 0 && index < currentOffer.Length && currentOffer[index] != null)
        {
            BuffData toBanish = currentOffer[index];

            if (PlayerStats.Instance != null && PlayerStats.Instance.CurrentAstralShards >= toBanish.banishCost)
            {
                PlayerStats.Instance.SpendAstralShards(toBanish.banishCost);

                if (!string.IsNullOrEmpty(toBanish.buffID) && !bannedBuffs.Contains(toBanish.buffID))
                {
                    bannedBuffs.Add(toBanish.buffID);
                }

                OnBuffBanished?.Invoke(toBanish.buffID);

                RollNewOffer();
            }
            else
            {
                Debug.LogWarning("BuffSelectionManager: Not enough Astral Shards to banish!");
            }
        }
    }
}
