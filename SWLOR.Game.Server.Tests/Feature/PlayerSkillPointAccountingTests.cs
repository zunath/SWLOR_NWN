using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerSkillPointAccountingTests
{
    [Test]
    public void TotalSkillPoints_IncludesStartingPointsAndEarnedSkillPoints()
    {
        var dbPlayer = new Player
        {
            TotalSPAcquired = Skill.SkillCap
        };

        Skill.GetTotalSkillPoints(dbPlayer).Should().Be(410);
        Skill.TotalSkillPointCap.Should().Be(410);
    }

    [Test]
    public void TotalSkillPoints_CapsEarnedSkillPointsAtSkillCap()
    {
        var dbPlayer = new Player
        {
            TotalSPAcquired = Skill.SkillCap + 25
        };

        Skill.GetTotalSkillPoints(dbPlayer).Should().Be(Skill.TotalSkillPointCap);
    }

    [Test]
    public void TotalSkillPoints_NeverDropsBelowStartingSkillPoints()
    {
        var dbPlayer = new Player
        {
            TotalSPAcquired = -25
        };

        Skill.GetTotalSkillPoints(dbPlayer).Should().Be(Skill.StartingSkillPoints);
    }

    [Test]
    public void TotalContributingSkillRanks_SumsCurrentContributingSkillRanks()
    {
        var dbPlayer = new Player();
        dbPlayer.Skills[SkillType.Armor] = new PlayerSkill { Rank = 20 };
        dbPlayer.Skills[SkillType.Piloting] = new PlayerSkill { Rank = -5 };
        dbPlayer.Skills[SkillType.Basic] = new PlayerSkill { Rank = 20 };
        dbPlayer.Skills[(SkillType)9999] = new PlayerSkill { Rank = 50 };

        Skill.GetTotalContributingSkillRanks(dbPlayer).Should().Be(20);
    }

    [TestCase(true, Skill.SkillCap - 1, false)]
    [TestCase(true, Skill.SkillCap, false)]
    [TestCase(true, Skill.SkillCap + 1, true)]
    [TestCase(false, Skill.SkillCap + 1, false)]
    public void SkillRankDecay_OnlyOccursAboveTheContributingSkillCap(
        bool contributesToSkillCap,
        int totalRanks,
        bool expected)
    {
        var method = typeof(Skill).GetMethod(
            "ShouldDecaySkillRank",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var result = (bool)method!.Invoke(null, new object[] { contributesToSkillCap, totalRanks })!;

        result.Should().Be(expected);
    }

    [Test]
    public void FullRebuild_ResetSeedsSpendableSPFromStartingAndEarnedSkillPoints()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterFullRebuildViewModel.cs"));
        var resetAction = ExtractMethod(source, "public Action OnClickResetEverything() => () =>");

        resetAction.Should().Contain("void ResetSkillPointPool()");
        resetAction.Should().Contain("dbPlayer.UnallocatedSP = Skill.GetTotalSkillPoints(dbPlayer);");
        resetAction.IndexOf("RefundAllPerks();", StringComparison.Ordinal)
            .Should()
            .BeLessThan(resetAction.IndexOf("ResetSkillPointPool();", StringComparison.Ordinal));
        resetAction.IndexOf("ResetSkillPointPool();", StringComparison.Ordinal)
            .Should()
            .BeLessThan(resetAction.IndexOf("RefundAllSkills();", StringComparison.Ordinal));
    }

    [Test]
    public void PlayerSPDisplays_UseTotalSpendableSkillPoints()
    {
        var root = FindRepositoryRoot();
        var characterSheet = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterSheetViewModel.cs"));
        var characterSheetDefinition = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "CharacterSheetDefinition.cs"));
        var perks = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PerksViewModel.cs"));

        characterSheetDefinition.Should().Contain("AddBoundValueRow(resourceCol, \"Ranks\", model => model.SkillRanks");
        characterSheetDefinition.Should().Contain("$\"Skill ranks contributing to the {Skill.SkillCap}-rank limit.\"");
        characterSheetDefinition.Should().NotContain("400-rank limit");
        characterSheet.Should().Contain("public bool ShowSkillRanks");
        characterSheet.Should().Contain("public string SkillRanks");
        characterSheet.Should().Contain("SkillRanks = $\"{Skill.GetTotalContributingSkillRanks(dbPlayer)} / {Skill.SkillCap}\";");
        characterSheet.Should().Contain("Skill.GetTotalSkillPoints(dbPlayer)} / {Skill.TotalSkillPointCap}");
        perks.Should().Contain("Total SP: {Skill.GetTotalSkillPoints(dbPlayer)} / {Skill.TotalSkillPointCap}");
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
