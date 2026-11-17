#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The companion control instance with the calls into the services that are not converted yet answered by the test,
/// and the clock set by the test. The seams are in partial files by the part whose code calls them (<c>.Control</c>, <c>.Store</c>,
/// <c>.Authority</c>; <c>.Shared</c> holds the seams several parts call).
/// </summary>
public sealed partial class TestCompanionControl : CompanionControlService
{
    public TestCompanionControl(IAIService ai, ICompanionLookup beastMastery)
        : base(ai, beastMastery, FacadeForwarder.For<IAbilityActivationService>(), FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IActivityService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IPlayerNameService>(), FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatQueriesService>())
    {
    }

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

    // One accessor per part of the CompanionControl split (Readmes/MaintainabilityRefactorCreaturesSplit.md). Each is the one
    // instance until its part moves; the tests call the accessor of the part that owns the member.

    /// <summary>The orders, the owner events and the combat round (part CC-1).</summary>
    public TestCompanionControl Control => this;

    /// <summary>The registered companions and their state (part CC-2).</summary>
    public TestCompanionControl Store => this;

    /// <summary>The mode position and the follow action (part CC-3).</summary>
    public TestCompanionControl Positioning => this;

    /// <summary>The authorized target, the defensive threats and the hostile ability checks (part CC-4).</summary>
    public TestCompanionControl Authority => this;
}
