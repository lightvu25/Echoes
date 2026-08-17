using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public static class FusionCombatUtility
{
    public static void PlayExplosionFeedback(
        Vector2 position,
        GameObject explosionVfx,
        float shakeScale = 0.65f)
    {
        if (explosionVfx != null)
        {
            ObjectPoolManager.SpawnObject(
                explosionVfx,
                position,
                Quaternion.identity,
                ObjectPoolManager.PoolType.ParticleSystem);
        }

        GameFeelManager.Instance?.ProcessExplosion(position, shakeScale);
    }

    private static readonly Collider2D[] Hits = new Collider2D[64];

    public static bool TryCreateGroundLine(Vector2 targetPosition, float length, out Vector2 start, out Vector2 end, float visualGroundOffset = 0.08f)
    {
        int groundMask = LayerMask.GetMask("Ground", "OneWayPlatform");
        Vector2 rayOrigin = targetPosition + Vector2.up * 0.5f;
        RaycastHit2D groundHit = Physics2D.Raycast(rayOrigin, Vector2.down, 8f, groundMask);

        if (groundHit.collider == null)
        {
            start = default;
            end = default;
            return false;
        }

        float halfLength = Mathf.Max(0.5f, length * 0.5f);
        Vector2 center = groundHit.point + Vector2.up * visualGroundOffset;
        start = center + Vector2.left * halfLength;
        end = center + Vector2.right * halfLength;
        return true;
    }

    public static bool IsFusionHit(AttackHitbox.HitEventArgs hit, string modifierId)
    {
        return hit != null && hit.target != null && !hit.target.IsDead &&
               hit.damageInfo.activeEcho != null &&
               hit.damageInfo.activeEcho.uniqueModifierID == modifierId;
    }

    public static EchoStatusReceiver GetStatus(IDamageable target)
    {
        if (target == null || target.Transform == null) return null;
        EchoStatusReceiver status = target.Transform.GetComponent<EchoStatusReceiver>();
        return status != null ? status : target.Transform.gameObject.AddComponent<EchoStatusReceiver>();
    }

    public static void DealDamage(IDamageable target, int amount, EchoModifierContext context,
        DamageSourceType source = DamageSourceType.FusionSecondary, bool trueDamage = false)
    {
        if (target == null || target.IsDead || amount <= 0) return;

        int baseAmount = amount;
        if (!trueDamage)
        {
            float reduction = DamageCalculator.CalculateDefenseReduction(target.Defense);
            baseAmount = Mathf.Max(1, Mathf.CeilToInt(amount / Mathf.Max(0.01f, 1f - reduction)));
        }

        DamageInfo damage = DamageInfo.Create(baseAmount, context.PlayerGameObject);
        damage.damageSource = source;
        damage.isTrueDamage = trueDamage;
        damage.procCoefficient = 0f;
        target.TakeDamage(damage);
    }

    public static void DealArea(Vector2 center, float radius, int amount, EchoModifierContext context,
        IDamageable excluded = null, bool trueDamage = false, DamageSourceType source = DamageSourceType.FusionSecondary)
    {
        if (context.PlayerAttackHitbox == null) return;

        int count = Physics2D.OverlapCircle(center, radius, CreateFilter(context), Hits);
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        for (int i = 0; i < count; i++)
        {
            IDamageable target = Hits[i] != null ? Hits[i].GetComponentInParent<IDamageable>() : null;
            if (target == null || target is FusionGlitchNode || target == excluded || target.IsDead || !damaged.Add(target)) continue;
            DealDamage(target, amount, context, source, trueDamage);
        }
    }

    public static void ApplyBurnArea(Vector2 center, float radius, float duration, EchoModifierContext context)
    {
        if (context.PlayerAttackHitbox == null) return;
        int count = Physics2D.OverlapCircle(center, radius, CreateFilter(context), Hits);
        HashSet<IDamageable> affected = new HashSet<IDamageable>();
        for (int i = 0; i < count; i++)
        {
            IDamageable target = Hits[i] != null ? Hits[i].GetComponentInParent<IDamageable>() : null;
            if (target == null || target is FusionGlitchNode || target.IsDead || !affected.Add(target)) continue;
            GetStatus(target)?.ApplyBurn(duration, null, context.PlayerGameObject);
        }
    }

    public static void ApplyFreezeArea(Vector2 center, float radius, float duration, EchoModifierContext context)
    {
        if (context.PlayerAttackHitbox == null) return;
        int count = Physics2D.OverlapCircle(center, radius, CreateFilter(context), Hits);
        HashSet<IDamageable> affected = new HashSet<IDamageable>();
        for (int i = 0; i < count; i++)
        {
            IDamageable target = Hits[i] != null ? Hits[i].GetComponentInParent<IDamageable>() : null;
            if (target == null || target is FusionGlitchNode || target.IsDead || !affected.Add(target)) continue;
            GetStatus(target)?.ApplyFreeze(duration);
        }
    }

    public static void ApplyVoidMarkArea(Vector2 center, float radius, EchoModifierContext context)
    {
        ForEachTarget(center, radius, context, target => GetStatus(target).IsVoidMarked = true);
    }

    public static void DealAreaToFrozen(Vector2 center, float radius, int amount, EchoModifierContext context,
        IDamageable excluded = null)
    {
        GameObject explosionVfx = context.EchoExplosionPrefab;
        ForEachTarget(center, radius, context, target =>
        {
            if (target == excluded) return;
            EchoStatusReceiver status = GetStatus(target);
            if (status != null && status.IsFrozen)
            {
                DealDamage(target, amount, context, DamageSourceType.FusionSecondary, true);
                PlayExplosionFeedback(target.Transform.position, explosionVfx, 0.55f);
            }
        });
    }

    public static void DetonateMarkedArea(Vector2 center, float radius, int amount, EchoModifierContext context)
    {
        ForEachTarget(center, radius, context, target =>
        {
            EchoStatusReceiver status = GetStatus(target);
            if (status == null || (!status.IsVoidMarked && !status.IsBurning)) return;
            DealDamage(target, amount, context, DamageSourceType.FusionField, true);
            status.IsVoidMarked = false;
            status.ForceRemoveBurn();
        });
    }

    public static void ShatterFrozenArea(Vector2 center, float radius, EchoModifierContext context)
    {
        ForEachTarget(center, radius, context, target =>
        {
            EchoStatusReceiver status = GetStatus(target);
            if (status != null && status.IsFrozen) status.ForceRemoveFreeze();
        });
    }

    public static void DealLine(Vector2 start, Vector2 end, float width, int amount, EchoModifierContext context,
        IDamageable excluded = null)
    {
        if (context.PlayerAttackHitbox == null) return;
        Vector2 delta = end - start;
        Vector2 center = (start + end) * 0.5f;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        int count = Physics2D.OverlapBox(center, new Vector2(delta.magnitude, width), angle,
            CreateFilter(context), Hits);
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        for (int i = 0; i < count; i++)
        {
            IDamageable target = Hits[i] != null ? Hits[i].GetComponentInParent<IDamageable>() : null;
            if (target == null || target is FusionGlitchNode || target == excluded || target.IsDead || !damaged.Add(target)) continue;
            DealDamage(target, amount, context, DamageSourceType.FusionField);
        }
    }

    public static void PullArea(Vector2 center, float radius, float strength, EchoModifierContext context)
    {
        if (context.PlayerAttackHitbox == null) return;
        int count = Physics2D.OverlapCircle(center, radius, CreateFilter(context), Hits);
        HashSet<Rigidbody2D> moved = new HashSet<Rigidbody2D>();
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D body = Hits[i] != null ? Hits[i].GetComponentInParent<Rigidbody2D>() : null;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic || !moved.Add(body)) continue;
            Vector2 direction = center - body.position;
            body.AddForce(direction.normalized * strength, ForceMode2D.Impulse);
        }
    }

    public static GameObject SpawnVfx(GameObject prefab, Vector3 position, float lifetime = 1f)
    {
        if (prefab == null) return null;
        GameObject instance = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity,
            ObjectPoolManager.PoolType.GameObject);
        BlackHoleEffect blackHole = instance.GetComponent<BlackHoleEffect>();
        if (blackHole != null) blackHole.enabled = false;
        ReturnToPool returner = instance.GetComponent<ReturnToPool>();
        if (returner != null) returner.ConfigureDelay(lifetime);
        return instance;
    }

    public static IDamageable FindNearestTarget(Vector2 center, float radius, EchoModifierContext context,
        HashSet<IDamageable> excluded)
    {
        IDamageable nearest = null;
        float nearestDistance = float.MaxValue;
        ForEachTarget(center, radius, context, target =>
        {
            if (excluded.Contains(target) || target is FusionGlitchNode) return;
            float distance = Vector2.SqrMagnitude((Vector2)target.Transform.position - center);
            if (distance < nearestDistance) { nearestDistance = distance; nearest = target; }
        });
        return nearest;
    }

    private static void ForEachTarget(Vector2 center, float radius, EchoModifierContext context, Action<IDamageable> action)
    {
        if (context.PlayerAttackHitbox == null) return;
        int count = Physics2D.OverlapCircle(center, radius, CreateFilter(context), Hits);
        HashSet<IDamageable> affected = new HashSet<IDamageable>();
        for (int i = 0; i < count; i++)
        {
            IDamageable target = Hits[i] != null ? Hits[i].GetComponentInParent<IDamageable>() : null;
            if (target == null || target is FusionGlitchNode || target.IsDead || !affected.Add(target)) continue;
            action(target);
        }
    }

    private static ContactFilter2D CreateFilter(EchoModifierContext context)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(context.PlayerAttackHitbox.TargetLayers);
        filter.useTriggers = true;
        return filter;
    }
}

