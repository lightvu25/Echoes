using UnityEngine;

public class EchoVFXColorSync : MonoBehaviour
{
    private SpriteRenderer[] spriteRenderers;
    private TrailRenderer[] trailRenderers;
    private ParticleSystem[] particleSystems;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        if (PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.ActiveEcho != null)
        {
            Color echoColor = PlayerInventoryCore.Instance.ActiveEcho.trailColor;

            // Color all SpriteRenderers
            foreach (var sr in spriteRenderers)
            {
                if (sr != null) sr.color = echoColor;
            }

            // Color all TrailRenderers
            foreach (var tr in trailRenderers)
            {
                if (tr != null)
                {
                    tr.startColor = echoColor;
                    tr.endColor = new Color(echoColor.r, echoColor.g, echoColor.b, 0f);
                }
            }

            // Color all ParticleSystems
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = echoColor;
                }
            }
        }
    }
}
