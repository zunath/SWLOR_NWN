using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class SupportCombatPointTests
{
    [Test]
    public void ShipRepairModules_CreditEnemiesFightingTheRepairedShips()
    {
        var repairModules = ReadSources("ShipModuleDefinition")
            .Where(module => module.Source.Contains("Space.RestoreHull(") || module.Source.Contains("Space.RestoreShield("))
            .ToList();

        repairModules.Select(module => module.Name).Should().Contain(new[]
        {
            "HullRepairerModuleDefinition.cs",
            "ShieldRepairerModuleDefinition.cs",
            "RepairFieldGeneratorModuleDefinition.cs",
            "BulwarkShieldGeneratorModuleDefinition.cs",
        });

        foreach (var (name, source) in repairModules)
        {
            source.Should().Contain(
                "CombatPoint.AddSupportCombatPoint(activator,",
                $"{name} repairs other ships, so the enemies fighting them must award the repairer Piloting XP");
            source.Should().NotContain(
                "AddCombatPointToAllTagged",
                $"{name} must not limit repair XP to enemies the repairer already damaged, which already pay full Piloting XP");
        }
    }

    [Test]
    public void AllySupportAbilities_CreditEnemiesFightingTheSupportedAllies()
    {
        var tagOnly = ReadSources("AbilityDefinition")
            .Where(ability => ability.Source.Contains("AddCombatPointToAllTagged"))
            .Select(ability => ability.Name)
            .ToList();

        tagOnly.Should().BeEquivalentTo(
            new[] { "BreakMoraleAbilityDefinition.cs" },
            "support abilities must also credit the enemies fighting the allies they support, or a pure supporter earns no XP");

        var breakMorale = ReadSources("AbilityDefinition").Single(ability => ability.Name == "BreakMoraleAbilityDefinition.cs").Source;
        breakMorale.Should().Contain(
            "CombatPoint.AddCombatPoint(activator, hostile, SkillType.Leadership, 2);",
            "Break Morale must credit the enemies it debuffs, not only enemies the leader already hit");
    }

    [Test]
    public void HealingAbilities_AwardSupportCombatPoints()
    {
        var healingAbilities = ReadSources("AbilityDefinition")
            .Where(ability => ability.Source.Contains(".IsHealingAbility()"))
            .ToList();

        healingAbilities.Select(ability => ability.Name).Should().Contain(new[]
        {
            "BenevolenceAbilityDefinition.cs",
            "RenewalAbilityDefinition.cs",
            "InnervateAbilityDefinition.cs",
            "MedKitAbilityDefinition.cs",
        });

        foreach (var (name, source) in healingAbilities)
        {
            (source.Contains("CombatPoint.AddSupportCombatPoint(") ||
             source.Contains("FirstAidTreatmentAdjustments.GrantCombatPoint("))
                .Should().BeTrue($"{name} heals allies, so the enemies fighting them must award the healer XP");
        }
    }

    private static List<(string Name, string Source)> ReadSources(string featureFolder)
    {
        var directory = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Feature", featureFolder);
        return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Select(file => (Path.GetFileName(file), File.ReadAllText(file)))
            .ToList();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
