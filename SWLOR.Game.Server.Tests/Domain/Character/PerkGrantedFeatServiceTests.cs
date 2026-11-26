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
/// The granted feat part over the perk catalog scripted by <see cref="PerkTestBase"/>: the lookups its boot handlers build and reset, the
/// active ability feat a perk level replaces, the restore after a login and the sync. The order of the steps on the restore and sync paths
/// is pinned through the bench's timeline.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkGrantedFeatServiceTests : PerkTestBase
{
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

    // ---- the boot handlers ----

    [Test]
    public void FeatLookups_AreEmptyUntilTheCatalogIsCached()
    {
        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 2).Should().BeEmpty();
        _bench.Feats.GetAllActiveAbilityFeats(Plain).Should().BeEmpty();
        _bench.Feats.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatOne, Plain, 3).Should().BeTrue("nothing is known to replace it");
    }

    [Test]
    public void FeatLookups_OfAPerkTheCatalogDoesNotHoldAreEmpty()
    {
        Load();

        _bench.Feats.GetGrantedFeatsForPerkLevel(PerkType.ThrowRock, 2).Should().BeEmpty();
        _bench.Feats.GetCurrentActiveAbilityFeats(PerkType.ThrowRock, 2).Should().BeEmpty();
        _bench.Feats.GetGrantedFeatsForPerkLevel(Dormant, 1).Should().BeEmpty("a perk without feats");
    }

    [Test]
    public void FeatLookups_ClampALevelAboveTheCatalogMaxLevelToIt()
    {
        Load();

        _bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 3)
            .Should().Equal(_bench.Feats.GetGrantedFeatsForPerkLevel(Plain, 99));
        _bench.Feats.GetCurrentActiveAbilityFeats(Plain, 3)
            .Should().Equal(_bench.Feats.GetCurrentActiveAbilityFeats(Plain, 99));
    }

    [Test]
    public void CachePerkFeatData_OfANewInstanceBuildsItsOwnTablesAndLeavesTheOldOnesAlone()
    {
        Load();
        var reloaded = new TestPerkGrantedFeatService(_bench, _bench.Db, _bench.QuickCast, _bench.BeastMastery, _bench.Catalog);
        _bench.Abilities.Remove(PerkBench.ActiveFeatOne);

        reloaded.CachePerkFeatData();

        reloaded.GetAllActiveAbilityFeats(Plain).Should().Equal(PerkBench.ActiveFeatTwo);
        _bench.Feats.GetAllActiveAbilityFeats(Plain).Should().Equal(PerkBench.ActiveFeatOne, PerkBench.ActiveFeatTwo);
    }

    [Test]
    public void CachePerkFeatData_IsARerunBootHandlerOnTheCacheAfterEvent()
    {
        var method = typeof(PerkGrantedFeatService).GetMethod(nameof(PerkGrantedFeatService.CachePerkFeatData))!;

        method.GetCustomAttributes(typeof(BootPhaseAttribute), false).Cast<BootPhaseAttribute>().Single().Reload
            .Should().Be(BootReload.Rerun, "a hot reload builds new lookups and nothing else would fill them");
        method.GetCustomAttributes(typeof(NWNEventHandler), false).Cast<NWNEventHandler>().Single().Script
            .Should().Be(ScriptName.OnModuleCacheAfter);
        typeof(PerkGrantedFeatService).GetMethods().Where(m => m.GetCustomAttributes(typeof(NWNEventHandler), false).Length > 0)
            .Should().ContainSingle("the part has no other boot handler");
    }

    [Test]
    public void TheDefaultSeams_AreTheOriginalCalls()
    {
        using var facades = new FacadeContainer();
        using var scope = facades.Install();

        _bench.Feats.DefaultAllAbilityDetails().Should().NotBeNull();
        _bench.Feats.DefaultIsDroid(PerkBench.Npc).Should().BeFalse();
    }
}
