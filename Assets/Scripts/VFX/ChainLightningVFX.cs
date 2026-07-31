using UnityEngine;
using System.Collections;

public class ChainLightningVFX : MonoBehaviour
{
    private LineRenderer line;
    private AudioSource audioSource;
    private float lifetime = 0.15f;
    
    [SerializeField] private ParticleSystem startImpactVFX;
    [SerializeField] private ParticleSystem endImpactVFX;
    
    private Vector2 startPos;
    private Vector2 endPos;
    private int segments = 8;
    
    // Width animation curve: 0.12 down to 0
    private AnimationCurve widthCurve = new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(1f, 0f));
    private float timer = 0f;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (line != null)
        {
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
        }
        
        Transform startTransform = transform.Find("StartImpactVFX");
        if (startTransform != null) startImpactVFX = startTransform.GetComponent<ParticleSystem>();
        
        Transform endTransform = transform.Find("EndImpactVFX");
        if (endTransform != null) endImpactVFX = endTransform.GetComponent<ParticleSystem>();
    }

    public void Initialize(Vector2 start, Vector2 end)
    {
        startPos = start;
        endPos = end;
        
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.Play();
        }

        if (startImpactVFX != null)
        {
            startImpactVFX.transform.position = start;
            startImpactVFX.Play();
        }
        
        if (endImpactVFX != null)
        {
            endImpactVFX.transform.position = end;
            endImpactVFX.Play();
        }

        if (line != null)
        {
            line.positionCount = segments;
            UpdateLightning();
            InvokeRepeating(nameof(UpdateLightning), 0.02f, 0.02f);
        }

        StartCoroutine(AnimateAndDestroy());
    }

    private void UpdateLightning()
    {
        if (line == null) return;
        
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            if (i > 0 && i < segments - 1)
            {
                Vector2 offset = Random.insideUnitCircle * 0.2f;
                pos += (Vector3)offset;
            }
            
            float pixelsPerUnit = 64f;
            pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
            pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;
            
            line.SetPosition(i, pos);
        }
    }

    private IEnumerator AnimateAndDestroy()
    {
        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            float t = timer / lifetime;
            
            if (line != null)
            {
                line.widthMultiplier = widthCurve.Evaluate(t);
            }
            
            yield return null;
        }

        CancelInvoke(nameof(UpdateLightning));
        
        if (startImpactVFX != null)
        {
            startImpactVFX.transform.parent = null;
            Destroy(startImpactVFX.gameObject, 1f);
        }
        if (endImpactVFX != null)
        {
            endImpactVFX.transform.parent = null;
            Destroy(endImpactVFX.gameObject, 1f);
        }

        Destroy(gameObject);
    }
}
