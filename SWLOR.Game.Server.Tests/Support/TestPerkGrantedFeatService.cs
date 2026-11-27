using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The granted feat part with its calls into unconverted code answered by the bench: the ability details by feat and the droid test.
/// It reads the catalog through the stay-part double, so the real boot handlers fill and reset its tables from the bench's definitions.
/// </summary>
public sealed class TestPerkGrantedFeatService : PerkGrantedFeatService
{
    private readonly PerkBench _bench;

    public TestPerkGrantedFeatService(
        PerkBench bench,
        IDBService db,
        IFeatHotBar quickCast,
        ICompanionLookup companions,
        IPerkService perk)
        : base(db, quickCast, companions, FacadeForwarder.For<IAbilityCatalog>(), perk)
    {
        _bench = bench;
    }

    protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _bench.Abilities;

    protected override bool IsDroid(uint creature) => _bench.Droids.Contains(creature);

    /// <summary>The seams' default bodies, for the test that proves each one is the original call.</summary>
    public IReadOnlyDictionary<FeatType, AbilityDetail> DefaultAllAbilityDetails() => base.GetAllAbilityDetails();

    public bool DefaultIsDroid(uint creature) => base.IsDroid(creature);
}
