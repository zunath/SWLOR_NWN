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
/// The refund part over the perk catalog scripted by <see cref="PerkTestBase"/>: the status effects a perk refund removes, the source-owned
/// refunds and the skill decay handler. The order of the steps on the refund and decay paths is pinned through the bench's timeline.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkRefundServiceTests : PerkTestBase
{
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

    // ---- skill decay ----

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

    [Test]
    public void RemovePerkLevelOnSkillDecay_IsAnEventHandlerOnTheLoseSkillEvent()
    {
        var method = typeof(PerkRefundService).GetMethod(nameof(PerkRefundService.RemovePerkLevelOnSkillDecay))!;

        method.GetCustomAttributes(typeof(NWNEventHandler), false).Cast<NWNEventHandler>().Single().Script
            .Should().Be(ScriptName.OnSwlorLoseSkill);
        method.GetCustomAttributes(typeof(BootPhaseAttribute), false).Should().BeEmpty("it runs on a game event, not at boot");
    }

    [Test]
    public void TheDefaultSeams_AreTheOriginalCalls()
    {
        var refreshed = new List<uint>();
        var trackers = ScriptedProxy.Create<IStatDrivenTrackerRefresh>(out var script);
        script.On("RefreshStatDrivenTrackerEffects", args =>
        {
            refreshed.Add((uint)args[0]!);
            return null;
        });
        using var container = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton<IStatDrivenTrackerRefresh>(trackers)));
        using var installed = container.Install();
        var refund = new TestPerkRefundService(_bench, _bench.Db, _bench.QuickCast, _bench.StatusEffects, _bench.Catalog, _bench.Feats);

        refund.DefaultAllAbilityDetails().Should().NotBeNull();
        refund.DefaultRefreshStatDrivenTrackerEffects(PerkBench.Player);

        refreshed.Should().Equal(PerkBench.Player);
    }
}
