#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part BM-2, <see cref="BeastSpawnService"/>, over a <see cref="TestBeastMastery"/> (the catalog, the IDs and the stat application) with the
/// feat grant answered by the test: it is written to the list the host records its seams in.
/// </summary>
public sealed class TestBeastSpawnService : BeastSpawnService
{
    private readonly TestBeastMastery _host;

    public TestBeastSpawnService(TestBeastMastery host, IDBService db)
        : base(db, FacadeForwarder.For<IAIService>(), FacadeForwarder.For<ICombatRecorderService>(), FacadeForwarder.For<IPerkGrantedFeatService>(), host)
    {
        _host = host;
    }

    protected override void SyncGrantedFeats(uint creature, PerkType perkType, int perkLevel, bool addByLevel) =>
        _host.Record($"SyncGrantedFeats {creature} {perkType} {perkLevel} {addByLevel}");
}
