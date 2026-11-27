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
/// The perk instance over a scripted world: the cache build, the table and level reads and the unlock. The stat bonuses are in
/// <see cref="PerkStatBonusServiceTests"/>, force affinity in <see cref="PerkForceAffinityServiceTests"/>, the granted feats in
/// <see cref="PerkGrantedFeatServiceTests"/> and the refunds in <see cref="PerkRefundServiceTests"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkServiceTests : PerkTestBase
{
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
    }

    [Test]
    public void TheDefaultDefinitionSearch_FindsEveryPerkListOfTheServer()
    {
        var found = _bench.Catalog.DefaultPerkListDefinitionTypes().ToList();

        found.Should().NotBeEmpty().And.OnlyContain(type => typeof(IPerkListDefinition).IsAssignableFrom(type) && !type.IsAbstract);
        found.Should().Contain(typeof(SWLOR.Game.Server.Feature.PerkDefinition.ArmorPerkDefinition));
    }

    [Test]
    public void GetPerkMaxLevel_IsTheHighestLevelOfAPerkOrZeroForOneTheCatalogDoesNotHold()
    {
        _bench.CacheData();

        _bench.Catalog.GetPerkMaxLevel(Plain).Should().Be(3);
        _bench.Catalog.GetPerkMaxLevel(BeastOnly).Should().Be(4, "the last level key, not the number of levels");
        _bench.Catalog.GetPerkMaxLevel(PerkType.ThrowRock).Should().Be(0);
        _bench.Catalog.GetPerkMaxLevel(PerkType.Invalid).Should().Be(0);
    }

    [Test]
    public void GetPerksWithSkillRequirement_ListsThePerksALevelOfWhichNeedsTheSkill()
    {
        _bench.CacheData();

        _bench.Catalog.GetPerksWithSkillRequirement(SkillType.Mimicry).Should().Equal(Plain);
        _bench.Catalog.GetPerksWithSkillRequirement(SkillType.Piloting).Should().BeEmpty("no perk needs it");
    }
}
