#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// What the test answers for the four companion control instances of a <see cref="TestCompanionControl"/>: the clock, the
/// droids, the busy creatures and the answers of the services that are not converted yet. The instances share one object so
/// a test sets a value once.
/// </summary>
public sealed class CompanionTestState
{
    public DateTime Now { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public HashSet<uint> Droids { get; } = new();
    public HashSet<uint> Busy { get; } = new();
    public Dictionary<uint, ActivityStatusType> BusyTypes { get; } = new();
    public HashSet<uint> RestEffect { get; } = new();
    public List<uint> ClearedBusy { get; } = new();
    public List<uint> InterruptedActivations { get; } = new();
    public List<uint> Dequeued { get; } = new();
    public Dictionary<uint, DateTime> OffensiveAt { get; } = new();
    public float WeaponRange { get; set; } = 5f;
    public Dictionary<uint, List<FeatType>> Feats { get; } = new();
    public Dictionary<FeatType, AbilityDetail> Registered { get; } = new();
    public HashSet<FeatType> UsableFeats { get; } = new();
    public Dictionary<PerkType, int> PerkLevels { get; } = new();
    public List<int> UsedLevels { get; } = new();
    public int AbilityLookups { get; set; }
}
