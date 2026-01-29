using System;
using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioSource stepAudioSource;

    private EnemyInteract enemyInteract;
    private EnemyMovement enemyMovement;

    private void Awake()
    {
        enemyInteract = GetComponent<EnemyInteract>();
        enemyMovement = GetComponent<EnemyMovement>();
    }

    private void Start()
    {
        enemyInteract.OnAttack += EnemyInteract_OnAttack;
        // Có thể thêm event OnStep nếu muốn
    }

    private void EnemyInteract_OnAttack(object sender, EventArgs e)
    {
        if (attackAudioSource != null)
            attackAudioSource.Play();
    }
}