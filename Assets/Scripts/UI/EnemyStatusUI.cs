using UnityEngine;
using UnityEngine.UI;

public class EnemyStatusUI : MonoBehaviour
{
    [Tooltip("Reference to the status receiver on this enemy.")]
    [SerializeField] private EchoStatusReceiver statusReceiver;
    
    [Header("Status Icon GameObjects")]
    [SerializeField] private GameObject slowIcon;
    [SerializeField] private GameObject freezeIcon;
    [SerializeField] private GameObject burnIcon;
    [SerializeField] private GameObject silenceIcon;

    private void Awake()
    {
        // Auto-fetch if not assigned
        if (statusReceiver == null)
        {
            statusReceiver = GetComponentInParent<EchoStatusReceiver>();
        }
    }

    private void Update()
    {
        if (statusReceiver == null) return;

        // Toggle icons based on active statuses
        if (slowIcon != null) slowIcon.SetActive(statusReceiver.IsSlowed);
        if (freezeIcon != null) freezeIcon.SetActive(statusReceiver.IsFrozen);
        if (burnIcon != null) burnIcon.SetActive(statusReceiver.IsBurning);
        if (silenceIcon != null) silenceIcon.SetActive(statusReceiver.IsSilenced);
    }
}
