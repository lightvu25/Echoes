using UnityEngine;

public enum EchoRange { Melee, Mid, Ranged, Hybrid }

public enum EchoAudioMoment
{
    Activation,
    Attack,
    Hit
}

[System.Serializable]
public struct EchoAudioCue
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
    [Tooltip("Adds a small pitch variation so repeated Echo sounds feel less mechanical.")]
    public bool randomizePitch;

    public EchoAudioCue(AudioClip clip, float volume, bool randomizePitch)
    {
        this.clip = clip;
        this.volume = Mathf.Clamp01(volume);
        this.randomizePitch = randomizePitch;
    }

    public bool IsConfigured => clip != null;
    public float SafeVolume => Mathf.Clamp01(volume);
}

[CreateAssetMenu(fileName = "New Echo", menuName = "Data/Echo Data")]
public class EchoData : ItemBaseData
{
    public override ItemCategory Category => ItemCategory.Echo;

    [Header("Echo Info")]
    public EchoType echoType;
    public bool isFusionResult;

    [Header("Combat Stats")]
    public float baseDamageMultiplier = 1.0f;
    public float attacksPerSecond = 1.0f;
    public EchoRange rangeCategory;

    [Header("Status Effect")]
    public bool hasStatusEffect;
    [Range(0f, 1f)]
    public float statusProcCoefficient;

    [Header("Unique Mechanics")]
    public string uniqueModifierID;

    [Header("Visual Effects")]
    public Color trailColor = Color.white;
    public ParticleSystem stateVFXPrefab;
    public GameObject hitImpactPrefab;
    public GameObject voidMarkVFXPrefab;
    public GameObject chainLightningVFXPrefab;

    [Header("Audio Effects")]
    [Tooltip("Played once when this Echo becomes the active equipped Echo.")]
    public EchoAudioCue activationAudio = new EchoAudioCue(null, 0.8f, false);
    [Tooltip("Played when an attack using this Echo is committed, even if the attack misses.")]
    public EchoAudioCue attackAudio = new EchoAudioCue(null, 0.8f, true);
    [Tooltip("Played only when an Echo attack successfully damages a target.")]
    public EchoAudioCue hitAudio = new EchoAudioCue(null, 0.8f, true);

    [HideInInspector] public int level = 1;
    [HideInInspector] public float currentStatusProc;

    public void InitRuntime()
    {
        level = 1;
        currentStatusProc = statusProcCoefficient;
    }

    public void Stack(EchoData other)
    {
        level++;
        currentStatusProc = Mathf.Clamp01(currentStatusProc + other.statusProcCoefficient * 0.5f);
    }

    public EchoAudioCue GetAudioCue(EchoAudioMoment moment)
    {
        return moment switch
        {
            EchoAudioMoment.Activation => activationAudio,
            EchoAudioMoment.Attack => attackAudio,
            EchoAudioMoment.Hit => hitAudio,
            _ => default
        };
    }
}
