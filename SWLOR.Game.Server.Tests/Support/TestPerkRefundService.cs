using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The refund part with its calls into unconverted code answered by the bench: the ability details by feat and the stat-driven tracker
/// refresh. It reads the catalog and the feat sync through the other perk doubles, so the decay handler runs the real sync.
/// </summary>
public sealed class TestPerkRefundService : PerkRefundService
{
    private readonly PerkBench _bench;

    public TestPerkRefundService(
        PerkBench bench,
        IDBService db,
        IFeatHotBar quickCast,
        IStatusEffectService statusEffect,
        IPerkService perk,
        IPerkGrantedFeatService perkGrantedFeat)
        : base(db, quickCast, statusEffect, FacadeForwarder.For<IAbilityCatalog>(), perk, perkGrantedFeat,
            FacadeForwarder.For<IStatDrivenTrackerRefresh>())
    {
        _bench = bench;
    }

    protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _bench.Abilities;

    protected override void RefreshStatDrivenTrackerEffects(uint creature) =>
        _bench.Engine.Note($"GameServices.Combat.RefreshStatDrivenTrackerEffects:{creature}:saved={_bench.Db.Saved.Count}");

    /// <summary>The seams' default bodies, for the test that proves each one is the original call.</summary>
    public IReadOnlyDictionary<FeatType, AbilityDetail> DefaultAllAbilityDetails() => base.GetAllAbilityDetails();

    public void DefaultRefreshStatDrivenTrackerEffects(uint creature) => base.RefreshStatDrivenTrackerEffects(creature);
}
