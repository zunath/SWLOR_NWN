using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class HeavyVibrobladeDefenseTests
{
    [Test]
    public void HeavyVibrobladeDefenseStatusEffects_MatchCombatBible()
    {
        var bastion = new BastionStanceStatusEffect();
        bastion.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(20);
        bastion.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(15);
        bastion.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(15);
        bastion.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);
        bastion.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-20);

        var crushingBlow = new CrushingBlowStatusEffect();
        crushingBlow.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);

        var flash = new FlashStatusEffect(20);
        flash.StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(-20);
        flash.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);

        var fortress1 = new FortressStrikeStatusEffect(10);
        fortress1.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(10);
        var fortress2 = new FortressStrikeStatusEffect(20);
        fortress2.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(20);
        var fortress3 = new FortressStrikeStatusEffect(30);
        fortress3.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(30);

        var rampart = new RampartStatusEffect();
        rampart.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-15);

        var absoluteDefense = new AbsoluteDefenseStatusEffect();
        absoluteDefense.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-15);
        absoluteDefense.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-15);
        absoluteDefense.StatGroup.Stats[StatType.MindResistance].Should().Be(0);
        absoluteDefense.StatGroup.Stats[StatType.MobilityResistance].Should().Be(0);
    }

    [Test]
    public void GuardiansResolve_IsCrossSkillAndUsesStatSelectors()
    {
        var perks = BuildHeavyVibrobladeDefensePerksWithout2daLookup();
        var guardiansResolve = perks[PerkType.GuardiansResolve];
        AssertPerkLevel(
            guardiansResolve,
            "Guardian's Resolve",
            1,
            4,
            28,
            null,
            "When a Heavy Vibroblade Defense ability grants you Physical Defense or reduces incoming damage, you also gain a damage absorption shield equal to 12% of maximum HP for 12 seconds. You heal for 15% of damage absorbed. This can trigger once every 30 seconds.",
            StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType,
            StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType,
            StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType,
            StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType,
            StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent,
            StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds,
            StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds);
        AssertStatBonus(guardiansResolve.PerkLevels[1], StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType, (int)PerkType.FortressStrike);
        AssertStatBonus(guardiansResolve.PerkLevels[1], StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType, (int)PerkType.BastionStance);
        AssertStatBonus(guardiansResolve.PerkLevels[1], StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType, (int)PerkType.Rampart);
        AssertStatBonus(guardiansResolve.PerkLevels[1], StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType, (int)PerkType.AbsoluteDefense);
        AssertStatBonus(guardiansResolve.PerkLevels[1], StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 12);
        AssertStatBonus(guardiansResolve.PerkLevels[1], StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds, 12);
        AssertStatBonus(guardiansResolve.PerkLevels[1], StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds, 30);

        AssertStatBonus(perks[PerkType.AngerStrike].PerkLevels[1], StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPerkCategory, (int)PerkCategoryType.HeavyVibrobladeDefense);
        AssertStatBonus(perks[PerkType.CrushingBlow].PerkLevels[1], StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPerkCategory, (int)PerkCategoryType.HeavyVibrobladeDefense);

        AssertAbilityPerk(new FortressStrikeAbilityDefinition().BuildAbilities()[FeatType.FortressStrike1], PerkType.FortressStrike);
        AssertAbilityPerk(new BastionStanceAbilityDefinition().BuildAbilities()[FeatType.BastionStance1], PerkType.BastionStance);
        AssertAbilityPerk(new RampartAbilityDefinition().BuildAbilities()[FeatType.Rampart1], PerkType.Rampart);
        AssertAbilityPerk(new AbsoluteDefenseAbilityDefinition().BuildAbilities()[FeatType.AbsoluteDefense1], PerkType.AbsoluteDefense);
        AssertAbilityPerk(new FlashAbilityDefinition().BuildAbilities()[FeatType.Flash1], PerkType.Flash);
        AssertAbilityPerk(new EarthshatterAbilityDefinition().BuildAbilities()[FeatType.Earthshatter1], PerkType.Earthshatter);
        AssertAbilityPerk(new SacrificialBladeAbilityDefinition().BuildAbilities()[FeatType.SacrificialBlade1], PerkType.SacrificialBlade);
    }

    [Test]
    public void NonHostileDefenseAbilities_ResolveActivatedSkillForGuardianResolve()
    {
        AssertActivatedSkill(new BastionStanceAbilityDefinition().BuildAbilities()[FeatType.BastionStance1], SkillType.HeavyVibroblade);
        AssertActivatedSkill(new RampartAbilityDefinition().BuildAbilities()[FeatType.Rampart1], SkillType.HeavyVibroblade);
        AssertActivatedSkill(new AbsoluteDefenseAbilityDefinition().BuildAbilities()[FeatType.AbsoluteDefense1], SkillType.HeavyVibroblade);
    }

    [Test]
    public void HeavyVibrobladeDefenseFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.FortressStrike1, "ife_fortstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.BastionStance1, "ife_baststnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Flash1, "ife_flash1", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.FortressStrike2, "ife_fortstrk2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Rampart1, "ife_ramp1", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.Earthshatter1, "ife_earth1", "0x3E", "1", "rectangle", "8", "2.5", "17"),
            (FeatType.FortressStrike3, "ife_fortstrk3", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.AbsoluteDefense1, "ife_absdef1", "0x01", "0", "****", "****", "****", "****")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be(targetShape);
            abilityRow["TargetSizeX"].Should().Be(targetSizeX);
            abilityRow["TargetSizeY"].Should().Be(targetSizeY);
            abilityRow["TargetFlags"].Should().Be(targetFlags);
        }
    }

    private static void AssertPerkLevel(
    PerkDetail perk,
    string name,
    int level,
    int price,
    int skillRank,
    FeatType? grantedFeat,
    string description,
    params StatType[] statTypes)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.HeavyVibrobladeDefense);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.HeavyVibroblade, skillRank);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
            perkLevel.StatBonuses.Select(x => x.Stat).Should().HaveCount(statTypes.Length).And.Contain(statTypes);
        else
            perkLevel.StatBonuses.Should().BeEmpty();
    }

    private static void AssertStatBonus(PerkLevel level, StatType statType, int value)
    {
        level.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == statType)
            .Which
            .Calculate(0)
            .Should()
            .Be(value);
    }

    private static void AssertAbilityPerk(
        AbilityDetail ability,
        PerkType perkType)
    {
        ability.EffectiveLevelPerkType.Should().Be(perkType);
    }

    private static void AssertActivatedSkill(
        AbilityDetail ability,
        SkillType skillType)
    {
        var method = typeof(Combat).GetMethod(
            "ResolveActivatedAbilitySkillType",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var resolved = (SkillType)method!.Invoke(null, new object[] { 0u, ability, new AbilityImpactSummary() })!;
        resolved.Should().Be(skillType);
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
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.HeavyVibroblade);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(activationType);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.BreaksStealth.Should().BeTrue();

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

        ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();
    }

    private static void AssertSkillRequirement(PerkLevel level, SkillType skill, int rank)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementSkill>()
            .Should()
            .ContainSingle()
            .Which;

        requirement.Type.Should().Be(skill);
        requirement.RequiredRank.Should().Be(rank);
    }

    private static Dictionary<PerkType, PerkDetail> BuildHeavyVibrobladeDefensePerksWithout2daLookup()
    {
        var definition = new HeavyVibrobladePerkDefinition();
        var methodNames = new[]
        {
            "AbsoluteDefense",
            "AngerStrike",
            "BastionStance",
            "BloodWeapon",
            "CriticalWard",
            "CrushingBlow",
            "DefensiveHarmony",
            "Earthshatter",
            "Flash",
            "FortressStrike",
            "GuardiansReaping",
            "GuardiansResolve",
            "LastStand",
            "Rampart",
            "UnbreakableWill"
        };

        foreach (var methodName in methodNames)
        {
            typeof(HeavyVibrobladePerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(HeavyVibrobladePerkDefinition)
            .GetField("_builder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var i = 0; i < header.Length && i + 1 < cells.Length; i++)
            {
                values[header[i]] = cells[i + 1];
            }

            result[row] = values;
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

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
