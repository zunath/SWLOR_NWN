using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The perk instance over a scripted world: the cache build, the level and feat reads, force affinity, stat bonuses, the
/// feat sync and refund paths and the skill decay handler. The order of the steps on the restore, sync, refund and decay
/// paths is pinned through the bench's timeline.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkServiceTests
{
    private const PerkType Plain = PerkType.AlphaRhythm;
    private const PerkType Unlockable = PerkType.Antitoxin;
    private const PerkType Dormant = PerkType.ApexBite;
    private const PerkType BeastOnly = PerkType.CourageousResolve;
    private const PerkType Light = PerkType.BeaconTargeting;
    private const PerkType Dark = PerkType.BlasterBeacon;
    private const PerkType Targeted = PerkType.BlastRadius;
    private const PerkType Marked = PerkType.BodyguardsResolve;

    private PerkBench _bench = null!;
    private PerkService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new PerkBench();
        _service = _bench.Service;
        BuildWorld();
    }

    [TearDown]
    public void TearDown() => _bench.Dispose();

    private static PerkDetail NewPerk(PerkType type, PerkCategoryType category = PerkCategoryType.General,
        bool active = true, PerkGroupType group = PerkGroupType.Player) => new()
    {
        Type = type,
        Name = type.ToString(),
        Category = category,
        IsActive = active,
        GroupType = group,
    };

    private void BuildWorld()
    {
        var plain = NewPerk(Plain);
        plain.StatBonuses.Add(new PerkStatBonus(StatType.AttackPercentAdjustment, 5));
        plain.PerkLevels[1] = new PerkLevel { Price = 1, GrantedFeats = { PerkBench.PassiveFeat } };
        plain.PerkLevels[2] = new PerkLevel
        {
            Price = 2,
            GrantedFeats = { PerkBench.ActiveFeatOne },
            Requirements = { new PerkRequirementSkill(SkillType.Mimicry, 25) },
            StatBonuses = { new PerkStatBonus(StatType.AttackPercentAdjustment, 3) },
        };
        plain.PerkLevels[3] = new PerkLevel
        {
            Price = 3,
            GrantedFeats = { PerkBench.ActiveFeatTwo },
            Requirements = { new PerkRequirementSkill(SkillType.Mimicry, 60) },
        };
        plain.EquippedTriggers.Add((_, _, _, _, _) => { });
        plain.UnequippedTriggers.Add((_, _, _, _, _) => { });
        plain.PurchasedTriggers.Add(_ => { });
        plain.RefundedTriggers.Add(player => _bench.Engine.Note($"RefundTrigger:{player}:saved={_bench.Db.Saved.Count}"));
        _bench.Definitions[Plain] = plain;

        var unlockable = NewPerk(Unlockable);
        unlockable.PerkLevels[1] = new PerkLevel { Price = 1, Requirements = { new PerkRequirementUnlock(Unlockable) } };
        _bench.Definitions[Unlockable] = unlockable;

        var dormant = NewPerk(Dormant, active: false);
        dormant.PerkLevels[1] = new PerkLevel { Price = 1 };
        _bench.Definitions[Dormant] = dormant;

        var beastOnly = NewPerk(BeastOnly, PerkCategoryType.FirstAid, group: PerkGroupType.Beast);
        beastOnly.PerkLevels[1] = new PerkLevel { Price = 1 };
        beastOnly.PerkLevels[4] = new PerkLevel { Price = 1 };
        _bench.Definitions[BeastOnly] = beastOnly;

        var light = NewPerk(Light);
        light.ForceAffinityType = ForceAffinityType.Light;
        light.PerkLevels[1] = new PerkLevel { Price = 1 };
        _bench.Definitions[Light] = light;

        var dark = NewPerk(Dark);
        dark.ForceAffinityType = ForceAffinityType.Dark;
        dark.PerkLevels[1] = new PerkLevel { Price = 1 };
        _bench.Definitions[Dark] = dark;

        // The primary stat names the targeted perk, the adjustment stat is what it adds.
        var targeted = NewPerk(Targeted);
        targeted.PerkLevels[1] = new PerkLevel
        {
            Price = 1,
            StatBonuses =
            {
                new PerkStatBonus(StatType.DefensePercentAdjustment, (int)Marked),
                new PerkStatBonus(StatType.PhysicalDefensePercentAdjustment, 0),
                new PerkStatBonus(StatType.AccuracyPercentAdjustment, 7),
            },
        };
        _bench.Definitions[Targeted] = targeted;

        var marked = NewPerk(Marked);
        marked.PerkLevels[1] = new PerkLevel { Price = 1 };
        _bench.Definitions[Marked] = marked;

        _bench.Abilities[PerkBench.ActiveFeatOne] = new AbilityDetail
        {
            Name = "One",
            EffectiveLevelPerkType = Plain,
            RecastGroup = RecastGroup.Flash,
        };
        _bench.Abilities[PerkBench.ActiveFeatTwo] = new AbilityDetail
        {
            Name = "Two",
            EffectiveLevelPerkType = Plain,
            StatusEffectTypesRemovedOnPerkRefund = { typeof(string) },
            SourceOwnedStatusEffectTypesRemovedOnPerkRefund = { typeof(int) },
        };
    }

    private void Load()
    {
        _service.CacheData();
        _service.CachePerkFeatData();
    }

    private Player AddPlayer(Action<Player>? configure = null)
    {
        var player = new Player { Id = "uuid-" + PerkBench.Player };
        configure?.Invoke(player);
        _bench.Db.With(player);
        return player;
    }

    private IReadOnlyList<string> Sequence(params string[] members) =>
        _bench.Engine.Calls
            .Where(call => call.Member == TimelineNWScript.NoteMember || members.Contains(call.Member))
            .Select(call => call.Member == TimelineNWScript.NoteMember ? (string)call.Arguments[0]! : call.Member)
            .ToList();

    // ---- cache build ----

    [Test]
    public void CacheData_FillsEveryCacheInDefinitionOrder()
    {
        _service.CacheData();

        _service.GetAllPerks().Keys.Should().Equal(
            new[] { Plain, Unlockable, Dormant, BeastOnly, Light, Dark, Targeted, Marked });
        _service.GetAllPerkCategories().Should().ContainKey(PerkCategoryType.General);
        _service.GetAllPerkCategories()[PerkCategoryType.FirstAid].IsActive.Should().BeFalse();
        _service.GetAllActivePerks(PerkGroupType.Player).Keys.Should().NotContain(Dormant).And.Contain(Plain);
        _service.GetAllActivePerks(PerkGroupType.Beast).Keys.Should().Equal(BeastOnly);
        _service.GetAllActivePerkCategories(PerkGroupType.Player).Keys.Should().Contain(PerkCategoryType.General);
        var beastCategories = () => _service.GetAllActivePerkCategories(PerkGroupType.Beast);
        beastCategories.Should().Throw<KeyNotFoundException>("the only beast perk is in an inactive category, as on master");
        _service.GetActivePerksInCategory(PerkGroupType.Player, PerkCategoryType.General).Keys.Should().Contain(Plain);
        _service.GetCharacterType(CharacterType.Standard).Name.Should().Be("Standard");
    }

    [Test]
    public void CacheData_ComputesTiersMaxLevelsAndUnlockPerks()
    {
        Load();

        _service.GetPerkLevelTier(Plain, 1).Should().Be(1);
        _service.GetPerkLevelTier(Plain, 2).Should().Be(3);
        _service.GetPerkLevelTier(Plain, 3).Should().Be(5, "the tier is clamped to 5");
        _service.GetPerkLevelTier(Plain, 9).Should().Be(0);
        _service.GetPerkLevelTier(Dormant, 1).Should().Be(1);
        _service.GetPerkLevelTier(PerkType.Invalid, 1).Should().Be(0);
        _service.GetPerkLevelTier(Unlockable, 1).Should().Be(0, "an unlock requirement stops the tier scan of its perk");

        // A creature with no variable gets the max level of the perk.
        _service.GetPerkLevel(PerkBench.Npc, Plain).Should().Be(3);
        _service.GetPerkLevel(PerkBench.Npc, BeastOnly).Should().Be(4);
    }

    [Test]
    public void CacheData_CachesTriggersOfActivePerksOnly()
    {
        _service.CacheData();

        _service.GetAllEquipTriggers().Should().ContainKey(Plain).WhoseValue.Should().HaveCount(1);
        _service.GetAllUnequipTriggers().Should().ContainKey(Plain);
        _service.GetAllPurchaseTriggers().Should().ContainKey(Plain);
        _service.GetAllRefundTriggers().Should().ContainKey(Plain);
        _service.GetAllEquipTriggers().Should().NotContainKey(Dormant);

        _bench.Definitions[Dormant].PurchasedTriggers.Add(_ => { });
        _service.CacheData();
        _service.GetAllPurchaseTriggers().Should().NotContainKey(Dormant, "an inactive perk has no triggers");
    }

    [Test]
    public void LookupMembers_AnswerForKnownAndUnknownPerks()
    {
        _service.CacheData();

        _service.GetPerkDetails(Plain).Name.Should().Be("AlphaRhythm");
        _service.TryGetPerkDetails(Plain, out var detail).Should().BeTrue();
        detail.Type.Should().Be(Plain);
        _service.TryGetPerkDetails(PerkType.Invalid, out _).Should().BeFalse();
        _service.GetPerkCategoryType(Plain).Should().Be(PerkCategoryType.General);
        _service.GetPerkCategoryType(PerkType.Invalid).Should().Be(PerkCategoryType.Invalid);
        _service.GetPerkCategoryDetails(PerkCategoryType.General).Name.Should().Be("General");

        _service.IsPerkInCategory(Plain, (int)PerkCategoryType.General).Should().BeTrue();
        _service.IsPerkInCategory(Plain, (int)PerkCategoryType.FirstAid).Should().BeFalse();
        _service.IsPerkInCategory(Plain, 0).Should().BeFalse();
        _service.IsPerkInCategory(Plain, 99999).Should().BeFalse();
        _service.IsPerkInCategory(PerkType.Invalid, (int)PerkCategoryType.General).Should().BeFalse();
    }

    [Test]
    public void GetAllPerks_HandsOutACopyAndTheTriggerTablesThemselves()
    {
        _service.CacheData();

        _service.GetAllPerks().Should().NotBeSameAs(_service.GetAllPerks());
        _service.GetAllEquipTriggers().Should().BeSameAs(_service.GetAllEquipTriggers());
    }

    // ---- feat lookups ----

    [Test]
    public void FeatLookups_GrantOnlyTheCurrentActiveAbilityOfALevel()
    {
        Load();

        _service.GetGrantedFeatsForPerkLevel(Plain, 1).Should().Equal(PerkBench.PassiveFeat);
        _service.GetGrantedFeatsForPerkLevel(Plain, 2).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatOne);
        _service.GetGrantedFeatsForPerkLevel(Plain, 3).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatTwo);
        _service.GetGrantedFeatsForPerkLevel(Plain, 40).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatTwo);
        _service.GetGrantedFeatsForPerkLevel(Plain, 0).Should().BeEmpty();
        _service.GetGrantedFeatsForPerkLevel(Dormant, 1).Should().BeEmpty();
        _service.GetCurrentActiveAbilityFeats(Plain, 1).Should().BeEmpty();
        _service.GetCurrentActiveAbilityFeats(Plain, 2).Should().Equal(PerkBench.ActiveFeatOne);
        _service.GetCurrentActiveAbilityFeats(Plain, 3).Should().Equal(PerkBench.ActiveFeatTwo);
        _service.GetAllActiveAbilityFeats(Plain).Should().Equal(PerkBench.ActiveFeatOne, PerkBench.ActiveFeatTwo);
        _service.GetAllActiveAbilityFeats(Dormant).Should().BeEmpty();
        _service.GetActiveAbilityRecastGroup(Plain).Should().Be(RecastGroup.Flash);
        _service.GetActiveAbilityRecastGroup(Dormant).Should().Be(RecastGroup.Invalid);
    }

    [Test]
    public void IsCurrentActiveAbilityFeat_ReplacesOnlyActiveAbilityFeats()
    {
        Load();

        _service.IsCurrentActiveAbilityFeat(PerkBench.PassiveFeat, Plain, 2).Should().BeTrue("not an active ability feat");
        _service.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatOne, Plain, 2).Should().BeTrue();
        _service.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatOne, Plain, 3).Should().BeFalse("level 3 replaced it");
        _service.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatTwo, Plain, 1).Should().BeTrue(
            "no active ability is current at level 1, so nothing is replaced");
        _service.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatTwo, Plain, 3).Should().BeTrue();
    }

    [Test]
    public void FeatLookups_LoadOnFirstUseAfterTheAbilitiesAppear()
    {
        var abilities = new Dictionary<FeatType, AbilityDetail>(_bench.Abilities);
        _bench.Abilities.Clear();
        _service.CacheData();
        _service.CachePerkFeatData();
        _service.GetGrantedFeatsForPerkLevel(Plain, 2).Should().BeEmpty("the ability catalog is empty");
        _service.GetAllActiveAbilityFeats(Plain).Should().BeEmpty();

        foreach (var (feat, ability) in abilities)
            _bench.Abilities[feat] = ability;
        _service.GetGrantedFeatsForPerkLevel(Plain, 2).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatOne);
        _service.GetActiveAbilityRecastGroup(Plain).Should().Be(RecastGroup.Flash);
    }

    [Test]
    public void CachePerkFeatData_RebuildsFromScratchOnEveryCall()
    {
        Load();
        _bench.Abilities.Remove(PerkBench.ActiveFeatOne);

        _service.CachePerkFeatData();

        _service.GetAllActiveAbilityFeats(Plain).Should().Equal(PerkBench.ActiveFeatTwo);
        _service.GetGrantedFeatsForPerkLevel(Plain, 2).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatOne);
    }

    // ---- force affinity ----

    [Test]
    public void GetForceAffinity_IsZeroForNpcsAndDmsAndClampedForPlayers()
    {
        _bench.StatAdjustments[(PerkBench.Npc, StatType.ForceAffinity)] = 8;
        _bench.StatAdjustments[(PerkBench.Dm, StatType.ForceAffinity)] = 8;
        _bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = 25;
        _bench.StatAdjustments[(PerkBench.Master, StatType.ForceAffinity)] = -25;

        _service.GetForceAffinity(PerkBench.Npc).Should().Be(0);
        _service.GetForceAffinity(PerkBench.Dm).Should().Be(0);
        _service.GetForceAffinity(PerkBench.Player).Should().Be(10);
        _service.GetForceAffinity(PerkBench.Master).Should().Be(-10);
    }

    [Test]
    public void TryGetForceSideAffinity_FollowsTheSideOfThePerk()
    {
        _service.CacheData();
        _bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = 6;

        _service.TryGetForceSideAffinity(PerkBench.Player, Light, out var light).Should().BeTrue();
        light.Should().Be(6);
        _service.TryGetForceSideAffinity(PerkBench.Player, Dark, out var dark).Should().BeTrue();
        dark.Should().Be(-6);
        _service.TryGetForceSideAffinity(PerkBench.Player, Plain, out var plain).Should().BeFalse();
        plain.Should().Be(0);
        _service.TryGetForceSideAffinity(PerkBench.Player, PerkType.Invalid, out _).Should().BeFalse();
        _service.TryGetForceSideAffinity(PerkBench.Player, PerkType.ThrowRock, out _).Should().BeFalse("not a cached perk");

        _bench.Definitions[Light].ForceAffinityType = (ForceAffinityType)7;
        _service.CacheData();
        _service.TryGetForceSideAffinity(PerkBench.Player, Light, out _).Should().BeFalse("an undefined side has no affinity");
    }

    [Test]
    public void ForceAffinityMultipliers_ScaleAndCap()
    {
        _service.CacheData();
        _bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = 10;
        _bench.StatAdjustments[(PerkBench.Master, StatType.ForceAffinity)] = -7;

        _service.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, Light).Should().BeApproximately(1.5f, 0.0001f);
        _service.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, Dark).Should().BeApproximately(0.5f, 0.0001f);
        _service.GetForceAffinityMagnitudeMultiplier(PerkBench.Master, Light).Should().BeApproximately(0.65f, 0.0001f);
        _service.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, Plain).Should().Be(1f);
        _service.GetForceAffinityRestorativeMagnitudeMultiplier(PerkBench.Player, Light).Should().Be(1.25f);
        _service.GetForceAffinityRestorativeMagnitudeMultiplier(PerkBench.Master, Light).Should().BeApproximately(0.65f, 0.0001f);
        _service.GetForceAffinityHitChanceAdjustment(PerkBench.Player, Light).Should().Be(5);
        _service.GetForceAffinityHitChanceAdjustment(PerkBench.Master, Light).Should().Be(-4, "the half is floored");
        _service.GetForceAffinityHitChanceAdjustment(PerkBench.Player, Plain).Should().Be(0);
    }

    [Test]
    public void ApplyForceAffinityMagnitude_RoundsAwayFromZeroAndKeepsNonPositiveAmounts()
    {
        _service.CacheData();
        _bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = 10;
        _bench.StatAdjustments[(PerkBench.Master, StatType.ForceAffinity)] = 1;

        _service.ApplyForceAffinityMagnitude(PerkBench.Player, Light, 10).Should().Be(15);
        _service.ApplyForceAffinityMagnitude(PerkBench.Player, Dark, 1).Should().Be(1, "never below 1");
        _service.ApplyForceAffinityMagnitude(PerkBench.Player, Light, 0).Should().Be(0);
        _service.ApplyForceAffinityMagnitude(PerkBench.Player, Light, -4).Should().Be(-4);
        _service.ApplyForceAffinityMagnitude(PerkBench.Player, Plain, 9).Should().Be(9, "a neutral multiplier changes nothing");
        _service.ApplyForceAffinityMagnitude(PerkBench.Master, Light, 10).Should().Be(11);
        _service.ApplyForceAffinityMagnitude(PerkBench.Player, Light, 10f).Should().BeApproximately(15f, 0.0001f);
        _service.ApplyForceAffinityMagnitude(PerkBench.Player, Light, -3f).Should().Be(-3f);
        _service.ApplyForceAffinityRestorativeMagnitude(PerkBench.Player, Light, 100).Should().Be(125);
        _service.ApplyForceAffinityRestorativeMagnitude(PerkBench.Player, Light, 0).Should().Be(0);
    }

    // ---- stat bonuses ----

    [Test]
    public void GetStatBonus_AddsPerkAndLevelBonusesOfThePlayersLevel()
    {
        _service.CacheData();
        AddPlayer(player => player.Perks[Plain] = 2);

        _service.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(8);
        _service.GetStatBonus(PerkBench.Player, StatType.AccuracyPercentAdjustment).Should().Be(0);
    }

    [Test]
    public void GetStatBonus_SkipsLevelsTheCreatureDoesNotHave()
    {
        _service.CacheData();
        AddPlayer(player => player.Perks[Plain] = 1);

        _service.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(5);
        _service.GetStatBonus(PerkBench.Master, StatType.AttackPercentAdjustment).Should().Be(0, "no record, no level");
    }

    [Test]
    public void GetStatBonus_ReadsTheVariableOfACreatureThatIsNotAPlayer()
    {
        _service.CacheData();

        _service.GetStatBonus(PerkBench.Npc, StatType.AttackPercentAdjustment).Should().Be(0);

        _bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)Plain}")] = 2;
        _service.GetStatBonus(PerkBench.Npc, StatType.AttackPercentAdjustment).Should().Be(8);

        _bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)Plain}")] = 3;
        _service.GetStatBonus(PerkBench.Npc, StatType.AttackPercentAdjustment).Should().Be(5, "level 3 has no level bonus");
    }

    [Test]
    public void GetStatBonus_UsesThePerkLevelOfDroidsAndPossessedPlayers()
    {
        _service.CacheData();
        _bench.DroidPerks[Plain] = 2;

        _service.GetStatBonus(PerkBench.DroidCreature, StatType.AttackPercentAdjustment).Should().Be(8);

        _bench.LocalInts[(PerkBench.PossessedDm, $"PERK_LEVEL_{(int)Plain}")] = 1;
        _service.GetStatBonus(PerkBench.PossessedDm, StatType.AttackPercentAdjustment).Should().Be(5,
            "a possessed DM reads its variable, not a record");
    }

    [Test]
    public void GetStatSources_NamesEachContributingPerk()
    {
        _service.CacheData();
        AddPlayer(player => player.Perks[Plain] = 2);

        var sources = _service.GetStatSources(PerkBench.Player, StatType.AttackPercentAdjustment);

        sources.Should().ContainSingle();
        sources[0].Key.Should().Be($"perk:{(int)Plain}");
        sources[0][StatType.AttackPercentAdjustment].Should().Be(8);

        _service.GetStatSources(PerkBench.Player, StatType.AccuracyPercentAdjustment).Should().BeEmpty();
        _service.GetStatSources(PerkBench.Player, StatType.Invalid).Should().BeEmpty();
        _service.GetStatSources(PerkBench.Npc, StatType.AttackPercentAdjustment).Should().BeEmpty("the level is 0");
    }

    [Test]
    public void GetTargetedStatBonus_AppliesTheAdjustmentOnlyToTheNamedPerk()
    {
        _service.CacheData();
        AddPlayer(player => player.Perks[Targeted] = 1);

        int Bonus(PerkType target) => _service.GetTargetedStatBonus(
            PerkBench.Player,
            target,
            StatType.DefensePercentAdjustment,
            StatType.PhysicalDefensePercentAdjustment,
            StatType.AccuracyPercentAdjustment);

        Bonus(Marked).Should().Be(7);
        Bonus(Plain).Should().Be(0);
        Bonus(PerkType.Invalid).Should().Be(0);
        _service.GetTargetedStatBonus(
            PerkBench.Player, Marked, StatType.DefensePercentAdjustment, StatType.PhysicalDefensePercentAdjustment,
            StatType.Invalid).Should().Be(0);
        _service.GetTargetedStatBonus(
            PerkBench.Master, Marked, StatType.DefensePercentAdjustment, StatType.PhysicalDefensePercentAdjustment,
            StatType.AccuracyPercentAdjustment).Should().Be(0, "the master has no level of the perk");
    }

    [Test]
    public void GetTargetedStatBonus_MatchesTheSecondaryStatToo()
    {
        _service.CacheData();
        AddPlayer(player => player.Perks[Targeted] = 1);

        _service.GetTargetedStatBonus(
            PerkBench.Player, Marked, StatType.PhysicalDefensePercentAdjustment, StatType.DefensePercentAdjustment,
            StatType.AccuracyPercentAdjustment).Should().Be(7);
    }

    // ---- perk levels ----

    [Test]
    public void GetPerkLevel_ReadsThePlayersRecord()
    {
        _service.CacheData();

        _service.GetPerkLevel(PerkBench.Player, Plain).Should().Be(0, "no record");
        AddPlayer(player => player.Perks[Plain] = 2);
        _service.GetPerkLevel(PerkBench.Player, Plain).Should().Be(2);
        _service.GetPerkLevel(PerkBench.Player, Light).Should().Be(0);
        _service.GetPerkLevel(PerkBench.Player, PerkType.Invalid).Should().Be(0);
    }

    [Test]
    public void GetPerkLevel_HandlesDmsDroidsAndCreatures()
    {
        _service.CacheData();
        AddPlayer(player => player.Perks[Plain] = 2);

        _service.GetPerkLevel(PerkBench.Dm, Plain).Should().Be(0);
        _service.GetPerkLevel(PerkBench.PossessedDm, Plain).Should().Be(3, "a possessed DM is a creature: no variable, the max level");
        _bench.DroidPerks[Plain] = 3;
        _service.GetPerkLevel(PerkBench.DroidCreature, Plain).Should().Be(3);
        _service.GetPerkLevel(PerkBench.DroidCreature, Light).Should().Be(0);
        _bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)Plain}")] = 2;
        _service.GetPerkLevel(PerkBench.Npc, Plain).Should().Be(2);
        _service.GetPerkLevel(PerkBench.Npc, Light).Should().Be(1, "no variable: the perk's max level");
    }

    [Test]
    public void GetPerkLevel_OfABeastIsTheHighestLevelItsMasterMeets()
    {
        _service.CacheData();
        _bench.Db.With(new Beast { Id = $"beast-{PerkBench.BeastCreature}", Perks = { [Plain] = 3 } });
        _bench.Db.With(new Player { Id = $"uuid-{PerkBench.Master}" });

        _bench.Definitions[Plain].PerkLevels[3].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[3].Requirements.Add(new ScriptedPerkRequirement(_ => ""));
        _service.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(3, "level 3 has a requirement that the scripted check passes");

        _bench.Definitions[Plain].PerkLevels[3].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[3].Requirements.Add(new ScriptedPerkRequirement(_ => "no"));
        _bench.Definitions[Plain].PerkLevels[2].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[2].Requirements.Add(new ScriptedPerkRequirement(_ => " "));
        _service.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(2, "the first level whose requirement passes");

        _bench.Definitions[Plain].PerkLevels[2].Requirements.Clear();
        _service.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(2, "no requirements at level 2");

        _service.GetPerkLevel(PerkBench.BeastCreature, Light).Should().Be(0, "the beast has not bought the perk");
    }

    [Test]
    public void GetPerkLevel_OfABeastIsZeroWithoutARecordOrAMaster()
    {
        _service.CacheData();

        _service.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(0, "no beast record");

        _bench.Db.With(new Beast { Id = $"beast-{PerkBench.BeastCreature}", Perks = { [Plain] = 1 } });
        _bench.Masters.Remove(PerkBench.BeastCreature);
        _service.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(0, "no master");

        _bench.Masters[PerkBench.BeastCreature] = PerkBench.Npc;
        _service.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(0, "the master is not a player");
    }

    [Test]
    public void GetPerkLevel_OfABeastWhoseLevelsAllFailIsZero()
    {
        _service.CacheData();
        _bench.Db.With(new Beast { Id = $"beast-{PerkBench.BeastCreature}", Perks = { [Plain] = 3 } });
        foreach (var level in _bench.Definitions[Plain].PerkLevels.Values)
        {
            level.Requirements.Clear();
            level.Requirements.Add(new ScriptedPerkRequirement(_ => "no"));
        }

        _service.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(0);
    }

    [Test]
    public void GetPlayerEffectivePerkLevel_IsTheHighestLevelWhoseRequirementsPass()
    {
        _service.CacheData();
        AddPlayer(player => player.Perks[Plain] = 3);
        var passes = new Dictionary<int, bool> { [2] = true, [3] = false };
        _bench.Definitions[Plain].PerkLevels[2].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[2].Requirements.Add(new ScriptedPerkRequirement(_ => passes[2] ? "" : "x"));
        _bench.Definitions[Plain].PerkLevels[3].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[3].Requirements.Add(new ScriptedPerkRequirement(_ => passes[3] ? "" : "x"));

        _service.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(2);
        passes[3] = true;
        _service.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(3);
        passes[3] = false;
        passes[2] = false;
        _service.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(1, "level 1 has no requirements");
    }

    [Test]
    public void GetPlayerEffectivePerkLevel_IsZeroWhenNothingPasses()
    {
        _service.CacheData();
        AddPlayer(player => player.Perks[Light] = 1);
        _bench.Definitions[Light].PerkLevels[1].Requirements.Add(new ScriptedPerkRequirement(_ => "no"));

        _service.GetPlayerEffectivePerkLevel(PerkBench.Player, Light).Should().Be(0);
    }

    [Test]
    public void GetPlayerEffectivePerkLevel_IsZeroForNonPlayersAndMissingRecords()
    {
        _service.CacheData();

        _service.GetPlayerEffectivePerkLevel(PerkBench.Npc, Plain).Should().Be(0);
        _service.GetPlayerEffectivePerkLevel(PerkBench.Dm, Plain).Should().Be(0);
        _service.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(0, "no record");
        AddPlayer();
        _service.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(0, "the perk is not owned");
    }

    // ---- restore and sync ----

    [Test]
    public void SyncGrantedFeats_RemovesReplacedActiveFeatsThenGrantsMissingOnesThenAppliesPreferences()
    {
        Load();
        _bench.KnownFeats.Add(PerkBench.PassiveFeat);
        _bench.KnownFeats.Add(PerkBench.ActiveFeatOne);

        _service.SyncGrantedFeats(PerkBench.Player, Plain, 3, addByLevel: false);

        Sequence().Should().Equal(
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatOne}",
            $"Creature.AddFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatTwo}",
            $"QuickCast.ApplyPreferences:{PerkBench.Player}");
    }

    [Test]
    public void SyncGrantedFeats_AddsByLevelWhenAsked()
    {
        Load();

        _service.SyncGrantedFeats(PerkBench.Player, Plain, 2, addByLevel: true);

        Sequence().Should().Equal(
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatTwo}",
            $"Creature.AddFeatByLevel:{PerkBench.Player}:{(int)PerkBench.PassiveFeat}:1",
            $"Creature.AddFeatByLevel:{PerkBench.Player}:{(int)PerkBench.ActiveFeatOne}:1",
            $"QuickCast.ApplyPreferences:{PerkBench.Player}");
    }

    [Test]
    public void SyncGrantedFeats_DoesNothingForAnInvalidCreature()
    {
        Load();
        _bench.Invalid.Add(PerkBench.Player);

        _service.SyncGrantedFeats(PerkBench.Player, Plain, 2, addByLevel: true);

        Sequence().Should().BeEmpty();
    }

    [Test]
    public void RestorePlayerFeats_SyncsEachOwnedActivePlayerPerkInRecordOrder()
    {
        Load();
        AddPlayer(player =>
        {
            player.Perks[Light] = 1;
            player.Perks[Plain] = 1;
            player.Perks[Dormant] = 1;
            player.Perks[BeastOnly] = 1;
            player.Perks[Dark] = 0;
            player.Perks[PerkType.ThrowRock] = 2;
        });

        _service.RestorePlayerFeats(PerkBench.Player);

        Sequence().Should().Equal(
            $"QuickCast.ApplyPreferences:{PerkBench.Player}",
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatOne}",
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatTwo}",
            $"Creature.AddFeatByLevel:{PerkBench.Player}:{(int)PerkBench.PassiveFeat}:1",
            $"QuickCast.ApplyPreferences:{PerkBench.Player}");
        _bench.Db.Saved.Should().BeEmpty("a restore does not save");
    }

    [Test]
    public void RestorePlayerFeats_DoesNothingWithoutAValidPlayerOrARecord()
    {
        Load();

        _service.RestorePlayerFeats(PerkBench.Player);
        _bench.Invalid.Add(PerkBench.Master);
        _service.RestorePlayerFeats(PerkBench.Master);

        Sequence().Should().BeEmpty();
        _bench.Db.Reads.Should().Equal(new[] { $"uuid-{PerkBench.Player}" });
    }

    // ---- refund ----

    [Test]
    public void RemoveStatusEffectsOnPerkRefund_RemovesDeclaredEffectsThenSourceOwnedOnesThenRefreshes()
    {
        Load();

        _service.RemoveStatusEffectsOnPerkRefund(PerkBench.Player, Plain);

        Sequence().Should().Equal(
            $"StatusEffect.Remove:{PerkBench.Player}:String:saved=0",
            $"StatusEffect.RemoveBySource:{PerkBench.Player}:Int32:Two:saved=0",
            $"Combat.RefreshStatDrivenTrackerEffects:{PerkBench.Player}:saved=0");
    }

    [Test]
    public void RemoveStatusEffectsOnPerkRefund_IgnoresInvalidPerksAndCreatures()
    {
        Load();

        _service.RemoveStatusEffectsOnPerkRefund(PerkBench.Player, PerkType.Invalid);
        _bench.Invalid.Add(PerkBench.Master);
        _service.RemoveStatusEffectsOnPerkRefund(PerkBench.Master, Plain);

        Sequence().Should().BeEmpty();
    }

    [Test]
    public void GetSourceOwnedStatusEffectRefunds_KeepsEachAbilityOfASharedStatus()
    {
        var first = new AbilityDetail { Name = "A", EffectiveLevelPerkType = Plain };
        var second = new AbilityDetail { Name = "B", EffectiveLevelPerkType = Plain };
        var other = new AbilityDetail { Name = "C", EffectiveLevelPerkType = Light };
        foreach (var ability in new[] { first, second, other })
        {
            ability.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Add(typeof(string));
            ability.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Add(typeof(string));
        }

        var refunds = _service.GetSourceOwnedStatusEffectRefunds(new[] { first, second, other }, Plain);

        refunds.Select(refund => refund.Ability.Name).Should().Equal("A", "B");
        refunds.Select(refund => refund.StatusEffectType).Should().OnlyContain(type => type == typeof(string));
    }

    [Test]
    public void ShouldEnforceActiveAbilityFeatReplacement_DependsOnWhoOwnsTheLevel()
    {
        _service.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Player, Plain).Should().BeTrue();
        _service.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.DroidCreature, Plain).Should().BeTrue();
        _service.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.BeastCreature, Plain).Should().BeTrue();
        _service.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.PossessedDm, Plain).Should().BeFalse();
        _service.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Npc, Plain).Should().BeFalse();
        _bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)Plain}")] = 1;
        _service.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Npc, Plain).Should().BeTrue();
        _service.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Player, PerkType.Invalid).Should().BeFalse();
        _bench.Invalid.Add(PerkBench.Player);
        _service.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Player, Plain).Should().BeFalse();
    }

    // ---- unlock ----

    [Test]
    public void UnlockPerkForPlayer_StampsTheTimeAndSaves()
    {
        _service.CacheData();
        var player = AddPlayer();
        var now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

        using (Clock.OverrideForTests(() => now))
        {
            _service.UnlockPerkForPlayer(PerkBench.Player, Unlockable);
        }

        player.UnlockedPerks[Unlockable].Should().Be(now);
        _bench.Db.Saved.Should().Equal(new EntityBase[] { player });
    }

    [Test]
    public void UnlockPerkForPlayer_DoesNothingWhenThereIsNothingToUnlock()
    {
        _service.CacheData();
        var player = AddPlayer(p => p.UnlockedPerks[Unlockable] = DateTime.MinValue);

        _service.UnlockPerkForPlayer(PerkBench.Player, Unlockable);
        _service.UnlockPerkForPlayer(PerkBench.Player, Plain);
        _service.UnlockPerkForPlayer(PerkBench.Npc, Unlockable);
        _service.UnlockPerkForPlayer(PerkBench.Dm, Unlockable);

        player.UnlockedPerks[Unlockable].Should().Be(DateTime.MinValue, "already unlocked");
        _bench.Db.Saved.Should().BeEmpty();
    }

    // ---- skill decay ----

    private FacadeContainer NewContainerOverTheBenchDatabase() =>
        new(services => services.Replace(ServiceDescriptor.Singleton<IDBService>(_bench.Db)));

    [Test]
    public void RemovePerkLevelOnSkillDecay_RefundsTheLostLevelsInTheOldOrder()
    {
        Load();
        var player = AddPlayer(p =>
        {
            p.Perks[Plain] = 3;
            p.UnallocatedSP = 10;
            p.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = 5 };
        });
        _bench.KnownFeats.Add(PerkBench.PassiveFeat);
        _bench.Db.Saved.Clear();
        _bench.Engine.ClearCalls();

        using var container = NewContainerOverTheBenchDatabase();
        using (container.Install())
        {
            _service.RemovePerkLevelOnSkillDecay();
        }

        player.Perks[Plain].Should().Be(1, "the first level whose requirements still pass");
        player.UnallocatedSP.Should().Be(15, "levels 3 and 2 cost 3 and 2");
        Sequence("FloatingTextStringOnCreature", "ExportSingleCharacter").Should().Equal(
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatTwo}",
            "FloatingTextStringOnCreature",
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatOne}",
            "FloatingTextStringOnCreature",
            // SyncGrantedFeats at the new level, before the save.
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatOne}",
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatTwo}",
            $"QuickCast.ApplyPreferences:{PerkBench.Player}",
            // Then the save, and everything after it sees the saved player.
            $"StatusEffect.Remove:{PerkBench.Player}:String:saved=1",
            $"StatusEffect.RemoveBySource:{PerkBench.Player}:Int32:Two:saved=1",
            $"Combat.RefreshStatDrivenTrackerEffects:{PerkBench.Player}:saved=1",
            $"RefundTrigger:{PerkBench.Player}:saved=1",
            "ExportSingleCharacter");
        _bench.Db.Saved.Should().Equal(new EntityBase[] { player });

        var messages = _bench.Engine.CallsTo("FloatingTextStringOnCreature").Select(call => (string)call.Arguments[0]!).ToList();
        messages[0].Should().Be("Perk 'AlphaRhythm' level 3 was refunded because your skill fell under the minimum requirements. You reclaimed 3 SP.");
        messages[1].Should().Contain("level 2").And.Contain("You reclaimed 2 SP.");
    }

    [Test]
    public void RemovePerkLevelOnSkillDecay_ToLevelZeroSkipsTheFeatSync()
    {
        Load();
        _bench.Definitions[Plain].PerkLevels[1].Requirements.Add(new PerkRequirementSkill(SkillType.Mimicry, 3));
        var player = AddPlayer(p =>
        {
            p.Perks[Plain] = 1;
            p.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = 1 };
        });
        _bench.Engine.ClearCalls();

        using var container = NewContainerOverTheBenchDatabase();
        using (container.Install())
        {
            _service.RemovePerkLevelOnSkillDecay();
        }

        player.Perks[Plain].Should().Be(0);
        Sequence("ExportSingleCharacter").Should().NotContain($"QuickCast.ApplyPreferences:{PerkBench.Player}");
        Sequence("ExportSingleCharacter").Last().Should().Be("ExportSingleCharacter");
    }

    [Test]
    public void RemovePerkLevelOnSkillDecay_LeavesPerksWhoseEffectiveLevelHolds()
    {
        Load();
        var player = AddPlayer(p =>
        {
            p.Perks[Plain] = 2;
            p.Perks[Light] = 1;
            p.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = 90 };
        });
        _bench.Engine.ClearCalls();

        using var container = NewContainerOverTheBenchDatabase();
        using (container.Install())
        {
            _service.RemovePerkLevelOnSkillDecay();
        }

        player.Perks[Plain].Should().Be(2);
        _bench.Db.Saved.Should().BeEmpty();
        Sequence("ExportSingleCharacter").Should().Equal("ExportSingleCharacter");
    }

    [Test]
    public void RemovePerkLevelOnSkillDecay_IgnoresASkillNoPerkNeeds()
    {
        Load();
        _bench.DecayedSkill = SkillType.Piloting;
        _bench.Engine.ClearCalls();

        _service.RemovePerkLevelOnSkillDecay();

        _bench.Engine.CallsTo("ExportSingleCharacter").Should().BeEmpty();
        _bench.Db.Reads.Should().BeEmpty();
    }

    // ---- the seams' default bodies ----

    [Test]
    public void TheDefaultSeams_AreTheOriginalCalls()
    {
        // The aggregate seam calls the Stat facade, which resolves its instance from a container.
        using var container = NewContainerOverTheBenchDatabase();
        using var installed = container.Install();

        _bench.Service.DefaultAggregateStatAdjustment(StatType.AttackPercentAdjustment, 4, 3).Should().Be(7);
        _bench.Service.DefaultIsDroid(PerkBench.Npc).Should().BeFalse();
        _bench.Service.DefaultDroidControllerItem(PerkBench.DroidCreature).Should().Be(PerkBench.ObjectInvalid);
        using var facades = new FacadeContainer();
        using var scope = facades.Install();
        _bench.Service.DefaultAllAbilityDetails().Should().NotBeNull();
    }

    [Test]
    public void TheDefaultDefinitionSearch_FindsEveryPerkListOfTheServer()
    {
        var found = _bench.Service.DefaultPerkListDefinitionTypes().ToList();

        found.Should().NotBeEmpty().And.OnlyContain(type => typeof(IPerkListDefinition).IsAssignableFrom(type) && !type.IsAbstract);
        found.Should().Contain(typeof(SWLOR.Game.Server.Feature.PerkDefinition.ArmorPerkDefinition));
    }
}
