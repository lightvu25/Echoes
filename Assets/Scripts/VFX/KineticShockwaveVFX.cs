using UnityEngine;
using System.Collections;

public class KineticShockwaveVFX : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float duration = 0.25f;
    private float maxRadius = 3f;

    public void Initialize(Vector2 center)
    {
        transform.position = center;
        
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 0.9f, 0.5f, 0.8f);
        lineRenderer.endColor = new Color(1f, 0.5f, 0.1f, 0f);
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.sortingLayerName = "VFX";
        lineRenderer.sortingOrder = 90;
        
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        
        lineRenderer.numCornerVertices = 0;
        lineRenderer.numCapVertices = 0;
        
        int segments = 36;
        lineRenderer.positionCount = segments;
        
        StartCoroutine(ExpandAndFade(segments));
    }

    private IEnumerator ExpandAndFade(int segments)
    {
        float timer = 0f;
        Color startC = lineRenderer.startColor;
        Color endC = lineRenderer.endColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // Ease out expansion
            float currentRadius = Mathf.Lerp(0f, maxRadius, 1f - Mathf.Pow(1f - t, 3f)); 
            
            float alpha = 1f - t;
            startC.a = alpha * 0.8f;
            endC.a = 0f;

            lineRenderer.startColor = startC;
            lineRenderer.endColor = endC;

            for (int i = 0; i < segments; i++)
            {
                float angle = ((float)i / segments) * 360f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * currentRadius;
                
                // Snap to pixel grid (64 Pixels Per Unit) for retro feel
                float ppu = 64f;
                pos.x = Mathf.Round(pos.x * ppu) / ppu;
                pos.y = Mathf.Round(pos.y * ppu) / ppu;
                
                lineRenderer.SetPosition(i, pos);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
