using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The perk instance with its calls into unconverted code answered by the bench. The seams are split into partial files by the
/// part whose code calls each one (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>), so the PR that moves a part
/// moves one file with it: <c>TestPerkService.Catalog.cs</c>, <c>.StatBonus.cs</c>, <c>.Affinity.cs</c>, <c>.Feats.cs</c> and
/// <c>.Refund.cs</c>. This file holds the constructor and the bench link.
/// </summary>
public sealed partial class TestPerkService : PerkService
{
    private readonly PerkBench _bench;

    public TestPerkService(
        PerkBench bench,
        IDBService db,
        IQuickCastService quickCast,
        ICompanionLookup beastMastery,
        IStatusEffectService statusEffect)
        : base(db, quickCast, beastMastery, statusEffect, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<ICombatService>())
    {
        _bench = bench;
    }
}
