using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 7 of the status effect split, <c>StatusEffectSessionService</c>, with the ability details the toggles are found in and the
/// native effect tag read answered by the bench.
/// </summary>
public sealed class TestStatusEffectSessionService : StatusEffectSessionService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectSessionService(
        StatusEffectBench bench,
        IStatusEffectService statusEffect,
        IStatusEffectApplicationService application,
        IStatusEffectNativeService native,
        IStatusEffectCatalogService statusCatalog,
        IDBService db)
        : base(statusEffect, application, native, statusCatalog, db, FacadeForwarder.For<IAbilityCatalog>())
    {
        _bench = bench;
    }

    protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _bench.AbilityDetails;

    protected override bool MayHaveEffectTag(uint creature, string tag) => _bench.NativeTags.Contains(tag);
}
