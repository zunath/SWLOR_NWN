using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Perks;

public class CapstoneAbilityTests
{
    [Test]
    public void PlayerCapstones_ShareCooldownAndBypassRecastReduction()
    {
        var root = FindRepositoryRoot();
        var abilityRoot = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition");
        var capstoneFiles = Directory
            .GetFiles(abilityRoot, "*AbilityDefinition.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("RecastGroup.Capstone"))
            .ToArray();

        capstoneFiles.Should().HaveCount(34);

        foreach (var file in capstoneFiles)
        {
            var source = File.ReadAllText(file);
            source.Should().Contain("RecastGroup.Capstone");
            source.Should().Contain("CapstoneAbility.RecastDelaySeconds");
            source.Should().NotContain(".IsWeaponAbility()");
        }

        var beastmasterCapstones = Directory
            .GetFiles(Path.Combine(abilityRoot, "Beastmaster"), "*AbilityDefinition.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("RecastGroup.Capstone"));
        beastmasterCapstones.Should().BeEmpty();

        var usePerkFeat = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs"));
        usePerkFeat.Should().Contain("ShouldIgnoreRecastReduction(ability)");
        usePerkFeat.Should().Contain("ability?.RecastGroup == RecastGroup.Capstone");

        var recast = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Recast.cs"));
        recast.Should().Contain("if (group == RecastGroup.Capstone)");

        var cooldownVisual = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "AbilityCooldownVisual.cs"));
        cooldownVisual.Should().Contain("group != RecastGroup.Capstone");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
