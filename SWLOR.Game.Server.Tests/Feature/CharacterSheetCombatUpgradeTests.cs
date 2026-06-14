using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class CharacterSheetCombatUpgradeTests
{
    [Test]
    public void CharacterSheet_DisplaysDefenseAndResistanceAsSeparateSurfaces()
    {
        var root = FindRepositoryRoot();
        var viewModel = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterSheetViewModel.cs"));
        var definition = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "CharacterSheetDefinition.cs"));

        viewModel.Should().Contain("public int PhysicalDefense");
        viewModel.Should().Contain("public int ForceDefense");
        viewModel.Should().Contain("PhysicalDefense = Stat.GetDefense(_target, CombatDamageType.Physical, AbilityType.Vitality);");
        viewModel.Should().Contain("ForceDefense = Stat.GetDefense(_target, CombatDamageType.Force, AbilityType.Willpower);");
        viewModel.Should().NotContain("StatusResistances");
        viewModel.Should().NotContain("DefenseElemental");

        definition.Should().Contain("\"Physical DEF\", model => model.PhysicalDefense");
        definition.Should().Contain("\"Force DEF\", model => model.ForceDefense");
        definition.Should().Contain("\"TYPE\", 90f, \"Resistance family.\"");
        definition.Should().Contain("model => model.ResistanceNames");
        definition.Should().Contain("model => model.ResistanceScores");
        definition.Should().Contain("model => model.ResistanceDamageTaken");
        definition.Should().Contain("model => model.ResistanceStatusDurations");
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
