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
        definition.Should().Contain("\"Weapon Acc.\", model => model.WeaponAccuracy");
        definition.Should().Contain("\"Force Acc.\", model => model.ForceAccuracy");
        viewModel.Should().Contain("public int WeaponAccuracy");
        viewModel.Should().Contain("public int ForceAccuracy");
        viewModel.Should().Contain("WeaponAccuracy = Stat.GetAccuracy(_target, mainHand, accuracyStatOverride, SkillType.Invalid);");
        viewModel.Should().Contain("ForceAccuracy = Stat.GetAccuracy(_target, forceAccuracyWeapon, AbilityType.Willpower, SkillType.Force);");
        definition.Should().Contain("\"TYPE\", 90f, \"Resistance family.\"");
        definition.Should().Contain("model => model.ResistanceNames");
        definition.Should().Contain("model => model.ResistanceScores");
        definition.Should().Contain("model => model.ResistanceDamageTaken");
        definition.Should().Contain("model => model.ResistanceStatusDurations");
    }

    [Test]
    public void CharacterSheet_DisplaysConditionalRangedEvasionAdjustment()
    {
        var root = FindRepositoryRoot();
        var viewModel = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterSheetViewModel.cs"));

        viewModel.Should().Contain("AddStat(\"Ranged Evasion\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.RangedEvasionPercentAdjustment))");
        viewModel.Should().Contain("Negative values slow attacks.");
    }

    [Test]
    public void PlayerDamageRefresh_RunsDamageTakenEffectsBeforeRefreshingSheet()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PlayerStatusWindow.cs"));
        var method = ExtractMethod(source, "public static void PlayerDamaged()");
        var sharedDamageEffects = "Combat.ApplyDamageTakenEffects(player, GetLastDamager(player), GetTotalDamageDealt());";
        var sheetRefresh = "Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));";

        method.Should().Contain(sharedDamageEffects);
        method.Should().NotContain("ExecuteScript(");
        method.Should().Contain(sheetRefresh);
        method.IndexOf(sharedDamageEffects, StringComparison.Ordinal)
            .Should()
            .BeLessThan(method.IndexOf(sheetRefresh, StringComparison.Ordinal));
    }

    [Test]
    public void CharacterSheet_PlayerStatusRefreshesCombatStats()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterSheetViewModel.cs"));
        var method = ExtractMethod(source, "void IGuiRefreshable<PlayerStatusRefreshEvent>.Refresh(PlayerStatusRefreshEvent payload)");

        method.Should().Contain("RefreshStats();");
        method.Should().Contain("RefreshEquipmentStats();");
        method.IndexOf("RefreshStats();", StringComparison.Ordinal)
            .Should()
            .BeLessThan(method.IndexOf("RefreshEquipmentStats();", StringComparison.Ordinal));
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        openBraceIndex.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
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
