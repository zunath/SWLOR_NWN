using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class PerkStatBonusTests
{
    [Test]
    public void StatBonusAggregation_DoesNotUseNpcAbilityRankFallback()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Perk.cs"));
        var getStatBonus = ExtractMethod(source, "public static int GetStatBonus(uint creature, StatType stat)");
        var getTargetedStatBonus = ExtractMethod(source, "public static int GetTargetedStatBonus(");
        var cacheStatBonusPerk = ExtractMethod(source, "private static void CacheStatBonusPerk");
        var calculateStatBonuses = ExtractMethod(source, "private static int CalculateStatBonuses");
        var statBonusLevel = ExtractMethod(source, "private static int GetStatBonusPerkLevel");
        var getPerkLevel = ExtractMethod(source, "public static int GetPerkLevel(uint creature, PerkType perkType)");

        source.Should().Contain("_statBonusGroupsByStat");
        source.Should().Contain("_targetedStatBonusGroupsByAdjustmentStat");
        source.Should().NotContain("_perksByStatBonus");
        source.Should().NotContain("_perksWithStatBonuses");
        cacheStatBonusPerk.Should().Contain("perkDetail.StatBonuses");
        cacheStatBonusPerk.Should().Contain("foreach (var (level, perkLevel) in perkDetail.PerkLevels)");
        cacheStatBonusPerk.Should().Contain("GetOrCreateStatBonusGroup(statBonus.Stat, perkType, perkDetail)");
        cacheStatBonusPerk.Should().Contain("GetOrCreateTargetedStatBonusGroup(adjustmentStat, perkType)");

        getStatBonus.Should().Contain("_statBonusGroupsByStat.TryGetValue(stat, out var statBonusGroups)");
        getStatBonus.Should().Contain("foreach (var statBonusGroup in statBonusGroups)");
        getStatBonus.Should().Contain("foreach (var statBonus in statBonusGroup.PerkBonuses)");
        getStatBonus.Should().Contain("statBonusGroup.LevelBonuses.TryGetValue(level, out var levelBonuses)");
        getStatBonus.Should().NotContain("statBonus.Stat == stat");
        getStatBonus.Should().Contain("var level = GetStatBonusPerkLevel(creature, statBonusGroup.PerkType);");
        getStatBonus.Should().NotContain("var level = GetPerkLevel(creature, perkType);");

        getTargetedStatBonus.Should().Contain("_targetedStatBonusGroupsByAdjustmentStat.TryGetValue(adjustmentStatType, out var targetedStatBonusGroups)");
        getTargetedStatBonus.Should().Contain("foreach (var targetedStatBonusGroup in targetedStatBonusGroups)");
        getTargetedStatBonus.Should().Contain("CalculateStatBonuses(creature, bonusesByStat, primaryPerkStatType)");
        getTargetedStatBonus.Should().Contain("CalculateStatBonuses(creature, bonusesByStat, adjustmentStatType)");
        getTargetedStatBonus.Should().NotContain("foreach (var statBonus in perkLevel.StatBonuses)");
        getTargetedStatBonus.Should().NotContain("statBonus.Stat == primaryPerkStatType");
        getTargetedStatBonus.Should().Contain("var level = GetStatBonusPerkLevel(creature, targetedStatBonusGroup.PerkType);");
        getTargetedStatBonus.Should().NotContain("var level = GetPerkLevel(creature, perkType);");
        calculateStatBonuses.Should().Contain("bonusesByStat.TryGetValue(stat, out var statBonuses)");

        statBonusLevel.Should().Contain("return GetPerkLevel(creature, perkType);");
        statBonusLevel.Should().Contain("Droid.IsDroid(creature)");
        statBonusLevel.Should().Contain("BeastMastery.IsPlayerBeast(creature)");
        statBonusLevel.Should().Contain("return GetLocalInt(creature, $\"PERK_LEVEL_{(int)perkType}\");");
        statBonusLevel.Should().NotContain("perkMaxLevel");

        getPerkLevel.Should().Contain("return perkLevel > 0 ? perkLevel : perkMaxLevel;");
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
}
