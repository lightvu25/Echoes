using NUnit.Framework;
using UnityEngine;

public class KineticEliteDuelistTests
{
    [Test]
    public void EmpoweredAttack_TriggersOnConfiguredCadence()
    {
        Assert.That(KineticDuelistRules.IsEmpoweredAttack(1, 3), Is.False);
        Assert.That(KineticDuelistRules.IsEmpoweredAttack(2, 3), Is.False);
        Assert.That(KineticDuelistRules.IsEmpoweredAttack(3, 3), Is.True);
        Assert.That(KineticDuelistRules.IsEmpoweredAttack(6, 3), Is.True);
    }

    [Test]
    public void NormalStrike_IdentifiesEchoAndAppliesBaseKineticKnockback()
    {
        EchoData echo = ScriptableObject.CreateInstance<EchoData>();
        try
        {
            DamageInfo damage = DamageInfo.CreateWithKnockback(20, null, Vector2.right, 4f);

            KineticDuelistRules.ApplyAttackModifiers(ref damage, echo, false, 1.25f, 1.35f, 2.25f);

            Assert.That(damage.activeEcho, Is.SameAs(echo));
            Assert.That(damage.knockbackForce, Is.EqualTo(5f).Within(0.001f));
            Assert.That(damage.multiplicativeStack, Is.EqualTo(1f).Within(0.001f));
            Assert.That(damage.isPiercing, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(echo);
        }
    }

    [Test]
    public void EmpoweredStrike_BoostsDamageKnockbackAndPiercing()
    {
        EchoData echo = ScriptableObject.CreateInstance<EchoData>();
        try
        {
            DamageInfo damage = DamageInfo.CreateWithKnockback(20, null, Vector2.right, 4f);

            KineticDuelistRules.ApplyAttackModifiers(ref damage, echo, true, 1.25f, 1.35f, 2.25f);

            Assert.That(damage.activeEcho, Is.SameAs(echo));
            Assert.That(damage.multiplicativeStack, Is.EqualTo(1.35f).Within(0.001f));
            Assert.That(damage.knockbackForce, Is.EqualTo(9f).Within(0.001f));
            Assert.That(damage.isPiercing, Is.True);
            Assert.That(damage.hitFreezeTime, Is.EqualTo(0.06f).Within(0.001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(echo);
        }
    }
}
