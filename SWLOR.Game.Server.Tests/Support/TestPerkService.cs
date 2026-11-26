using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The perk instance with its calls into unconverted code answered by the bench. The seams are split into partial files by the
/// part whose code calls each one (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>), so the PR that moves a part
/// moves one file with it: <c>TestPerkService.Catalog.cs</c> holds the stay-part's seams; the stat bonus, force affinity, granted feat and
/// refund parts have their own doubles, <c>TestPerkStatBonusService</c>, <c>TestPerkForceAffinityService</c>,
/// <c>TestPerkGrantedFeatService</c> and <c>TestPerkRefundService</c>. This file holds the constructor and the bench link.
/// </summary>
public sealed partial class TestPerkService : PerkService
{
    private readonly PerkBench _bench;

    public TestPerkService(
        PerkBench bench,
        IDBService db,
        ICompanionLookup beastMastery)
        : base(db, beastMastery)
    {
        _bench = bench;
    }
}
