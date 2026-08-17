#if UNITY_EDITOR
using System;

/// <summary>
/// Compatibility entry point for code that previously invoked the TSV importer.
/// Fusion data is now generated deterministically by <see cref="FusionDataGenerator"/>.
/// </summary>
[Obsolete("Use FusionDataGenerator.GenerateFusionData instead.")]
public static class FusionRecipeImporter
{
    public static void ImportRecipes()
    {
        FusionDataGenerator.GenerateFusionData();
    }
}
#endif
