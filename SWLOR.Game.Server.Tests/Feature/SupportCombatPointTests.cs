using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class SupportCombatPointTests
{
    [Test]
    public void ShipSupportModules_CreditEnemiesInTheFight()
    {
        var shipModules = ReadSources("ShipModuleDefinition");

        shipModules.Where(module => module.Source.Contains("AddCombatPointToAllTagged")).Select(module => module.Name)
            .Should().BeEmpty("enemies the pilot already hit pay full Piloting XP, so tag-only credit makes support modules award nothing");

        var supportModules = shipModules
            .Where(module => module.Source.Contains("Space.RestoreHull(") ||
                             module.Source.Contains("Space.RestoreShield(") ||
                             module.Source.Contains("Space.RestoreCapacitor(") ||
                             module.Name == "CapitalEwarModuleDefinition.cs")
            .ToList();

        supportModules.Select(module => module.Name).Should().Contain(new[]
        {
            "HullRepairerModuleDefinition.cs",
            "ShieldRepairerModuleDefinition.cs",
            "RepairFieldGeneratorModuleDefinition.cs",
            "BulwarkShieldGeneratorModuleDefinition.cs",
            "HypermatterInjectorModuleDefinition.cs",
            "CapitalEwarModuleDefinition.cs",
        });

        foreach (var (name, source) in supportModules)
        {
            source.Should().Contain(
                "CombatPoint.AddSupportCombatPoint(activator,",
                $"{name} supports ships in a fight, so the enemies in that fight must award the pilot Piloting XP");
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
            .Where(ability => ability.Name.EndsWith("AbilityDefinition.cs") &&
                              (ability.Source.Contains(".IsHealingAbility()") ||
                               ability.Source.Contains("ScheduleFriendlyZoneHealing(")))
            .ToList();

        healingAbilities.Select(ability => ability.Name).Should().Contain(new[]
        {
            "BenevolenceAbilityDefinition.cs",
            "RenewalAbilityDefinition.cs",
            "ForceSanctuaryAbilityDefinition.cs",
            "InnervateAbilityDefinition.cs",
            "MedKitAbilityDefinition.cs",
            "ResuscitationAbilityDefinition.cs",
        });

        foreach (var (name, source) in healingAbilities)
        {
            source.Should().MatchRegex(
                @"(CombatPoint\.Add(Revive)?SupportCombatPoint|FirstAidTreatmentAdjustments\.GrantCombatPoint)\(",
                $"{name} heals allies, so the enemies fighting them must award the healer XP");
        }
    }

    [Test]
    public void HealingOverTime_CreditsEnemiesOnEachTick()
    {
        var regenerativeHeals = Regex.Matches(
                string.Join("\n", ReadSources("AbilityDefinition").Select(ability => ability.Source)),
                @"new RegenerativeHealingStatusEffect\([^;]*?\),")
            .Select(match => match.Value)
            .ToList();

        regenerativeHeals.Should().NotBeEmpty();
        foreach (var construction in regenerativeHeals)
        {
            construction.Should().MatchRegex(
                @"SkillType\.\w+, (supportCredited: )?credited",
                "a heal over time keeps healing after the cast, so its ticks must credit enemies that engage the patient later");
        }
    }

    [Test]
    public void SupportRiders_ReportTheirRecipients()
    {
        var abilities = ReadSources("AbilityDefinition");
        var riderCallers = abilities
            .Where(ability => ability.Name.EndsWith("AbilityDefinition.cs") &&
                              (ability.Source.Contains("ForceControlHealingEffects.ApplyRestorativeControlPower(") ||
                               ability.Source.Contains("LeadershipAbilityEffects.ApplyBolsterResolve(")))
            .ToList();

        riderCallers.Select(ability => ability.Name).Should().Contain(new[]
        {
            "BenevolenceAbilityDefinition.cs",
            "RenewalAbilityDefinition.cs",
            "ForceSanctuaryAbilityDefinition.cs",
            "RousingShoutAbilityDefinition.cs",
            "CleanseOrderAbilityDefinition.cs",
        });

        foreach (var (name, source) in riderCallers)
        {
            source.Should().NotMatchRegex(
                @"(?m)^\s*(ForceControlHealingEffects\.ApplyRestorativeControlPower|LeadershipAbilityEffects\.ApplyBolsterResolve)\(",
                $"{name} must include the allies its rider heals or buffs in its support credit");
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
