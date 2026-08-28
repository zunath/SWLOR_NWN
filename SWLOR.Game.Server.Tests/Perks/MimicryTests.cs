using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class MimicryTests
{
    private const string MimicryTechniqueNamespace = "SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry";
    private const string NpcAbilityNamespace = "SWLOR.Game.Server.Feature.AbilityDefinition.NPC";
    private const int CustomTlkStrrefOffset = 16777216;

    // Feat, display name, required Mimicry rank, slot cost, source NPC feat this technique is copied from.
    // Mirrors the ORIGINAL "Technique pool" table (the first 10 techniques shipped). These rows
    // pin exact tuning for the original pool and must not be touched when the pool expands;
    // full-pool coverage is asserted separately by the reflection-driven tests below.
    private static readonly (FeatType Technique, string Name, int RequiredRank, int SlotCost, FeatType SourceFeat)[] TechniqueTable =
    {
        (FeatType.ToxicSpitTechnique, "Toxic Spit", 28, 1, FeatType.ToxicSpit),
        (FeatType.FrostSpitTechnique, "Frost Spit", 3, 1, FeatType.FrostSpit),
        (FeatType.RakingClawsTechnique, "Raking Claws", 6, 1, FeatType.RakingClaws),
        (FeatType.SonicShriekTechnique, "Sonic Shriek", 0, 2, FeatType.SonicShriek),
        (FeatType.TailSweepTechnique, "Tail Sweep", 10, 2, FeatType.TailSweep),
        (FeatType.StaticWebTechnique, "Static Web", 1, 2, FeatType.StaticWeb),
        (FeatType.GoringChargeTechnique, "Goring Charge", 14, 2, FeatType.GoringCharge),
        (FeatType.ToxicCloudTechnique, "Toxic Cloud", 33, 3, FeatType.ToxicCloud),
        (FeatType.ScorchingBreathTechnique, "Scorching Breath", 50, 3, FeatType.ScorchingBreath),
        (FeatType.TerrifyingBellowTechnique, "Terrifying Bellow", 11, 3, FeatType.TerrifyingBellow),
    };

    [Test]
    public void MimicryTechniques_RegisterFeatsWithCommonAbilityContract()
    {
        foreach (var entry in TechniqueTable)
        {
            var abilities = BuildTechnique(entry.Technique);

            abilities.Should().ContainKey(entry.Technique, $"{entry.Technique} should register itself");
            var ability = abilities[entry.Technique];

            ((int)entry.Technique).Should().BeInRange(2796, 2805);
            ability.Name.Should().NotBeNullOrWhiteSpace();
            ability.IsMimicryTechnique.Should().BeTrue();
            ability.MimicrySkillRequirement.Should().BeInRange(0, 50);
            ability.MimicrySlotCost.Should().BeInRange(1, 3);
            ability.MimicrySourceFeat.Should().NotBe(FeatType.Invalid);
            ability.EffectiveLevelPerkType.Should().Be(PerkType.CombatAnalyzer);
            ability.SkillType.Should().Be(SkillType.Mimicry);
            ability.IsHostileAbility.Should().BeTrue($"{entry.Technique} is copied from a hostile NPC ability");
        }
    }

    [Test]
    public void MimicryTechniques_RankRequirementAndSlotCostMatchPoolTable()
    {
        foreach (var entry in TechniqueTable)
        {
            var ability = BuildTechnique(entry.Technique)[entry.Technique];

            ability.Name.Should().Be(entry.Name);
            ability.MimicrySkillRequirement.Should().Be(entry.RequiredRank,
                $"{entry.Technique} rank requirement should match the reviewed encounter progression");
            ability.MimicrySlotCost.Should().Be(entry.SlotCost, $"{entry.Technique} slot cost should match the pool table");
            ability.MimicrySourceFeat.Should().Be(entry.SourceFeat);
        }
    }

    [Test]
    public void MimicryTechniqueSourceFeats_MatchRegisteredNpcAbilityDefinitions()
    {
        foreach (var entry in TechniqueTable)
        {
            var npcAbilities = BuildNpcSource(entry.SourceFeat);

            npcAbilities.Should().ContainKey(
                entry.SourceFeat,
                $"{entry.Technique}'s MimicrySourceFeat ({entry.SourceFeat}) should be a real, registered NPC ability");
        }
    }

    // Registry-driven contract check covering every technique in the (currently 88-strong) pool,
    // not just the original 10 pinned above. Discovers definitions by reflection so it stays valid
    // as the technique pool grows.
    [Test]
    public void MimicryTechniques_AllRegisterFeatsWithCommonAbilityContract()
    {
        var npcAbilitiesByFeat = BuildAllAbilities(NpcAbilityNamespace)
            .ToDictionary(a => a.Feat, a => a.Detail);
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);

        techniques.Should().NotBeEmpty("technique discovery should find definitions in the Mimicry ability namespace");

        foreach (var technique in techniques)
        {
            var feat = technique.Feat;
            var ability = technique.Detail;

            ability.Name.Should().NotBeNullOrWhiteSpace($"{feat} should have a display name");
            ability.Name.Should().NotContain("Technique", $"{feat}'s player-facing name should not carry the 'Technique' label");
            ability.IsMimicryTechnique.Should().BeTrue($"{feat} should be marked as a Mimicry technique");
            ability.MimicrySkillRequirement.Should().BeInRange(0, 50,
                $"{feat}'s MimicrySkillRequirement should be a valid skill rank");
            ability.MimicrySlotCost.Should().BeInRange(1, 3, $"{feat}'s MimicrySlotCost should be between 1 and 3");
            ability.EffectiveLevelPerkType.Should().Be(PerkType.CombatAnalyzer, $"{feat} should scale with Combat Analyzer level");
            ability.SkillType.Should().Be(SkillType.Mimicry, $"{feat} should use the Mimicry skill");
            ability.MimicrySourceFeat.Should().NotBe(FeatType.Invalid, $"{feat} should declare a MimicrySourceFeat");

            npcAbilitiesByFeat.Should().ContainKey(ability.MimicrySourceFeat,
                $"{feat}'s MimicrySourceFeat ({ability.MimicrySourceFeat}) should be a registered NPC ability");

            var sourceAbility = npcAbilitiesByFeat[ability.MimicrySourceFeat];
            ability.Name.Should().Be(sourceAbility.Name,
                $"{feat}'s name should match the creature ability it replicates ({ability.MimicrySourceFeat})");

            // Passive traits have no activation, and stances / non-damaging support utilities are not
            // hostile casts, so hostility (an activation concept mirrored from the source) only applies
            // to ordinary active techniques.
            if (!ability.IsMimicryTrait && !ability.IsMimicryStance && !ability.IsMimicryUtility)
            {
                ability.IsHostileAbility.Should().Be(sourceAbility.IsHostileAbility,
                    $"{feat}'s IsHostileAbility should mirror its source NPC ability {ability.MimicrySourceFeat}'s hostility " +
                    "(self-buff sources are not hostile, so techniques copied from them shouldn't be either)");
            }
        }
    }

    [Test]
    public void MimicryStatusDurationsAndDetonationOrderMatchTheReviewedBiblePayloads()
    {
        var root = FindRepositoryRoot();
        var seismic = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Mimicry",
            "SeismicSlamTechniqueAbilityDefinition.cs"));
        var rupturing = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Mimicry",
            "RupturingQuakeTechniqueAbilityDefinition.cs"));
        var disorienting = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Mimicry",
            "DisorientingScreechTechniqueAbilityDefinition.cs"));
        var lockstep = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Mimicry",
            "LockstepCrushTechniqueAbilityDefinition.cs"));
        var merciless = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Mimicry",
            "MercilessAngleTechniqueAbilityDefinition.cs"));

        seismic.Should().MatchRegex(@"ResolveSkillType\(activator, profile\),\s*28,\s*6,\s*typeof\(KnockdownStatusEffect\)");
        rupturing.Should().MatchRegex(@"ResolveSkillType\(activator, profile\),\s*40,\s*6,\s*typeof\(KnockdownStatusEffect\)");
        rupturing.Should().Contain("StatusEffect.ApplyStatusEffect<SunderStatusEffect>(activator, hitTarget, 30f");
        rupturing.Should().NotContain("additionalStatusEffects: new[] { typeof(SunderStatusEffect) }");
        disorienting.Should().MatchRegex(@"ResolveSkillType\(activator, profile\),\s*0,\s*30,\s*typeof\(DisorientedStatusEffect\)");
        lockstep.Should().Contain("StatusEffect.ApplyStatusEffect<SunderStatusEffect>(activator, target, 30f");
        lockstep.Should().NotContain("additionalStatusEffects: new[] { typeof(SunderStatusEffect) }");
        merciless.Should().Contain("if (StatusEffect.HasStatusEffect(target, typeof(BleedStatusEffect), typeof(HemorrhageStatusEffect)))");
        merciless.Should().Contain("StatusEffect.ApplyStatusEffect<HemorrhageStatusEffect>(activator, target, 30f");
        merciless.Should().NotContain("typeof(HemorrhageStatusEffect),\r\n                CombatImpactAreaShape.Cone");
    }

    [Test]
    public void ShockTechniques_DescriptionsExposeTheirActualForceSuppressionContract()
    {
        var root = FindRepositoryRoot();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));
        var tlkEntries = ReadTlkEntries(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json")));
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace)
            .ToDictionary(technique => technique.Feat, technique => technique.Detail);

        foreach (var feat in new[] { FeatType.DarkShockTechnique, FeatType.NullShockTechnique })
        {
            var ability = techniques[feat];
            ability.SkillType.Should().Be(SkillType.Mimicry);
            ability.CombatImpactDamageAbility.Should().Be(AbilityType.Willpower,
                $"{feat}'s hostile hit check should use WIL even though the technique deals no direct damage");

            var descriptionStrref = int.Parse(featRows[(int)feat]["DESCRIPTION"]);
            var description = tlkEntries[descriptionStrref - CustomTlkStrrefOffset];
            description.Should().Contain("separate WIL-based Mimicry hit checks");
            description.Should().Contain(
                "reducing Attack by 10% and Force Attack by an additional 15% (25% total)");
            description.Should().Contain("Deals no direct damage.");
        }

        var suppression = new ForceSuppressionStatusEffect();
        suppression.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
        suppression.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-15);
    }

    [Test]
    public void MimicryDescriptions_DistinguishAccuracyRatingSkillScalingAndDamageType()
    {
        var root = FindRepositoryRoot();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));
        var tlkEntries = ReadTlkEntries(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json")));
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace)
            .ToDictionary(technique => technique.Feat, technique => technique.Detail);

        string Description(FeatType feat)
        {
            var descriptionStrref = int.Parse(featRows[(int)feat]["DESCRIPTION"]);
            return tlkEntries[descriptionStrref - CustomTlkStrrefOffset];
        }

        foreach (var (feat, percent) in new[]
                 {
                     (FeatType.MindSpikeTechnique, 6),
                     (FeatType.RangefinderShotTechnique, 8)
                 })
        {
            techniques[feat].MimicryTraitStats[StatType.AccuracyPercentAdjustment].Should().Be(percent);
            Description(feat).Should().Contain($"increases your Accuracy rating by {percent}%");
            Description(feat).Should().Contain("including Force and Mimicry");
            Description(feat).Should().Contain($"does not add {percent} percentage points directly to hit chance");
        }

        foreach (var feat in new[] { FeatType.FinalEclipseTechnique, FeatType.FinalLineTechnique })
        {
            techniques[feat].SkillType.Should().Be(SkillType.Mimicry);
            techniques[feat].CombatImpactDamageAbility.Should().Be(AbilityType.Might);
            Description(feat).Should().StartWith("Uses Mimicry rank and MGT for its hit check.");
        }

        Description(FeatType.FinalEclipseTechnique).Should().Contain(
            "Force is the damage type; this remains a Mimicry ability.");
        Description(FeatType.FinalLineTechnique).Should().Contain(
            "After the normal damage roll, each hit gains +0% damage at full HP, scaling smoothly up to +35% as that target nears defeat.");
    }

    // Every damaging (active) technique must declare a scaling attribute so its damage tracks player
    // stats like native abilities (a technique with no CombatImpactDamageAbility gets zero stat
    // scaling). Assignment is spread across all six attributes rather than defaulting to one stat.
    // Trait techniques are passive (no activated damage), so they are exempt and validated separately.
    [Test]
    public void MimicryTechniques_DeclareScalingStatsSpanningAllSixAttributes()
    {
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);
        var activeTechniques = techniques
            .Where(t => !t.Detail.IsMimicryTrait && !t.Detail.IsMimicryStance && !t.Detail.IsMimicryUtility)
            .ToList();
        var usedStats = new HashSet<AbilityType>();

        foreach (var technique in activeTechniques)
        {
            technique.Detail.CombatImpactDamageAbility.Should().NotBe(AbilityType.Invalid,
                $"{technique.Feat} should declare a scaling attribute via CombatImpactDamageAbility");
            usedStats.Add(technique.Detail.CombatImpactDamageAbility);
        }

        var allAttributes = new[]
        {
            AbilityType.Might, AbilityType.Perception, AbilityType.Vitality,
            AbilityType.Agility, AbilityType.Willpower, AbilityType.Social
        };
        foreach (var attribute in allAttributes)
        {
            usedStats.Should().Contain(attribute,
                $"at least one active technique should scale off {attribute} so no attribute is unused");
        }
    }

    // Trait techniques are passive: while equipped they contribute static stats read straight from
    // the loadout, rather than applying a status effect to the wearer. They must declare at least one
    // stat or resistance, have no impact action, and (being non-damaging) must not declare a combat
    // scaling attribute. Equipping a trait deliberately applies no persistent status effect: the
    // bonus never changes while the trait is slotted, so there is no transient state for the status
    // icon bar to communicate. On-hit proc traits still inflict status effects on their targets;
    // that is a combat payload, not part of the trait's own lifecycle.
    [Test]
    public void MimicryTraits_ArePassiveAndDeclareStaticStats()
    {
        var traits = BuildAllAbilities(MimicryTechniqueNamespace)
            .Where(t => t.Detail.IsMimicryTrait)
            .ToList();

        traits.Should().NotBeEmpty("some techniques are converted to passive traits");

        foreach (var trait in traits)
        {
            (trait.Detail.MimicryTraitStats.Count + trait.Detail.MimicryTraitResistances.Count)
                .Should().BeGreaterThan(0,
                    $"{trait.Feat} is a trait and should declare the stats granted while equipped");
            trait.Detail.MimicryTraitStats.Keys.Should().NotContain(StatType.Invalid,
                $"{trait.Feat} should not declare an invalid stat");
            trait.Detail.MimicryTraitResistances.Keys.Should().NotContain(ResistanceType.Invalid,
                $"{trait.Feat} should not declare an invalid resistance");
            trait.Detail.ImpactAction.Should().BeNull(
                $"{trait.Feat} is a passive trait and should not have an activated impact action");
            trait.Detail.CombatImpactDamageAbility.Should().Be(AbilityType.Invalid,
                $"{trait.Feat} is a passive trait and should not declare a combat scaling attribute");
        }
    }

    [Test]
    public void MimicryTraits_AlternateLoadoutRolesAreMutuallyExclusive()
    {
        Ability.CacheData();
        Mimicry.CacheData();

        var techniques = BuildAllAbilities(MimicryTechniqueNamespace)
            .ToDictionary(technique => technique.Feat, technique => technique.Detail);

        techniques[FeatType.ChitinGuardTechnique].MimicryTraitFamily
            .Should().Be(MimicryTraitFamily.Carapace);
        techniques[FeatType.IronCarapaceTechnique].MimicryTraitFamily
            .Should().Be(MimicryTraitFamily.Carapace);
        techniques[FeatType.ForceRendTechnique].MimicryTraitFamily
            .Should().Be(MimicryTraitFamily.ForceOffense);
        techniques[FeatType.EssenceScarTechnique].MimicryTraitFamily
            .Should().Be(MimicryTraitFamily.ForceOffense);

        var loadout = new Player();
        loadout.EquippedTechniques.Add(FeatType.IronCarapaceTechnique);
        loadout.EquippedTechniques.Add(FeatType.ForceRendTechnique);

        Mimicry.GetTraitFamilyConflict(loadout, FeatType.ChitinGuardTechnique)
            .Should().Be(FeatType.IronCarapaceTechnique);
        Mimicry.GetTraitFamilyConflict(loadout, FeatType.EssenceScarTechnique)
            .Should().Be(FeatType.ForceRendTechnique);
        Mimicry.GetTraitFamilyConflict(loadout, FeatType.ApexCollapseTechnique)
            .Should().Be(FeatType.Invalid,
                "Apex Collapse keeps its defense tradeoff by allowing one, but not both, carapace traits");
    }

    [Test]
    public void MimicryTraitLoadouts_StayWithinTheReviewedTenSlotCeilings()
    {
        var traits = BuildAllAbilities(MimicryTechniqueNamespace)
            .Where(t => t.Detail.IsMimicryTrait)
            .Select(t => t.Detail)
            .ToArray();
        var maximumByStat = new Dictionary<StatType, int>();
        var maximumByResistance = new Dictionary<ResistanceType, int>();
        var maximumCombinedDefense = 0;
        var maximumCombinedResistance = 0;

        void Enumerate(
            int index,
            int usedSlots,
            Dictionary<StatType, int> stats,
            Dictionary<ResistanceType, int> resistances,
            HashSet<MimicryTraitFamily> families)
        {
            if (index == traits.Length)
            {
                foreach (var (stat, value) in stats)
                    maximumByStat[stat] = Math.Max(maximumByStat.GetValueOrDefault(stat), value);
                foreach (var (resistance, value) in resistances)
                    maximumByResistance[resistance] = Math.Max(maximumByResistance.GetValueOrDefault(resistance), value);

                maximumCombinedDefense = Math.Max(
                    maximumCombinedDefense,
                    stats.GetValueOrDefault(StatType.PhysicalDefensePercentAdjustment) +
                    stats.GetValueOrDefault(StatType.ForceDefensePercentAdjustment));
                maximumCombinedResistance = Math.Max(maximumCombinedResistance, resistances.Values.Sum());
                return;
            }

            Enumerate(index + 1, usedSlots, stats, resistances, families);

            var trait = traits[index];
            if (usedSlots + trait.MimicrySlotCost > 10)
                return;
            if (trait.MimicryTraitFamily != MimicryTraitFamily.None &&
                families.Contains(trait.MimicryTraitFamily))
                return;

            var withStats = new Dictionary<StatType, int>(stats);
            foreach (var (stat, value) in trait.MimicryTraitStats)
                withStats[stat] = withStats.GetValueOrDefault(stat) + value;
            var withResistances = new Dictionary<ResistanceType, int>(resistances);
            foreach (var (resistance, value) in trait.MimicryTraitResistances)
                withResistances[resistance] = withResistances.GetValueOrDefault(resistance) + value;
            var withFamilies = new HashSet<MimicryTraitFamily>(families);
            if (trait.MimicryTraitFamily != MimicryTraitFamily.None)
                withFamilies.Add(trait.MimicryTraitFamily);

            Enumerate(index + 1, usedSlots + trait.MimicrySlotCost, withStats, withResistances, withFamilies);
        }

        Enumerate(
            0,
            0,
            new Dictionary<StatType, int>(),
            new Dictionary<ResistanceType, int>(),
            new HashSet<MimicryTraitFamily>());

        maximumByStat.Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.AccuracyPercentAdjustment] = 18,
            [StatType.AttackPercentAdjustment] = 6,
            [StatType.CriticalRatePercentAdjustment] = 6,
            [StatType.DamageDealtBleedChance] = 37,
            [StatType.DamageDealtFreezingChance] = 33,
            [StatType.DamageDealtHemorrhageChance] = 15,
            [StatType.DamageDealtPoisonChance] = 18,
            [StatType.DamageDealtShockChance] = 18,
            [StatType.DamageDealtSunderChance] = 21,
            [StatType.ForceAttackPercentAdjustment] = 8,
            [StatType.ForceDefensePercentAdjustment] = 15,
            [StatType.PhysicalDefensePercentAdjustment] = 15
        });
        maximumByResistance.Should().BeEquivalentTo(new Dictionary<ResistanceType, int>
        {
            [ResistanceType.Fire] = 20,
            [ResistanceType.Poison] = 20,
            [ResistanceType.Trauma] = 25
        });
        maximumCombinedDefense.Should().Be(25,
            "only one defensive carapace trait may contribute to a loadout");
        maximumCombinedResistance.Should().Be(55,
            "only one defensive carapace trait may contribute to a loadout");
    }

    // The builder is the boundary where a bad trait declaration should fail loudly. An Invalid stat
    // or resistance would otherwise be stored and summed into the player's totals at runtime under a
    // sentinel key that no consumer reads, silently costing the trait its bonus.
    [Test]
    public void MimicryTraitBuilder_RejectsInvalidStatsAndResistances()
    {
        static AbilityBuilder Trait() => new AbilityBuilder()
            .Create(FeatType.ChitinGuardTechnique, PerkType.CombatAnalyzer)
            .Name("Contract Test")
            .MimicryTrait(FeatType.ChitinGuard, 19, 2);

        Trait().Invoking(b => b.MimicryTraitStat(StatType.Invalid, 10))
            .Should().Throw<ArgumentException>("an Invalid stat is not a real stat");
        Trait().Invoking(b => b.MimicryTraitResistance(ResistanceType.Invalid, 10))
            .Should().Throw<ArgumentException>("an Invalid resistance is not a real resistance");
        Trait().Invoking(b => b.MimicryTraitFamily(MimicryTraitFamily.None))
            .Should().Throw<ArgumentException>("None does not identify an exclusive trait family");

        // Both helpers require MimicryTrait first, so a plain technique cannot accrue trait stats.
        static AbilityBuilder PlainTechnique() => new AbilityBuilder()
            .Create(FeatType.ToxicSpitTechnique, PerkType.CombatAnalyzer)
            .Name("Contract Test")
            .MimicryTechnique(FeatType.ToxicSpit, 24, 1);

        PlainTechnique().Invoking(b => b.MimicryTraitStat(StatType.AccuracyPercentAdjustment, 4))
            .Should().Throw<ArgumentException>("only traits declare trait stats");
        PlainTechnique().Invoking(b => b.MimicryTraitResistance(ResistanceType.Fire, 20))
            .Should().Throw<ArgumentException>("only traits declare trait resistances");
        PlainTechnique().Invoking(b => b.MimicryTraitFamily(MimicryTraitFamily.Carapace))
            .Should().Throw<ArgumentException>("only traits declare exclusive trait families");
    }

    [TestCase(-1)]
    [TestCase(51)]
    public void MimicryTechniqueBuilder_RejectsInvalidSkillRequirements(int skillRequirement)
    {
        var builder = new AbilityBuilder()
            .Create(FeatType.ToxicSpitTechnique, PerkType.CombatAnalyzer)
            .Name("Contract Test");

        builder.Invoking(b => b.MimicryTechnique(FeatType.ToxicSpit, skillRequirement, 1))
            .Should().Throw<ArgumentException>("technique requirements must fit within the Mimicry skill's 0-50 range");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void MimicryTechniqueBuilder_DoesNotDiscardExplicitAreaRange(bool targetingFirst)
    {
        var builder = new AbilityBuilder()
            .Create(FeatType.ToxicSpitTechnique, PerkType.CombatAnalyzer)
            .Name("Contract Test")
            .IsCastedAbility()
            .IsAreaAbility()
            .HasMaxRange(8f);

        if (targetingFirst)
        {
            builder
                .HasTargetingCone(
                    Spell.CryoBileTechnique,
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .MimicryTechnique(FeatType.ToxicSpit, 24, 1);
        }
        else
        {
            builder
                .MimicryTechnique(FeatType.ToxicSpit, 24, 1)
                .HasTargetingCone(
                    Spell.CryoBileTechnique,
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);
        }

        var detail = builder.Build()[FeatType.ToxicSpitTechnique];
        detail.HasExplicitMaxRange.Should().BeTrue();
        detail.MaxRange.Should().Be(8f);
    }

    // The declaration tests above prove traits carry data, but not that the data is ever read. The
    // stat and resistance pipelines are where a trait actually becomes a bonus, and deleting either
    // hook would silently zero every trait while leaving all the declaration tests green. A true
    // runtime assertion is not possible here -- Mimicry.GetStatBonus calls GetIsPC and DB.Get, and
    // this suite has no engine or database -- so the integration points are pinned at the source
    // level, matching how CombatDamageTests guards its own wiring.
    [Test]
    public void MimicryTraitBonuses_AreWiredIntoTheStatAndResistancePipelines()
    {
        var root = FindRepositoryRoot();
        var statSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Stat.cs"));
        var resistanceSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Resistance.cs"));
        var mimicrySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Mimicry.cs"));

        statSource.Should().Contain("Mimicry.GetStatBonus(creature, stat)",
            "equipped trait stats reach the player only through the stat pipeline");
        resistanceSource.Should().Contain("Mimicry.GetResistanceBonus(creature, type)",
            "equipped trait resistances reach the player only through the resistance pipeline");

        // Traits are deliberately not status effects: nothing should grant or revoke them, and the
        // resonance set bonus is derived on read rather than re-applied on loadout change.
        mimicrySource.Should().NotContain("TechniqueResonanceStatusEffect",
            "the elemental-resonance set bonus is derived from the loadout, not applied as an effect");
        mimicrySource.Should().NotContain("MimicryTraitStatusEffect",
            "traits contribute static stats and no longer apply a status effect");
    }

    // Active damage techniques must register their damage element so the elemental-resonance
    // loadout bonus can count them (InnateAbility.BuildArea/BuildSingle set MimicryElement from the
    // damage type). A technique left with an Invalid element would be silently omitted from resonance.
    [Test]
    public void MimicryActiveTechniques_RegisterTheirDamageElement()
    {
        var activeTechniques = BuildAllAbilities(MimicryTechniqueNamespace)
            .Where(t => !t.Detail.IsMimicryTrait && !t.Detail.IsMimicryStance && !t.Detail.IsMimicryUtility)
            .ToList();

        activeTechniques.Should().NotBeEmpty("most techniques are active damage abilities");

        foreach (var technique in activeTechniques)
        {
            technique.Detail.MimicryElement.Should().NotBe(CombatDamageType.Invalid,
                $"{technique.Feat} is an active technique and must register a damage element for elemental resonance");
        }
    }

    // The core completeness requirement: every NPC ability the game registers must be learnable
    // through Mimicry, and every technique must copy a real NPC ability. Asserted bidirectionally
    // and 1:1 so the pool can never drift out of sync with the NPC ability roster.
    [Test]
    public void MimicryTechniques_EveryRegisteredNpcAbilityHasExactlyOneTechniqueTwin()
    {
        var npcAbilities = BuildAllAbilities(NpcAbilityNamespace);
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);

        npcAbilities.Should().NotBeEmpty("NPC ability discovery should find definitions in the NPC ability namespace");
        techniques.Should().NotBeEmpty("technique discovery should find definitions in the Mimicry ability namespace");

        var npcFeats = npcAbilities.Select(a => a.Feat).ToList();
        npcFeats.Should().OnlyHaveUniqueItems("each NPC ability definition should register a distinct FeatType");
        var npcFeatSet = npcFeats.ToHashSet();

        var techniqueFeats = techniques.Select(t => t.Feat).ToList();
        techniqueFeats.Should().OnlyHaveUniqueItems("each Mimicry technique should register a distinct FeatType");

        var techniquesBySourceFeat = techniques
            .GroupBy(t => t.Detail.MimicrySourceFeat)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Every technique's source feat must be a real, registered NPC ability.
        foreach (var technique in techniques)
        {
            npcFeatSet.Should().Contain(technique.Detail.MimicrySourceFeat,
                $"{technique.Feat}'s MimicrySourceFeat ({technique.Detail.MimicrySourceFeat}) should be a registered NPC ability");
        }

        // Every registered NPC ability must be learnable through exactly one technique.
        foreach (var npc in npcAbilities)
        {
            techniquesBySourceFeat.Should().ContainKey(npc.Feat,
                $"NPC ability {npc.Feat} (from {npc.DefinitionType.Name}) has no Mimicry technique that copies it");

            techniquesBySourceFeat[npc.Feat].Should().ContainSingle(
                $"NPC ability {npc.Feat} should have exactly one Mimicry technique twin, " +
                $"found {(techniquesBySourceFeat.TryGetValue(npc.Feat, out var twins) ? twins.Count : 0)}");
        }
    }

    [Test]
    public void MimicryPerkDefinition_BuildsExpectedPerksAndLevels()
    {
        var perks = BuildPerksWithout2daLookup(new MimicryPerkDefinition(),
            "CombatAnalyzer", "AnalyzerMemory", "PatternRecognition", "OverclockedAnalyzer");

        perks.Should().HaveCount(4);
        perks.Values.Should().OnlyContain(perk => perk.Category == PerkCategoryType.Mimicry);

        // Combat Analyzer I unlocks the system; subsequent levels improve technique potency.
        // Individual technique availability is governed directly by Mimicry skill requirements.
        var combatAnalyzer = perks[PerkType.CombatAnalyzer];
        combatAnalyzer.Name.Should().Be("Combat Analyzer");
        combatAnalyzer.PerkLevels.Should().HaveCount(4);
        combatAnalyzer.RefundedTriggers.Should().ContainSingle();
        var expectedAnalyzerPrices = new[] { 2, 3, 3, 3 };
        var expectedAnalyzerSkillRanks = new[] { 0, 15, 30, 45 };
        var expectedAnalyzerDescriptions = new[]
        {
            "Grants a combat analyzer capable of recording enemy creature techniques. Unlocks technique learning and the Techniques window. Provides 2 technique slots.",
            "Upgrades the combat analyzer, increasing equipped technique potency by 5%.",
            "Further upgrades the combat analyzer, increasing equipped technique potency by 10% in total.",
            "Maximizes the combat analyzer, increasing equipped technique potency by 15% in total.",
        };
        for (var level = 1; level <= 4; level++)
        {
            combatAnalyzer.PerkLevels[level].Price.Should().Be(expectedAnalyzerPrices[level - 1]);
            AssertSkillRequirement(combatAnalyzer.PerkLevels[level], SkillType.Mimicry, expectedAnalyzerSkillRanks[level - 1]);
            combatAnalyzer.PerkLevels[level].Description.Should().Be(
                expectedAnalyzerDescriptions[level - 1],
                "Combat Analyzer ranks improve potency but never gate technique rank bands");
        }
        combatAnalyzer.PerkLevels.Values.Should().OnlyContain(
            lvl => !lvl.Requirements.OfType<PerkRequirementMustHavePerk>().Any(),
            "Combat Analyzer is the root perk and requires no other perk");

        var memory = perks[PerkType.AnalyzerMemory];
        memory.Name.Should().Be("Analyzer Memory");
        memory.PerkLevels.Should().HaveCount(3);
        memory.RefundedTriggers.Should().ContainSingle();
        var expectedMemoryPrices = new[] { 2, 3, 4 };
        var expectedMemorySkillRanks = new[] { 10, 25, 40 };
        for (var level = 1; level <= 3; level++)
        {
            memory.PerkLevels[level].Price.Should().Be(expectedMemoryPrices[level - 1]);
            AssertSkillRequirement(memory.PerkLevels[level], SkillType.Mimicry, expectedMemorySkillRanks[level - 1]);
            AssertMustHavePerkRequirement(memory.PerkLevels[level], PerkType.CombatAnalyzer);
        }

        var pattern = perks[PerkType.PatternRecognition];
        pattern.Name.Should().Be("Pattern Recognition");
        pattern.PerkLevels.Should().HaveCount(2);
        pattern.RefundedTriggers.Should().BeEmpty();
        var expectedPatternPrices = new[] { 2, 3 };
        var expectedPatternSkillRanks = new[] { 10, 30 };
        for (var level = 1; level <= 2; level++)
        {
            pattern.PerkLevels[level].Price.Should().Be(expectedPatternPrices[level - 1]);
            AssertSkillRequirement(pattern.PerkLevels[level], SkillType.Mimicry, expectedPatternSkillRanks[level - 1]);
            AssertMustHavePerkRequirement(pattern.PerkLevels[level], PerkType.CombatAnalyzer);
        }

        var overclocked = perks[PerkType.OverclockedAnalyzer];
        overclocked.Name.Should().Be("Overclocked Analyzer");
        overclocked.PerkLevels.Should().HaveCount(1);
        overclocked.RefundedTriggers.Should().ContainSingle();
        overclocked.PerkLevels[1].Price.Should().Be(6);
        AssertSkillRequirement(overclocked.PerkLevels[1], SkillType.Mimicry, 50);
        AssertMustHavePerkRequirement(overclocked.PerkLevels[1], PerkType.CombatAnalyzer);

        // Each perk grants exactly its icon feat at level 1 (perk-window icon resolution): the trait
        // perks grant a passive marker feat, the capstone grants the Overload active ability feat.
        // Technique feats are granted only by the equip system, never by perks.
        var expectedIconFeats = new Dictionary<PerkType, FeatType>
        {
            [PerkType.CombatAnalyzer] = FeatType.CombatAnalyzerTrait,
            [PerkType.AnalyzerMemory] = FeatType.AnalyzerMemoryTrait,
            [PerkType.PatternRecognition] = FeatType.PatternRecognitionTrait,
            [PerkType.OverclockedAnalyzer] = FeatType.Overload,
        };
        var techniqueFeats = BuildAllAbilities(MimicryTechniqueNamespace).Select(x => x.Feat).ToHashSet();

        foreach (var (perkType, perk) in perks)
        {
            perk.PerkLevels[1].GrantedFeats.Should().Equal(new[] { expectedIconFeats[perkType] },
                $"{perk.Name} level 1 should grant only its icon feat");

            foreach (var level in perk.PerkLevels.Values)
            {
                level.GrantedFeats.Should().NotContain(feat => techniqueFeats.Contains(feat),
                    $"{perk.Name} should never grant technique feats directly");
            }
        }
    }

    // Registry-driven 2DA linkage check covering every technique in the pool. Replaces the old
    // fixed SPELLID range assertion (1599-1608), which only fit the original 10-row pool, with a
    // check that each technique's SPELLID resolves and round-trips through spells.2da and
    // CLS_FEAT_FIGHT.2da regardless of how many techniques exist.
    [Test]
    public void MimicryTechniques_Feat2daRowsLinkToSpellsAndClassFeatTable()
    {
        var root = FindRepositoryRoot();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));
        var spellRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "spells.2da")));
        var classFeatRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "CLS_FEAT_FIGHT.2da")));

        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);
        techniques.Should().NotBeEmpty();

        foreach (var technique in techniques)
        {
            var feat = technique.Feat;
            var featId = (int)feat;

            featRows.Should().ContainKey(featId, $"feat.2da should have a row for {feat}");
            var featRow = featRows[featId];
            featRow["LABEL"].Should().Be(feat.ToString(), $"{feat}'s feat.2da LABEL should match its enum name");
            featRow["ICON"].Should().NotBeNullOrWhiteSpace($"{feat} should have a feat.2da ICON");
            var iconResRef = featRow["ICON"];
            File.Exists(Path.Combine(root.FullName, "SWLOR_Haks", "sw_ability", $"{iconResRef}.tga"))
                .Should()
                .BeTrue($"{feat}'s feat icon '{iconResRef}' should exist");

            var matchingTechniqueSpellRows = spellRows.Values
                .Where(row => row.GetValueOrDefault("Label") == feat.ToString())
                .ToList();
            matchingTechniqueSpellRows.Should().ContainSingle($"{feat} should have exactly one matching spells.2da row");
            matchingTechniqueSpellRows[0]["IconResRef"].Should().Be(
                iconResRef,
                $"{feat}'s feat and spell rows should use the same curated technique icon");

            featRow.TryGetValue("SPELLID", out var spellIdText).Should().BeTrue($"{feat}'s feat.2da row should have a SPELLID column");

            if (technique.Detail.IsMimicryTrait)
            {
                // Passive traits are not cast: their feat.2da row must use the blank SPELLID sentinel.
                spellIdText.Should().Be("****",
                    $"{feat} is a passive trait and should have a blank SPELLID (****), was '{spellIdText}'");
            }
            else
            {
                int.TryParse(spellIdText, out var spellId).Should().BeTrue($"{feat}'s SPELLID should be numeric, was '{spellIdText}'");

                spellRows.Should().ContainKey(spellId, $"spells.2da should have a row for {feat} (spell id {spellId})");
                var spellRow = spellRows[spellId];
                spellRow.TryGetValue("FeatID", out var featIdText).Should().BeTrue($"{feat}'s spells.2da row should have a FeatID column");
                int.TryParse(featIdText, out var linkedFeatId).Should().BeTrue($"{feat}'s spells.2da FeatID should be numeric, was '{featIdText}'");
                linkedFeatId.Should().Be(featId, $"{feat}'s spells.2da row should point back at its feat.2da row");
            }

            var classFeatRowMatches = classFeatRows.Values.Where(row =>
                row.TryGetValue("FeatIndex", out var featIndexText) &&
                int.TryParse(featIndexText, out var featIndex) &&
                featIndex == featId).ToList();

            classFeatRowMatches.Should().ContainSingle(
                $"CLS_FEAT_FIGHT.2da should expose {feat} (feat id {featId}) exactly once");
            var classFeatRow = classFeatRowMatches[0];
            classFeatRow["List"].Should().Be("1", $"{feat} should be List=1 in CLS_FEAT_FIGHT.2da");
            classFeatRow["GrantedOnLevel"].Should().Be("99", $"{feat} should be GrantedOnLevel=99 in CLS_FEAT_FIGHT.2da");
            classFeatRow["OnMenu"].Should().Be("1", $"{feat} should be OnMenu=1 in CLS_FEAT_FIGHT.2da");
        }
    }

    // feat.2da targeting metadata must match each technique's shape so the client presents the
    // correct activation UX. Mirrors the AGENTS.md area-targeting convention:
    //   - single-target hostile cast  -> HostileFeat=1, TARGETSELF blank (shows a hostile cursor)
    //   - aimed area (line/cone, or a sphere placed at a chosen location) -> HostileFeat=1,
    //     TARGETSELF blank: the player picks the direction or ground point with a cursor, exactly
    //     like Earthshatter. A cone's geometry originating on the caster does not make it
    //     self-cast - the cursor chooses where it points.
    //   - self-centered area / stance / self-or-ally utility -> TARGETSELF=1, HostileFeat blank
    //   - passive trait -> both blank (never cast)
    // Guards against techniques shipping with unset or inverted targeting columns (TARGETSELF=1 on
    // an aimed shape silently kills its declared client targeting - no cursor, always fires from
    // the caster's facing).
    [Test]
    public void MimicryTechniques_Feat2daTargetingMatchesAbilityShape()
    {
        var root = FindRepositoryRoot();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));

        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);
        techniques.Should().NotBeEmpty();

        foreach (var technique in techniques)
        {
            var feat = technique.Feat;
            var detail = technique.Detail;

            featRows.Should().ContainKey((int)feat, $"feat.2da should have a row for {feat}");
            var row = featRows[(int)feat];
            var targetSelf = row.GetValueOrDefault("TARGETSELF", "****");
            var hostile = row.GetValueOrDefault("HostileFeat", "****");

            if (detail.IsMimicryTrait)
            {
                targetSelf.Should().Be("****", $"{feat} is a passive trait and is never cast");
                hostile.Should().Be("****", $"{feat} is a passive trait and is not a hostile cast");
                continue;
            }

            // An aimed area lets the player choose a direction or ground point, so it keeps the
            // hostile cursor; only self-centered areas, stances, and self/ally utility self-cast.
            var isAimedArea = detail.Targeting is { } targeting &&
                              (targeting.Shape is AbilityTargetingShapeType.Rect or AbilityTargetingShapeType.Cone ||
                               targeting.Shape == AbilityTargetingShapeType.Sphere &&
                               !targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf));
            var selfCasts = !isAimedArea &&
                            (detail.IsAreaAbility || detail.IsMimicryStance || detail.IsMimicryUtility);

            if (selfCasts)
            {
                targetSelf.Should().Be("1", $"{feat} originates on the caster and must not present a target cursor (TARGETSELF=1)");
                hostile.Should().Be("****", $"{feat} originates on the caster and must not be a hostile-cursor cast");
            }
            else
            {
                hostile.Should().Be("1", $"{feat} presents a hostile target cursor (HostileFeat=1)");
                targetSelf.Should().Be("****", $"{feat} takes a manual cursor and must not self-target");
            }
        }
    }

    [Test]
    public void MimicryDirectionAimedAreas_DoNotRangeCheckTheCursorPoint()
    {
        var offenders = BuildAllAbilities(MimicryTechniqueNamespace)
            .Where(technique => technique.Detail.RequiresLocationTarget)
            .Where(technique => technique.Detail.Targeting.Shape is
                AbilityTargetingShapeType.Rect or AbilityTargetingShapeType.Cone)
            .Where(technique => technique.Detail.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf))
            .Where(technique => technique.Detail.HasExplicitMaxRange)
            .Select(technique => technique.Feat)
            .ToList();

        offenders.Should().BeEmpty(
            "a line or cone cursor selects direction; the authored shape length limits actual reach, " +
            "so the clicked ground point must not produce an out-of-range rejection");
    }

    [Test]
    public void MimicryDirectionAimedAreas_ExplicitlyBackOffsetTheirOrigin()
    {
        var offenders = BuildAllAbilities(MimicryTechniqueNamespace)
            .Where(technique => technique.Detail.RequiresLocationTarget)
            .Where(technique => technique.Detail.Targeting.Shape is
                AbilityTargetingShapeType.Rect or AbilityTargetingShapeType.Cone)
            .Where(technique => technique.Detail.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf))
            .Where(technique => !technique.Detail.Targeting.Flags.HasFlag(AbilityTargetingFlags.BackOffsetOrigin))
            .Select(technique => technique.Feat)
            .ToList();

        offenders.Should().BeEmpty(
            "only explicitly flagged Mimicry lines and cones should move their apex behind the caster");
    }

    // Registry-driven TLK check covering every technique in the pool. Replaces the old fixed
    // strref-id range assertion (192553-192573), which only fit the original 10-row pool, with a
    // check that every technique's FEAT/DESCRIPTION strrefs are custom TLK references that
    // resolve to non-empty text, regardless of how many techniques exist or which TLK ids they use.
    [Test]
    public void MimicryTechniques_TlkEntriesAreNonEmptyForFeatAndDescriptionStrrefs()
    {
        var root = FindRepositoryRoot();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));
        var tlkEntries = ReadTlkEntries(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json")));

        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);
        techniques.Should().NotBeEmpty();

        foreach (var technique in techniques)
        {
            var feat = technique.Feat;
            var featId = (int)feat;

            featRows.Should().ContainKey(featId, $"feat.2da should have a row for {feat}");
            var featRow = featRows[featId];

            AssertCustomTlkStrrefIsNonEmpty(featRow, tlkEntries, feat, "FEAT", "name");
            AssertCustomTlkStrrefIsNonEmpty(featRow, tlkEntries, feat, "DESCRIPTION", "description");
        }
    }

    [Test]
    public void Mimicry_RequirementsCoverEveryRankAndRespectReviewedEncounterOrder()
    {
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace)
            .ToDictionary(x => x.Feat, x => x.Detail);

        techniques.Values
            .Select(detail => detail.MimicrySkillRequirement)
            .Distinct()
            .OrderBy(requirement => requirement)
            .Should()
            .Equal(Enumerable.Range(0, 51), "every Mimicry rank must unlock at least one technique");

        techniques[FeatType.SonicShriekTechnique]
            .MimicrySkillRequirement.Should().Be(0, "CZ-220 Mynocks are a level-1 source");
        techniques[FeatType.DisorientingScreechTechnique]
            .MimicrySkillRequirement.Should().Be(0, "CZ-220 Mynocks are a level-1 source");
        techniques[FeatType.PrecisionShotTechnique]
            .MimicrySkillRequirement.Should().Be(1, "CZ-220 Probe Droids are harder than the starter Mynocks");
        techniques[FeatType.StaticWebTechnique]
            .MimicrySkillRequirement.Should().Be(1, "CZ-220 Probe Droids are harder than the starter Mynocks");
        techniques[FeatType.SuppressingShotTechnique]
            .MimicrySkillRequirement.Should().Be(1, "CZ-220 Probe Droids are harder than the starter Mynocks");
        techniques[FeatType.WardenWallTechnique]
            .MimicrySkillRequirement.Should().Be(47, "level-50 boss techniques begin the final progression band");
        techniques[FeatType.ApexCollapseTechnique]
            .MimicrySkillRequirement.Should().Be(50, "apex boss techniques remain rank-50 rewards");
    }

    [Test]
    public void Mimicry_SkillDecayHandlerRevalidatesEquippedTechniques()
    {
        var handler = typeof(Mimicry).GetMethod(
            nameof(Mimicry.OnMimicrySkillDecay),
            BindingFlags.Public | BindingFlags.Static);

        handler.Should().NotBeNull();
        handler!.GetCustomAttributes<NWNEventHandler>()
            .Select(attribute => attribute.Script)
            .Should()
            .Contain(ScriptName.OnSwlorLoseSkill,
                "every Mimicry rank loss must immediately enforce equipped technique requirements");
    }

    [Test]
    public void MimicryTechniqueActivation_RequiresTheTechniqueToRemainEquipped()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs"));

        abilitySource.Should().Contain("ability.IsMimicryTechnique &&");
        abilitySource.Should().Contain("!Mimicry.IsTechniqueEquipped(activator, abilityType)",
            "both the initial and post-cast activation checks use CanUseAbility, so an unequipped technique cannot resolve");

        var equipped = new Player();
        equipped.EquippedTechniques.Add(FeatType.WardenWallTechnique);

        Mimicry.IsTechniqueEquipped(equipped, FeatType.WardenWallTechnique).Should().BeTrue();
        Mimicry.IsTechniqueEquipped(equipped, FeatType.SustainBurnTechnique).Should().BeFalse();
        Mimicry.IsTechniqueEquipped((Player)null, FeatType.WardenWallTechnique).Should().BeFalse();
    }

    [Test]
    public void MimicryTechniqueUnequip_RemovesDeclaredPersistentEffects()
    {
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace)
            .ToDictionary(technique => technique.Feat, technique => technique.Detail);

        foreach (var feat in new[]
                 {
                     FeatType.ApexCollapseTechnique,
                     FeatType.SustainBurnTechnique,
                     FeatType.WardenWallTechnique
                 })
        {
            techniques[feat].IsMimicryStance.Should().BeTrue();
            techniques[feat].StatusEffectTypesRemovedOnPerkRefund.Should().ContainSingle(
                $"{feat} must declare its permanent wearer effect for revocation cleanup");
        }

        techniques[FeatType.WardenWallTechnique]
            .SourceOwnedStatusEffectTypesRemovedOnPerkRefund
            .Should().Contain(typeof(WardenWallAuraStatusEffect),
                "unequipping Warden Wall must also remove the aura it granted to nearby allies");

        var root = FindRepositoryRoot();
        var mimicrySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Mimicry.cs"));
        var revokeStart = mimicrySource.IndexOf("private static void RevokeTechniqueFeat", StringComparison.Ordinal);
        var revokeEnd = mimicrySource.IndexOf("private static bool IsFeatOnHotBar", revokeStart, StringComparison.Ordinal);
        var revokeBody = mimicrySource[revokeStart..revokeEnd];

        revokeBody.Should().Contain("detail.StatusEffectTypesRemovedOnPerkRefund");
        revokeBody.Should().Contain("StatusEffect.RemoveStatusEffect(player, statusEffectType, false);");
        revokeBody.Should().Contain("detail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund");
        revokeBody.Should().Contain("StatusEffect.RemoveStatusEffectsFromAllTargetsBySource(");
    }

    [Test]
    public void Mimicry_LearnChanceScalesWithSkillRankPatternRecognitionAndPerception()
    {
        // Baseline: Mimicry rank at the technique requirement, no Pattern Recognition, Perception at the
        // baseline (10) contributes nothing beyond the flat 20% base chance.
        Mimicry.CalculateLearnChance(0, 0, 0, 10).Should().Be(20, "base chance with no bonuses");

        // Perception above the baseline adds 1% per point.
        Mimicry.CalculateLearnChance(0, 0, 0, 17).Should().Be(27, "7 Perception above baseline adds 7%");

        // Perception below the baseline never reduces the chance below the other contributions.
        Mimicry.CalculateLearnChance(0, 0, 0, 5).Should().Be(20, "Perception below baseline is floored at 0 contribution");

        // Skill rank above the technique requirement adds 2% per rank; Pattern Recognition adds 10% per level.
        Mimicry.CalculateLearnChance(20, 15, 2, 10).Should().Be(20 + 2 * 5 + 20, "rank delta and pattern recognition stack");

        // Everything is capped at the maximum learn chance.
        Mimicry.CalculateLearnChance(45, 0, 2, 60).Should().Be(75, "combined bonuses are clamped to the cap");
    }

    [Test]
    public void Mimicry_IsACombatPointEarningSkill()
    {
        // Mimicry combat points come from two sources: casting techniques, and the Combat Analyzer
        // recording nearby enemies' technique use (analysis points). Both only convert to Mimicry
        // skill XP when the creature dies if Mimicry is a non-Exempt combat-point category. The
        // attribute default is Exempt, so this guards that Mimicry stays a CP-earning skill.
        var attribute = typeof(SkillType).GetField(nameof(SkillType.Mimicry))!
            .GetCustomAttribute<SkillAttribute>();
        attribute.Should().NotBeNull();
        attribute!.CombatPointCategory.Should().Be(CombatPointCategoryType.Utility);
    }

    [Test]
    public void Mimicry_SetBonusPotencyRewardsSharedDamageTypes()
    {
        Ability.CacheData();
        Mimicry.CacheData();

        Mimicry.GetSetBonusPotency(new Player()).Should().Be(0, "an empty loadout has no resonance");

        var singles = new Player();
        singles.EquippedTechniques.Add(FeatType.ToxicSpitTechnique);  // Poison
        singles.EquippedTechniques.Add(FeatType.FrostSpitTechnique);  // Ice
        Mimicry.GetSetBonusPotency(singles).Should().Be(0, "no damage type has two equipped techniques");

        var pair = new Player();
        pair.EquippedTechniques.Add(FeatType.ToxicSpitTechnique);   // Poison
        pair.EquippedTechniques.Add(FeatType.ToxicCloudTechnique);  // Poison
        Mimicry.GetSetBonusPotency(pair).Should().Be(5, "two techniques share the Poison damage type");
    }

    [Test]
    public void Mimicry_MaxSlotsScalesWithCombatAnalyzerAndAnalyzerMemoryPerkLevels()
    {
        var noAnalyzer = new Player();
        Mimicry.GetMaxSlots(noAnalyzer).Should().Be(0, "players without Combat Analyzer get no technique slots");

        var memoryWithoutAnalyzer = new Player();
        memoryWithoutAnalyzer.Perks[PerkType.AnalyzerMemory] = 3;
        Mimicry.GetMaxSlots(memoryWithoutAnalyzer).Should().Be(0, "Analyzer Memory alone should not grant slots without Combat Analyzer");

        var baseAnalyzer = new Player();
        baseAnalyzer.Perks[PerkType.CombatAnalyzer] = 1;
        Mimicry.GetMaxSlots(baseAnalyzer).Should().Be(2);

        var memoryLevel1 = new Player();
        memoryLevel1.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel1.Perks[PerkType.AnalyzerMemory] = 1;
        Mimicry.GetMaxSlots(memoryLevel1).Should().Be(4, "Analyzer Memory grants +2 slots per rank");

        var memoryLevel2 = new Player();
        memoryLevel2.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel2.Perks[PerkType.AnalyzerMemory] = 2;
        Mimicry.GetMaxSlots(memoryLevel2).Should().Be(6);

        var memoryLevel3 = new Player();
        memoryLevel3.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel3.Perks[PerkType.AnalyzerMemory] = 3;
        Mimicry.GetMaxSlots(memoryLevel3).Should().Be(8);

        var overclocked = new Player();
        overclocked.Perks[PerkType.CombatAnalyzer] = 1;
        overclocked.Perks[PerkType.AnalyzerMemory] = 3;
        overclocked.Perks[PerkType.OverclockedAnalyzer] = 1;
        Mimicry.GetMaxSlots(overclocked).Should().Be(10, "2 base + 6 from Analyzer Memory + 2 from the Overclocked Analyzer capstone");
    }

    [Test]
    public void Mimicry_UsedSlotsSumsEquippedTechniqueSlotCosts()
    {
        Ability.CacheData();
        Mimicry.CacheData();

        var empty = new Player();
        Mimicry.GetUsedSlots(empty).Should().Be(0);

        var equipped = new Player();
        equipped.EquippedTechniques.Add(FeatType.ToxicSpitTechnique); // 1 slot
        equipped.EquippedTechniques.Add(FeatType.SonicShriekTechnique); // 2 slots
        equipped.EquippedTechniques.Add(FeatType.ToxicCloudTechnique); // 3 slots

        Mimicry.GetUsedSlots(equipped).Should().Be(6);
    }

    private static Dictionary<FeatType, AbilityDetail> BuildTechnique(FeatType technique)
    {
        IAbilityListDefinition definition = technique switch
        {
            FeatType.ToxicSpitTechnique => new ToxicSpitTechniqueAbilityDefinition(),
            FeatType.FrostSpitTechnique => new FrostSpitTechniqueAbilityDefinition(),
            FeatType.RakingClawsTechnique => new RakingClawsTechniqueAbilityDefinition(),
            FeatType.SonicShriekTechnique => new SonicShriekTechniqueAbilityDefinition(),
            FeatType.TailSweepTechnique => new TailSweepTechniqueAbilityDefinition(),
            FeatType.StaticWebTechnique => new StaticWebTechniqueAbilityDefinition(),
            FeatType.GoringChargeTechnique => new GoringChargeTechniqueAbilityDefinition(),
            FeatType.ToxicCloudTechnique => new ToxicCloudTechniqueAbilityDefinition(),
            FeatType.ScorchingBreathTechnique => new ScorchingBreathTechniqueAbilityDefinition(),
            FeatType.TerrifyingBellowTechnique => new TerrifyingBellowTechniqueAbilityDefinition(),
            _ => throw new ArgumentOutOfRangeException(nameof(technique), technique, "Unknown Mimicry technique")
        };

        return definition.BuildAbilities();
    }

    private static Dictionary<FeatType, AbilityDetail> BuildNpcSource(FeatType sourceFeat)
    {
        IAbilityListDefinition definition = sourceFeat switch
        {
            FeatType.ToxicSpit => new ToxicSpitAbilityDefinition(),
            FeatType.FrostSpit => new FrostSpitAbilityDefinition(),
            FeatType.RakingClaws => new RakingClawsAbilityDefinition(),
            FeatType.SonicShriek => new SonicShriekAbilityDefinition(),
            FeatType.TailSweep => new TailSweepAbilityDefinition(),
            FeatType.StaticWeb => new StaticWebAbilityDefinition(),
            FeatType.GoringCharge => new GoringChargeAbilityDefinition(),
            FeatType.ToxicCloud => new ToxicCloudAbilityDefinition(),
            FeatType.ScorchingBreath => new ScorchingBreathAbilityDefinition(),
            FeatType.TerrifyingBellow => new TerrifyingBellowAbilityDefinition(),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceFeat), sourceFeat, "Unknown Mimicry source NPC feat")
        };

        return definition.BuildAbilities();
    }

    // Reflection discovers every IAbilityListDefinition declared directly in the given namespace,
    // instantiates each one, and builds its abilities. Used to derive expectations from the actual
    // registered content instead of a hand-maintained list, so tests stay valid as the technique
    // pool (and NPC ability roster) grows or shrinks.
    private static List<DiscoveredAbility> BuildAllAbilities(string namespaceName)
    {
        var definitionTypes = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.Namespace == namespaceName &&
                           typeof(IAbilityListDefinition).IsAssignableFrom(type));

        var discovered = new List<DiscoveredAbility>();
        foreach (var definitionType in definitionTypes)
        {
            var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (feat, detail) in definition.BuildAbilities())
            {
                discovered.Add(new DiscoveredAbility(definitionType, feat, detail));
            }
        }

        return discovered;
    }

    private readonly record struct DiscoveredAbility(Type DefinitionType, FeatType Feat, AbilityDetail Detail);

    private static void AssertCustomTlkStrrefIsNonEmpty(
        Dictionary<string, string> featRow,
        IReadOnlyDictionary<int, string> tlkEntries,
        FeatType feat,
        string column,
        string label)
    {
        featRow.TryGetValue(column, out var strrefText).Should().BeTrue($"{feat}'s feat.2da row should have a {column} column");
        int.TryParse(strrefText, out var strref).Should().BeTrue($"{feat}'s {column} strref should be numeric, was '{strrefText}'");
        strref.Should().BeGreaterThanOrEqualTo(CustomTlkStrrefOffset,
            $"{feat}'s {column} strref should reference a custom TLK entry (>= {CustomTlkStrrefOffset})");

        var tlkId = strref - CustomTlkStrrefOffset;
        tlkEntries.Should().ContainKey(tlkId, $"TLK id {tlkId} ({feat} {label}) should exist");
        tlkEntries[tlkId].Should().NotBeNullOrWhiteSpace($"TLK id {tlkId} ({feat} {label}) should have non-empty text");
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

    private static void AssertMustHavePerkRequirement(PerkLevel level, PerkType requiredPerk)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementMustHavePerk>()
            .Should()
            .ContainSingle()
            .Which;

        typeof(PerkRequirementMustHavePerk)
            .GetField("_mustHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(requiredPerk);
    }

    private static IReadOnlyDictionary<int, string> ReadTlkEntries(FileInfo file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(file.FullName));
        return document.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("id").GetInt32(),
                entry => entry.TryGetProperty("text", out var text) ? text.GetString() ?? string.Empty : string.Empty);
    }

    // Invokes the definition's private per-perk builder methods and reads the builder's perk
    // dictionary directly, skipping PerkBuilder.Build()'s feat.2da icon lookup (needs a live engine).
    // Same pattern as BeastmasterCombatUpgradeTests.BuildPerksWithout2daLookup.
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

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
