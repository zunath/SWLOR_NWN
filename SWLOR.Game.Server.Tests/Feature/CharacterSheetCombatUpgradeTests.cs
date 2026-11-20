using SWLOR.Game.Server.Tests.Support;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class CharacterSheetCombatUpgradeTests
{
    [Test]
    public void ClosedCharacterSheet_RejectsItsPendingHitPointRefreshBeforeReadingTheEngine()
    {
        var viewModel = TestContent.WithoutServices.Create<CharacterSheetViewModel>();
        viewModel.OnWindowClosed().Invoke();

        // An old callback can arrive after the same view model has been reopened.
        // It must exit before any native calls, which are unavailable in this test.
        var refresh = typeof(CharacterSheetViewModel).GetMethod(
            "RefreshLiveHitPoints", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Action invokeOldRefresh = () => refresh.Invoke(viewModel, new object[] { 0 });

        invokeOldRefresh.Should().NotThrow();
    }

    [Test]
    public void CompanionResources_RefreshOpenTargetSheetsAndReportGainsToTheirOwner()
    {
        var root = FindRepositoryRoot().FullName;
        var viewModel = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "CharacterSheetViewModel.cs"));
        var refresh = ExtractMethod(viewModel, "void IGuiRefreshable<PlayerStatusRefreshEvent>.Refresh");
        refresh.Should().NotContain("GetIsPC(_target)", "inspected beasts must receive resource updates");
        refresh.Should().Contain("RefreshStats()");

        var publisher = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Feature", "PlayerStatusWindow.cs"));
        foreach (var method in new[] { "public static void PlayerFPAdjusted()", "public static void PlayerSTMAdjusted()" })
            ExtractMethod(publisher, method).Should().Contain("Gui.PublishCharacterSheetRefreshEvent(player");

        var feedback = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Domain", "Foundation", "PlayerFeedbackService.cs"));
        var restore = ExtractMethod(feedback, "public void SendResourceRestored(");
        restore.Should().Contain("GetMaster(creature)");
        restore.Should().Contain("SendMessageToPC(receiver");
        restore.Should().Contain("GetDisplayName(receiver, creature)");

        var stats = StatSource.Read();
        var regeneration = ExtractMethod(stats, "private void RestoreNPCStats(");
        regeneration.Should().Contain("if (fp != previousFP)");
        regeneration.Should().Contain("ExecuteScript(\"pc_fp_adjusted\", self)");
        regeneration.Should().Contain("if (stm != previousSTM)");
        regeneration.Should().Contain("ExecuteScript(\"pc_stm_adjusted\", self)");
        regeneration.Should().NotContain("SendResourceRestored", "natural regeneration stays silent");
    }

    [Test]
    public void ForceAttackPercentage_UsesTheSameCombinedModifierAsCombat()
    {
        var root = FindRepositoryRoot().FullName;
        var viewModel = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "CharacterSheetViewModel.cs"));
        viewModel.Should().Contain("CombatRating.GetAttackPercentAdjustment(_target, SkillType.Force)");
        var stats = StatSource.Read();
        ExtractMethod(stats, "private int ApplyPostAttackStatusModifiers(")
            .Should().Contain("GetAttackPercentAdjustment(creature, skillType)");
    }

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
        var ratings = ReadWindowSource("CharacterSheetWeaponRatings.cs");
        ratings.Should().Contain("host.PhysicalDefense = CombatRating.GetDefense(host.Target, CombatDamageType.Physical, AbilityType.Vitality);");
        ratings.Should().Contain("host.ForceDefense = CombatRating.GetDefense(host.Target, CombatDamageType.Force, AbilityType.Willpower);");
        viewModel.Should().NotContain("StatusResistances");
        viewModel.Should().NotContain("DefenseElemental");

        definition.Should().Contain("\"Physical DEF\", model => model.PhysicalDefense");
        definition.Should().Contain("\"Force DEF\", model => model.ForceDefense");
        definition.Should().Contain("\"Main-hand Acc.\", model => model.WeaponAccuracy");
        definition.Should().Contain("\"Off-hand Acc.\", model => model.OffHandAccuracy");
        definition.Should().Contain("Weapon bonuses do not combine.");
        definition.Should().Contain("\"Force Acc.\", model => model.ForceAccuracy");
        viewModel.Should().Contain("public int WeaponAccuracy");
        viewModel.Should().Contain("public int ForceAccuracy");
        ratings.Should().Contain("host.WeaponAccuracy = CombatRating.GetAccuracy(host.Target, mainHand, accuracyStatOverride, SkillType.Invalid);");
        viewModel.Should().Contain("public string OffHandAccuracy");
        ReadWindowSource("CharacterSheetWeaponTiming.cs").Should().Contain("return GetIsObjectValid(weapon) && Item.IsAttackWeaponType(GetBaseItemType(weapon)) ? weapon : OBJECT_INVALID;");
        ratings.Should().Contain("var hasOffhandAttack = GetIsObjectValid(offhandAttackWeapon);");
        ratings.Should().Contain("host.OffHandAccuracy = hasOffhandAttack ? offhandAccuracy.ToString() : \"-\";", "an empty off-hand, inactive companion or shield must display a dash");
        ratings.Should().Contain("ignoreWeaponAccuracyStatOverride: true");
        definition.Should().Contain("\"TYPE\", 90f, \"Resistance family.\"");
        definition.Should().Contain("model => model.ResistanceNames");
        definition.Should().Contain("model => model.ResistanceScores");
        definition.Should().Contain("model => model.ResistanceDamageTaken");
        definition.Should().Contain("model => model.ResistanceStatusDurations");
    }

    [Test]
    public void CrossSkillPreview_ReadsEachHandsOrdinaryProfileWithoutConsumingCombatState()
    {
        var ratings = ReadWindowSource("CharacterSheetWeaponRatings.cs");
        var estimate = ReadWindowSource("CharacterSheetDamageEstimate.cs");
        var refresh = ExtractMethod(ratings, "public void Refresh(ICharacterSheetHost host)")
                      + ExtractMethod(estimate, "public (string, string) GetCombatInfo(ICharacterSheetHost host, uint item)");
        refresh.Should().Contain("host.WeaponAccuracy += PairedRangedTraits.GetOrdinaryAccuracy(host.Target)");
        refresh.Should().Contain("offhandAccuracy += PairedRangedTraits.GetOrdinaryAccuracy(host.Target)");
        refresh.Should().Contain("GetEquipmentDamageAbility(host.Target, item)");
        refresh.Should().Contain("PairedRangedTraits.GetOrdinaryDamagePercentAdjustment(host.Target, GetAttackTarget(host.Target))");
        refresh.Should().Contain("CombatDamageModifiers.ApplyOrdinaryAttackDamageMultiplier(host.Target, min)");
        refresh.Should().Contain("CombatDamageModifiers.ApplyOrdinaryAttackDamageMultiplier(host.Target, max)");
        refresh.Should().Contain("StatType.OrdinaryAttackDamagePercentAdjustment");
        refresh.Should().NotContain("if (skill == SkillType.TwinPistol)\n                {", "ordinary trait bonuses must apply across weapon skills");
        refresh.Should().NotContain("CaptureOrdinary", "opening the sheet cannot capture or commit an attack");
        refresh.Should().NotContain("RecordOrdinary");
        refresh.Should().NotContain("Consume");
        ExtractMethod(estimate, "public AbilityType GetEquipmentDamageAbility(")
            .Should().Contain("ItemPropertyType.DamageStat");
    }

    [Test]
    public void OffHandWeapon_AddsItsValuesToTheExistingAttackDelayAndCriticalRateFields()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature",
            "GuiDefinition", "ViewModel", "CharacterSheetViewModel.cs"));
        var definition = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature",
            "GuiDefinition", "CharacterSheetDefinition.cs"));

        var timing = ReadWindowSource("CharacterSheetWeaponTiming.cs");
        var refresh = ExtractMethod(ReadWindowSource("CharacterSheetWeaponRatings.cs"), "public void Refresh(ICharacterSheetHost host)");
        refresh.Should().Contain("var offhandAttackWeapon = CharacterSheetWeaponTiming.GetOffhandAttackWeapon(host);");
        refresh.Should().Contain("host.AttackTooltip = FormatOffHandTooltip(host.AttackDescription, hasOffhandAttack");
        refresh.Should().Contain("CombatRating.GetAttack(host.Target, CharacterSheetDamageEstimate.GetEquipmentDamageAbility(host.Target, offhandAttackWeapon),");
        refresh.Should().Contain("Skill.GetWeaponSkill(host.Target, offhandAttackWeapon)).ToString()");
        ExtractMethod(timing, "public uint GetOffhandAttackWeapon(ICharacterSheetHost host)")
            .Should().Contain("TwinPistolEquipment.GetCompanionWeapon(host.Target)")
            .And.Contain("EquipmentPredicates.GetOffhandAttackWeapon(host.Target)");
        var stats = ExtractMethod(source, "private void RefreshCharacterStatsList()");
        stats.Should().Contain("AddStat(\"Critical Rate\", FormatPercent(GetCriticalRate(combatProfile.Skill)), FormatOffHandTooltip(");
        stats.Should().Contain("GetIsObjectValid(offhandWeapon) ? FormatPercent(GetCriticalRate(Skill.GetWeaponSkill(_target, offhandWeapon))) : null");
        stats.Should().NotContain("AddStat(\"Off-hand", "off-hand values join the existing rows instead of adding rows");
        ExtractMethod(timing, "public (string Value, string Tooltip) GetAttackDelayInfo(ICharacterSheetHost host)")
            .Should().Contain("if (mainHandDelay > 0 && offHandDelay > 0)");

        definition.Should().Contain("\"Attack\", model => model.Attack, CharacterSheetViewModel.AttackDescription, model => model.AttackTooltip");
    }

    [Test]
    public void CombatPanel_KeepsItsExistingRowsInOrder()
    {
        var definition = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Feature",
            "GuiDefinition", "CharacterSheetDefinition.cs"));
        var labels = System.Text.RegularExpressions.Regex.Matches(definition, "AddBoundValueRow\\(combatCol, \"([^\"]+)\"")
            .Select(match => match.Groups[1].Value);

        labels.Should().Equal("Main Hand", "Off Hand", "Atk Delay", "Attack", "Force Attack", "Main-hand Acc.",
            "Off-hand Acc.", "Force Acc.", "Evasion", "Physical DEF", "Force DEF");
    }

    [TestCase("Increases crits.", "7%", "Increases crits. Off hand: 7%.")]
    [TestCase("Increases crits.", null, "Increases crits.")]
    [TestCase("Increases crits.", "", "Increases crits.")]
    public void OffHandTooltip_AppendsTheOffHandValueOnlyWhenThereIsOne(string description, string offHand, string expected)
    {
        CharacterSheetFormat.FormatOffHandTooltip(description, offHand).Should().Be(expected);
    }

    [TestCase(220, 220, 0, 3833, " | 2 attacks per cycle, about 1.92s each (Delay 220 main + 220 off)")]
    [TestCase(230, 220, 30, 3258, " | 2 attacks per cycle, about 1.63s each (Delay 230 main + 220 off, off hand -30%)")]
    public void PairedDelayTooltip_ShowsEachHandAndTheTimePerAttack(int main, int off, int haste, int effective, string expected)
    {
        var tooltip = CharacterSheetFormat.FormatPairedDelay(main, off, haste, effective);
        tooltip.Should().Be(expected);
        tooltip.Should().NotContain("\n", "NUI tooltips render a line break as a question mark");
    }

    [Test]
    public void AccuracyWeaponSelection_UsesMainHandWithOffHandFallback()
    {
        var selectWeapon = typeof(CharacterSheetWeaponTiming).GetMethod(
            "SelectForceAccuracyWeapon",
            BindingFlags.Public | BindingFlags.Static);

        selectWeapon.Should().NotBeNull();
        ((uint)selectWeapon!.Invoke(null, new object[] { 11u, 22u, true })!).Should().Be(11u);
        ((uint)selectWeapon.Invoke(null, new object[] { 11u, 22u, false })!).Should().Be(22u);
    }

    [Test]
    public void BeastWeaponAccuracy_ReadsTheClawSoSkinAccuracyCountsOnce()
    {
        var refresh = ExtractMethod(ReadWindowSource("CharacterSheetWeaponRatings.cs"), "public void Refresh(ICharacterSheetHost host)");
        var beastBranch = ExtractMethod(refresh, "if (BeastMastery.IsPlayerBeast(host.Target))");

        beastBranch.Should().Contain("mainHand = GetItemInSlot(InventorySlot.CreatureLeft, host.Target);");
        beastBranch.Should().NotContain("InventorySlot.CreatureArmor",
            "CombatRating.GetAccuracy already adds the skin's Accuracy through the NPC stats");
    }

    [Test]
    public void ForceAccuracy_IgnoresWeaponStatOverrideButRetainsWeaponAccuracyBonus()
    {
        var applyItemProperty = typeof(CombatRatingService).GetMethod(
            "ApplyAccuracyItemProperty",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var service = RuntimeHelpers.GetUninitializedObject(typeof(CombatRatingService));

        applyItemProperty.Should().NotBeNull();

        var weaponStatOverride = ((AbilityType StatOverride, int AccuracyBonus))applyItemProperty!.Invoke(
            service,
            new object[] { AbilityType.Willpower, 7, ItemPropertyType.AccuracyStat, (int)AbilityType.Agility, false })!;
        weaponStatOverride.Should().Be((AbilityType.Agility, 7));

        var forceStatOverride = ((AbilityType StatOverride, int AccuracyBonus))applyItemProperty.Invoke(
            service,
            new object[] { AbilityType.Willpower, 7, ItemPropertyType.AccuracyStat, (int)AbilityType.Agility, true })!;
        forceStatOverride.Should().Be((AbilityType.Willpower, 7));

        var forceAccuracyBonus = ((AbilityType StatOverride, int AccuracyBonus))applyItemProperty.Invoke(
            service,
            new object[] { AbilityType.Willpower, 7, ItemPropertyType.Accuracy, 4, true })!;
        forceAccuracyBonus.Should().Be((AbilityType.Willpower, 11));

        // The retired native properties no longer grant accuracy; the item migration converts them.
        foreach (var legacyType in new[] { ItemPropertyType.AccuracyBonus, ItemPropertyType.EnhancementBonus })
        {
            var legacyBonus = ((AbilityType StatOverride, int AccuracyBonus))applyItemProperty.Invoke(
                service,
                new object[] { AbilityType.Willpower, 7, legacyType, 5, true })!;
            legacyBonus.Should().Be((AbilityType.Willpower, 7));
        }
    }

    [Test]
    public void ForceHitChecks_UseTheSameWillpowerAccuracyModeAsTheCharacterSheet()
    {
        var combat = CombatSource.Read();
        var method = ExtractMethod(combat, "private static int GetAbilityAccuracy(");

        method.Should().Contain("var usesForceAccuracy = skillType == SkillType.Force;");
        method.Should().Contain("usesForceAccuracy ? AbilityType.Willpower : statOverride");
        method.Should().Contain("ignoreWeaponAccuracyStatOverride: usesForceAccuracy");
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
    public void CharacterSheet_IncludesLimitedHasteAndLeadershipDamageReduction()
    {
        var viewModel = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterSheetViewModel.cs"));

        var attackDelay = ExtractMethod(ReadWindowSource("CharacterSheetWeaponTiming.cs"), "public (string Value, string Tooltip) GetAttackDelayInfo(ICharacterSheetHost host)");
        attackDelay.Should().Contain("WeaponAttackTiming.GetTimingSkill(host.Target)");
        attackDelay.Should().Contain("StatusEffectCombatEvent.TryGetLimitedAttackDelayReduction(");
        attackDelay.Should().Contain("limitedAttackDelayReductionPercent");
        attackDelay.Should().Contain("CombatAttackTiming.CalculateAttackDelay(");

        var damageTaken = ExtractMethod(viewModel, "private int GetDamageTakenPercent(CombatDamageType damageType)");
        damageTaken.Should().Contain("StatType.LeadershipPhysicalDamageTakenPercentAdjustment");
        damageTaken.Should().Contain("StatType.LeadershipForceDamageTakenPercentAdjustment");
        damageTaken.Should().Contain("StatType.LeadershipOtherDamageTakenPercentAdjustment");
        damageTaken.Should().Contain("StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment");
        damageTaken.Should().Contain("StatType.LeadershipRecoveryForceDamageTakenPercentAdjustment");
        damageTaken.Should().Contain("percent = ApplyDamageTakenPercentAdjustment(percent, leadershipAdjustment);");
        damageTaken.Should().NotContain("+ otherLeadershipAdjustment");
        damageTaken.IndexOf("ApplyDamageTakenPercentAdjustment(100, typeAdjustment)", StringComparison.Ordinal)
            .Should().BeLessThan(
                damageTaken.IndexOf("ApplyDamageTakenPercentAdjustment(percent, leadershipAdjustment)", StringComparison.Ordinal));
        damageTaken.IndexOf("ApplyDamageTakenPercentAdjustment(percent, leadershipAdjustment)", StringComparison.Ordinal)
            .Should().BeLessThan(
                damageTaken.IndexOf("Stat.GetStatAdjustment(_target, StatType.DamageTakenPercentAdjustment)", StringComparison.Ordinal),
                "every Leadership channel must remain a separate multiplicative stage before the generic adjustment");

        var applyAdjustment = typeof(CharacterSheetViewModel).GetMethod(
            "ApplyDamageTakenPercentAdjustment",
            BindingFlags.Static | BindingFlags.NonPublic);
        applyAdjustment.Should().NotBeNull();
        var afterTypedAdjustment = (int)applyAdjustment!.Invoke(null, new object[] { 100, -20 })!;
        var afterLeadershipAdjustment = (int)applyAdjustment.Invoke(null, new object[] { afterTypedAdjustment, -20 })!;
        afterLeadershipAdjustment.Should().Be(64,
            "the sheet must mirror runtime's separate multiplicative typed and Leadership stages");

        var criticalRate = ExtractMethod(viewModel, "private int GetCriticalRate(SkillType skillType)");
        criticalRate.Should().Contain("GameServices.CombatDamageModifiers.GetSkillCriticalRatePercentAdjustment(_target, skillType)");
        criticalRate.Should().NotContain("GetPersistentNextSkillAbilityCriticalRateBonus");
        viewModel.Should().Contain("AddStat(\"Next Ability Crit\"");
        viewModel.Should().Contain("GameServices.CombatAbilityBonus.GetPersistentNextSkillAbilityCriticalRateBonus(_target, combatProfile.Skill)");
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
        var sharedDamageEffects = "GameServices.CombatDamageTaken.ApplyDamageTakenEffects(player, GetLastDamager(player), GetTotalDamageDealt());";
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

    private static string ReadWindowSource(string file) => File.ReadAllText(Path.Combine(
        FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Feature", "GuiDefinition", "CharacterSheetWindow", file));

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
