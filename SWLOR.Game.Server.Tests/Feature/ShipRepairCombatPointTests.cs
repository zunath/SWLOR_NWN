using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class ShipRepairCombatPointTests
{
    [Test]
    public void ShipRepairModules_CreditEnemiesFightingTheRepairedShips()
    {
        var root = FindRepositoryRoot();
        var shipModuleDirectory = new DirectoryInfo(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ShipModuleDefinition"));

        var repairModules = shipModuleDirectory.GetFiles("*.cs")
            .Select(file => (file.Name, Source: File.ReadAllText(file.FullName)))
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
