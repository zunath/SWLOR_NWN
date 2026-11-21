using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class CharacterSheetStatCoverageTests
{
    [Test]
    public void CharacterSheet_DisplaysPercentAdjustmentsForEveryFoldedCombatRating()
    {
        var rows = ReadRows();

        // Attack and Force Attack already surfaced their percent adjustments. Accuracy, Evasion and
        // Defense fold theirs into the Attributes tab the same way, so they belong on the Stats tab too.
        rows.Should().Contain("addStat(\"Accuracy %\", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.AccuracyPercentAdjustment))");
        rows.Should().Contain("addStat(\"Evasion %\", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.EvasionPercentAdjustment))");
        rows.Should().Contain("addStat(\"Physical DEF %\", FormatPercent(CombatRating.GetDefensePercentAdjustment(host.Target, CombatDamageType.Physical))");
        rows.Should().Contain("addStat(\"Force DEF %\", FormatPercent(CombatRating.GetDefensePercentAdjustment(host.Target, CombatDamageType.Force))");
    }

    [Test]
    public void CharacterSheet_DistinguishesAccuracyRatingFromDirectAbilityHitChance()
    {
        var rows = ReadRows();

        rows.Should().Contain(
            "Direct percentage-point change to hit chance for weapon-skill and Force-skill ability hit checks only. Does not affect Mimicry abilities or the underlying Accuracy rating.");
        rows.Should().Contain(
            "Percentage bonus or penalty applied to the underlying Accuracy rating for attacks and ability hit checks, including Force and Mimicry. It is not a direct percentage-point change to hit chance");
        rows.Should().Contain("already included in the Main-hand Accuracy, Off-hand Accuracy, and Force Accuracy ratings shown on the Attributes tab.");
    }

    [Test]
    public void CharacterSheet_DisplaysGlobalOutputAndSustainStats()
    {
        var rows = ReadRows();

        rows.Should().Contain("addStat(\"Damage Dealt\", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.DamageDealtPercentAdjustment))");
        rows.Should().Contain("addStat(\"Weapon/Force Damage\", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.WeaponAndForceDamageDealtPercentAdjustment))");
        rows.Should().Contain("addStat(\"Healing Received\", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.HealingReceivedPercentAdjustment))");
        rows.Should().Contain("addStat(\"FP Cost\", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.FPCostPercentAdjustment))");
        rows.Should().Contain("addStat(\"STM Cost\", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.AbilityStaminaCostPercentAdjustment))");
        rows.Should().Contain("addStat(\"Experience\", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.ExperiencePercentAdjustment))");
    }

    [Test]
    public void CharacterSheet_DisplaysOffHandHasteAlongsideMainHandHaste()
    {
        var rows = ReadRows();

        rows.Should().Contain("addStat(\"Haste\", FormatPercent(CombatAttackTiming.CalculateAttackDelayReduction(host.Target))");
        rows.Should().Contain("addStat(\"Off-Hand Haste\", FormatPercent(CombatAttackTiming.CalculateOffhandAttackDelayReduction(host.Target))");
    }

    [Test]
    public void CharacterSheet_DisplaysHighResourceAbilityBonusesAndTheirLiveState()
    {
        var rows = ReadRows();

        rows.Should().Contain("AddHighResourceAbilityDamageStats(host, addStat);");
        rows.Should().Contain("Stat.GetStatSources(host.Target, payload)");
        rows.Should().Contain("GroupBy(source => source[thresholdStat])");
        rows.Should().NotContain("\"Balanced Current\"");
        rows.Should().NotContain("\"Balanced Attunement\"");
        rows.Should().Contain("StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent");
        rows.Should().Contain("StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent");
        rows.Should().Contain("StatType.HighFPAndStaminaAbilityDamageBonus");
        rows.Should().Contain("StatType.HighFPAndStaminaAbilityDamagePercentAdjustment");
        rows.Should().Contain("CombatResources.IsCurrentFPAndStaminaAbovePercent(host.Target, threshold)");

    }

    [Test]
    public void CharacterSheet_DisplaysDetectionAndStealth()
    {
        var rows = ReadRows();

        rows.Should().Contain("addStat(\"Detection\", Stat.GetDetection(host.Target).ToString()");
        rows.Should().Contain("addStat(\"Stealth\", Stat.GetStealth(host.Target).ToString()");
    }

    [Test]
    public void CharacterSheet_RefreshesWhenTemporaryStatAdjustmentsChange()
    {
        var viewModel = ReadViewModel();
        var temporaryModifiers = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Domain",
            "Character",
            "TemporaryStatModifierService.cs"));

        viewModel.Should().Contain("IGuiRefreshable<StatAdjustmentRefreshEvent>");
        viewModel.Should().Contain("public void Refresh(StatAdjustmentRefreshEvent payload)");
        temporaryModifiers.Should().Contain("_gui.PublishRefreshEvent(creature, new StatAdjustmentRefreshEvent())");
        temporaryModifiers.Should().Contain("if (PurgeExpired(creature))");
    }

    [Test]
    public void CharacterSheet_RefreshesWhenStatusEffectStatsChange()
    {
        var viewModel = ReadViewModel();
        var statusEffects = StatusEffectSource.Read();

        viewModel.Should().Contain("IGuiRefreshable<StatusEffectReceivedRefreshEvent>");
        viewModel.Should().Contain("IGuiRefreshable<StatusEffectRemovedRefreshEvent>");
        viewModel.Should().Contain("public void Refresh(StatusEffectReceivedRefreshEvent payload)");
        viewModel.Should().Contain("public void Refresh(StatusEffectRemovedRefreshEvent payload)");
        statusEffects.Should().Contain(
            "_gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectReceivedRefreshEvent())");
        statusEffects.Should().Contain(
            "_gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectRemovedRefreshEvent())");
        statusEffects.Should().Contain("if (!isReplacement)");
        statusEffects.Should().Contain("isReplacement: true");
        statusEffects.Should().Contain("bool isReplacement = false");
        statusEffects.Should().MatchRegex(@"removeNativeEffect,\s+isReplacement\);");

        // The refresh itself (only sheets that are open and viewing the target) is covered by GuiServiceTests.
    }

    [Test]
    public void CharacterSheet_ReportsGuardReductionInsteadOfHardcodingIt()
    {
        var rows = ReadRows();

        // Guard reduction is adjustable via GuardDamageReductionPercentAdjustment, so the tooltip
        // must not claim a fixed 20% and the real value gets its own row.
        rows.Should().Contain("addStat(\"Guard Reduction\", FormatPercent(CombatDamageTaken.GetGuardDamageReductionPercent(host.Target))");
        rows.Should().NotContain("Chance to reduce damage by 20% and increase enmity gain.");
    }

    [Test]
    public void StatAndCombatServices_ExposeTheAccessorsTheCharacterSheetConsumes()
    {
        // The sheet reaches the stat service through GameServices.Stat, typed as its interface.
        var stat = StatSource.ReadInterfaces();
        var combat = CombatSource.Read();

        // The sheet must reuse these rather than re-deriving the shield bonus or the Guard clamp.
        stat.Should().Contain("int GetDefensePercentAdjustment(uint creature, CombatDamageType type);");
        combat.Should().Contain("public static int GetGuardDamageReductionPercent(uint defender)");
    }

    private static string ReadViewModel()
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterSheetViewModel.cs"));
    }

    private static string ReadRows()
    {
        return string.Concat(
            new[] { "Survival", "Accuracy", "Damage", "Utility" }.Select(rows => File.ReadAllText(Path.Combine(
                FindRepositoryRoot().FullName,
                "SWLOR.Game.Server",
                "Feature",
                "GuiDefinition",
                "CharacterSheetWindow",
                $"CharacterSheet{rows}Rows.cs"))));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null &&
               !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root containing SWLOR.Game.Server.sln must be discoverable");
        return directory;
    }
}
