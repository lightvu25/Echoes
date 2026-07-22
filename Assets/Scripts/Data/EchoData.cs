using UnityEngine;

public enum EchoRange { Melee, Mid, Ranged, Hybrid }

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
}
