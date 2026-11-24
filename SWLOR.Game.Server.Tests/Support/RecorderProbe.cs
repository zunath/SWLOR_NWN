#nullable enable
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class RecorderProbe : CombatRecorderService
{
    private readonly string _logDirectory;

    public RecorderProbe(IDMPresenceService presence, IDBService db, string logDirectory)
        : this(new RecorderProbeCore(), presence, db, logDirectory)
    {
    }

    private RecorderProbe(RecorderProbeCore core, IDMPresenceService presence, IDBService db, string logDirectory)
        : base(core, presence, db, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<ICombatQueriesService>())
    {
        Core = core;
        _logDirectory = logDirectory;
    }

    /// <summary>The session the recorder writes through, which the native hooks open their scopes on.</summary>
    public RecorderProbeCore Core { get; }

    public int Hooks { get; private set; }
    public Exception? HookFailure { get; set; }
    public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();
    public Dictionary<uint, AbilityImpactSummary> ActiveImpacts { get; } = new();
    public Dictionary<uint, int> Fp { get; } = new();
    public Dictionary<uint, int> Stamina { get; } = new();
    public Dictionary<uint, int> HitPoints { get; } = new();
    public Dictionary<uint, CombatRecordTemporaryHitPointPool[]> Pools { get; } = new();
    public List<string> WriteLines => Core.WriteLines;
    public ScheduledTask? Scheduled { get; private set; }
    public ulong Frame
    {
        get => Core.Frame;
        set => Core.Frame = value;
    }

    public long Millis
    {
        get => Core.Millis;
        set => Core.Millis = value;
    }

    protected override void InstallEffectHooks()
    {
        if (HookFailure != null)
            throw HookFailure;
        Hooks++;
    }

    protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => Abilities;
    protected override bool IsFeatRegistered(FeatType feat) => Abilities.ContainsKey(feat);
    protected override AbilityDetail GetAbilityDetail(FeatType feat) => Abilities[feat];
    protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint creature) => ActiveImpacts.GetValueOrDefault(creature)!;
    protected override SkillType GetEquippedWeaponSkillType(uint creature) => SkillType.Lightsaber;
    protected override int GetCurrentFP(uint creature, Player dbPlayer = null!) => Fp.GetValueOrDefault(creature, 11);
    protected override int GetMaxFP(uint creature, Player dbPlayer = null!) => 22;
    protected override int GetCurrentStamina(uint creature, Player dbPlayer = null!) => Stamina.GetValueOrDefault(creature, 33);
    protected override int GetMaxStamina(uint creature, Player dbPlayer = null!) => 44;
    protected override int GetTemporaryHitPoints(uint creature) => 7;
    protected override int GetHitPointsExcludingTemporary(uint creature) => HitPoints.GetValueOrDefault(creature, 80);
    protected override CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(uint creature) =>
        Pools.GetValueOrDefault(creature)!;
    protected override string GetLogDirectory() => _logDirectory;

    protected override IDisposable ScheduleRepeating(Action task, TimeSpan schedule, TimeSpan delay)
    {
        Scheduled = new ScheduledTask(task);
        return Scheduled;
    }
}
