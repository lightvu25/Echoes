using UnityEngine;

/// <summary>Shared pitch-variation policy for project sound effects.</summary>
public static class AudioPitchUtility
{
    public static float GetRandomPitch(float minimum = 0.9f, float maximum = 1.1f)
    {
        float safeMinimum = Mathf.Clamp(Mathf.Min(minimum, maximum), 0.1f, 3f);
        float safeMaximum = Mathf.Clamp(Mathf.Max(minimum, maximum), safeMinimum, 3f);
        return Random.Range(safeMinimum, safeMaximum);
    }
}
