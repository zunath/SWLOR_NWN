using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ForceLightConsularTests
{
    [Test]
    public void ForceLightConsularPerkLevels_MatchCombatBible()
    {
        var perks = BuildForceLightConsularPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.Benevolence], "Benevolence", 1, 2, null, FeatType.Benevolence1,
            "Restores 8% of the target's maximum HP plus WIL scaling to a single target. Healing gains +25% when targeting someone other than yourself.");
        AssertPerkLevel(perks[PerkType.Pacify], "Pacify", 1, 2, 5, FeatType.Pacify1,
            "Reduce a target's outgoing weapon and force damage by 5% for 20 seconds.");
        AssertPerkLevel(perks[PerkType.Renewal], "Renewal", 1, 3, 8, FeatType.Renewal1,
            "Applies regeneration to a single ally, restoring 2% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.");
        AssertPerkLevel(perks[PerkType.Clarity], "Clarity", 1, 3, 12, FeatType.Clarity1,
            "Restores 10% of maximum STM to an ally and increases physical and force ability hit chance by 4% for 15 seconds. Self-target restores FP instead.");
        AssertPerkLevel(perks[PerkType.MindTrick], "Mind Trick", 1, 3, 15, FeatType.MindTrick1,
            "Confuse one non-mechanical target for 5 seconds.");
        AssertPerkLevel(perks[PerkType.Benevolence], "Benevolence", 2, 3, 18, FeatType.Benevolence2,
            "Restores 14% of the target's maximum HP plus WIL scaling to a single target. Healing gains +25% when targeting someone other than yourself.");
        AssertPerkLevel(perks[PerkType.ComprehendSpeech], "Comprehend Speech", 1, 2, 22, FeatType.ComprehendSpeech1,
            "For 15 minutes, you count as having 15 additional ranks in all languages for understanding spoken speech.");
        AssertPerkLevel(perks[PerkType.Renewal], "Renewal", 2, 4, 25, FeatType.Renewal2,
            "Applies regeneration to a single ally, restoring 3% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.");
        AssertPerkLevel(perks[PerkType.Pacify], "Pacify", 2, 3, 28, FeatType.Pacify2,
            "Reduce up to 2 nearby enemies' outgoing weapon and force damage by 8% for 20 seconds.");
        AssertPerkLevel(perks[PerkType.ForceMend], "Force Mend", 1, 4, 30, FeatType.ForceMend1,
            "Removes one major negative effect from a single ally and restores HP equal to 16% of the target's maximum HP plus WIL scaling.");
        AssertPerkLevel(perks[PerkType.MindTrick], "Mind Trick", 2, 3, 35, FeatType.MindTrick2,
            "Confuse up to 2 non-mechanical targets for 5 seconds.");
        AssertPerkLevel(perks[PerkType.Clarity], "Clarity", 2, 3, 38, FeatType.Clarity2,
            "Restores 18% of maximum STM to an ally and increases physical and force ability hit chance by 6% for 15 seconds. Self-target restores FP instead.");
        AssertPerkLevel(perks[PerkType.ForceSanctuary], "Force Sanctuary", 1, 4, 40, FeatType.ForceSanctuary1,
            "Creates a 4m sanctuary for 18 seconds. Allies inside gain regeneration equal to 2% of maximum HP plus WIL scaling every 3 seconds and take 5% less force damage.");
        AssertPerkLevel(perks[PerkType.Benevolence], "Benevolence", 3, 4, 42, FeatType.Benevolence3,
            "Restores 20% of the target's maximum HP plus WIL scaling to a single target. Healing gains +25% when targeting someone other than yourself.");
        AssertPerkLevel(perks[PerkType.Renewal], "Renewal", 3, 4, 45, FeatType.Renewal3,
            "Applies regeneration to a single ally, restoring 4% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.");
        AssertPerkLevel(perks[PerkType.Pacify], "Pacify", 3, 3, 48, FeatType.Pacify3,
            "Reduces nearby enemies' outgoing weapon and force damage by 12% for 20 seconds.");
        AssertPerkLevel(perks[PerkType.CircleOfHarmony], "Circle of Harmony", 1, 5, 50, FeatType.CircleOfHarmony1,
            "Nearby allies, including you, recover 14% of maximum HP plus WIL scaling, remove one standard negative effect, and restore 1 FP and 1 STM every 3 seconds for 45 seconds.");

        AssertUniversalForcePower(perks[PerkType.MindTrick]);
    }

    [Test]
    public void ForceLightConsularAbilities_MatchCombatBible()
    {
        var benevolence = new BenevolenceAbilityDefinition().BuildAbilities();
        AssertAbility(benevolence[FeatType.Benevolence1], "Benevolence I", 1, RecastGroup.Benevolence, 8f, 1f, 3, false, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(benevolence[FeatType.Benevolence2], "Benevolence II", 2, RecastGroup.Benevolence, 8f, 1f, 5, false, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(benevolence[FeatType.Benevolence3], "Benevolence III", 3, RecastGroup.Benevolence, 8f, 1f, 7, false, true, true, false, AbilityActivationType.Casted, 15f);

        var pacify = new PacifyAbilityDefinition().BuildAbilities();
        AssertAbility(pacify[FeatType.Pacify1], "Pacify I", 1, RecastGroup.Pacify, 24f, 1f, 3, true, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(pacify[FeatType.Pacify2], "Pacify II", 2, RecastGroup.Pacify, 24f, 1f, 5, true, false, false, true, AbilityActivationType.Casted, 5f);
        AssertAbility(pacify[FeatType.Pacify3], "Pacify III", 3, RecastGroup.Pacify, 30f, 1f, 7, true, false, false, true, AbilityActivationType.Casted, 5f);

        var renewal = new RenewalAbilityDefinition().BuildAbilities();
        AssertAbility(renewal[FeatType.Renewal1], "Renewal I", 1, RecastGroup.Renewal, 24f, 1f, 4, false, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(renewal[FeatType.Renewal2], "Renewal II", 2, RecastGroup.Renewal, 24f, 1f, 5, false, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(renewal[FeatType.Renewal3], "Renewal III", 3, RecastGroup.Renewal, 24f, 1f, 7, false, true, true, false, AbilityActivationType.Casted, 15f);

        var clarity = new ClarityAbilityDefinition().BuildAbilities();
        AssertAbility(clarity[FeatType.Clarity1], "Clarity I", 1, RecastGroup.Clarity, 45f, 1f, 4, false, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(clarity[FeatType.Clarity2], "Clarity II", 2, RecastGroup.Clarity, 45f, 1f, 6, false, true, true, false, AbilityActivationType.Casted, 15f);

        var mindTrick = new MindTrickAbilityDefinition().BuildAbilities();
        AssertAbility(mindTrick[FeatType.MindTrick1], "Mind Trick I", 1, RecastGroup.MindTrick, 60f, 1f, 4, true, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(mindTrick[FeatType.MindTrick2], "Mind Trick II", 2, RecastGroup.MindTrick, 60f, 1f, 5, true, true, true, false, AbilityActivationType.Casted, 15f);

        var comprehendSpeech = new ComprehendSpeechAbilityDefinition().BuildAbilities()[FeatType.ComprehendSpeech1];
        AssertAbility(comprehendSpeech, "Comprehend Speech", 1, RecastGroup.ComprehendSpeech, 30f, 0f, 2, false, false, true, false, AbilityActivationType.Casted, 5f);

        var forceMend = new ForceMendAbilityDefinition().BuildAbilities()[FeatType.ForceMend1];
        AssertAbility(forceMend, "Force Mend", 1, RecastGroup.ForceMend, 30f, 1f, 6, false, true, true, false, AbilityActivationType.Casted, 15f);

        var forceSanctuary = new ForceSanctuaryAbilityDefinition().BuildAbilities()[FeatType.ForceSanctuary1];
        AssertAbility(forceSanctuary, "Force Sanctuary", 1, RecastGroup.ForceSanctuary, 90f, 1.5f, 8, false, false, false, true, AbilityActivationType.Casted, 5f);

        var circleOfHarmony = new CircleOfHarmonyAbilityDefinition().BuildAbilities()[FeatType.CircleOfHarmony1];
        AssertAbility(circleOfHarmony, "Circle of Harmony", 1, RecastGroup.Capstone, 345f, 1.5f, 10, false, false, false, true, AbilityActivationType.Casted, 5f);
    }

    [Test]
    public void ForceLightConsularStatusEffects_MatchCombatBible()
    {
        var pacify1 = new Pacify1StatusEffect();
        pacify1.StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(-5);
        pacify1.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        pacify1.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);

        var pacify2 = new Pacify2StatusEffect();
        pacify2.StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(-8);
        pacify2.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        pacify2.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);

        var pacify3 = new Pacify3StatusEffect();
        pacify3.StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(-12);
        pacify3.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        pacify3.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);

        var clarity1 = new Clarity1StatusEffect();
        clarity1.StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(4);
        clarity1.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        clarity1.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);

        var clarity2 = new Clarity2StatusEffect();
        clarity2.StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(6);
        clarity2.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        clarity2.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);

        var comprehendSpeech = new ComprehendSpeech1StatusEffect();
        comprehendSpeech.StatGroup.Stats[StatType.LanguageComprehension].Should().Be(15);

        var forceSanctuary = new ForceSanctuary1StatusEffect();
        forceSanctuary.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-5);

        var circleOfHarmony = new CircleOfHarmony1StatusEffect();
        circleOfHarmony.Frequency.Should().Be(3f);
    }

    [Test]
    public void ForceLightConsularFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.Benevolence1, "ife_bnvlnc1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.Pacify1, "ife_pcfy1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.Renewal1, "ife_rnwl1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.Clarity1, "ife_clrty1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.MindTrick1, "ife_mndtrck1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.Benevolence2, "ife_bnvlnc2", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ComprehendSpeech1, "ife_cmprhndspch1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Renewal2, "ife_rnwl2", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.Pacify2, "ife_pcfy2", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.ForceMend1, "ife_forcemnd1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.MindTrick2, "ife_mndtrck2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.Clarity2, "ife_clrty2", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForceSanctuary1, "ife_forcesnctry1", "M", "0x3E", "0", "sphere", "4", "****", "1"),
            (FeatType.Benevolence3, "ife_bnvlnc3", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.Renewal3, "ife_rnwl3", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.Pacify3, "ife_pcfy3", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.CircleOfHarmony1, "ife_circhrmny1", "P", "0x01", "0", "sphere", "5", "****", "17")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
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
        int? skillRank,
        FeatType? grantedFeat,
        string description)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.ForceLight);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertCharacterRequirement(perkLevel, CharacterType.ForceSensitive);

        if (skillRank.HasValue)
            AssertSkillRequirement(perkLevel, SkillType.Force, skillRank.Value);
        else
            perkLevel.Requirements.OfType<PerkRequirementSkill>().Should().BeEmpty();

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int fpCost,
        bool isHostile,
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType,
        float maxRange)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Force);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(activationType);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.MaxRange.Should().Be(maxRange);
        ability.BreaksStealth.Should().BeTrue();

        ability.Requirements
            .OfType<AbilityRequirementFP>()
            .Should()
            .ContainSingle()
            .Which
            .RequiredFP
            .Should()
            .Be(fpCost);
        ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
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

    private static void AssertCharacterRequirement(PerkLevel level, CharacterType characterType)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementCharacterType>()
            .Should()
            .ContainSingle()
            .Which;

        typeof(PerkRequirementCharacterType)
            .GetField("_requiredCharacterType", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(characterType);
    }

    private static void AssertUniversalForcePower(PerkDetail perk)
    {
        perk.ForceAffinityType.Should().BeNull();
        perk.StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.ForceAffinity);
    }

    private static Dictionary<PerkType, PerkDetail> BuildForceLightConsularPerksWithout2daLookup()
    {
        var definition = new ForceLightConsularPerkDefinition();
        var methodNames = new[]
        {
            "Benevolence",
            "CircleOfHarmony",
            "Clarity",
            "ComprehendSpeech",
            "ForceMend",
            "ForceSanctuary",
            "MindTrick",
            "Pacify",
            "Renewal"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ForceLightConsularPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ForceLightConsularPerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
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
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
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
