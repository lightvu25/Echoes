using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class AfterburnerGroundLineTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null) Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void TryCreateGroundLine_ProjectsAHorizontalSixUnitFieldOntoGround()
    {
        GameObject ground = CreateObject("Afterburner Test Ground");
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.transform.position = new Vector3(1000f, -0.5f, 0f);
        BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
        groundCollider.size = new Vector2(20f, 1f);
        Physics2D.SyncTransforms();

        bool foundGround = FusionCombatUtility.TryCreateGroundLine(
            new Vector2(1002f, 2f), 6f, out Vector2 start, out Vector2 end);

        Assert.That(foundGround, Is.True);
        Assert.That(end.x - start.x, Is.EqualTo(6f).Within(0.001f));
        Assert.That(start.y, Is.EqualTo(end.y).Within(0.001f));
        Assert.That(start.y, Is.EqualTo(0.08f).Within(0.001f));
        Assert.That((start.x + end.x) * 0.5f, Is.EqualTo(1002f).Within(0.001f));
    }

    [Test]
    public void DealLine_DealsFusionFieldDamageWithoutAddingGenericBurn()
    {
        GameObject player = CreateObject("Afterburner Test Player");
        AttackHitbox hitbox = player.AddComponent<AttackHitbox>();
        FieldInfo targetLayersField = typeof(AttackHitbox).GetField(
            "targetLayers", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(targetLayersField, Is.Not.Null);
        targetLayersField.SetValue(hitbox, (LayerMask)(1 << LayerMask.NameToLayer("Enemy")));

        GameObject enemy = CreateObject("Afterburner Test Enemy");
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.transform.position = new Vector3(0f, 0.5f, 0f);
        enemy.AddComponent<BoxCollider2D>().size = Vector2.one;
        AfterburnerDamageableProbe damageable = enemy.AddComponent<AfterburnerDamageableProbe>();
        Physics2D.SyncTransforms();

        EchoModifierContext context = new EchoModifierContext
        {
            PlayerGameObject = player,
            PlayerAttackHitbox = hitbox
        };

        FusionCombatUtility.DealLine(
            new Vector2(-3f, 0.08f), new Vector2(3f, 0.08f), 1.25f, 7, context);

        Assert.That(damageable.HitCount, Is.EqualTo(1));
        Assert.That(damageable.LastDamage.baseDamage, Is.EqualTo(7));
        Assert.That(damageable.LastDamage.damageSource, Is.EqualTo(DamageSourceType.FusionField));
        Assert.That(enemy.GetComponent<EchoStatusReceiver>(), Is.Null);
    }

    private GameObject CreateObject(string name)
    {
        GameObject instance = new GameObject(name);
        createdObjects.Add(instance);
        return instance;
    }
}

public sealed class AfterburnerDamageableProbe : MonoBehaviour, IDamageable
{
    public int HitCount { get; private set; }
    public DamageInfo LastDamage { get; private set; }
    public bool IsDead => false;
    public Transform Transform => transform;
    public float Defense => 0f;

    public void TakeDamage(DamageInfo damageInfo)
    {
        HitCount++;
        LastDamage = damageInfo;
    }
}
