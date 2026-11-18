#nullable enable
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class PlainRecorder : CombatRecorderService
{
    public PlainRecorder(IDMPresenceService presence, IDBService db)
        : base(presence, db, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<ICombatQueriesService>())
    {
    }

    public ulong CallFrame() => CurrentFrame();
    public long CallMillis() => UnixTimeMilliseconds();
    public DateTime CallUtcNow() => UtcNow();
    public void CallWrite() => WriteLog(LogGroup.DM, "default seam");
    public string CallLogDirectory() => GetLogDirectory();
}
