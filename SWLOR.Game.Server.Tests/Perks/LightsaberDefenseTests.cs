using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LightsaberDefenseTests
{
    [Test]
    public void LightsaberDefensePerkLevels_MatchCombatBible()
    {
        var perks = BuildLightsaberDefensePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.DeflectionTraining], "Deflection Training", 1, 2, 5, null,
            "Grants +15 Attack Deflection.",
            StatType.AttackDeflection);
        AssertPerkLevel(perks[PerkType.ReactiveDeflection], "Reactive Deflection", 1, 2, 8, null,
            "When you deflect an attack, restore 2 FP.",
            StatType.DeflectionFPRestore);
        AssertPerkLevel(perks[PerkType.TauntingDeflection], "Taunting Deflection", 1, 4, 10, FeatType.TauntingDeflection1,
            "Goads all nearby enemies into attacking you and grants the buff Taunting Deflection, which increases your attack deflection by 10 for 30 seconds.");
        AssertPerkLevel(perks[PerkType.DeflectionRiposte], "Deflection Riposte", 1, 3, 12, null,
            "When you deflect an attack, your next attack receives +20% critical chance. Effect wears off after 15 seconds.",
            StatType.DeflectionNextSkillAbilityCriticalRatePercentAdjustment,
            StatType.DeflectionNextSkillAbilityCriticalRateWindowSeconds);
        AssertPerkLevel(perks[PerkType.DeflectionTraining], "Deflection Training", 2, 3, 15, null,
            "Grants +25 Attack Deflection.",
            StatType.AttackDeflection);
        AssertPerkLevel(perks[PerkType.DeflectivePresence], "Deflective Presence", 1, 3, 18, null,
            "When you deflect an attack, receive the Deflective Presence buff which improves your Enmity acquisition by 20% for 12 seconds.",
            StatType.DeflectionEnmityPercentAdjustment);
        AssertPerkLevel(perks[PerkType.GuardiansInfluence], "Guardian's Influence", 1, 3, 20, FeatType.GuardiansInfluence1,
            "Allies within the area of effect (sphere) gain +15 attack deflection chance for 1 minute. You do not receive this benefit.");
        AssertPerkLevel(perks[PerkType.ReactiveDeflection], "Reactive Deflection", 2, 3, 22, null,
            "When you deflect an attack, restore 4 FP.",
            StatType.DeflectionFPRestore);
        AssertPerkLevel(perks[PerkType.DeflectionMastery], "Deflection Mastery", 1, 3, 25, null,
            "When you deflect an attack, your defense and force defense increase by 15% for 12 seconds.",
            StatType.DeflectionDefensePercentAdjustment,
            StatType.DeflectionForceDefensePercentAdjustment);
        AssertPerkLevel(perks[PerkType.DeflectionCounter], "Deflection Counter", 1, 3, 28, null,
            "After deflecting an attack, your next attack has no delay.",
            StatType.DeflectionNextSkillAbilityNoDelay,
            StatType.DeflectionNextSkillAbilityNoDelayWindowSeconds);
        AssertPerkLevel(perks[PerkType.PunishingStrike], "Punishing Strike", 1, 3, 30, FeatType.PunishingStrike1,
            "Deals weapon DMG + 20 to enemies within the area of effect (sphere) near you and gain increased enmity toward you.");
        AssertPerkLevel(perks[PerkType.DeflectionTraining], "Deflection Training", 3, 4, 32, null,
            "Grants +35 Attack Deflection.",
            StatType.AttackDeflection);
        AssertPerkLevel(perks[PerkType.OverwhelmingDefense], "Overwhelming Defense", 1, 3, 35, null,
            "After deflecting an attack, your next attack deals +20 DMG.",
            StatType.DeflectionNextSkillAbilityDamageBonus,
            StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds);
        AssertPerkLevel(perks[PerkType.GuardiansChallenge], "Guardian's Challenge", 1, 4, 38, FeatType.GuardiansChallenge1,
            "All enemies within the area of effect (cone) take weapon DMG + 35 and gain increased enmity toward you.");
        AssertPerkLevel(perks[PerkType.ImpenetrableGuard], "Impenetrable Guard", 1, 4, 42, FeatType.ImpenetrableGuard1,
            "While active, grants +15% attack deflection, +10% enmity generation, -20% attack, -20% force attack.");
        AssertPerkLevel(perks[PerkType.ThunderousChallenge], "Thunderous Challenge", 1, 4, 45, FeatType.ThunderousChallenge1,
            "Deals weapon DMG + 35 to enemies within the area of effect (line) from your position and gain increased enmity toward you.");
        AssertPerkLevel(perks[PerkType.GuardianMaster], "Guardian Master", 1, 4, 50, FeatType.GuardianMaster1,
            "Grants the ability Guardian's Wrath which guarantees all attacks toward you will be deflected for 30 seconds. Additionally, increases your natural attack deflection cap to 75% when equipped with a lightsaber.",
            StatType.AttackDeflectionChanceCap);
    }

    [Test]
    public void LightsaberDefenseAbilities_MatchCombatBible()
    {
        var tauntingDeflection = new TauntingDeflectionAbilityDefinition().BuildAbilities()[FeatType.TauntingDeflection1];
        AssertAbility(tauntingDeflection, "Taunting Deflection", 1, RecastGroup.TauntingDeflection, 30f, 0f, 5, true, false, false, true, AbilityActivationType.Casted);

        var guardiansInfluence = new GuardiansInfluenceAbilityDefinition().BuildAbilities()[FeatType.GuardiansInfluence1];
        AssertAbility(guardiansInfluence, "Guardian's Influence", 1, RecastGroup.GuardiansInfluence, 300f, 2f, 7, false, false, false, true, AbilityActivationType.Casted);

        var punishingStrike = new PunishingStrikeAbilityDefinition().BuildAbilities()[FeatType.PunishingStrike1];
        AssertAbility(punishingStrike, "Punishing Strike", 1, RecastGroup.PunishingStrike, 90f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var guardiansChallenge = new GuardiansChallengeAbilityDefinition().BuildAbilities()[FeatType.GuardiansChallenge1];
        AssertAbility(guardiansChallenge, "Guardian's Challenge", 1, RecastGroup.GuardiansChallenge, 90f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var impenetrableGuard = new ImpenetrableGuardAbilityDefinition().BuildAbilities()[FeatType.ImpenetrableGuard1];
        AssertAbility(impenetrableGuard, "Impenetrable Guard", 1, RecastGroup.ImpenetrableGuard, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var thunderousChallenge = new ThunderousChallengeAbilityDefinition().BuildAbilities()[FeatType.ThunderousChallenge1];
        AssertAbility(thunderousChallenge, "Thunderous Challenge", 1, RecastGroup.ThunderousChallenge, 120f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);

        var guardianMaster = new GuardianMasterAbilityDefinition().BuildAbilities()[FeatType.GuardianMaster1];
        AssertAbility(guardianMaster, "Guardian Master", 1, RecastGroup.Capstone, 1800f, 2f, 25, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void LightsaberDefenseStatusEffects_MatchCombatBible()
    {
        var tauntingDeflection = new TauntingDeflectionStatusEffect();
        tauntingDeflection.StatGroup.Stats[StatType.AttackDeflection].Should().Be(10);

        var deflectingAura = new DeflectingAuraStatusEffect();
        deflectingAura.StatGroup.Stats[StatType.AttackDeflection].Should().Be(15);

        var impenetrableGuard = new ImpenetrableGuardStatusEffect();
        impenetrableGuard.StatGroup.Stats[StatType.AttackDeflection].Should().Be(15);
        impenetrableGuard.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(10);
        impenetrableGuard.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);
        impenetrableGuard.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-20);
        impenetrableGuard.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(0);
        impenetrableGuard.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);

        var guardiansWrath = new GuardiansWrathStatusEffect();
        guardiansWrath.StatGroup.Stats[StatType.AttackDeflection].Should().Be(100);
        guardiansWrath.StatGroup.Stats[StatType.AttackDeflectionChanceCap].Should().Be(100);
    }

    [Test]
    public void LightsaberDefenseFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.TauntingDeflection1, "ife_tauntdefl1", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.GuardiansInfluence1, "ife_guardinfl1", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.PunishingStrike1, "ife_punstrk1", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.GuardiansChallenge1, "ife_guardchal1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.ImpenetrableGuard1, "ife_impengrd1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ThunderousChallenge1, "ife_thndrschal1", "0x3E", "1", "rectangle", "2.5", "8", "17"),
            (FeatType.GuardianMaster1, "ife_guardmstr1", "0x01", "0", "****", "****", "****", "****")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            spellRow["TargetType"].Should().Be(targetType);
            spellRow["HostileSetting"].Should().Be(hostileSetting);
            spellRow["TargetShape"].Should().Be(targetShape);
            spellRow["TargetSizeX"].Should().Be(targetSizeX);
            spellRow["TargetSizeY"].Should().Be(targetSizeY);
            spellRow["TargetFlags"].Should().Be(targetFlags);
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
        perk.Category.Should().Be(PerkCategoryType.LightsaberDefense);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Lightsaber, skillRank);
        AssertCharacterRequirement(perkLevel, CharacterType.ForceSensitive);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
            perkLevel.StatBonuses.Select(x => x.Stat).Should().Contain(statTypes);
        else
            perkLevel.StatBonuses.Should().BeEmpty();
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
            .GetField("_requiredCharacterType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(characterType);
    }

    private static Dictionary<PerkType, PerkDetail> BuildLightsaberDefensePerksWithout2daLookup()
    {
        var definition = new LightsaberPerkDefinition();
        var methodNames = new[]
        {
            "DeflectionCounter",
            "DeflectionMastery",
            "DeflectionRiposte",
            "DeflectionTraining",
            "DeflectivePresence",
            "GuardianMaster",
            "GuardiansChallenge",
            "GuardiansInfluence",
            "ImpenetrableGuard",
            "OverwhelmingDefense",
            "PunishingStrike",
            "ReactiveDeflection",
            "TauntingDeflection",
            "ThunderousChallenge"
        };

        foreach (var methodName in methodNames)
        {
            typeof(LightsaberPerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(LightsaberPerkDefinition)
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
