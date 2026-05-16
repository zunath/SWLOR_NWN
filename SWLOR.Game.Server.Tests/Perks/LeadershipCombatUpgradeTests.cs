using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Leadership;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LeadershipCombatUpgradeTests
{
    [Test]
    public void LeadershipBibleManifest_ContainsBatch()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText((root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv").FullName);
        var perkNames = new[]
        {
            "Rallying Standard I", "Press the Attack I", "Coordinated Focus I", "Mark Target I",
            "Charge Order I", "Press the Attack II", "Rallying Standard II", "Break Morale I",
            "Coordinated Focus II", "Command Radius I", "Mark Target II", "Charge Order II",
            "Press the Attack III", "Break Morale II", "Coordinated Focus III", "Command Radius II",
            "Decisive Command", "Watchful Presence I", "Rousing Shout I", "Steady Formation I",
            "Bolster Resolve I", "Field Recovery I", "Rousing Shout II", "Watchful Presence II",
            "Cleanse Order I", "Steady Formation II", "Triage Protocol I", "Bolster Resolve II",
            "Field Recovery II", "Rousing Shout III", "Cleanse Order II", "Watchful Presence III",
            "Triage Protocol II", "Hold the Line"
        };

        foreach (var perkName in perkNames)
        {
            manifest.Should().Contain($"\"{perkName}\"");
        }
    }

    [Test]
    public void LeadershipPerkLevels_MatchCombatBible()
    {
        var vanguard = BuildPerksWithout2daLookup(
            new LeadershipVanguardCommandPerkDefinition(),
            "RallyingStandard",
            "PressTheAttack",
            "CoordinatedFocus",
            "MarkTarget",
            "ChargeOrder",
            "BreakMorale",
            "CommandRadius",
            "DecisiveCommand");

        AssertPerkLevel(vanguard[PerkType.RallyingStandard], "Rallying Standard", 1, 2, null, FeatType.RallyingStandard1);
        AssertPerkLevel(vanguard[PerkType.PressTheAttack], "Press the Attack", 1, 2, 5, FeatType.PressTheAttack1);
        AssertPerkLevel(vanguard[PerkType.CoordinatedFocus], "Coordinated Focus", 1, 3, 8, FeatType.CoordinatedFocus1);
        AssertPerkLevel(vanguard[PerkType.MarkTarget], "Mark Target", 1, 3, 12, FeatType.MarkTarget1);
        AssertPerkLevel(vanguard[PerkType.ChargeOrder], "Charge Order", 1, 2, 15, FeatType.ChargeOrder1);
        AssertPerkLevel(vanguard[PerkType.PressTheAttack], "Press the Attack", 2, 3, 18, FeatType.PressTheAttack2);
        AssertPerkLevel(vanguard[PerkType.RallyingStandard], "Rallying Standard", 2, 3, 22, FeatType.RallyingStandard2);
        AssertPerkLevel(vanguard[PerkType.BreakMorale], "Break Morale", 1, 3, 25, FeatType.BreakMorale1);
        AssertPerkLevel(vanguard[PerkType.CoordinatedFocus], "Coordinated Focus", 2, 4, 28, FeatType.CoordinatedFocus2);
        AssertPerkLevel(vanguard[PerkType.CommandRadius], "Command Radius", 1, 3, 30, null,
            (StatType.LeadershipCommandRadiusBonusMeters, 2),
            (StatType.VanguardCommandDurationBonusBaseSeconds, 0),
            (StatType.VanguardCommandDurationBonusMaximumSeconds, 2));
        AssertPerkLevel(vanguard[PerkType.MarkTarget], "Mark Target", 2, 4, 35, FeatType.MarkTarget2);
        AssertPerkLevel(vanguard[PerkType.ChargeOrder], "Charge Order", 2, 3, 38, FeatType.ChargeOrder2);
        AssertPerkLevel(vanguard[PerkType.PressTheAttack], "Press the Attack", 3, 4, 40, FeatType.PressTheAttack3);
        AssertPerkLevel(vanguard[PerkType.BreakMorale], "Break Morale", 2, 4, 42, FeatType.BreakMorale2);
        AssertPerkLevel(vanguard[PerkType.CoordinatedFocus], "Coordinated Focus", 3, 4, 45, FeatType.CoordinatedFocus3);
        AssertPerkLevel(vanguard[PerkType.CommandRadius], "Command Radius", 2, 3, 48, null,
            (StatType.LeadershipCommandRadiusBonusMeters, 4),
            (StatType.VanguardCommandDurationBonusBaseSeconds, 2),
            (StatType.VanguardCommandDurationBonusMaximumSeconds, 4));
        AssertPerkLevel(vanguard[PerkType.DecisiveCommand], "Decisive Command", 1, 5, 50, FeatType.DecisiveCommand1);

        var steward = BuildPerksWithout2daLookup(
            new LeadershipFieldStewardPerkDefinition(),
            "WatchfulPresence",
            "RousingShout",
            "SteadyFormation",
            "BolsterResolve",
            "FieldRecovery",
            "CleanseOrder",
            "TriageProtocol",
            "HoldTheLine");

        AssertPerkLevel(steward[PerkType.WatchfulPresence], "Watchful Presence", 1, 2, null, FeatType.WatchfulPresence1);
        AssertPerkLevel(steward[PerkType.RousingShout], "Rousing Shout", 1, 2, 5, FeatType.RousingShout1);
        AssertPerkLevel(steward[PerkType.SteadyFormation], "Steady Formation", 1, 3, 8, FeatType.SteadyFormation1);
        AssertPerkLevel(steward[PerkType.BolsterResolve], "Bolster Resolve", 1, 3, 12, FeatType.BolsterResolve1);
        AssertPerkLevel(steward[PerkType.FieldRecovery], "Field Recovery", 1, 2, 15, FeatType.FieldRecovery1);
        AssertPerkLevel(steward[PerkType.RousingShout], "Rousing Shout", 2, 3, 18, FeatType.RousingShout2);
        AssertPerkLevel(steward[PerkType.WatchfulPresence], "Watchful Presence", 2, 3, 22, FeatType.WatchfulPresence2);
        AssertPerkLevel(steward[PerkType.CleanseOrder], "Cleanse Order", 1, 3, 25, FeatType.CleanseOrder1);
        AssertPerkLevel(steward[PerkType.SteadyFormation], "Steady Formation", 2, 4, 28, FeatType.SteadyFormation2);
        AssertPerkLevel(steward[PerkType.TriageProtocol], "Triage Protocol", 1, 3, 30, null,
            (StatType.FieldStewardTriageProtocolLevel, 1));
        AssertPerkLevel(steward[PerkType.BolsterResolve], "Bolster Resolve", 2, 4, 35, FeatType.BolsterResolve2);
        AssertPerkLevel(steward[PerkType.FieldRecovery], "Field Recovery", 2, 3, 38, FeatType.FieldRecovery2);
        AssertPerkLevel(steward[PerkType.RousingShout], "Rousing Shout", 3, 4, 40, FeatType.RousingShout3);
        AssertPerkLevel(steward[PerkType.CleanseOrder], "Cleanse Order", 2, 4, 42, FeatType.CleanseOrder2);
        AssertPerkLevel(steward[PerkType.WatchfulPresence], "Watchful Presence", 3, 4, 45, FeatType.WatchfulPresence3);
        AssertPerkLevel(steward[PerkType.TriageProtocol], "Triage Protocol", 2, 3, 48, null,
            (StatType.FieldStewardTriageProtocolLevel, 2),
            (StatType.FieldStewardDurationBonusSeconds, 2));
        AssertPerkLevel(steward[PerkType.HoldTheLine], "Hold the Line", 1, 5, 50, FeatType.HoldTheLine1);
    }

    [Test]
    public void LeadershipAbilities_MatchCombatBible()
    {
        var press = new PressTheAttackAbilityDefinition().BuildAbilities();
        AssertAbility(press[FeatType.PressTheAttack1], "Press the Attack I", 1, RecastGroup.PressTheAttack, 45f, 0f, 4, false, true, false, false);
        AssertAbility(press[FeatType.PressTheAttack2], "Press the Attack II", 2, RecastGroup.PressTheAttack, 45f, 0f, 6, false, true, false, false);
        AssertAbility(press[FeatType.PressTheAttack3], "Press the Attack III", 3, RecastGroup.PressTheAttack, 60f, 0f, 9, false, true, false, false);

        var mark = new MarkTargetAbilityDefinition().BuildAbilities();
        AssertAbility(mark[FeatType.MarkTarget1], "Mark Target I", 1, RecastGroup.MarkTarget, 45f, 1f, 5, true, false, true, true);
        AssertAbility(mark[FeatType.MarkTarget2], "Mark Target II", 2, RecastGroup.MarkTarget, 45f, 1f, 7, true, false, true, true);

        var breakMorale = new BreakMoraleAbilityDefinition().BuildAbilities();
        AssertAbility(breakMorale[FeatType.BreakMorale1], "Break Morale I", 1, RecastGroup.BreakMorale, 90f, 0.5f, 7, true, true, false, false);
        AssertAbility(breakMorale[FeatType.BreakMorale2], "Break Morale II", 2, RecastGroup.BreakMorale, 90f, 0.5f, 9, true, true, false, false);

        var decisive = new DecisiveCommandAbilityDefinition().BuildAbilities()[FeatType.DecisiveCommand1];
        AssertAbility(decisive, "Decisive Command", 1, RecastGroup.Capstone, 1800f, 1f, 15, false, true, false, false);

        var rousing = new RousingShoutAbilityDefinition().BuildAbilities();
        AssertAbility(rousing[FeatType.RousingShout1], "Rousing Shout I", 1, RecastGroup.RousingShout, 90f, 1f, 6, false, false, true, true);
        AssertAbility(rousing[FeatType.RousingShout2], "Rousing Shout II", 2, RecastGroup.RousingShout, 90f, 1f, 8, false, false, true, true);
        AssertAbility(rousing[FeatType.RousingShout3], "Rousing Shout III", 3, RecastGroup.RousingShout, 90f, 1f, 10, false, false, true, true);

        var bolster = new BolsterResolveAbilityDefinition().BuildAbilities();
        AssertAbility(bolster[FeatType.BolsterResolve1], "Bolster Resolve I", 1, RecastGroup.BolsterResolve, 45f, 1f, 4, false, true, false, false);
        AssertAbility(bolster[FeatType.BolsterResolve2], "Bolster Resolve II", 2, RecastGroup.BolsterResolve, 45f, 1f, 8, false, true, false, false);

        var cleanse = new CleanseOrderAbilityDefinition().BuildAbilities();
        AssertAbility(cleanse[FeatType.CleanseOrder1], "Cleanse Order I", 1, RecastGroup.CleanseOrder, 90f, 1f, 6, false, true, false, false);
        AssertAbility(cleanse[FeatType.CleanseOrder2], "Cleanse Order II", 2, RecastGroup.CleanseOrder, 90f, 1f, 9, false, false, true, true);

        var hold = new HoldTheLineAbilityDefinition().BuildAbilities()[FeatType.HoldTheLine1];
        AssertAbility(hold, "Hold the Line", 1, RecastGroup.Capstone, 1800f, 1f, 15, false, true, false, false);

        AssertAura(new RallyingStandardAbilityDefinition().BuildAbilities()[FeatType.RallyingStandard1], "Rallying Standard I", 1, RecastGroup.RallyingStandard);
        AssertAura(new RallyingStandardAbilityDefinition().BuildAbilities()[FeatType.RallyingStandard2], "Rallying Standard II", 2, RecastGroup.RallyingStandard);
        AssertAura(new CoordinatedFocusAbilityDefinition().BuildAbilities()[FeatType.CoordinatedFocus1], "Coordinated Focus I", 1, RecastGroup.CoordinatedFocus);
        AssertAura(new CoordinatedFocusAbilityDefinition().BuildAbilities()[FeatType.CoordinatedFocus2], "Coordinated Focus II", 2, RecastGroup.CoordinatedFocus);
        AssertAura(new CoordinatedFocusAbilityDefinition().BuildAbilities()[FeatType.CoordinatedFocus3], "Coordinated Focus III", 3, RecastGroup.CoordinatedFocus);
        AssertAura(new ChargeOrderAbilityDefinition().BuildAbilities()[FeatType.ChargeOrder1], "Charge Order I", 1, RecastGroup.ChargeOrder);
        AssertAura(new ChargeOrderAbilityDefinition().BuildAbilities()[FeatType.ChargeOrder2], "Charge Order II", 2, RecastGroup.ChargeOrder);
        AssertAura(new WatchfulPresenceAbilityDefinition().BuildAbilities()[FeatType.WatchfulPresence1], "Watchful Presence I", 1, RecastGroup.WatchfulPresence);
        AssertAura(new WatchfulPresenceAbilityDefinition().BuildAbilities()[FeatType.WatchfulPresence2], "Watchful Presence II", 2, RecastGroup.WatchfulPresence);
        AssertAura(new WatchfulPresenceAbilityDefinition().BuildAbilities()[FeatType.WatchfulPresence3], "Watchful Presence III", 3, RecastGroup.WatchfulPresence);
        AssertAura(new SteadyFormationAbilityDefinition().BuildAbilities()[FeatType.SteadyFormation1], "Steady Formation I", 1, RecastGroup.SteadyFormation);
        AssertAura(new SteadyFormationAbilityDefinition().BuildAbilities()[FeatType.SteadyFormation2], "Steady Formation II", 2, RecastGroup.SteadyFormation);
        AssertAura(new FieldRecoveryAbilityDefinition().BuildAbilities()[FeatType.FieldRecovery1], "Field Recovery I", 1, RecastGroup.FieldRecovery);
        AssertAura(new FieldRecoveryAbilityDefinition().BuildAbilities()[FeatType.FieldRecovery2], "Field Recovery II", 2, RecastGroup.FieldRecovery);
    }

    [Test]
    public void LeadershipStatuses_MatchCombatBibleStats()
    {
        AssertAppliedStat(new RallyingStandard1StatusEffect(), StatType.AbilityHitChancePercentAdjustment, 2);
        AssertAppliedStat(new RallyingStandard2StatusEffect(), StatType.AbilityHitChancePercentAdjustment, 3);
        AssertAppliedStat(new PressTheAttack1StatusEffect(), StatType.DamageDealtPercentAdjustment, 4);
        AssertAppliedStat(new PressTheAttack2StatusEffect(), StatType.DamageDealtPercentAdjustment, 6);
        AssertAppliedStat(new PressTheAttack3StatusEffect(), StatType.DamageDealtPercentAdjustment, 8);
        AssertAppliedStat(new PressTheAttack3StatusEffect(), StatType.AbilityHitChancePercentAdjustment, 3);
        AssertAppliedStat(new DecisiveCommand1StatusEffect(), StatType.DamageDealtPercentAdjustment, 14);
        AssertAppliedStat(new DecisiveCommand1StatusEffect(), StatType.AbilityHitChancePercentAdjustment, 8);
        AssertAppliedStat(new DecisiveCommand1StatusEffect(), StatType.CriticalRatePercentAdjustment, 8);

        AssertAppliedStat(new WatchfulPresence3StatusEffect(), StatType.PhysicalDamageTakenPercentAdjustment, -5);
        AssertAppliedStat(new WatchfulPresence3StatusEffect(), StatType.ForceDamageTakenPercentAdjustment, -5);
        AssertAppliedStat(new SteadyFormation2StatusEffect(), StatType.EvasionPercentAdjustment, 3);
        AssertAppliedStat(new SteadyFormation2StatusEffect(), StatType.MindResistance, 35);
        AssertAppliedStat(new SteadyFormation2StatusEffect(), StatType.MobilityResistance, 35);
        AssertAppliedStat(new RousingShout3StatusEffect(), StatType.DamageTakenPercentAdjustment, -14);
        AssertAppliedStat(new BolsterResolve2StatusEffect(), StatType.DamageTakenPercentAdjustment, -8);
        AssertAppliedStat(new CleanseOrder2StatusEffect(), StatType.DamageTakenPercentAdjustment, -10);
        AssertAppliedStat(new TriageProtocol2StatusEffect(), StatType.HealingReceivedPercentAdjustment, 8);
        AssertAppliedStat(new HoldTheLine1StatusEffect(), StatType.DamageTakenPercentAdjustment, -25);
        AssertAppliedStat(new HoldTheLine1StatusEffect(), StatType.MindStatusImmunity, 1);
        AssertAppliedStat(new HoldTheLine1StatusEffect(), StatType.MobilityStatusImmunity, 1);
    }

    [Test]
    public void LeadershipFeatAndAbilityIcons_AreAssignedAndUnique()
    {
        var labels = new HashSet<string>
        {
            "PressTheAttack1", "MarkTarget1", "PressTheAttack2", "BreakMorale1",
            "MarkTarget2", "PressTheAttack3", "BreakMorale2", "DecisiveCommand1",
            "RousingShout1", "BolsterResolve1", "RousingShout2", "CleanseOrder1",
            "BolsterResolve2", "RousingShout3", "CleanseOrder2", "HoldTheLine1",
            "RallyingStandard1", "CoordinatedFocus1", "ChargeOrder1", "RallyingStandard2",
            "CoordinatedFocus2", "ChargeOrder2", "CoordinatedFocus3", "WatchfulPresence1",
            "SteadyFormation1", "FieldRecovery1", "WatchfulPresence2", "SteadyFormation2",
            "FieldRecovery2", "WatchfulPresence3"
        };
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da", "ICON");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da", "IconResRef");
        var targetIcons = new List<string>();

        foreach (var label in labels)
        {
            var featIcon = featRows.Values.Should().ContainSingle(x => x.Label == label).Which.Icon;
            var spellIcon = spellRows.Values.Should().ContainSingle(x => x.Label == label).Which.Icon;
            targetIcons.Add(featIcon);

            featIcon.Should().NotBe("****");
            spellIcon.Should().NotBe("****");
            featIcon.Should().Be(spellIcon);

            featRows.Values.Where(x => x.Icon == featIcon && !labels.Contains(x.Label)).Should().BeEmpty();
            spellRows.Values.Where(x => x.Icon == spellIcon && !labels.Contains(x.Label)).Should().BeEmpty();
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();
        }

        targetIcons.Should().OnlyHaveUniqueItems();
    }

    private static void AssertAppliedStat(IStatusEffect statusEffect, StatType statType, int expected)
    {
        statusEffect.ApplyEffect(0, 0, -1);
        statusEffect.StatGroup.Stats[statType].Should().Be(expected);
    }

    private static void AssertAura(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup)
    {
        AssertAbility(ability, name, level, recastGroup, 60f, 2f, null, false, true, false, false);
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int? staminaCost,
        bool isHostile,
        bool isArea,
        bool isSingleTarget,
        bool requiresTarget)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Leadership);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(AbilityActivationType.Casted);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.BreaksStealth.Should().BeTrue();
        ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();

        if (staminaCost.HasValue)
        {
            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredSTM
                .Should()
                .Be(staminaCost.Value);
        }
        else
        {
            ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
        }
    }

    private static void AssertPerkLevel(
        PerkDetail perk,
        string name,
        int level,
        int price,
        int? leadershipRequirement,
        FeatType? grantedFeat,
        params (StatType Stat, int Value)[] statBonuses)
    {
        perk.Name.Should().Be(name);
        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);

        if (leadershipRequirement.HasValue)
        {
            var requirement = perkLevel.Requirements
                .OfType<PerkRequirementSkill>()
                .Should()
                .ContainSingle()
                .Which;

            requirement.Type.Should().Be(SkillType.Leadership);
            requirement.RequiredRank.Should().Be(leadershipRequirement.Value);
        }
        else
        {
            perkLevel.Requirements.OfType<PerkRequirementSkill>().Should().BeEmpty();
        }

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        perkLevel.StatBonuses.Should().HaveCount(statBonuses.Length);
        foreach (var (stat, value) in statBonuses)
        {
            perkLevel.StatBonuses
                .Should()
                .ContainSingle(x => x.Stat == stat)
                .Which
                .Calculate(0)
                .Should()
                .Be(value);
        }
    }

    private static Dictionary<PerkType, PerkDetail> BuildPerksWithout2daLookup<T>(T definition, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            typeof(T)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(T)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static Dictionary<int, TwoDaIconRow> Read2da(PathInfo path, string iconColumn)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var labelIndex = Array.IndexOf(header, "LABEL");
        if (labelIndex < 0)
            labelIndex = Array.IndexOf(header, "Label");

        var iconIndex = Array.IndexOf(header, iconColumn);
        var result = new Dictionary<int, TwoDaIconRow>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            result[row] = new TwoDaIconRow(cells[labelIndex + 1], cells[iconIndex + 1]);
        }

        return result;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "swlor2_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record TwoDaIconRow(string Label, string Icon);

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
