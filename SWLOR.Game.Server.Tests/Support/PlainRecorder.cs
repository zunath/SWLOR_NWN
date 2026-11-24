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
    private readonly CombatRecordSessionCore _core;

    public PlainRecorder(IDMPresenceService presence, IDBService db)
        : this(new CombatRecordSessionCore(), presence, db)
    {
    }

    private PlainRecorder(CombatRecordSessionCore core, IDMPresenceService presence, IDBService db)
        : base(core, presence, db, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<ICombatQueriesService>())
    {
        _core = core;
    }

    public ulong CallFrame() => _core.CurrentFrame();
    public long CallMillis() => _core.UnixTimeMilliseconds();
    public DateTime CallUtcNow() => _core.UtcNow();
    public void CallWrite() => _core.WriteLog(LogGroup.DM, "default seam");
    public string CallLogDirectory() => GetLogDirectory();
}
