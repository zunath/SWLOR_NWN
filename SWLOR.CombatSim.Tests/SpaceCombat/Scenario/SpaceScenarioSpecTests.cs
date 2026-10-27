using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.SpaceCombat.Report;
using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.Tests.SpaceCombat.Scenario;

[TestFixture]
public class SpaceScenarioSpecTests
{
    private static string Write(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"space-scenario-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static void ShouldBeRejected(Action<SpaceScenarioSpec> spoil, string message)
    {
        var scenario = SpaceTestShips.Scenario(SpaceTestShips.Ship("A", weapons: SpaceTestShips.Laser()), SpaceTestShips.Ship("B"));
        spoil(scenario);

        var act = () => SpaceScenarioValidator.Validate(scenario);

        act.Should().Throw<InvalidDataException>().WithMessage($"*{message}*");
    }

    [Test]
    public void ShippedSpaceScenarios_LoadAndRun()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Scenarios", "Space");
        var files = Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.json").ToList() : new List<string>();

        files.Should().NotBeEmpty();
        foreach (var file in files)
        {
            var scenario = SpaceScenarioSpec.Load(file);
            SpaceBatchRunner.Run(scenario, runs: 5, seed: 1).Runs.Should().Be(5);
        }
    }

    [Test]
    public void Load_ReadsEnumsAndNamesWithoutRegardToCase()
    {
        var path = Write("""
            { "name": "x", "attackers": { "ships": [ { "name": "A", "size": "medium", "maxHull": 10,
                "weapons": [ { "name": "W", "family": "launcher", "damageType": "explosive", "shieldBypass": true } ] } ] },
              "defenders": { "passive": true, "ships": [ { "name": "B", "maxHull": 10 } ] } }
            """);
        try
        {
            var scenario = SpaceScenarioSpec.Load(path);

            scenario.Attackers.Ships[0].Size.Should().Be(ShipSize.Medium);
            scenario.Attackers.Ships[0].Weapons[0].Family.Should().Be(ModuleFamily.Launcher);
            scenario.Attackers.Ships[0].Weapons[0].DamageType.Should().Be(SpaceDamageType.Explosive);
            scenario.Defenders.Passive.Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Load_WithAMisspelledKey_Fails()
    {
        var path = Write("""{ "name": "x", "attackerz": {} }""");
        try
        {
            var act = () => SpaceScenarioSpec.Load(path);

            act.Should().Throw<InvalidDataException>().WithMessage("*attackerz*");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Validate_RejectsNumbersTheSimulationCannotUse()
    {
        ShouldBeRejected(s => s.Defenders.Ships.Clear(), "defenders needs at least one ship");
        ShouldBeRejected(s => s.TimeStepSeconds = 0, "timeStepSeconds");
        ShouldBeRejected(s => s.SizeDamagePenaltyPerStep = 1.5, "sizeDamagePenaltyPerStep");
        ShouldBeRejected(s => s.Attackers.Ships[0].MaxHull = 0, "maxHull");
        ShouldBeRejected(s => s.Attackers.Ships[0].Weapons[0].CycleSeconds = 0, "cycleSeconds");
        ShouldBeRejected(s => s.Attackers.Ships[0].Weapons[0].Rank = 51, "rank");
        ShouldBeRejected(s => s.Attackers.Ships[0].Weapons[0].ShieldDamageMultiplier = 0, "shieldDamageMultiplier");
        ShouldBeRejected(s => s.Attackers.Ships[0].Resistances.EM = 101, "resistance");
        ShouldBeRejected(s => s.Attackers.Ships[0].Abilities.Add(new BurstAbilitySpec { Name = "Burst", Weapon = "Missing" }), "borrows weapon 'Missing'");
    }
}
