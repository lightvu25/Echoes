#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class GameFeelFeedbackTests
{
    private readonly List<Object> createdObjects = new List<Object>();

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
    public void Presets_UseDedicatedTuningForEachImpactType()
    {
        GameFeelManager manager = CreateManager();

        Assert.That(manager.GetPreset(GameFeelImpactType.Dash).intensity,
            Is.LessThan(manager.GetPreset(GameFeelImpactType.NormalHit).intensity));
        Assert.That(manager.GetPreset(GameFeelImpactType.CriticalHit).intensity,
            Is.GreaterThan(manager.GetPreset(GameFeelImpactType.NormalHit).intensity));
        Assert.That(manager.GetPreset(GameFeelImpactType.PlungeImpact).duration,
            Is.GreaterThan(manager.GetPreset(GameFeelImpactType.NormalHit).duration));
        Assert.That(manager.GetPreset(GameFeelImpactType.Explosion).useDistanceFalloff, Is.True);
    }

    [Test]
    public void ProcessHit_HighNonCriticalDamageStillUsesNormalHit()
    {
        GameFeelManager manager = CreateManager();
        GameObject attacker = CreateObject("Attacker");
        GameObject victim = CreateObject("Victim");
        DamageInfo info = DamageInfo.Create(500, attacker);

        manager.ProcessHit(attacker, victim, info, 500);

        Assert.That(manager.LastPlayedImpact, Is.EqualTo(GameFeelImpactType.NormalHit));
    }

    [Test]
    public void ProcessHit_CriticalAndKineticRouteToTheirOwnPresets()
    {
        GameFeelManager manager = CreateManager();
        GameObject attacker = CreateObject("Attacker");
        GameObject victim = CreateObject("Victim");

        DamageInfo critical = DamageInfo.Create(10, attacker);
        critical.isCritical = true;
        manager.ProcessHit(attacker, victim, critical, 10);
        Assert.That(manager.LastPlayedImpact, Is.EqualTo(GameFeelImpactType.CriticalHit));

        EchoData kineticEcho = ScriptableObject.CreateInstance<EchoData>();
        createdObjects.Add(kineticEcho);
        kineticEcho.uniqueModifierID = "KINETIC_FORCE";
        DamageInfo kinetic = DamageInfo.Create(10, attacker);
        kinetic.activeEcho = kineticEcho;
        manager.ProcessHit(attacker, victim, kinetic, 10);
        Assert.That(manager.LastPlayedImpact, Is.EqualTo(GameFeelImpactType.KineticImpact));
    }

    [Test]
    public void ProcessHit_RejectedDamageDoesNotPlayFeedback()
    {
        GameFeelManager manager = CreateManager();
        GameObject attacker = CreateObject("Attacker");
        GameObject victim = CreateObject("Victim");
        DamageInfo info = DamageInfo.Create(20, attacker);

        manager.ProcessHit(attacker, victim, info, 0);

        Assert.That(manager.LastPlayedImpact, Is.Null);
    }

    [Test]
    public void ProcessHit_PlayerVictimDefersToPlayerCombatFeedback()
    {
        GameFeelManager manager = CreateManager();
        GameObject attacker = CreateObject("Enemy Attacker");
        GameObject victim = CreateObject("Player Victim");
        victim.AddComponent<PlayerCombat>();
        DamageInfo info = DamageInfo.Create(20, attacker);

        manager.ProcessHit(attacker, victim, info, 20);

        Assert.That(manager.LastPlayedImpact, Is.Null);
    }

    [Test]
    public void WhiteFlashMaterial_UsesTheSolidWhiteShader()
    {
        Shader shader = Shader.Find("Echoes/VFX/Solid White Sprite");
        Material material = FindHitFlashMaterial();

        Assert.That(shader, Is.Not.Null);
        Assert.That(material, Is.Not.Null);
        Assert.That(material.shader, Is.EqualTo(shader));
        Assert.That(material.HasProperty("_FlashColor"), Is.True);
    }

    [Test]
    public void SpriteColorFlasher_FlashesAllCharacterPartsAndRestoresWhenDisabled()
    {
        GameObject root = CreateObject("Flash Test Character");
        SpriteColorFlasher flasher = root.AddComponent<SpriteColorFlasher>();
        AssignHitFlashMaterial(flasher);
        SpriteRenderer body = CreateRenderer(root.transform, "Body");
        SpriteRenderer weapon = CreateRenderer(root.transform, "Weapon");
        SpriteRenderer minimap = CreateRenderer(root.transform, "Minimap Icon");

        Material original = new Material(Shader.Find("Sprites/Default"));
        createdObjects.Add(original);
        body.sharedMaterial = original;
        weapon.sharedMaterial = original;
        minimap.sharedMaterial = original;

        flasher.FlashColor(0.02f, Color.white);

        Assert.That(body.sharedMaterial.shader.name, Is.EqualTo("Echoes/VFX/Solid White Sprite"));
        Assert.That(weapon.sharedMaterial.shader.name, Is.EqualTo("Echoes/VFX/Solid White Sprite"));
        Assert.That(minimap.sharedMaterial, Is.EqualTo(original));

        flasher.enabled = false;

        Assert.That(body.sharedMaterial, Is.EqualTo(original));
        Assert.That(weapon.sharedMaterial, Is.EqualTo(original));
        Assert.That(flasher.IsFlashing, Is.False);
    }

    [Test]
    public void PlayerVisual_CreatesDashTrailAndParticleChildren()
    {
        GameObject player = CreateObject("Player Visual Test");
        PlayerVisual visual = player.AddComponent<PlayerVisual>();
        if (player.transform.Find("Dash Streak") == null)
            visual.SendMessage("Awake");

        Transform streak = player.transform.Find("Dash Streak");
        Transform particles = player.transform.Find("Dash Particles");

        Assert.That(streak, Is.Not.Null);
        Assert.That(streak.GetComponent<TrailRenderer>(), Is.Not.Null);
        Assert.That(particles, Is.Not.Null);
        Assert.That(particles.GetComponent<ParticleSystem>(), Is.Not.Null);
        Assert.That(particles.GetComponent<ParticleSystem>().isPlaying, Is.False);
    }

    [Test]
    public void PlayerVisual_DashAfterimagesAreEmittedAtEqualWorldDistances()
    {
        GameObject player = CreateObject("Player Visual Distance Test");
        PlayerVisual visual = player.AddComponent<PlayerVisual>();
        if (player.transform.Find("Dash Particles") == null)
            visual.SendMessage("Awake");

        SerializedObject serializedVisual = new SerializedObject(visual);
        serializedVisual.FindProperty("dashParticleSpacing").floatValue = 0.25f;
        serializedVisual.ApplyModifiedPropertiesWithoutUndo();

        MethodInfo emitAlongSegment = typeof(PlayerVisual).GetMethod(
            "EmitDashParticlesAlongSegment",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(emitAlongSegment, Is.Not.Null);

        object[] arguments = { Vector3.zero, Vector3.right, 0f };
        emitAlongSegment.Invoke(visual, arguments);

        ParticleSystem particleSystem = player.transform
            .Find("Dash Particles")
            .GetComponent<ParticleSystem>();
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[8];
        int particleCount = particleSystem.GetParticles(particles);

        Assert.That(particleCount, Is.EqualTo(4));
        List<float> particlePositions = new List<float>(particleCount);
        for (int i = 0; i < particleCount; i++)
            particlePositions.Add(particles[i].position.x);
        particlePositions.Sort();

        for (int i = 0; i < particlePositions.Count; i++)
        {
            float expectedX = (i + 1) * 0.25f;
            Assert.That(particlePositions[i], Is.EqualTo(expectedX).Within(0.001f));
        }
    }

    [Test]
    public void PlayerVisual_DashTrailKeepsPositiveWorldScaleForBothFacings()
    {
        GameObject player = CreateObject("Player Visual Trail Facing Test");
        PlayerVisual visual = player.AddComponent<PlayerVisual>();
        if (player.transform.Find("Dash Streak") == null)
            visual.SendMessage("Awake");

        Transform trail = player.transform.Find("Dash Streak");
        Assert.That(trail, Is.Not.Null);

        MethodInfo stabilizeTrail = typeof(PlayerVisual).GetMethod(
            "StabilizeDashTrailScale",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(stabilizeTrail, Is.Not.Null);

        player.transform.localScale = Vector3.one;
        stabilizeTrail.Invoke(visual, null);
        Assert.That(trail.lossyScale.x, Is.GreaterThan(0f));

        player.transform.localScale = new Vector3(-1f, 1f, 1f);
        stabilizeTrail.Invoke(visual, null);
        Assert.That(trail.lossyScale.x, Is.GreaterThan(0f));
        Assert.That(trail.localScale.x, Is.LessThan(0f));

        player.transform.localScale = Vector3.one;
        stabilizeTrail.Invoke(visual, null);
        Assert.That(trail.lossyScale.x, Is.GreaterThan(0f));
        Assert.That(trail.localScale.x, Is.GreaterThan(0f));
    }

    private GameFeelManager CreateManager()
    {
        GameObject gameObject = CreateObject("Game Feel Test Manager");
        GameFeelManager manager = gameObject.AddComponent<GameFeelManager>();
        if (GameFeelManager.Instance != manager) manager.SendMessage("Awake");
        return manager;
    }

    private GameObject CreateObject(string name)
    {
        GameObject instance = new GameObject(name);
        createdObjects.Add(instance);
        return instance;
    }

    private SpriteRenderer CreateRenderer(Transform parent, string name)
    {
        GameObject child = CreateObject(name);
        child.transform.SetParent(parent, false);
        return child.AddComponent<SpriteRenderer>();
    }

    private static Material FindHitFlashMaterial()
    {
        string[] guids = AssetDatabase.FindAssets("Hit Flash White t:Material");
        Assert.That(guids, Is.Not.Empty, "The hit-flash material asset is missing.");
        return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static void AssignHitFlashMaterial(SpriteColorFlasher flasher)
    {
        SerializedObject serializedFlasher = new SerializedObject(flasher);
        SerializedProperty materialProperty = serializedFlasher.FindProperty("whiteFlashMaterial");
        materialProperty.objectReferenceValue = FindHitFlashMaterial();
        serializedFlasher.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
