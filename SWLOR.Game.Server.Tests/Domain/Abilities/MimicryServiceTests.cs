using FluentAssertions;
using NUnit.Framework;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The Mimicry stay-part and the witness and learn code: the technique cache, the table reads, the trait bonuses, witnessing, learning and
/// the handlers. The trait stat bonuses and the elemental resonance are read from the Character service (<c>MimicryTraitStatsService</c>),
/// built here from the same ability details over a stat reader that answers Trait Potency. The slots, equipping and saved builds are
/// <see cref="MimicryLoadoutServiceTests"/>. The engine and the NWNX plugins are recording fakes, the database an in-memory one.
/// </summary>
[TestFixture]
public class MimicryServiceTests : MimicryTestBase
{
    // ---- The technique cache --------------------------------------------------------------------------------------

    [Test]
    public void CacheData_IndexesTheTechniquesAndResolvesTheirPresentationData()
    {
        Technique(Fire1, source: NpcFire1);
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.MimicryPotencyPercent] = 3 });
        _mimicry.Abilities[Plain] = new AbilityDetail { Name = "Not a technique" };
        _twoDa[("ICON", (int)Fire1)] = "ife_fire";
        _twoDa[("DESCRIPTION", (int)Fire1)] = "7";
        _twoDa[("ICON", (int)Trait1)] = "ife_trait";

        _mimicry.Techniques.CacheData();

        _mimicry.Techniques.TechniqueCount.Should().Be(2);
        _mimicry.Techniques.IsTechnique(Fire1).Should().BeTrue();
        _mimicry.Techniques.IsTechnique(Plain).Should().BeFalse();
        _mimicry.Techniques.GetTechniqueDetail(Fire1).Should().BeSameAs(_mimicry.Abilities[Fire1]);
        _mimicry.Techniques.GetTechniqueDetail(Plain).Should().BeNull();
        _mimicry.Techniques.GetTechniqueIcon(Fire1).Should().Be("ife_fire");
        _mimicry.Techniques.GetTechniqueIcon(Plain).Should().BeEmpty();
        _mimicry.Techniques.GetTechniqueDescription(Fire1).Should().Be("text-7");
        _mimicry.Techniques.GetTechniqueDescription(Trait1).Should().BeEmpty("a technique without a description string reference has no text");
        _mimicry.Techniques.GetTechniqueDescription(Plain).Should().BeEmpty();
    }

    [Test]
    public void AHotReload_KeepsTheAuthoredDescriptionsTheBootReadBeforeTheTlkOverrides()
    {
        _ = new LocalVariables(_engine);
        _engine.SetReturn("GetModule", 1u);
        Technique(Fire1);
        _twoDa[("DESCRIPTION", (int)Fire1)] = "7";
        _mimicry.Techniques.CacheData();
        _mimicry.Techniques.CarryTechniqueDescriptions();

        // The TLK overrides of the boot add hotbar headers, so the rerun reads changed text.
        _engine.SetReturnHandler("GetStringByStrRef", args => $"header + text-{(int)args[0]!}");
        Technique(Ice1);
        _twoDa[("DESCRIPTION", (int)Ice1)] = "8";
        _mimicry.Techniques.CacheData();
        _mimicry.Techniques.AdoptTechniqueDescriptions();

        _mimicry.Techniques.GetTechniqueDescription(Fire1).Should().Be("text-7");
        _mimicry.Techniques.GetTechniqueDescription(Ice1).Should().Be("header + text-8", "a technique new in the build keeps what it read");
        _mimicry.Techniques.AdoptTechniqueDescriptions();
        _mimicry.Techniques.GetTechniqueDescription(Fire1).Should().Be("text-7", "the carry is taken once and nothing else changes");
    }

    [Test]
    public void CacheData_RebuildsTheCacheAndSchedulesTheWitnessSweepOnce()
    {
        Technique(Fire1);
        _mimicry.Techniques.CacheData();
        _mimicry.Abilities.Clear();
        Technique(Ice1);

        _mimicry.Techniques.CacheData();

        _mimicry.Techniques.IsTechnique(Fire1).Should().BeFalse();
        _mimicry.Techniques.IsTechnique(Ice1).Should().BeTrue();
        _mimicry.Scheduled.Should().ContainSingle().Which.Interval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Test]
    public void CacheData_KeepsTheTechniquesWhenTheEngineCannotReadThe2da()
    {
        Technique(Fire1);
        _twoDa[("ICON", (int)Fire1)] = "ife_fire";
        _engine.SetReturnHandler("Get2DAString", _ => throw new InvalidOperationException("no engine"));

        _mimicry.Techniques.CacheData();

        _mimicry.Techniques.IsTechnique(Fire1).Should().BeTrue();
        _mimicry.Techniques.GetTechniqueIcon(Fire1).Should().BeEmpty();
        _mimicry.Techniques.GetTechniqueDescription(Fire1).Should().BeEmpty();
    }

    // ---- Trait bonuses --------------------------------------------------------------------------------------------

    private void EquipForBonuses(params FeatType[] feats)
    {
        Record.EquippedTechniques = feats.ToList();
        _mimicry.Techniques.CacheData();
        _traitStats.CacheData();
    }

    [Test]
    public void GetStatSources_ListsEachEquippedTraitThatAdjustsTheStat()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        Trait(Trait2, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 0 });
        EquipForBonuses(Trait1, Trait2);

        var sources = _traitStats.GetStatSources(Player, StatType.StaffMightModifierDamageMultiplier, _statReader);

        sources.Should().ContainSingle();
        sources[0].Key.Should().Be($"trait:{(int)Trait1}");
        sources[0][StatType.StaffMightModifierDamageMultiplier].Should().Be(10);
    }

    [Test]
    public void GetStatSources_ScalesTheTraitByThePotencyStat()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        EquipForBonuses(Trait1);
        _mimicry.StatAdjustments[StatType.MimicryTraitPotencyPercent] = 50;

        var sources = _traitStats.GetStatSources(Player, StatType.StaffMightModifierDamageMultiplier, _statReader);

        sources[0][StatType.StaffMightModifierDamageMultiplier].Should().Be(15);
    }

    [Test]
    public void GetStatSources_IsEmptyForUnknownStatsNonPlayersAndMissingRecords()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        EquipForBonuses(Trait1);

        _traitStats.GetStatSources(Player, StatType.MimicryPotencyPercent, _statReader).Should().BeEmpty("no trait adjusts that stat");
        _traitStats.GetStatSources(Npc, StatType.StaffMightModifierDamageMultiplier, _statReader).Should().BeEmpty("an NPC has no loadout");
        _isDm = true;
        _traitStats.GetStatSources(Player, StatType.StaffMightModifierDamageMultiplier, _statReader).Should().BeEmpty("a DM has no loadout");
        _isDm = false;
        _engine.SetReturnHandler("GetObjectUUID", _ => "nobody");
        _traitStats.GetStatSources(Player, StatType.StaffMightModifierDamageMultiplier, _statReader).Should().BeEmpty("there is no record");
    }

    [Test]
    public void GetStatBonus_SumsTheEquippedTraitsWithTheirPotency()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        Trait(Trait2, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 3 });
        EquipForBonuses(Trait1, Trait2);

        _traitStats.GetStatBonus(Player, StatType.StaffMightModifierDamageMultiplier, _statReader).Should().Be(13);

        _mimicry.StatAdjustments[StatType.MimicryTraitPotencyPercent] = 50;
        _traitStats.GetStatBonus(Player, StatType.StaffMightModifierDamageMultiplier, _statReader).Should().Be(15 + 5, "each amount is raised and rounded up");
    }

    [Test]
    public void GetStatBonus_DoesNotApplyPotencyToThePotencyStatItself()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.MimicryTraitPotencyPercent] = 10 });
        EquipForBonuses(Trait1);
        _mimicry.StatAdjustments[StatType.MimicryTraitPotencyPercent] = 100;

        _traitStats.GetStatBonus(Player, StatType.MimicryTraitPotencyPercent, _statReader).Should().Be(10);
    }

    [Test]
    public void GetStatBonus_AddsTheElementalResonanceToTechniquePotency()
    {
        Technique(Fire1, element: CombatDamageType.Fire);
        Technique(Fire2, element: CombatDamageType.Fire);
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.MimicryPotencyPercent] = 2 });
        EquipForBonuses(Fire1, Fire2, Trait1);

        _traitStats.GetStatBonus(Player, StatType.MimicryPotencyPercent, _statReader).Should().Be(5 + 2);
    }

    [Test]
    public void GetStatBonus_GivesOnlyTheResonanceWhenNoTraitAdjustsPotency()
    {
        Technique(Fire1, element: CombatDamageType.Fire);
        Technique(Fire2, element: CombatDamageType.Fire);
        EquipForBonuses(Fire1, Fire2);

        _traitStats.GetStatBonus(Player, StatType.MimicryPotencyPercent, _statReader).Should().Be(5);
    }

    [Test]
    public void GetStatBonus_IsZeroWithoutContributorsOrALoadout()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        EquipForBonuses(Trait1);

        _traitStats.GetStatBonus(Player, StatType.MimicryTraitPotencyPercent, _statReader).Should().Be(0, "no trait adjusts that stat");
        _traitStats.GetStatBonus(Npc, StatType.StaffMightModifierDamageMultiplier, _statReader).Should().Be(0);
        _engine.SetReturnHandler("GetObjectUUID", _ => "nobody");
        _traitStats.GetStatBonus(Player, StatType.StaffMightModifierDamageMultiplier, _statReader).Should().Be(0);
    }

    [Test]
    public void GetResistanceBonus_SumsTheEquippedTraitsWithTheirPotency()
    {
        Trait(Trait1, resistances: new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 10 });
        Trait(Trait2, resistances: new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 4, [ResistanceType.Ice] = 6 });
        EquipForBonuses(Trait1, Trait2);

        _mimicry.Techniques.GetResistanceBonus(Player, ResistanceType.Fire).Should().Be(14);
        _mimicry.StatAdjustments[StatType.MimicryTraitPotencyPercent] = 50;
        _mimicry.Techniques.GetResistanceBonus(Player, ResistanceType.Fire).Should().Be(15 + 6);
    }

    [Test]
    public void GetResistanceBonus_IsZeroWithoutContributorsOrALoadout()
    {
        Trait(Trait1, resistances: new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 10 });
        EquipForBonuses(Trait1);

        _mimicry.Techniques.GetResistanceBonus(Player, ResistanceType.Ice).Should().Be(0);
        _mimicry.Techniques.GetResistanceBonus(Npc, ResistanceType.Fire).Should().Be(0);
        _engine.SetReturnHandler("GetObjectUUID", _ => "nobody");
        _mimicry.Techniques.GetResistanceBonus(Player, ResistanceType.Fire).Should().Be(0);
    }

    // ---- Witnessing and learning ----------------------------------------------------------------------------------

    private void WitnessSetup(int rank = 20, int rankRequired = 0)
    {
        Technique(Fire1, rank: rankRequired, source: NpcFire1);
        _db.With(NewPlayer(rank, analyzer: 1));
        _mimicry.Techniques.CacheData();
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 1;
        _nearby.Add(Player);
    }

    [Test]
    public void OnCreatureAbilityUsed_RecordsTheTechniqueForAnAnalyzingPlayerOnce()
    {
        WitnessSetup();

        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().ContainSingle().Which.Should().Contain("records Technique 3001");
    }

    [Test]
    public void OnCreatureAbilityUsed_WarnsWhenTheTechniqueIsBeyondTheAnalysisLevel()
    {
        WitnessSetup(rank: 2, rankRequired: 5);

        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().BeEmpty();
        _warnings.Should().Equal($"MIMICRY_RANK_{(int)Fire1}");
    }

    [Test]
    public void OnCreatureAbilityUsed_IgnoresPlayersCreaturesAndFeatsThatTeachNothing()
    {
        WitnessSetup();

        _mimicry.Witness.OnCreatureAbilityUsed(ObjectInvalid, NpcFire1);
        _mimicry.Witness.OnCreatureAbilityUsed(Player, NpcFire1);
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, Plain);
        _invalid.Add(Npc);
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().BeEmpty();
    }

    [Test]
    public void OnCreatureAbilityUsed_IgnoresATechniqueWhoseDetailIsGoneAndPlayersControlledByDms()
    {
        WitnessSetup();
        _controlledByDm.Add(Player);

        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().BeEmpty();
        _mimicry.Abilities.Remove(Fire1);
        _controlledByDm.Clear();
        _mimicry.Techniques.CacheData();
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        Messages().Should().BeEmpty();
    }

    [Test]
    public void OnCreatureAbilityUsed_IgnoresPlayersOutOfRangeInAnotherAreaOrWithoutTheAnalyzerOrWhoKnowTheTechnique()
    {
        WitnessSetup();
        _engine.SetReturn("GetDistanceBetween", 20f);
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _engine.SetReturn("GetDistanceBetween", 5f);

        _playerArea = Area + 1;
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _playerArea = Area;

        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 0;
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 1;

        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().BeEmpty();
    }

    [Test]
    public void OnCreatureDeath_LetsAWitnessLearnTheTechniqueAndPaysTheLearningXp()
    {
        WitnessSetup();
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _engine.ClearCalls();
        _self = Npc;
        SeedLearnRoll(succeeds: true, chance: 20 + 2 * 20);

        _mimicry.Witness.OnCreatureDeath();

        Record.LearnedTechniques.Should().ContainKey(Fire1);
        _db.Saved.Should().Contain(Record);
        Messages().Should().ContainSingle().Which.Should().Contain("You learned the technique: Technique 3001!");
        _mimicry.XpGrants.Should().ContainSingle().Which.Should().Be((Player, SkillType.Mimicry, 400, false, Npc));
        _playerPlugin.CallsTo("PlaySound").Should().ContainSingle().Which.Arguments[1].Should().Be("gui_prompt");
    }

    [Test]
    public void OnCreatureDeath_WarnsWhenTheDecodeRollFailsAndForgetsTheWitnesses()
    {
        WitnessSetup();
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _self = Npc;
        SeedLearnRoll(succeeds: false, chance: 60);

        _mimicry.Witness.OnCreatureDeath();

        Record.LearnedTechniques.Should().BeEmpty();
        _warnings.Should().Contain($"MIMICRY_DECODE_{(int)Fire1}");
        _mimicry.XpGrants.Should().BeEmpty();

        _warnings.Clear();
        _mimicry.Witness.OnCreatureDeath();
        _warnings.Should().BeEmpty("the witness entry is cleared on death");
    }

    [Test]
    public void OnCreatureDeath_SkipsWitnessesWhoCannotLearnRightNow()
    {
        WitnessSetup();
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _self = Npc;
        SeedLearnRoll(succeeds: true, chance: 60);

        _playerDead = true;
        _mimicry.Witness.OnCreatureDeath();
        Record.LearnedTechniques.Should().BeEmpty();
    }

    [TestCase("hp")]
    [TestCase("area")]
    [TestCase("dm")]
    [TestCase("learned")]
    [TestCase("rank")]
    [TestCase("unwitnessed")]
    public void TryLearn_IsSkippedWhen(string reason)
    {
        WitnessSetup();
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _self = Npc;
        SeedLearnRoll(succeeds: true, chance: 60);
        switch (reason)
        {
            case "hp": _playerHitPoints = 0; break;
            case "area": _playerArea = Area + 1; break;
            case "dm": _isDm = true; break;
            case "learned": Record.LearnedTechniques[Fire1] = DateTime.UtcNow; break;
            case "rank": Record.Skills[SkillType.Mimicry].Rank = 0; _mimicry.Abilities[Fire1].MimicrySkillRequirement = 9; break;
            case "unwitnessed": _nearby.Clear(); _nearby.Add(Bystander); break;
        }
        var before = Record.LearnedTechniques.Count;

        _mimicry.Witness.OnCreatureDeath();

        Record.LearnedTechniques.Count.Should().Be(before);
        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void OnCreatureDeath_DoesNothingForACreatureNobodyWitnessed()
    {
        WitnessSetup();
        _self = Npc;

        _mimicry.Witness.OnCreatureDeath();

        _engine.CountOf("GetFirstPC").Should().Be(0);
    }

    [TestCase(0, 10, 0, 10, 20 + 2 * -10 + 0 + 0)]
    [TestCase(20, 10, 2, 10, 20 + 2 * 10 + 20 + 0)]
    [TestCase(10, 0, 1, 15, 20 + 20 + 10 + 5)]
    [TestCase(50, 0, 3, 30, 75)]
    public void CalculateLearnChance_ScalesWithRankPerkAndPerceptionAndIsCapped(
        int rank, int required, int patternRecognition, int perception, int expected)
    {
        _mimicry.Witness.CalculateLearnChance(rank, required, patternRecognition, perception).Should().Be(expected);
    }

    [Test]
    public void CalculateLearnChance_IgnoresPerceptionBelowTheBaseline()
    {
        _mimicry.Witness.CalculateLearnChance(5, 5, 0, 3).Should().Be(20);
    }

    // ---- Observation XP -------------------------------------------------------------------------------------------

    [Test]
    public void CalculateAnalysisXP_IsFivePercentOfTheRankRelativeXpCappedAtThirty()
    {
        _mimicry.Witness.CalculateAnalysisXP(20, 10).Should().Be(Math.Min(30, 1000 * 5 / 100));
        _mimicry.Witness.CalculateAnalysisXP(15, 10).Should().Be(500 * 5 / 100);
        _mimicry.Witness.CalculateAnalysisXP(20, 10).Should().BeLessThanOrEqualTo(30);
        var maxRank = SkillType.Mimicry.GetAttribute<SkillType, SkillAttribute>().MaxRank;
        _mimicry.Witness.CalculateAnalysisXP(60, maxRank).Should().Be(0, "mastered Mimicry earns no observation XP");
    }

    private void Observe()
    {
        WitnessSetup();
        _mimicry.CombatPoints = true;
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
    }

    [Test]
    public void CombatPointXPDistributed_PaysTheObservationXpOnceWithoutBonuses()
    {
        Observe();
        _self = Player;
        _mimicry.NpcLevel = 30;

        _mimicry.Witness.CombatPointXPDistributed();
        _mimicry.Witness.CombatPointXPDistributed();

        var expected = _mimicry.Witness.CalculateAnalysisXP(30, 20);
        expected.Should().BeGreaterThan(0);
        _mimicry.XpGrants.Should().ContainSingle().Which.Should().Be((Player, SkillType.Mimicry, expected, true, Npc));
    }

    [Test]
    public void CombatPointXPDistributed_PaysNothingToAPlayerWhoWasNotObserving()
    {
        Observe();
        _self = Bystander;
        _db.With(new Player { Id = "uuid-1002" });

        _mimicry.Witness.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void CombatPointXPDistributed_PaysNothingWhenTheAnalyzerWasLostOrXpIsZero()
    {
        Observe();
        _self = Player;
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 0;
        _mimicry.Witness.CombatPointXPDistributed();
        _mimicry.XpGrants.Should().BeEmpty();

        Observe();
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 1;
        Record.Skills[SkillType.Mimicry].Rank = SkillType.Mimicry.GetAttribute<SkillType, SkillAttribute>().MaxRank;
        _mimicry.Witness.CombatPointXPDistributed();
        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void OnCreatureAbilityUsed_RecordsNoObservationWithoutCombatPointsOrTheAnalyzer()
    {
        WitnessSetup();
        _mimicry.CombatPoints = false;
        _mimicry.Witness.OnCreatureAbilityUsed(Npc, NpcFire1);
        _self = Player;
        _mimicry.Witness.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void ClearAnalysisOnPlayerExit_DropsTheLeavingPlayersObservationCredit()
    {
        Observe();
        _exiting = Player;

        _mimicry.Witness.ClearAnalysisOnPlayerExit();
        _self = Player;
        _mimicry.Witness.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void ClearAnalysisOnPlayerExit_IgnoresCreaturesAndDms()
    {
        Observe();
        _exiting = Npc;
        _mimicry.Witness.ClearAnalysisOnPlayerExit();
        _exiting = Player;
        _isDm = true;
        _mimicry.Witness.ClearAnalysisOnPlayerExit();
        _isDm = false;
        _mimicry.NpcLevel = 30;

        _mimicry.Witness.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().ContainSingle("the credit was never dropped");
    }

    [Test]
    public void TheScheduledSweep_ForgetsWitnessesAndObserversOfCreaturesThatAreGone()
    {
        Observe();
        _mimicry.Techniques.CacheData();
        _invalid.Add(Npc);

        _mimicry.Scheduled[0].Task();
        _invalid.Clear();
        _self = Npc;
        _mimicry.Witness.OnCreatureDeath();

        _engine.CountOf("GetFirstPC").Should().Be(1, "only the initial witness lookup ran; the death found no entry");
    }

    [Test]
    public void TheScheduledSweep_ForgetsObserversOfDeadCreaturesAndKeepsLivingOnes()
    {
        Observe();
        _mimicry.Techniques.CacheData();
        _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Npc);
        _mimicry.Scheduled[0].Task();
        _self = Player;
        _mimicry.NpcLevel = 30;

        _mimicry.Witness.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().BeEmpty();
    }

    // ---- Granting, slots and equipping ----------------------------------------------------------------------------

    [Test]
    public void GrantAllTechniques_MarksEveryUnlearnedTechniqueAsLearned()
    {
        Technique(Fire1);
        Technique(Ice1);
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;
        _mimicry.Techniques.CacheData();

        _mimicry.Witness.GrantAllTechniques(Player).Should().Be(1);
        Record.LearnedTechniques.Keys.Should().BeEquivalentTo(new[] { Fire1, Ice1 });
        _db.Saved.Should().ContainSingle();

        _mimicry.Witness.GrantAllTechniques(Player).Should().Be(0);
        _db.Saved.Should().ContainSingle("nothing changed the second time");
    }

    [Test]
    public void GrantAllTechniques_RefusesCreaturesAndDms()
    {
        Technique(Fire1);
        _mimicry.Techniques.CacheData();

        _mimicry.Witness.GrantAllTechniques(Npc).Should().Be(0);
        _isDm = true;
        _mimicry.Witness.GrantAllTechniques(Player).Should().Be(0);
    }

    [Test]
    public void GetSetBonusPotency_CountsEachElementWithTwoOrMoreTechniquesUpToTheCap()
    {
        Technique(Fire1, element: CombatDamageType.Fire);
        Technique(Fire2, element: CombatDamageType.Fire);
        Technique(Ice1, element: CombatDamageType.Ice);
        Technique((FeatType)3010, element: CombatDamageType.Ice);
        Technique((FeatType)3011, element: CombatDamageType.Ice);
        Technique((FeatType)3012, element: CombatDamageType.Invalid);
        _traitStats.CacheData();
        var player = NewPlayer(10, 1);

        _traitStats.GetSetBonusPotency(null!).Should().Be(0);
        player.EquippedTechniques = new List<FeatType> { Fire1, Ice1, (FeatType)3012, Plain };
        _traitStats.GetSetBonusPotency(player).Should().Be(0);
        player.EquippedTechniques = new List<FeatType> { Fire1, Fire2 };
        _traitStats.GetSetBonusPotency(player).Should().Be(5);
        player.EquippedTechniques = new List<FeatType> { Fire1, Fire2, Ice1, (FeatType)3010, (FeatType)3011 };
        _traitStats.GetSetBonusPotency(player).Should().Be(5 + 10);

        for (var i = 0; i < 6; i++)
            Technique((FeatType)(3020 + i), element: CombatDamageType.Fire);
        _traitStats.CacheData();
        player.EquippedTechniques = Enumerable.Range(3020, 6).Select(i => (FeatType)i).Concat(new[] { Fire1, Fire2 }).ToList();
        _traitStats.GetSetBonusPotency(player).Should().Be(20, "the bonus is capped");
    }

    // ---- The technique table reads ---------------------------------------------------------------------------------

    [Test]
    public void TryGetTechnique_ReadsTheTableWithoutNormalisingTheKey()
    {
        Technique(Fire1);
        _mimicry.Techniques.CacheData();

        _mimicry.Techniques.TryGetTechnique(Fire1, out var detail).Should().BeTrue();
        detail.Should().BeSameAs(_mimicry.Abilities[Fire1]);
        _mimicry.Techniques.TryGetTechnique(Plain, out detail).Should().BeFalse();
        detail.Should().BeNull();
        _mimicry.Techniques.GetTechniqueDetail(Fire1).Should().BeSameAs(_mimicry.Abilities[Fire1]);
    }

    [Test]
    public void GetTechnique_ReturnsTheDetailAndThrowsForAFeatThatIsNotATechnique()
    {
        Technique(Fire1);
        _mimicry.Techniques.CacheData();

        _mimicry.Techniques.GetTechnique(Fire1).Should().BeSameAs(_mimicry.Abilities[Fire1]);
        var missing = () => _mimicry.Techniques.GetTechnique(Plain);
        missing.Should().Throw<KeyNotFoundException>("a missing key throws as the table indexer does");
    }

    [Test]
    public void TryGetTechniqueForNpcFeat_ReturnsTheTechniqueAndItsDetail()
    {
        Technique(Fire1, source: NpcFire1);
        Technique(Ice1);
        _mimicry.Techniques.CacheData();

        _mimicry.Techniques.TryGetTechniqueForNpcFeat(NpcFire1, out var technique, out var detail).Should().BeTrue();
        technique.Should().Be(Fire1);
        detail.Should().BeSameAs(_mimicry.Abilities[Fire1]);
        _mimicry.Techniques.TryGetTechniqueForNpcFeat(Plain, out technique, out detail).Should().BeFalse("the feat teaches nothing");
        detail.Should().BeNull();
    }

    [Test]
    public void EnumerateTechniques_ListsEveryRegisteredTechnique()
    {
        Technique(Fire1);
        Technique(Ice1);
        Trait(Trait1);
        _mimicry.Techniques.CacheData();

        _mimicry.Techniques.EnumerateTechniques().Select(pair => pair.Key).Should().BeEquivalentTo(new[] { Fire1, Ice1, Trait1 });
        _mimicry.Techniques.EnumerateTechniques().Single(pair => pair.Key == Ice1).Value.Should().BeSameAs(_mimicry.Abilities[Ice1]);
        _mimicry.Techniques.TechniqueCount.Should().Be(3);
    }
}
