using System.Collections.Generic;
using UnityEngine;

public class PlayerEchoVFX : MonoBehaviour
{
    [Header("VFX Configuration")]
    [Tooltip("List all the different elements to pre-instantiate VFX for.")]
    public List<EchoData> availableEchoes;

    [Tooltip("Where should the VFX spawn? (If left blank, it spawns directly on the player)")]
    [SerializeField] private Transform vfxSpawnPoint;

    private Dictionary<EchoType, ParticleSystem> instantiatedVFX = new Dictionary<EchoType, ParticleSystem>();
    private EchoType currentActiveType = EchoType.None;

    private void Start()
    {
        // Pre-instantiate all stateVFXPrefab objects
        Transform spawnParent = vfxSpawnPoint != null ? vfxSpawnPoint : transform;

        if (availableEchoes != null)
        {
            foreach (var echo in availableEchoes)
            {
                if (echo != null && echo.stateVFXPrefab != null)
                {
                    if (!instantiatedVFX.ContainsKey(echo.echoType))
                    {
                        ParticleSystem newVfx = Instantiate(echo.stateVFXPrefab, spawnParent);
                        newVfx.transform.localPosition = Vector3.zero;
                        newVfx.transform.localRotation = Quaternion.identity;
                        newVfx.gameObject.SetActive(false);
                        instantiatedVFX.Add(echo.echoType, newVfx);
                    }
                }
            }
        }

        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged += UpdateActiveVFX;
            UpdateActiveVFX(); // Trigger once to set initial state
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged -= UpdateActiveVFX;
        }
    }

    private void UpdateActiveVFX()
    {
        EchoType newType = EchoType.None;
        
        if (PlayerInventoryCore.Instance != null)
        {
            EchoData activeEcho = PlayerInventoryCore.Instance.GetActiveEcho();
            if (activeEcho != null)
            {
                newType = activeEcho.echoType;
            }
        }

        if (newType != currentActiveType)
        {
            // Stop and deactivate old
            if (currentActiveType != EchoType.None && instantiatedVFX.ContainsKey(currentActiveType))
            {
                instantiatedVFX[currentActiveType].Stop();
                instantiatedVFX[currentActiveType].gameObject.SetActive(false);
            }

            currentActiveType = newType;

            // Start and activate new
            if (currentActiveType != EchoType.None && instantiatedVFX.ContainsKey(currentActiveType))
            {
                ParticleSystem activeVFX = instantiatedVFX[currentActiveType];
                activeVFX.gameObject.SetActive(true);
                if (!activeVFX.isPlaying)
                {
                    activeVFX.Play();
                }
            }
        }
    }
}
