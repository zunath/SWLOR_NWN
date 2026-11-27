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
/// The scripted world the perk tests share: the bench, the perk definitions the cache build finds and the helpers that add a player
/// record and read the timeline. <see cref="PerkServiceTests"/>, <see cref="PerkStatBonusServiceTests"/> and
/// <see cref="PerkForceAffinityServiceTests"/> derive from it, so each part is tested over the same catalog.
/// </summary>
[NonParallelizable]
public abstract class PerkTestBase
{
    protected const PerkType Plain = PerkType.AlphaRhythm;
    protected const PerkType Unlockable = PerkType.Antitoxin;
    protected const PerkType Dormant = PerkType.ApexBite;
    protected const PerkType BeastOnly = PerkType.CourageousResolve;
    protected const PerkType Light = PerkType.BeaconTargeting;
    protected const PerkType Dark = PerkType.BlasterBeacon;
    protected const PerkType Targeted = PerkType.BlastRadius;
    protected const PerkType Marked = PerkType.BodyguardsResolve;

    protected PerkBench _bench = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new PerkBench();
        BuildWorld();
    }

    [TearDown]
    public void TearDown() => _bench.Dispose();

    protected static PerkDetail NewPerk(PerkType type, PerkCategoryType category = PerkCategoryType.General,
        bool active = true, PerkGroupType group = PerkGroupType.Player) => new()
    {
        Type = type,
        Name = type.ToString(),
        Category = category,
        IsActive = active,
        GroupType = group,
    };

    protected void BuildWorld()
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

    /// <summary>A facade container whose database is the bench's, for the code that reaches the database through <c>GameServices</c>.</summary>
    protected FacadeContainer NewContainerOverTheBenchDatabase() =>
        new(services => services.Replace(ServiceDescriptor.Singleton<IDBService>(_bench.Db)));

    protected void Load()
    {
        _bench.CacheData();
        _bench.Feats.CachePerkFeatData();
    }

    protected Player AddPlayer(Action<Player>? configure = null)
    {
        var player = new Player { Id = "uuid-" + PerkBench.Player };
        configure?.Invoke(player);
        _bench.Db.With(player);
        return player;
    }

    protected IReadOnlyList<string> Sequence(params string[] members) =>
        _bench.Engine.Calls
            .Where(call => call.Member == TimelineNWScript.NoteMember || members.Contains(call.Member))
            .Select(call => call.Member == TimelineNWScript.NoteMember ? (string)call.Arguments[0]! : call.Member)
            .ToList();
}
