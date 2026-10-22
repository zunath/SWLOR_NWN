using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class UnarmedProgressionTests
{
    [Test]
    public void SkillUsesNormalWeaponProgressionForBothCharacterTypes()
    {
        var metadata = typeof(SkillType).GetField(nameof(SkillType.Unarmed))!.GetCustomAttribute<SkillAttribute>()!;
        Assert.Multiple(() =>
        {
            Assert.That(metadata.MaxRank, Is.EqualTo(50));
            Assert.That(metadata.IsActive && metadata.ContributesToSkillCap, Is.True);
            Assert.That(metadata.CombatPointCategory, Is.EqualTo(CombatPointCategoryType.Weapon));
            Assert.That(metadata.IsAvailableToCharacterType(CharacterType.Standard), Is.True);
            Assert.That(metadata.IsAvailableToCharacterType(CharacterType.ForceSensitive), Is.True);
        });
    }

    [Test]
    public void IndependentLinesRetainTheirSixtyPointBudgetAndLinearUnlocks()
    {
        var perks = new UnarmedPerkDefinition().BuildPerks();
        Assert.That(perks, Has.Count.EqualTo(20));
        foreach (var category in new[] { PerkCategoryType.UnarmedMartialArtist, PerkCategoryType.UnarmedBrawler })
        {
            var line = perks.Values.Where(perk => perk.Category == category).ToArray();
            var levels = line.SelectMany(perk => perk.PerkLevels.Values).ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(line, Has.Length.EqualTo(10));
                Assert.That(levels, Has.Length.EqualTo(18));
                Assert.That(levels.Sum(level => level.Price), Is.EqualTo(60));
                Assert.That(levels.SelectMany(level => level.Requirements).OfType<PerkRequirementCharacterType>(), Is.Empty);
                Assert.That(levels.SelectMany(level => level.Requirements).OfType<PerkRequirementSkill>().Select(requirement => requirement.Type).Distinct(), Is.EqualTo(new[] { SkillType.Unarmed }));
            });
            foreach (var level in levels)
            foreach (var requirement in level.Requirements.OfType<PerkRequirementMustHavePerk>())
            {
                var prerequisite = (PerkType)typeof(PerkRequirementMustHavePerk)
                    .GetField("_mustHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)!;
                var requiredPerk = perks[prerequisite];
                Assert.That(requiredPerk.Category, Is.EqualTo(category), "A line must never require a purchase in the other line.");
                Assert.That(requiredPerk.PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank,
                    Is.LessThanOrEqualTo(level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank),
                    "Every setup and spender must exist before its enhancement unlocks.");
            }
        }
    }

    [TestCase(PerkType.PracticedForm, 1, FeatType.PracticedFormTrait)]
    [TestCase(PerkType.PracticedForm, 2, FeatType.PracticedForm2Trait)]
    [TestCase(PerkType.PracticedForm, 3, FeatType.PracticedForm3Trait)]
    [TestCase(PerkType.HeavyBlows, 1, FeatType.HeavyBlowsTrait)]
    [TestCase(PerkType.HeavyBlows, 2, FeatType.HeavyBlows2Trait)]
    [TestCase(PerkType.HeavyBlows, 3, FeatType.HeavyBlows3Trait)]
    public void ConditionalPowerTraitsGrantTheCurrentRankTooltip(PerkType perk, int rank, FeatType feat)
    {
        var level = new UnarmedPerkDefinition().BuildPerks()[perk].PerkLevels[rank];
        Assert.That(level.GrantedFeats, Is.EqualTo(new[] { feat }));
    }

    [Test]
    public void OpenerAndBridgeWorkBeforeTheirEnhancementTraits()
    {
        var jab = new JabAbilityDefinition().BuildAbilities()[FeatType.Jab1].MeleeTechnique;
        var palm = new PalmStrikeAbilityDefinition().BuildAbilities()[FeatType.PalmStrike1].MeleeTechnique;
        var dirtyJab = new DirtyJabAbilityDefinition().BuildAbilities()[FeatType.DirtyJab1].MeleeTechnique;
        Assert.Multiple(() =>
        {
            Assert.That(jab.CreatedStage, Is.EqualTo(MeleeTechniqueStage.Opening));
            Assert.That(jab.SetupDurationSeconds, Is.EqualTo(4));
            Assert.That(jab.OrdinaryAccuracyBonus, Is.EqualTo(3));
            Assert.That(palm.ConsumedStages, Is.EqualTo(MeleeTechniqueStage.Opening));
            Assert.That(palm.CreatedStage, Is.EqualTo(MeleeTechniqueStage.Finish));
            Assert.That(palm.CreatesStageOnlyWhenSequenced, Is.True);
            Assert.That(dirtyJab.CreatedStage, Is.EqualTo(MeleeTechniqueStage.Distraction));
            Assert.That(dirtyJab.SetupDurationSeconds, Is.EqualTo(6));
            Assert.That(dirtyJab.SourceOwnedSetupStatusEffectFactory(), Is.TypeOf<RattledStatusEffect>());
        });
    }

    [Test]
    public void FinisherHasTwoHalfWeaponImpactsAndDistinctShortAndFullPayoffs()
    {
        var finisher = new FinishingCombinationAbilityDefinition().BuildAbilities()[FeatType.FinishingCombination2].MeleeTechnique;
        Assert.Multiple(() =>
        {
            Assert.That(finisher.ImpactCount, Is.EqualTo(2));
            Assert.That(finisher.WeaponDamagePercent, Is.EqualTo(50));
            Assert.That(finisher.OpeningPower, Is.EqualTo(8));
            Assert.That(finisher.FinishPower, Is.EqualTo(16));
            Assert.That(finisher.RefundEligible, Is.True);
        });
    }

    [Test]
    public void PerfectFormRequiresAndSpendsFinish()
    {
        var perfect = new PerfectFormAbilityDefinition().BuildAbilities()[FeatType.PerfectForm1].MeleeTechnique;
        Assert.Multiple(() =>
        {
            Assert.That(perfect.RequiredStages, Is.EqualTo(MeleeTechniqueStage.Finish));
            Assert.That(perfect.ConsumedStages, Is.EqualTo(MeleeTechniqueStage.Finish));
            Assert.That(perfect.FinishPower, Is.EqualTo(45));
            Assert.That(perfect.RefundEligible, Is.True);
        });
        foreach (var definition in new IAbilityListDefinition[]
                 {
                     new JabAbilityDefinition(), new PalmStrikeAbilityDefinition(), new FinishingCombinationAbilityDefinition(),
                     new DirtyJabAbilityDefinition(), new UppercutAbilityDefinition(), new LowKickAbilityDefinition(), new HaymakerAbilityDefinition()
                 })
            foreach (var ability in definition.BuildAbilities().Values)
                Assert.That(ability.MeleeTechnique.RequiredStages, Is.EqualTo(MeleeTechniqueStage.None), $"{ability.Name} can still start without a setup");
    }

    [TestCase(nameof(RecastGroup.UnarmedOpener), "Unarmed Opener", "Fist Opener")]
    [TestCase(nameof(RecastGroup.UnarmedResponse), "Unarmed Response", "Fist Response")]
    [TestCase(nameof(RecastGroup.UnarmedTechnique), "Unarmed Technique", "Fist Technique")]
    [TestCase(nameof(RecastGroup.FlowStance), "Flow Stance", "Flow Stance")]
    [TestCase(nameof(RecastGroup.BruiserStance), "Bruiser Stance", "Bruiser Stance")]
    public void UnarmedCooldownGroupsHaveIndependentIdsAndCorrectVisibleLabels(string groupName, string displayName, string shortName)
    {
        var field = typeof(RecastGroup).GetField(groupName)!;
        var group = (RecastGroup)field.GetValue(null)!;
        var aliases = typeof(RecastGroup).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(candidate => (RecastGroup)candidate.GetValue(null)! == group)
            .Select(candidate => candidate.Name).ToArray();
        Recast.CacheRecastGroups();
        Assert.Multiple(() =>
        {
            Assert.That(aliases, Is.EqualTo(new[] { groupName }), "Unarmed cooldowns must not share numeric IDs with any other enum label.");
            Assert.That(Recast.GetRecastGroupDisplayName(group), Is.EqualTo(displayName));
            Assert.That(Recast.GetRecastGroupName(group), Is.EqualTo(shortName));
            Assert.That(Recast.IsRecastGroupVisible(group), Is.True);
            Assert.That(shortName.Length, Is.LessThanOrEqualTo(14));
        });
    }

    [Test]
    public void EveryUnarmedRankRetainsItsIntendedSharedCooldown()
    {
        var definitions = new (IAbilityListDefinition Definition, RecastGroup Group)[]
        {
            (new JabAbilityDefinition(), RecastGroup.UnarmedOpener),
            (new DirtyJabAbilityDefinition(), RecastGroup.UnarmedOpener),
            (new PalmStrikeAbilityDefinition(), RecastGroup.UnarmedResponse),
            (new UppercutAbilityDefinition(), RecastGroup.UnarmedResponse),
            (new FinishingCombinationAbilityDefinition(), RecastGroup.UnarmedTechnique),
            (new LowKickAbilityDefinition(), RecastGroup.UnarmedTechnique),
            (new FlowStanceAbilityDefinition(), RecastGroup.FlowStance),
            (new BruiserStanceAbilityDefinition(), RecastGroup.BruiserStance),
            (new PerfectFormAbilityDefinition(), RecastGroup.Capstone),
            (new HaymakerAbilityDefinition(), RecastGroup.Capstone),
        };
        Assert.Multiple(() =>
        {
            Assert.That((int)RecastGroup.ChainLightning, Is.EqualTo(574));
            Assert.That((int)RecastGroup.BloodSiphon, Is.EqualTo(575));
            foreach (var (definition, group) in definitions)
            foreach (var ability in definition.BuildAbilities().Values)
                Assert.That(ability.RecastGroup, Is.EqualTo(group), ability.Name);
        });
    }

    [Test]
    public void MatchingAbilitiesShareCooldownsAndKeepTheirCommitment()
    {
        var jab = new JabAbilityDefinition().BuildAbilities()[FeatType.Jab4];
        var dirtyJab = new DirtyJabAbilityDefinition().BuildAbilities()[FeatType.DirtyJab4];
        var palm = new PalmStrikeAbilityDefinition().BuildAbilities()[FeatType.PalmStrike3];
        var uppercut = new UppercutAbilityDefinition().BuildAbilities()[FeatType.Uppercut3];
        var finisher = new FinishingCombinationAbilityDefinition().BuildAbilities()[FeatType.FinishingCombination2];
        var kick = new LowKickAbilityDefinition().BuildAbilities()[FeatType.LowKick2];
        var perfect = new PerfectFormAbilityDefinition().BuildAbilities()[FeatType.PerfectForm1];
        var haymaker = new HaymakerAbilityDefinition().BuildAbilities()[FeatType.Haymaker1];
        Assert.Multiple(() =>
        {
            Assert.That(jab.RecastGroup, Is.EqualTo(dirtyJab.RecastGroup));
            Assert.That(jab.RecastDelay(0), Is.EqualTo(18));
            Assert.That(palm.RecastGroup, Is.EqualTo(uppercut.RecastGroup));
            Assert.That(palm.RecastDelay(0), Is.EqualTo(24));
            Assert.That(finisher.RecastGroup, Is.EqualTo(kick.RecastGroup));
            Assert.That(finisher.RecastDelay(0), Is.EqualTo(30));
            Assert.That(uppercut.ActivationDelay(0, 0, 0), Is.Zero);
            Assert.That(perfect.RecastGroup, Is.EqualTo(RecastGroup.Capstone));
            Assert.That(haymaker.RecastGroup, Is.EqualTo(RecastGroup.Capstone));
            Assert.That(perfect.RecastDelay(0), Is.EqualTo(90));
            Assert.That(haymaker.ActivationDelay(0, 0, 0), Is.EqualTo(2));
            Assert.That(perfect.ActivationDelay(0, 0, 0), Is.EqualTo(2));
        });
        foreach (var ability in new[] { jab, dirtyJab, palm, uppercut, finisher, kick, perfect, haymaker })
        {
            Assert.That(ability.ActivationType, Is.EqualTo(AbilityActivationType.Casted));
            Assert.That(ability.RequiresTarget && ability.IsHostileAbility && ability.IsSingleTargetAbility, Is.True);
            Assert.That(ability.MaxRange, Is.EqualTo(7));
            Assert.That(ability.Requirements.OfType<AbilityRequirementFP>(), Is.Empty);
        }
    }

    [Test]
    public void StancesUseReactiveWindowsWithStancePenalties()
    {
        var flow = new FlowStanceStatusEffect();
        var bruiser = new BruiserStanceStatusEffect();
        Assert.Multiple(() =>
        {
            Assert.That(flow.StatGroup.Stats[StatType.AttackDelayReductionPercent], Is.Zero);
            Assert.That(flow.StatGroup.Stats[StatType.EvasionPercentAdjustment], Is.Zero);
            Assert.That(flow.StatGroup.Stats[StatType.TechniqueLine], Is.EqualTo(1));
            Assert.That(flow.StatGroup.Stats[StatType.TechniqueOpeningFinisherEvasionPercent], Is.EqualTo(20));
            Assert.That(flow.StatGroup.Stats[StatType.TechniqueOpeningFinisherMovementSpeedPercent], Is.EqualTo(15));
            Assert.That(flow.StatGroup.Stats[StatType.TechniqueFinishFinisherHastePercent], Is.EqualTo(15));
            Assert.That(flow.StatGroup.Stats[StatType.DefensePercentAdjustment], Is.EqualTo(-10));
            Assert.That(flow.StatGroup.Stats[StatType.TechniqueFinisherWindowDurationSeconds], Is.EqualTo(8));
            Assert.That(flow.StatGroup.Stats[StatType.TechniqueFinisherWindowCooldownSeconds], Is.EqualTo(12));
            Assert.That(bruiser.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment], Is.EqualTo(10));
            Assert.That(bruiser.StatGroup.Stats[StatType.TechniqueLine], Is.EqualTo(2));
            Assert.That(bruiser.StatGroup.Stats[StatType.TechniqueRetaliationPower], Is.EqualTo(12));
            Assert.That(bruiser.StatGroup.Stats[StatType.AttackPercentAdjustment], Is.EqualTo(-10));
            Assert.That(bruiser.StatGroup.Stats[StatType.TechniqueRetaliationDurationSeconds], Is.EqualTo(8));
            Assert.That(bruiser.StatGroup.Stats[StatType.TechniqueRetaliationCooldownSeconds], Is.EqualTo(18));
            Assert.That(bruiser.StatGroup.Stats[StatType.Guard], Is.Zero);
            Assert.That(bruiser.StatGroup.Stats[StatType.ForceDefensePercentAdjustment], Is.Zero);
        });
        foreach (var stance in new[] { new FlowStanceAbilityDefinition().BuildAbilities()[FeatType.FlowStance1], new BruiserStanceAbilityDefinition().BuildAbilities()[FeatType.BruiserStance1] })
        {
            Assert.That(stance.IsHostileAbility || stance.RequiresTarget, Is.False);
            Assert.That(stance.ActivationDelay(0, 0, 0), Is.EqualTo(2));
            Assert.That(stance.MeleeTechnique, Is.Null, "Stances must not enter the weapon-profile validation path.");
            Assert.That(stance.CustomValidation, Is.Null, "Stances do not require Power Gloves.");
        }
    }

    [Test]
    public void SetupAccuracyResistancesAndPaybackUseWeaponIndependentPayloads()
    {
        var perks = new UnarmedPerkDefinition().BuildPerks();
        Dictionary<StatType, int> Stats(PerkType perk) => perks[perk].PerkLevels[1].StatBonuses
            .ToDictionary(bonus => bonus.Stat, bonus => bonus.Calculate(uint.MaxValue));
        var measured = Stats(PerkType.MeasuredTechnique);
        var payback = Stats(PerkType.Payback);
        Assert.Multiple(() =>
        {
            Assert.That(Stats(PerkType.RootedTechnique)[StatType.MobilityResistance], Is.EqualTo(15));
            Assert.That(Stats(PerkType.HardKnocks)[StatType.TraumaResistance], Is.EqualTo(15));
            Assert.That(measured[StatType.TechniqueSetupAbilityAccuracy], Is.EqualTo(10));
            Assert.That(payback[StatType.DirectHostileDamageNextAbilityStaminaDiscount], Is.EqualTo(2));
            Assert.That(payback[StatType.DirectHostileDamageNextAbilityDiscountDurationSeconds], Is.EqualTo(8));
            Assert.That(payback[StatType.DirectHostileDamageNextAbilityDiscountCooldownSeconds], Is.EqualTo(18));
            Assert.That(payback[StatType.TechniquePresentationSkillType], Is.EqualTo((int)SkillType.Unarmed));
            Assert.That(payback.ContainsKey(StatType.TechniqueLine), Is.False);
        });
    }

    [Test]
    public void HeavyStrikesUseIndependentPowerRefundsAndPreservePressure()
    {
        var definitions = new[] { new UppercutAbilityDefinition().BuildAbilities(), new HaymakerAbilityDefinition().BuildAbilities() };
        foreach (var ability in definitions.SelectMany(definition => definition.Values))
        {
            var technique = ability.MeleeTechnique;
            Assert.Multiple(() =>
            {
                Assert.That(technique.ConsumedStages, Is.EqualTo(MeleeTechniqueStage.None));
                Assert.That(technique.PreservesMatchingSetup, Is.True);
                Assert.That(technique.UsesAbilityPower && technique.ConsumesRetaliation, Is.True);
                Assert.That(technique.RefundEligible, Is.True);
                Assert.That(technique.RefundRequiresSetup, Is.False);
            });
        }
        var perks = new UnarmedPerkDefinition().BuildPerks();
        foreach (var rank in new[] { 1, 2, 3 })
        {
            var bonuses = perks[PerkType.HeavyBlows].PerkLevels[rank].StatBonuses;
            Assert.That(bonuses.Single(bonus => bonus.Stat == StatType.TechniqueAbilityPower).Calculate(uint.MaxValue), Is.EqualTo(rank * 4));
            Assert.That(bonuses.Any(bonus => bonus.Stat == StatType.TechniqueConditionalPower), Is.False);
        }
        Assert.That(new FinishingCombinationAbilityDefinition().BuildAbilities().Values.All(ability => ability.MeleeTechnique.RefundRequiresSetup), Is.True);
    }

    [Test]
    public void MasteryCountsBridgesAndFullFinishersSeparately()
    {
        var palm = new PalmStrikeAbilityDefinition().BuildAbilities()[FeatType.PalmStrike1].MeleeTechnique;
        var finisher = new FinishingCombinationAbilityDefinition().BuildAbilities()[FeatType.FinishingCombination1].MeleeTechnique;
        var uppercut = new UppercutAbilityDefinition().BuildAbilities()[FeatType.Uppercut1].MeleeTechnique;
        Assert.Multiple(() =>
        {
            Assert.That(UnarmedMastery.GetObjectiveKey(palm, MeleeTechniqueStage.Opening), Is.EqualTo("unarmed_opening_spent"));
            Assert.That(UnarmedMastery.GetObjectiveKey(finisher, MeleeTechniqueStage.Opening), Is.Null);
            Assert.That(UnarmedMastery.GetObjectiveKey(finisher, MeleeTechniqueStage.Finish), Is.EqualTo("unarmed_finish_spent"));
            Assert.That(UnarmedMastery.GetObjectiveKey(uppercut, MeleeTechniqueStage.None, PerkType.Uppercut), Is.EqualTo("unarmed_distraction_spent"));
            Assert.That(UnarmedMastery.GetObjectiveKey(uppercut, MeleeTechniqueStage.None, PerkType.Haymaker), Is.Null);
            Assert.That(UnarmedMastery.GetObjectiveKey(uppercut, MeleeTechniqueStage.None), Is.Null);
        });
        var quests = new UnarmedMasteryQuestDefinition().BuildQuests();
        Assert.That(quests.Keys, Is.EquivalentTo(new[] { "perfect_form_mastery", "haymaker_mastery" }));
        Assert.That(quests["perfect_form_mastery"].States[1].GetObjectives().OfType<CombatObjective>().Select(objective => objective.Amount), Is.EqualTo(new[] { 30, 30 }));
        Assert.That(quests["haymaker_mastery"].States[1].GetObjectives().OfType<CombatObjective>().Single().Amount, Is.EqualTo(40));
    }
}
