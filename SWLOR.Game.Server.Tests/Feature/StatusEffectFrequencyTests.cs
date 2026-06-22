using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class StatusEffectFrequencyTests
{
    [Test]
    public void TickingStatusEffects_DeclareExplicitFrequency()
    {
        var root = FindRepositoryRoot();
        var statusEffectPath = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition");

        var missingFrequency = Directory
            .GetFiles(statusEffectPath, "*.cs")
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("protected override void Tick") &&
                       !Regex.IsMatch(text, @"public\s+override\s+float\s+Frequency\s*=>");
            })
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();

        missingFrequency.Should().BeEmpty(
            "periodic status effects should not rely on StatusEffectBase's 1-second default");
    }

    [TestCaseSource(nameof(ExpectedStatusEffectFrequencies))]
    public void StatusEffectFrequency_MatchesDocumentedCadence(string statusEffectName, float actualFrequency, float expectedFrequency)
    {
        actualFrequency.Should().Be(expectedFrequency, $"{statusEffectName} must tick at its documented cadence");
    }

    private static IEnumerable<TestCaseData> ExpectedStatusEffectFrequencies()
    {
        yield return Frequency(nameof(AdrenalStimStatusEffect), new AdrenalStimStatusEffect().Frequency, 3f);
        yield return Frequency(nameof(CalmingStanceStatusEffect), new CalmingStanceStatusEffect().Frequency, 1f);
        yield return Frequency(nameof(DecisiveCommand1StatusEffect), new DecisiveCommand1StatusEffect().Frequency, 3f);
        yield return Frequency(nameof(EmergencyCocktailStatusEffect), new EmergencyCocktailStatusEffect().Frequency, 3f);
        yield return Frequency(nameof(EmergencySealant1StatusEffect), new EmergencySealant1StatusEffect().Frequency, 3f);
        yield return Frequency(nameof(FieldRecovery1StatusEffect), new FieldRecovery1StatusEffect().Frequency, 4f);
        yield return Frequency(nameof(FieldRecovery2StatusEffect), new FieldRecovery2StatusEffect().Frequency, 4f);
        yield return Frequency(nameof(ForceBondedBeast1StatusEffect), new ForceBondedBeast1StatusEffect().Frequency, 3f);
        yield return Frequency(nameof(ForceConvergenceStatusEffect), new ForceConvergenceStatusEffect().Frequency, 2f);
        yield return Frequency(nameof(ForceErosionStatusEffect), new ForceErosionStatusEffect().Frequency, 1f);
        yield return Frequency(nameof(PowerSurgeStatusEffect), new PowerSurgeStatusEffect().Frequency, 4f);
        yield return Frequency(nameof(RegenerativeHealingStatusEffect), new RegenerativeHealingStatusEffect().Frequency, 3f);
        yield return Frequency(nameof(SaturationTossStatusEffect), new SaturationTossStatusEffect().Frequency, 4f);
        yield return Frequency(nameof(SereneFocusStatusEffect), new SereneFocusStatusEffect().Frequency, 6f);
    }

    private static TestCaseData Frequency(string statusEffectName, float actualFrequency, float expectedFrequency)
    {
        return new TestCaseData(statusEffectName, actualFrequency, expectedFrequency)
            .SetName($"{statusEffectName}_FrequencyMatchesDocumentedCadence");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
