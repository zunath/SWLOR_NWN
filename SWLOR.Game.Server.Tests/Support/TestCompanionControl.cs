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
/// The companion control instance (part CC-1) with the three parts it calls: the store (<see cref="Store"/>), the positioning
/// part (<see cref="Positioning"/>) and the target authority (<see cref="Authority"/>). The calls into the services that are
/// not converted yet are answered by the test and the clock is the test's: the values are on one <see cref="CompanionTestState"/>
/// the four instances share, and the properties here read and write it. The seams of this instance are in
/// <c>TestCompanionControl.Control.cs</c>; each part's are on its own double.
/// </summary>
public sealed partial class TestCompanionControl : CompanionControlService
{
    public TestCompanionControl(IAIService ai, IAIDecisionService decision, IAIActionService action, ICompanionLookup beastMastery)
        : this(ai, decision, action, beastMastery, new CompanionTestState())
    {
    }

    private TestCompanionControl(IAIService ai, IAIDecisionService decision, IAIActionService action, ICompanionLookup beastMastery, CompanionTestState state)
        : this(ai, decision, action, state, new TestCompanionControlStoreService(beastMastery, state))
    {
    }

    private TestCompanionControl(IAIService ai, IAIDecisionService decision, IAIActionService action, CompanionTestState state, TestCompanionControlStoreService store)
        : this(ai, decision, action, state, store, new TestCompanionPositioningService(store, state))
    {
    }

    private TestCompanionControl(
        IAIService ai, IAIDecisionService decision, IAIActionService action,
        CompanionTestState state,
        TestCompanionControlStoreService store,
        TestCompanionPositioningService positioning)
        : this(ai, decision, action, state, store, positioning, new TestCompanionTargetAuthorityService(store, positioning, state))
    {
    }

    private TestCompanionControl(
        IAIService ai, IAIDecisionService decision, IAIActionService action,
        CompanionTestState state,
        TestCompanionControlStoreService store,
        TestCompanionPositioningService positioning,
        TestCompanionTargetAuthorityService authority)
        : base(action, decision, ai, FacadeForwarder.For<IActivityService>(), FacadeForwarder.For<IPlayerNameService>(), FacadeForwarder.For<IStatusEffectService>(), store, positioning, authority)
    {
        State = state;
        Store = store;
        Positioning = positioning;
        Authority = authority;
    }

    private CompanionTestState State { get; }

    public DateTime Now { get => State.Now; set => State.Now = value; }
    public HashSet<uint> Droids => State.Droids;
    public HashSet<uint> Busy => State.Busy;
    public Dictionary<uint, ActivityStatusType> BusyTypes => State.BusyTypes;
    public HashSet<uint> RestEffect => State.RestEffect;
    public List<uint> ClearedBusy => State.ClearedBusy;
    public List<uint> InterruptedActivations => State.InterruptedActivations;
    public List<uint> Dequeued => State.Dequeued;
    public Dictionary<uint, DateTime> OffensiveAt => State.OffensiveAt;
    public float WeaponRange { get => State.WeaponRange; set => State.WeaponRange = value; }
    public Dictionary<uint, List<FeatType>> Feats => State.Feats;
    public Dictionary<FeatType, AbilityDetail> Registered => State.Registered;
    public HashSet<FeatType> UsableFeats => State.UsableFeats;
    public Dictionary<PerkType, int> PerkLevels => State.PerkLevels;
    public List<int> UsedLevels => State.UsedLevels;
    public int AbilityLookups { get => State.AbilityLookups; set => State.AbilityLookups = value; }

    // One accessor per part of the CompanionControl split (Readmes/MaintainabilityRefactorCreaturesSplit.md); the tests call
    // the accessor of the part that owns the member.

    /// <summary>The orders, the owner events and the combat round (part CC-1).</summary>
    public TestCompanionControl Control => this;

    /// <summary>The registered companions and their state (part CC-2).</summary>
    public TestCompanionControlStoreService Store { get; }

    /// <summary>The mode position and the follow action (part CC-3).</summary>
    public TestCompanionPositioningService Positioning { get; }

    /// <summary>The authorized target, the defensive threats and the hostile ability checks (part CC-4).</summary>
    public TestCompanionTargetAuthorityService Authority { get; }
}
