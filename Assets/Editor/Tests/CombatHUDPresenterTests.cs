using NUnit.Framework;

public class StatsUICombatHUDTests
{
    [Test]
    public void DefeatedEnemies_NeverDisplaysNegativeCount()
    {
        Assert.AreEqual("0", CombatHUDText.FormatDefeatedEnemies(-3));
    }

    [TestCase(null, 0, "0")]
    [TestCase("Awakened", 2, "2")]
    public void EvolutionTier_DisplaysOnlyTheNumericIndex(string tierName, int tierIndex, string expected)
    {
        Assert.AreEqual(expected, CombatHUDText.FormatEvolutionTier(tierName, tierIndex));
    }

    [Test]
    public void EncounterName_RemovesRuntimeCloneSuffix()
    {
        Assert.AreEqual("KINETIC ELITE", CombatHUDText.FormatEncounterName("Kinetic Elite(Clone)"));
    }
}
