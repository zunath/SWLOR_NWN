#nullable enable
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The respawn part over scripted neighbours; the unconverted log call is answered by the override.</summary>
internal sealed class TestDeathRespawn : DeathRespawnService
{
    public TestDeathRespawn(IDBService db, IAreaLookup areas, ICityUpgradeLookup property, IPlayerNameService names)
        : base(db, areas, property, names)
    {
    }

    public List<(LogGroup Group, string Details)> Logs { get; } = new();

    protected override void WriteLog(LogGroup group, string details) => Logs.Add((group, details));
}
