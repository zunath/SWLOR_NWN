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
        : base(presence, db, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<ICombatQueriesService>())
    {
        _logDirectory = logDirectory;
    }

    public int Hooks { get; private set; }
    public Exception? HookFailure { get; set; }
    public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();
    public Dictionary<uint, AbilityImpactSummary> ActiveImpacts { get; } = new();
    public Dictionary<uint, int> Fp { get; } = new();
    public Dictionary<uint, int> Stamina { get; } = new();
    public Dictionary<uint, int> HitPoints { get; } = new();
    public Dictionary<uint, CombatRecordTemporaryHitPointPool[]> Pools { get; } = new();
    public List<string> WriteLines { get; } = new();
    public ScheduledTask? Scheduled { get; private set; }
    public ulong Frame { get; set; }
    public long Millis { get; set; } = 1_700_000_000_000;

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

    protected override void WriteLog(LogGroup group, string details, bool printToConsole = false) => WriteLines.Add(details);
    protected override ulong CurrentFrame() => Frame;
    protected override long UnixTimeMilliseconds() => Millis;
}
