using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class CharacterSheetStatCoverageTests
{
    [Test]
    public void CharacterSheet_DisplaysPercentAdjustmentsForEveryFoldedCombatRating()
    {
        var viewModel = ReadViewModel();

        // Attack and Force Attack already surfaced their percent adjustments. Accuracy, Evasion and
        // Defense fold theirs into the Attributes tab the same way, so they belong on the Stats tab too.
        viewModel.Should().Contain("AddStat(\"Accuracy %\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.AccuracyPercentAdjustment))");
        viewModel.Should().Contain("AddStat(\"Evasion %\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.EvasionPercentAdjustment))");
        viewModel.Should().Contain("AddStat(\"Physical DEF %\", FormatPercent(Stat.GetDefensePercentAdjustment(_target, CombatDamageType.Physical))");
        viewModel.Should().Contain("AddStat(\"Force DEF %\", FormatPercent(Stat.GetDefensePercentAdjustment(_target, CombatDamageType.Force))");
    }

    [Test]
    public void CharacterSheet_DistinguishesAccuracyRatingFromDirectAbilityHitChance()
    {
        var viewModel = ReadViewModel();

        viewModel.Should().Contain(
            "Direct percentage-point change to hit chance for weapon-skill and Force-skill ability hit checks only. Does not affect Mimicry abilities or the underlying Accuracy rating.");
        viewModel.Should().Contain(
            "Percentage bonus or penalty applied to the underlying Accuracy rating for attacks and ability hit checks, including Force and Mimicry. It is not a direct percentage-point change to hit chance");
        viewModel.Should().Contain("already included in the Weapon Accuracy and Force Accuracy ratings shown on the Attributes tab.");
    }

    [Test]
    public void CharacterSheet_DisplaysGlobalOutputAndSustainStats()
    {
        var viewModel = ReadViewModel();

        viewModel.Should().Contain("AddStat(\"Damage Dealt\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.DamageDealtPercentAdjustment))");
        viewModel.Should().Contain("AddStat(\"Weapon/Force Damage\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.WeaponAndForceDamageDealtPercentAdjustment))");
        viewModel.Should().Contain("AddStat(\"Healing Received\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.HealingReceivedPercentAdjustment))");
        viewModel.Should().Contain("AddStat(\"FP Cost\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.FPCostPercentAdjustment))");
        viewModel.Should().Contain("AddStat(\"STM Cost\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.AbilityStaminaCostPercentAdjustment))");
        viewModel.Should().Contain("AddStat(\"Experience\", FormatPercent(Stat.GetStatAdjustment(_target, StatType.ExperiencePercentAdjustment))");
    }

    [Test]
    public void CharacterSheet_DisplaysOffHandHasteAlongsideMainHandHaste()
    {
        var viewModel = ReadViewModel();

        viewModel.Should().Contain("AddStat(\"Haste\", FormatPercent(Combat.CalculateAttackDelayReduction(_target))");
        viewModel.Should().Contain("AddStat(\"Off-Hand Haste\", FormatPercent(Combat.CalculateOffhandAttackDelayReduction(_target))");
    }

    [Test]
    public void CharacterSheet_DisplaysHighResourceAbilityBonusesAndTheirLiveState()
    {
        var viewModel = ReadViewModel();

        viewModel.Should().Contain("AddHighResourceAbilityDamageStats(AddStat);");
        viewModel.Should().Contain("\"High-Resource Ability DMG\"");
        viewModel.Should().NotContain("\"Balanced Current\"");
        viewModel.Should().Contain("StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent");
        viewModel.Should().Contain("\"Balanced Attunement\"");
        viewModel.Should().Contain("StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent");
        viewModel.Should().Contain("StatType.HighFPAndStaminaAbilityDamageBonus");
        viewModel.Should().Contain("StatType.HighFPAndStaminaAbilityDamagePercentAdjustment");
        viewModel.Should().Contain("Combat.IsCurrentFPAndStaminaAbovePercent(_target, flatThreshold)");
        viewModel.Should().Contain("Combat.IsCurrentFPAndStaminaAbovePercent(_target, percentThreshold)");
        viewModel.Should().Contain("active ? $\"Active (+{flatBonus} DMG)\" : $\"Inactive ({flatThreshold}% required)\"");
        viewModel.Should().Contain("active ? $\"Active (+{percentBonus}% DMG)\" : $\"Inactive ({percentThreshold}% required)\"");
    }

    [Test]
    public void CharacterSheet_DisplaysDetectionAndStealth()
    {
        var viewModel = ReadViewModel();

        viewModel.Should().Contain("AddStat(\"Detection\", Stat.GetDetection(_target).ToString()");
        viewModel.Should().Contain("AddStat(\"Stealth\", Stat.GetStealth(_target).ToString()");
    }

    [Test]
    public void CharacterSheet_RefreshesWhenTemporaryStatAdjustmentsChange()
    {
        var viewModel = ReadViewModel();
        var temporaryModifiers = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "CombatService",
            "TemporaryStatModifier.cs"));

        viewModel.Should().Contain("IGuiRefreshable<StatAdjustmentRefreshEvent>");
        viewModel.Should().Contain("public void Refresh(StatAdjustmentRefreshEvent payload)");
        temporaryModifiers.Should().Contain("Gui.PublishRefreshEvent(creature, new StatAdjustmentRefreshEvent())");
        temporaryModifiers.Should().Contain("if (PurgeExpired(creature))");
    }

    [Test]
    public void CharacterSheet_RefreshesWhenStatusEffectStatsChange()
    {
        var viewModel = ReadViewModel();
        var statusEffects = ReadService("StatusEffect.cs");

        viewModel.Should().Contain("IGuiRefreshable<StatusEffectReceivedRefreshEvent>");
        viewModel.Should().Contain("IGuiRefreshable<StatusEffectRemovedRefreshEvent>");
        viewModel.Should().Contain("public void Refresh(StatusEffectReceivedRefreshEvent payload)");
        viewModel.Should().Contain("public void Refresh(StatusEffectRemovedRefreshEvent payload)");
        statusEffects.Should().Contain(
            "Gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectReceivedRefreshEvent())");
        statusEffects.Should().Contain(
            "Gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectRemovedRefreshEvent())");
        statusEffects.Should().Contain("if (!isReplacement)");
        statusEffects.Should().Contain("isReplacement: true");
        statusEffects.Should().Contain("bool isReplacement = false");
        statusEffects.Should().MatchRegex(@"removeNativeEffect,\s+isReplacement\);");

        var gui = ReadService("Gui.cs");
        gui.Should().Contain("public static void PublishCharacterSheetRefreshEvent<T>(uint target, T payload)");
        gui.Should().Contain("for (var observer = GetFirstPC(); GetIsObjectValid(observer); observer = GetNextPC())");
        gui.Should().Contain("!viewModel.IsViewingTarget(target)");
    }

    [Test]
    public void CharacterSheet_ReportsGuardReductionInsteadOfHardcodingIt()
    {
        var viewModel = ReadViewModel();

        // Guard reduction is adjustable via GuardDamageReductionPercentAdjustment, so the tooltip
        // must not claim a fixed 20% and the real value gets its own row.
        viewModel.Should().Contain("AddStat(\"Guard Reduction\", FormatPercent(Combat.GetGuardDamageReductionPercent(_target))");
        viewModel.Should().NotContain("Chance to reduce damage by 20% and increase enmity gain.");
    }

    [Test]
    public void StatAndCombatServices_ExposeTheAccessorsTheCharacterSheetConsumes()
    {
        var stat = ReadService("Stat.cs");
        var combat = ReadService("Combat.cs");

        // The sheet must reuse these rather than re-deriving the shield bonus or the Guard clamp.
        stat.Should().Contain("public static int GetDefensePercentAdjustment(uint creature, CombatDamageType type)");
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

    private static string ReadService(string fileName)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            fileName));
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