public class FusionRuntimeHost : MonoBehaviour
{
    private const float BurningLineTickInterval = 0.5f;
    private const float BurningLineVisualSpacing = 0.75f;
    private const float BurningLineVisualLifetime = 0.45f;

    public void StartPlasmaChain(IDamageable firstTarget, int explosionDamage, EchoModifierContext context,
        GameObject chainVfxPrefab)
    {
        StartCoroutine(PlasmaChainRoutine(firstTarget, explosionDamage, context, chainVfxPrefab));
    }

    public void StartField(Vector2 center, float duration, float interval, float radius, int pulseDamage,
        int collapseDamage, EchoModifierContext context, bool trueDamage, GameObject vfxPrefab,
        bool pullTargets = false, bool freezeTargets = false, bool detonateStatuses = false,
        bool shatterFrozen = false)
    {
        StartCoroutine(FieldRoutine(center, duration, interval, radius, pulseDamage,
            collapseDamage, context, trueDamage, vfxPrefab, pullTargets, freezeTargets,
            detonateStatuses, shatterFrozen));
    }

    public void StartBurningLine(Vector2 start, Vector2 end, float width, float duration, int tickDamage,
        EchoModifierContext context, GameObject vfxPrefab)
    {
        StartCoroutine(BurningLineRoutine(start, end, width, duration, tickDamage, context, vfxPrefab));
    }

