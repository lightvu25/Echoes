using UnityEngine;

public class DebugAddCurrency : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Press this key in-game to add currency")]
    [SerializeField] private KeyCode debugKey = KeyCode.F2;
    
    [SerializeField] private int goldAmount = 1000;
    [SerializeField] private int shardsAmount = 1000;

    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            GiveCurrency();
        }
    }

    [ContextMenu("Give Currency Now")]
    public void GiveCurrency()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddGold(goldAmount);
            PlayerStats.Instance.AddAstralShards(shardsAmount);
            Debug.Log($"[Debug] Successfully gave {goldAmount} Gold and {shardsAmount} Shards!");
        }
        else
        {
            Debug.LogWarning("[Debug] Could not add currency. PlayerStats.Instance is missing in the scene!");
        }
    }
}
