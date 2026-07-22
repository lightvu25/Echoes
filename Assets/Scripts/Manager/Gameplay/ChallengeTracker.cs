using UnityEngine;
using System;

public class ChallengeTracker : MonoBehaviour
{
    private int _noHitKills = 0;
    private bool _failedNoHit = false;

    private void Start()
    {
        if (PlayerEventBus.Instance != null)
        {
            PlayerEventBus.Instance.OnEnemyKilled += HandleEnemyKilled;
            PlayerEventBus.Instance.OnBeforeDamageTaken += HandleBeforeDamageTaken;
            PlayerEventBus.Instance.OnRoomCleared += HandleRoomCleared;
        }
        
        // Reset the tracker at the start of the scene/level
        _failedNoHit = false;
        _noHitKills = 0;
        
        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentLevelNoHitKills = 0;
        }
    }

    private void OnDestroy()
    {
        if (PlayerEventBus.Instance != null)
        {
            PlayerEventBus.Instance.OnEnemyKilled -= HandleEnemyKilled;
            PlayerEventBus.Instance.OnBeforeDamageTaken -= HandleBeforeDamageTaken;
            PlayerEventBus.Instance.OnRoomCleared -= HandleRoomCleared;
        }
    }

    private void HandleEnemyKilled()
    {
        if (_failedNoHit) return;
        
        _noHitKills++;
        
        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentLevelNoHitKills = _noHitKills;
        }
    }

    private void HandleBeforeDamageTaken(ref int damageAmount, ref DamageInfo info)
    {
        if (_failedNoHit) return;

        if (damageAmount > 0)
        {
            _failedNoHit = true;
            _noHitKills = 0;
            
            if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
            {
                GameSession.Instance.currentRun.currentLevelNoHitKills = 0;
            }
            
            Debug.Log("[RunChallengeTracker] Player took damage. No-hit challenge failed for this level.");
        }
    }

    private void HandleRoomCleared()
    {
        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentLevelNoHitKills = _failedNoHit ? 0 : _noHitKills;
            GameSession.Instance.SaveCurrentRun();
        }
    }
}