    public void StopAllFields()
    {
        StopAllCoroutines();
    }

    private IEnumerator FieldRoutine(Vector2 center, float duration, float interval, float radius,
        int pulseDamage, int collapseDamage, EchoModifierContext context, bool trueDamage,
        GameObject vfxPrefab, bool pullTargets, bool freezeTargets, bool detonateStatuses,
        bool shatterFrozen)
    {
        FusionCombatUtility.SpawnVfx(vfxPrefab, center, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (pullTargets) FusionCombatUtility.PullArea(center, radius, 1.5f, context);
            FusionCombatUtility.DealArea(center, radius, pulseDamage, context, null, trueDamage, DamageSourceType.FusionField);
            if (freezeTargets) FusionCombatUtility.ApplyFreezeArea(center, radius, 1.25f, context);
            if (detonateStatuses) FusionCombatUtility.ApplyVoidMarkArea(center, radius, context);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        FusionCombatUtility.DealArea(center, radius, collapseDamage, context, null, trueDamage, DamageSourceType.FusionField);
        if (detonateStatuses)
            FusionCombatUtility.DetonateMarkedArea(center, radius, Mathf.Max(1, collapseDamage / 2), context);
        if (shatterFrozen) FusionCombatUtility.ShatterFrozenArea(center, radius, context);
    }

    private IEnumerator PlasmaChainRoutine(IDamageable firstTarget, int explosionDamage,
        EchoModifierContext context, GameObject chainVfxPrefab)
    {
        HashSet<IDamageable> visited = new HashSet<IDamageable>();
        IDamageable current = firstTarget;
        GameObject explosionVfx = context.EchoExplosionPrefab;
        
        for (int bounce = 0; bounce < 4 && current != null && !current.IsDead; bounce++)
        {
            visited.Add(current);
            Vector2 impact = current.Transform.position;
            
            FusionCombatUtility.PlayExplosionFeedback(impact, explosionVfx, 0.55f);
                
            FusionCombatUtility.DealArea(impact, 2.25f, explosionDamage, context);
            FusionCombatUtility.ApplyBurnArea(impact, 2.25f, 3f, context);

            IDamageable next = FusionCombatUtility.FindNearestTarget(impact, 5f, context, visited);
            if (next == null) yield break;
            if (chainVfxPrefab != null)
            {
                GameObject lightning = Instantiate(chainVfxPrefab);
                ChainLightningVFX vfx = lightning.GetComponent<ChainLightningVFX>();
                if (vfx != null) vfx.Initialize(impact, next.Transform.position);
            }
            yield return new WaitForSeconds(0.04f);
            current = next;
        }
    }

    private IEnumerator BurningLineRoutine(Vector2 start, Vector2 end, float width, float duration,
        int tickDamage, EchoModifierContext context, GameObject vfxPrefab)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SpawnBurningLineVisuals(start, end, vfxPrefab);
            FusionCombatUtility.DealLine(start, end, width, tickDamage, context);
            yield return new WaitForSeconds(BurningLineTickInterval);
            elapsed += BurningLineTickInterval;
        }
    }

    private static void SpawnBurningLineVisuals(Vector2 start, Vector2 end, GameObject vfxPrefab)
    {
        if (vfxPrefab == null) return;

        float length = Vector2.Distance(start, end);
        int visualCount = Mathf.Max(2, Mathf.CeilToInt(length / BurningLineVisualSpacing) + 1);
        for (int i = 0; i < visualCount; i++)
        {
            float t = visualCount == 1 ? 0.5f : i / (float)(visualCount - 1);
            GameObject visual = FusionCombatUtility.SpawnVfx(
                vfxPrefab, Vector2.Lerp(start, end, t), BurningLineVisualLifetime);
            if (visual == null) continue;

            visual.transform.rotation = vfxPrefab.transform.rotation;
            visual.transform.localScale = vfxPrefab.transform.localScale;

            ParticleSystem[] particleSystems = visual.GetComponentsInChildren<ParticleSystem>(true);
            for (int systemIndex = 0; systemIndex < particleSystems.Length; systemIndex++)
            {
                particleSystems[systemIndex].Clear(true);
                particleSystems[systemIndex].Play(true);
            }
        }
    }

}

public class FusionGlitchNode : MonoBehaviour, IDamageable
{
    private Action<FusionGlitchNode> onHit;
    private Action<FusionGlitchNode> onPulse;
    private Coroutine pulseRoutine;
    private bool consumed;
    public bool IsDead => consumed || !gameObject.activeInHierarchy;
    public Transform Transform => transform;
    public float Defense => 0f;

    public void Configure(Action<FusionGlitchNode> hitCallback, Action<FusionGlitchNode> pulseCallback)
    {
        onHit = hitCallback;
        onPulse = pulseCallback;
        consumed = false;
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null) collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.6f;
        collider.enabled = true;
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (consumed || damageInfo.BypassesInvincibilityFrames) return;
        consumed = true;
        onHit?.Invoke(this);
    }

    public void Consume()
    {
        if (consumed && !gameObject.activeSelf) return;
        consumed = true;
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }

    private void OnDisable()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = null;
        consumed = true;
        onHit = null;
        onPulse = null;
    }

    private IEnumerator PulseRoutine()
    {
        while (!consumed)
        {
            yield return new WaitForSeconds(1f);
            if (!consumed) onPulse?.Invoke(this);
        }
    }
}
