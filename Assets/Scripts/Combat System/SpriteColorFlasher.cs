using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Produces a true solid-colour sprite silhouette by temporarily swapping the
/// renderer material.  Unlike SpriteRenderer.color, this does not multiply the
/// original texture RGB, so a white flash is actually white.
/// </summary>
public class SpriteColorFlasher : MonoBehaviour
{
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    [Header("Optional Overrides")]
    [Tooltip("Solid-white sprite material assigned explicitly by the owning prefab.")]
    [SerializeField] private Material whiteFlashMaterial;
    [SerializeField] private SpriteRenderer[] flashRenderers;
    [SerializeField] private SpriteRenderer[] excludedRenderers;

    private readonly Dictionary<SpriteRenderer, Material[]> originalMaterials =
        new Dictionary<SpriteRenderer, Material[]>();
    private readonly Dictionary<SpriteRenderer, MaterialPropertyBlock> originalPropertyBlocks =
        new Dictionary<SpriteRenderer, MaterialPropertyBlock>();
    private Coroutine flashRoutine;

    public bool IsFlashing => flashRoutine != null;

    public void FlashColor(float duration, Color color)
    {
        FlashColor(null, duration, color);
    }

    // Compatibility overload retained for EnemyVisual and existing callers.
    public void FlashColor(SpriteRenderer preferredRenderer, float duration, Color color)
    {
        Material flashMaterial = ResolveFlashMaterial();
        if (flashMaterial == null)
        {
            Debug.LogWarning(
                $"[{nameof(SpriteColorFlasher)}] No white flash material is assigned on '{name}'.",
                this);
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreOriginalMaterials();
        }

        SpriteRenderer[] renderers = ResolveRenderers(preferredRenderer);
        if (renderers.Length == 0) return;

        flashRoutine = StartCoroutine(DoMaterialFlash(renderers, flashMaterial, duration, color));
    }

    private IEnumerator DoMaterialFlash(
        SpriteRenderer[] renderers,
        Material flashMaterial,
        float duration,
        Color color)
    {
        originalMaterials.Clear();
        originalPropertyBlocks.Clear();

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            originalMaterials[renderer] = renderer.sharedMaterials;
            MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(originalBlock);
            originalPropertyBlocks[renderer] = originalBlock;

            renderer.sharedMaterial = flashMaterial;
            MaterialPropertyBlock flashBlock = new MaterialPropertyBlock();
            flashBlock.SetColor(FlashColorId, color);
            renderer.SetPropertyBlock(flashBlock);
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duration));

        RestoreOriginalMaterials();
        flashRoutine = null;
    }

    private SpriteRenderer[] ResolveRenderers(SpriteRenderer preferredRenderer)
    {
        if (flashRenderers != null && flashRenderers.Length > 0)
            return FilterRenderers(flashRenderers);

        Transform entityRoot = ResolveEntityRoot();
        SpriteRenderer[] candidates = entityRoot.GetComponentsInChildren<SpriteRenderer>(false);
        List<SpriteRenderer> filtered = new List<SpriteRenderer>(candidates.Length);

        foreach (SpriteRenderer renderer in candidates)
        {
            if (renderer == null || IsExcluded(renderer) || IsUtilityRenderer(renderer)) continue;
            filtered.Add(renderer);
        }

        // If automatic filtering removed everything, the explicitly supplied
        // renderer is still safer than silently producing no feedback.
        if (filtered.Count == 0 && preferredRenderer != null && !IsExcluded(preferredRenderer))
            filtered.Add(preferredRenderer);

        return filtered.ToArray();
    }

    private SpriteRenderer[] FilterRenderers(SpriteRenderer[] candidates)
    {
        List<SpriteRenderer> filtered = new List<SpriteRenderer>(candidates.Length);
        foreach (SpriteRenderer renderer in candidates)
        {
            if (renderer != null && !IsExcluded(renderer)) filtered.Add(renderer);
        }
        return filtered.ToArray();
    }

    private Transform ResolveEntityRoot()
    {
        HealthSystem health = GetComponentInParent<HealthSystem>();
        return health != null ? health.transform : transform.root;
    }

    private bool IsExcluded(SpriteRenderer renderer)
    {
        if (excludedRenderers == null) return false;
        foreach (SpriteRenderer excluded in excludedRenderers)
        {
            if (excluded == renderer) return true;
        }
        return false;
    }

    private static bool IsUtilityRenderer(SpriteRenderer renderer)
    {
        string objectName = renderer.gameObject.name;
        return objectName.Contains("Minimap") ||
               objectName.Contains("Indicator") ||
               objectName.Contains("Prompt") ||
               objectName.Contains("Weak Point") ||
               objectName.Contains("Shared Vision");
    }

    private Material ResolveFlashMaterial()
    {
        return whiteFlashMaterial;
    }

    private void RestoreOriginalMaterials()
    {
        foreach (KeyValuePair<SpriteRenderer, Material[]> entry in originalMaterials)
        {
            SpriteRenderer renderer = entry.Key;
            if (renderer == null) continue;

            renderer.sharedMaterials = entry.Value;
            if (originalPropertyBlocks.TryGetValue(renderer, out MaterialPropertyBlock block))
                renderer.SetPropertyBlock(block);
            else
                renderer.SetPropertyBlock(null);
        }

        originalMaterials.Clear();
        originalPropertyBlocks.Clear();
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        RestoreOriginalMaterials();
    }
}
