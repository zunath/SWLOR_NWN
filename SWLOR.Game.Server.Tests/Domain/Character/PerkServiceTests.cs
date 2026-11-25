using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The perk instance over a scripted world: the cache build, the level and feat reads, the feat sync and refund paths and the
/// skill decay handler. The stat bonuses are in <see cref="PerkStatBonusServiceTests"/> and force affinity in
/// <see cref="PerkForceAffinityServiceTests"/>. The order of the steps on the restore, sync, refund and decay paths is pinned
/// through the bench's timeline.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkServiceTests : PerkTestBase
{
    // ---- cache build ----

    [Test]
    public void CacheData_FillsEveryCacheInDefinitionOrder()
    {
        _bench.CacheData();

        _bench.Catalog.GetAllPerks().Keys.Should().Equal(
            new[] { Plain, Unlockable, Dormant, BeastOnly, Light, Dark, Targeted, Marked });
        _bench.Catalog.GetAllPerkCategories().Should().ContainKey(PerkCategoryType.General);
        _bench.Catalog.GetAllPerkCategories()[PerkCategoryType.FirstAid].IsActive.Should().BeFalse();
        _bench.Catalog.GetAllActivePerks(PerkGroupType.Player).Keys.Should().NotContain(Dormant).And.Contain(Plain);
        _bench.Catalog.GetAllActivePerks(PerkGroupType.Beast).Keys.Should().Equal(BeastOnly);
        _bench.Catalog.GetAllActivePerkCategories(PerkGroupType.Player).Keys.Should().Contain(PerkCategoryType.General);
        var beastCategories = () => _bench.Catalog.GetAllActivePerkCategories(PerkGroupType.Beast);
        beastCategories.Should().Throw<KeyNotFoundException>("the only beast perk is in an inactive category, as on master");
        _bench.Catalog.GetActivePerksInCategory(PerkGroupType.Player, PerkCategoryType.General).Keys.Should().Contain(Plain);
        _bench.Catalog.GetCharacterType(CharacterType.Standard).Name.Should().Be("Standard");
    }

    [Test]
    public void CacheData_ComputesTiersMaxLevelsAndUnlockPerks()
    {
        Load();

        _bench.Catalog.GetPerkLevelTier(Plain, 1).Should().Be(1);
        _bench.Catalog.GetPerkLevelTier(Plain, 2).Should().Be(3);
        _bench.Catalog.GetPerkLevelTier(Plain, 3).Should().Be(5, "the tier is clamped to 5");
        _bench.Catalog.GetPerkLevelTier(Plain, 9).Should().Be(0);
        _bench.Catalog.GetPerkLevelTier(Dormant, 1).Should().Be(1);
        _bench.Catalog.GetPerkLevelTier(PerkType.Invalid, 1).Should().Be(0);
        _bench.Catalog.GetPerkLevelTier(Unlockable, 1).Should().Be(0, "an unlock requirement stops the tier scan of its perk");

        // A creature with no variable gets the max level of the perk.
        _bench.Catalog.GetPerkLevel(PerkBench.Npc, Plain).Should().Be(3);
        _bench.Catalog.GetPerkLevel(PerkBench.Npc, BeastOnly).Should().Be(4);
    }

    [Test]
    public void CacheData_CachesTriggersOfActivePerksOnly()
    {
        _bench.CacheData();

        _bench.Catalog.GetAllEquipTriggers().Should().ContainKey(Plain).WhoseValue.Should().HaveCount(1);
        _bench.Catalog.GetAllUnequipTriggers().Should().ContainKey(Plain);
        _bench.Catalog.GetAllPurchaseTriggers().Should().ContainKey(Plain);
        _bench.Catalog.GetAllRefundTriggers().Should().ContainKey(Plain);
        _bench.Catalog.GetAllEquipTriggers().Should().NotContainKey(Dormant);

        _bench.Definitions[Dormant].PurchasedTriggers.Add(_ => { });
        _bench.CacheData();
        _bench.Catalog.GetAllPurchaseTriggers().Should().NotContainKey(Dormant, "an inactive perk has no triggers");
    }

    [Test]
    public void LookupMembers_AnswerForKnownAndUnknownPerks()
    {
        _bench.CacheData();

        _bench.Catalog.GetPerkDetails(Plain).Name.Should().Be("AlphaRhythm");
        _bench.Catalog.TryGetPerkDetails(Plain, out var detail).Should().BeTrue();
        detail.Type.Should().Be(Plain);
        _bench.Catalog.TryGetPerkDetails(PerkType.Invalid, out _).Should().BeFalse();
        _bench.Catalog.GetPerkCategoryType(Plain).Should().Be(PerkCategoryType.General);
        _bench.Catalog.GetPerkCategoryType(PerkType.Invalid).Should().Be(PerkCategoryType.Invalid);
        _bench.Catalog.GetPerkCategoryDetails(PerkCategoryType.General).Name.Should().Be("General");

        _bench.Catalog.IsPerkInCategory(Plain, (int)PerkCategoryType.General).Should().BeTrue();
        _bench.Catalog.IsPerkInCategory(Plain, (int)PerkCategoryType.FirstAid).Should().BeFalse();
        _bench.Catalog.IsPerkInCategory(Plain, 0).Should().BeFalse();
        _bench.Catalog.IsPerkInCategory(Plain, 99999).Should().BeFalse();
        _bench.Catalog.IsPerkInCategory(PerkType.Invalid, (int)PerkCategoryType.General).Should().BeFalse();
    }

    [Test]
    public void GetAllPerks_HandsOutACopyAndTheTriggerTablesThemselves()
    {
        _bench.CacheData();

        _bench.Catalog.GetAllPerks().Should().NotBeSameAs(_bench.Catalog.GetAllPerks());
        _bench.Catalog.GetAllEquipTriggers().Should().BeSameAs(_bench.Catalog.GetAllEquipTriggers());
    }

    // ---- feat lookups ----

    [Test]
    public void FeatLookups_GrantOnlyTheCurrentActiveAbilityOfALevel()
    {
        Load();

        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 1).Should().Equal(PerkBench.PassiveFeat);
        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 2).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatOne);
        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 3).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatTwo);
        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 40).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatTwo);
        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 0).Should().BeEmpty();
        _bench.Feats.GetGrantedFeatsForPerkLevel(Dormant, 1).Should().BeEmpty();
        _bench.Feats.GetCurrentActiveAbilityFeats(Plain, 1).Should().BeEmpty();
        _bench.Feats.GetCurrentActiveAbilityFeats(Plain, 2).Should().Equal(PerkBench.ActiveFeatOne);
        _bench.Feats.GetCurrentActiveAbilityFeats(Plain, 3).Should().Equal(PerkBench.ActiveFeatTwo);
        _bench.Feats.GetAllActiveAbilityFeats(Plain).Should().Equal(PerkBench.ActiveFeatOne, PerkBench.ActiveFeatTwo);
        _bench.Feats.GetAllActiveAbilityFeats(Dormant).Should().BeEmpty();
        _bench.Feats.GetActiveAbilityRecastGroup(Plain).Should().Be(RecastGroup.Flash);
        _bench.Feats.GetActiveAbilityRecastGroup(Dormant).Should().Be(RecastGroup.Invalid);
    }

    [Test]
    public void IsCurrentActiveAbilityFeat_ReplacesOnlyActiveAbilityFeats()
    {
        Load();

        _bench.Feats.IsCurrentActiveAbilityFeat(PerkBench.PassiveFeat, Plain, 2).Should().BeTrue("not an active ability feat");
        _bench.Feats.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatOne, Plain, 2).Should().BeTrue();
        _bench.Feats.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatOne, Plain, 3).Should().BeFalse("level 3 replaced it");
        _bench.Feats.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatTwo, Plain, 1).Should().BeTrue(
            "no active ability is current at level 1, so nothing is replaced");
        _bench.Feats.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatTwo, Plain, 3).Should().BeTrue();
    }

    [Test]
    public void FeatLookups_LoadOnFirstUseAfterTheAbilitiesAppear()
    {
        var abilities = new Dictionary<FeatType, AbilityDetail>(_bench.Abilities);
        _bench.Abilities.Clear();
        _bench.CacheData();
        _bench.Feats.CachePerkFeatData();
        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 2).Should().BeEmpty("the ability catalog is empty");
        _bench.Feats.GetAllActiveAbilityFeats(Plain).Should().BeEmpty();

        foreach (var (feat, ability) in abilities)
            _bench.Abilities[feat] = ability;
        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 2).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatOne);
        _bench.Feats.GetActiveAbilityRecastGroup(Plain).Should().Be(RecastGroup.Flash);
    }

    [Test]
    public void CachePerkFeatData_RebuildsFromScratchOnEveryCall()
    {
        Load();
        _bench.Abilities.Remove(PerkBench.ActiveFeatOne);

        _bench.Feats.CachePerkFeatData();

        _bench.Feats.GetAllActiveAbilityFeats(Plain).Should().Equal(PerkBench.ActiveFeatTwo);
        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 2).Should().Equal(PerkBench.PassiveFeat, PerkBench.ActiveFeatOne);
    }

    // ---- perk levels ----

    [Test]
    public void GetPerkLevel_ReadsThePlayersRecord()
    {
        _bench.CacheData();

        _bench.Catalog.GetPerkLevel(PerkBench.Player, Plain).Should().Be(0, "no record");
        AddPlayer(player => player.Perks[Plain] = 2);
        _bench.Catalog.GetPerkLevel(PerkBench.Player, Plain).Should().Be(2);
        _bench.Catalog.GetPerkLevel(PerkBench.Player, Light).Should().Be(0);
        _bench.Catalog.GetPerkLevel(PerkBench.Player, PerkType.Invalid).Should().Be(0);
    }

    [Test]
    public void GetPerkLevel_HandlesDmsDroidsAndCreatures()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Plain] = 2);

        _bench.Catalog.GetPerkLevel(PerkBench.Dm, Plain).Should().Be(0);
        _bench.Catalog.GetPerkLevel(PerkBench.PossessedDm, Plain).Should().Be(3, "a possessed DM is a creature: no variable, the max level");
        _bench.DroidPerks[Plain] = 3;
        _bench.Catalog.GetPerkLevel(PerkBench.DroidCreature, Plain).Should().Be(3);
        _bench.Catalog.GetPerkLevel(PerkBench.DroidCreature, Light).Should().Be(0);
        _bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)Plain}")] = 2;
        _bench.Catalog.GetPerkLevel(PerkBench.Npc, Plain).Should().Be(2);
        _bench.Catalog.GetPerkLevel(PerkBench.Npc, Light).Should().Be(1, "no variable: the perk's max level");
    }

    [Test]
    public void GetPerkLevel_OfABeastIsTheHighestLevelItsMasterMeets()
    {
        _bench.CacheData();
        _bench.Db.With(new Beast { Id = $"beast-{PerkBench.BeastCreature}", Perks = { [Plain] = 3 } });
        _bench.Db.With(new Player { Id = $"uuid-{PerkBench.Master}" });

        _bench.Definitions[Plain].PerkLevels[3].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[3].Requirements.Add(new ScriptedPerkRequirement(_ => ""));
        _bench.Catalog.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(3, "level 3 has a requirement that the scripted check passes");

        _bench.Definitions[Plain].PerkLevels[3].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[3].Requirements.Add(new ScriptedPerkRequirement(_ => "no"));
        _bench.Definitions[Plain].PerkLevels[2].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[2].Requirements.Add(new ScriptedPerkRequirement(_ => " "));
        _bench.Catalog.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(2, "the first level whose requirement passes");

        _bench.Definitions[Plain].PerkLevels[2].Requirements.Clear();
        _bench.Catalog.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(2, "no requirements at level 2");

        _bench.Catalog.GetPerkLevel(PerkBench.BeastCreature, Light).Should().Be(0, "the beast has not bought the perk");
    }

    [Test]
    public void GetPerkLevel_OfABeastIsZeroWithoutARecordOrAMaster()
    {
        _bench.CacheData();

        _bench.Catalog.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(0, "no beast record");

        _bench.Db.With(new Beast { Id = $"beast-{PerkBench.BeastCreature}", Perks = { [Plain] = 1 } });
        _bench.Masters.Remove(PerkBench.BeastCreature);
        _bench.Catalog.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(0, "no master");

        _bench.Masters[PerkBench.BeastCreature] = PerkBench.Npc;
        _bench.Catalog.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(0, "the master is not a player");
    }

    [Test]
    public void GetPerkLevel_OfABeastWhoseLevelsAllFailIsZero()
    {
        _bench.CacheData();
        _bench.Db.With(new Beast { Id = $"beast-{PerkBench.BeastCreature}", Perks = { [Plain] = 3 } });
        foreach (var level in _bench.Definitions[Plain].PerkLevels.Values)
        {
            level.Requirements.Clear();
            level.Requirements.Add(new ScriptedPerkRequirement(_ => "no"));
        }

        _bench.Catalog.GetPerkLevel(PerkBench.BeastCreature, Plain).Should().Be(0);
    }

    [Test]
    public void GetPlayerEffectivePerkLevel_IsTheHighestLevelWhoseRequirementsPass()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Plain] = 3);
        var passes = new Dictionary<int, bool> { [2] = true, [3] = false };
        _bench.Definitions[Plain].PerkLevels[2].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[2].Requirements.Add(new ScriptedPerkRequirement(_ => passes[2] ? "" : "x"));
        _bench.Definitions[Plain].PerkLevels[3].Requirements.Clear();
        _bench.Definitions[Plain].PerkLevels[3].Requirements.Add(new ScriptedPerkRequirement(_ => passes[3] ? "" : "x"));

        _bench.Catalog.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(2);
        passes[3] = true;
        _bench.Catalog.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(3);
        passes[3] = false;
        passes[2] = false;
        _bench.Catalog.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(1, "level 1 has no requirements");
    }

    [Test]
    public void GetPlayerEffectivePerkLevel_IsZeroWhenNothingPasses()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Light] = 1);
        _bench.Definitions[Light].PerkLevels[1].Requirements.Add(new ScriptedPerkRequirement(_ => "no"));

        _bench.Catalog.GetPlayerEffectivePerkLevel(PerkBench.Player, Light).Should().Be(0);
    }

    [Test]
    public void GetPlayerEffectivePerkLevel_IsZeroForNonPlayersAndMissingRecords()
    {
        _bench.CacheData();

        _bench.Catalog.GetPlayerEffectivePerkLevel(PerkBench.Npc, Plain).Should().Be(0);
        _bench.Catalog.GetPlayerEffectivePerkLevel(PerkBench.Dm, Plain).Should().Be(0);
        _bench.Catalog.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(0, "no record");
        AddPlayer();
        _bench.Catalog.GetPlayerEffectivePerkLevel(PerkBench.Player, Plain).Should().Be(0, "the perk is not owned");
    }

    // ---- restore and sync ----

    [Test]
    public void SyncGrantedFeats_RemovesReplacedActiveFeatsThenGrantsMissingOnesThenAppliesPreferences()
    {
        Load();
        _bench.KnownFeats.Add(PerkBench.PassiveFeat);
        _bench.KnownFeats.Add(PerkBench.ActiveFeatOne);

        _bench.Feats.SyncGrantedFeats(PerkBench.Player, Plain, 3, addByLevel: false);

        Sequence().Should().Equal(
            $"QuickCast.RemoveFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatOne}",
            $"Creature.AddFeat:{PerkBench.Player}:{(int)PerkBench.ActiveFeatTwo}",
            $"QuickCast.ApplyPreferences:{PerkBench.Player}");
    }

    [Test]
    public void SyncGrantedFeats_AddsByLevelWhenAsked()
    {
        Load();

        _bench.Feats.SyncGrantedFeats(PerkBench.Player, Plain, 2, addByLevel: true);

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

        _bench.Feats.SyncGrantedFeats(PerkBench.Player, Plain, 2, addByLevel: true);

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

        _bench.Feats.RestorePlayerFeats(PerkBench.Player);

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

        _bench.Feats.RestorePlayerFeats(PerkBench.Player);
        _bench.Invalid.Add(PerkBench.Master);
        _bench.Feats.RestorePlayerFeats(PerkBench.Master);

        Sequence().Should().BeEmpty();
        _bench.Db.Reads.Should().Equal(new[] { $"uuid-{PerkBench.Player}" });
    }

    // ---- refund ----

    [Test]
    public void RemoveStatusEffectsOnPerkRefund_RemovesDeclaredEffectsThenSourceOwnedOnesThenRefreshes()
    {
        Load();

        _bench.Refund.RemoveStatusEffectsOnPerkRefund(PerkBench.Player, Plain);

        Sequence().Should().Equal(
            $"StatusEffect.Remove:{PerkBench.Player}:String:saved=0",
            $"StatusEffect.RemoveBySource:{PerkBench.Player}:Int32:Two:saved=0",
            $"GameServices.Combat.RefreshStatDrivenTrackerEffects:{PerkBench.Player}:saved=0");
    }

    [Test]
    public void RemoveStatusEffectsOnPerkRefund_IgnoresInvalidPerksAndCreatures()
    {
        Load();

        _bench.Refund.RemoveStatusEffectsOnPerkRefund(PerkBench.Player, PerkType.Invalid);
        _bench.Invalid.Add(PerkBench.Master);
        _bench.Refund.RemoveStatusEffectsOnPerkRefund(PerkBench.Master, Plain);

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

        var refunds = _bench.Refund.GetSourceOwnedStatusEffectRefunds(new[] { first, second, other }, Plain);

        refunds.Select(refund => refund.Ability.Name).Should().Equal("A", "B");
        refunds.Select(refund => refund.StatusEffectType).Should().OnlyContain(type => type == typeof(string));
    }

    [Test]
    public void ShouldEnforceActiveAbilityFeatReplacement_DependsOnWhoOwnsTheLevel()
    {
        _bench.Feats.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Player, Plain).Should().BeTrue();
        _bench.Feats.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.DroidCreature, Plain).Should().BeTrue();
        _bench.Feats.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.BeastCreature, Plain).Should().BeTrue();
        _bench.Feats.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.PossessedDm, Plain).Should().BeFalse();
        _bench.Feats.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Npc, Plain).Should().BeFalse();
        _bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)Plain}")] = 1;
        _bench.Feats.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Npc, Plain).Should().BeTrue();
        _bench.Feats.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Player, PerkType.Invalid).Should().BeFalse();
        _bench.Invalid.Add(PerkBench.Player);
        _bench.Feats.ShouldEnforceActiveAbilityFeatReplacement(PerkBench.Player, Plain).Should().BeFalse();
    }

    // ---- unlock ----

    [Test]
    public void UnlockPerkForPlayer_StampsTheTimeAndSaves()
    {
        _bench.CacheData();
        var player = AddPlayer();
        var now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

        using (Clock.OverrideForTests(() => now))
        {
            _bench.Catalog.UnlockPerkForPlayer(PerkBench.Player, Unlockable);
        }

        player.UnlockedPerks[Unlockable].Should().Be(now);
        _bench.Db.Saved.Should().Equal(new EntityBase[] { player });
    }

    [Test]
    public void UnlockPerkForPlayer_DoesNothingWhenThereIsNothingToUnlock()
    {
        _bench.CacheData();
        var player = AddPlayer(p => p.UnlockedPerks[Unlockable] = DateTime.MinValue);

        _bench.Catalog.UnlockPerkForPlayer(PerkBench.Player, Unlockable);
        _bench.Catalog.UnlockPerkForPlayer(PerkBench.Player, Plain);
        _bench.Catalog.UnlockPerkForPlayer(PerkBench.Npc, Unlockable);
        _bench.Catalog.UnlockPerkForPlayer(PerkBench.Dm, Unlockable);

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
            _bench.Refund.RemovePerkLevelOnSkillDecay();
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
            $"GameServices.Combat.RefreshStatDrivenTrackerEffects:{PerkBench.Player}:saved=1",
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
            _bench.Refund.RemovePerkLevelOnSkillDecay();
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
            _bench.Refund.RemovePerkLevelOnSkillDecay();
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

        _bench.Refund.RemovePerkLevelOnSkillDecay();

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

        _bench.StatBonus.DefaultAggregateStatAdjustment(StatType.AttackPercentAdjustment, 4, 3).Should().Be(7);
        _bench.StatBonus.DefaultIsDroid(PerkBench.Npc).Should().BeFalse();
        _bench.Catalog.DefaultIsDroid(PerkBench.Npc).Should().BeFalse();
        _bench.Catalog.DefaultDroidPerkLevel(PerkBench.DroidCreature, Plain).Should().Be(9);
        using var facades = new FacadeContainer();
        using var scope = facades.Install();
        _bench.Feats.DefaultAllAbilityDetails().Should().NotBeNull();
    }

    [Test]
    public void TheDefaultDefinitionSearch_FindsEveryPerkListOfTheServer()
    {
        var found = _bench.Catalog.DefaultPerkListDefinitionTypes().ToList();

        found.Should().NotBeEmpty().And.OnlyContain(type => typeof(IPerkListDefinition).IsAssignableFrom(type) && !type.IsAbstract);
        found.Should().Contain(typeof(SWLOR.Game.Server.Feature.PerkDefinition.ArmorPerkDefinition));
    }
}
