using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Katar;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class KatarVenomCurrentTests
{
    [Test]
    public void KatarVenomCurrentAbilities_MatchCombatBible()
    {
        var strikingCobra = new StrikingCobraAbilityDefinition().BuildAbilities();
        AssertAbility(strikingCobra[FeatType.StrikingCobra1], "Striking Cobra I", 1, RecastGroup.StrikingCobra, 30f, 0f, 3, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(strikingCobra[FeatType.StrikingCobra2], "Striking Cobra II", 2, RecastGroup.StrikingCobra, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(strikingCobra[FeatType.StrikingCobra3], "Striking Cobra III", 3, RecastGroup.StrikingCobra, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var staticPalm = new StaticPalmAbilityDefinition().BuildAbilities();
        AssertAbility(staticPalm[FeatType.StaticPalm1], "Static Palm I", 1, RecastGroup.StaticPalm, 30f, 0f, 3, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(staticPalm[FeatType.StaticPalm2], "Static Palm II", 2, RecastGroup.StaticPalm, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(staticPalm[FeatType.StaticPalm3], "Static Palm III", 3, RecastGroup.StaticPalm, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var cobraStance = new CobraStanceAbilityDefinition().BuildAbilities()[FeatType.CobraStance1];
        AssertAbility(cobraStance, "Cobra Stance", 1, RecastGroup.CobraStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var neuralShock = new NeuralShockAbilityDefinition().BuildAbilities()[FeatType.NeuralShock1];
        AssertAbility(neuralShock, "Neural Shock", 1, RecastGroup.NeuralShock, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var currentOverload = new CurrentOverloadAbilityDefinition().BuildAbilities()[FeatType.CurrentOverload1];
        AssertAbility(currentOverload, "Current Overload", 1, RecastGroup.CurrentOverload, 90f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var serpentsEclipse = new SerpentsEclipseAbilityDefinition().BuildAbilities()[FeatType.SerpentsEclipse1];
        AssertAbility(serpentsEclipse, "Serpent's Eclipse", 1, RecastGroup.Capstone, 345f, 2f, 15, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void KatarVenomCurrentStatusEffects_MatchCombatBible()
    {
        var cobraStance = new CobraStanceStatusEffect();
        cobraStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(10);
        cobraStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-15);
        cobraStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-15);

        var toxicRush = new ToxicRushStatusEffect();
        toxicRush.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(20);
        toxicRush.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(15);
        toxicRush.Categories.Should().Be(StatusEffectCategory.Buff);
        toxicRush.PersistsOnLogout.Should().BeFalse();
        toxicRush.SendsApplicationMessage.Should().BeFalse();
        toxicRush.SendsWornOffMessage.Should().BeFalse();

        var toxicRushStack = new ToxicRushStatusEffect(3, 4, 3);
        toxicRushStack.Stacks.Should().Be(3);
        toxicRushStack.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(12);
        toxicRushStack.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(9);
        ((ToxicRushStatusEffect)toxicRushStack.Clone()).Stacks.Should().Be(3);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);
    }

    [Test]
    public void ToxicRush_UsesVisibleStatusEffectFromDamageDealtHook()
    {
        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var damageDealtEffects = ExtractMethod(combatSource, "public static void ApplyDamageDealtEffects(");
        var toxicRushEffects = ExtractMethod(combatSource, "private static void ApplyToxicRushDamageDealtEffects(");
        var venomCurrentImpactRiders = ExtractMethod(combatSource, "private static void ApplyKatarVenomCurrentImpactRiders(");

        damageDealtEffects.Should().Contain("ApplyToxicRushDamageDealtEffects(attacker, defender, deliveryType);");
        toxicRushEffects.Should().Contain("deliveryType == CombatDamageDeliveryType.DamageOverTime");
        toxicRushEffects.Should().Contain("StatusEffect.GetStatusEffect<ToxicRushStatusEffect>(attacker)?.Stacks");
        toxicRushEffects.Should().Contain("new ToxicRushStatusEffect(stacks, haste, attack)");
        toxicRushEffects.Should().Contain("StatusEffect.ApplyStatusEffect(");
        toxicRushEffects.Should().NotContain("TemporaryStatModifier");
        venomCurrentImpactRiders.Should().NotContain("ApplyToxicRush");
    }

    [Test]
    public void KatarVenomCurrentFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.StrikingCobra1, "ife_strkngcobra1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StrikingCobra2, "ife_strkngcobra2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StrikingCobra3, "ife_strkngcobra3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StaticPalm1, "ife_statpalm1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StaticPalm2, "ife_statpalm2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StaticPalm3, "ife_statpalm3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CobraStance1, "ife_cobrastnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.NeuralShock1, "ife_neuralshok1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CurrentOverload1, "ife_currovld1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SerpentsEclipse1, "ife_serpecl1", "M", "0x3E", "1", "sphere", "5", "****", "1")
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
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

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
    int skillRank,
    FeatType? grantedFeat,
    string description,
    params StatType[] statTypes)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.KatarVenomCurrent);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Katar, skillRank);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
            perkLevel.StatBonuses.Select(x => x.Stat).Should().HaveCount(statTypes.Length).And.Contain(statTypes);
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
        ability.SkillType.Should().Be(SkillType.Katar);
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

    private static Dictionary<PerkType, PerkDetail> BuildKatarVenomCurrentPerksWithout2daLookup()
    {
        var definition = new KatarPerkDefinition();
        var methodNames = new[]
        {
            "CobraReflexes",
            "CobraStance",
            "CurrentOverload",
            "NeuralShock",
            "NeurotoxinMastery",
            "SerpentsEclipse",
            "SpreadingVenom",
            "StaticPalm",
            "StrikingCobra",
            "ToxicRush",
            "ToxicTempo",
            "TwinFangFlurry",
            "VenomRhythm",
            "VenomSplash"
        };

        foreach (var methodName in methodNames)
        {
            typeof(KatarPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(KatarPerkDefinition)
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
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
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

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
