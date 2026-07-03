using System;
using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioSource stepAudioSource;

    private EnemyBrain enemyBrain;

    private void Awake()
    {
        enemyBrain = GetComponentInParent<EnemyBrain>();
    }

    private void Start()
    {
        if (enemyBrain != null)
        {
            enemyBrain.OnAttack += EnemyBrain_OnAttack;
        }
    }

    private void Update()
    {
        if (enemyBrain != null && (enemyBrain.CurrentState == EnemyBrain.State.Patrol || enemyBrain.CurrentState == EnemyBrain.State.Chase))
        {
            if (!stepAudioSource.isPlaying)
                stepAudioSource.Play();
        }
        else
        {
            if (stepAudioSource.isPlaying)
                stepAudioSource.Stop();
        }
    }

    private void EnemyBrain_OnAttack(object sender, EventArgs e)
    {
        if (attackAudioSource != null)
            attackAudioSource.Play();
    }

    private void OnDestroy()
    {
        if (enemyBrain != null)
        {
            enemyBrain.OnAttack -= EnemyBrain_OnAttack;
        }
    }
}