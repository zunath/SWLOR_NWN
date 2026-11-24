#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The AI instance with the calls into services that are not converted yet under the test's control. The enmity
/// tables are a model kept here (<see cref="Tables"/>, <see cref="Proximity"/>); every other call is written into the
/// bench's call list as a note, in order with the engine calls, and answered with the delegate or set the test
/// configured. The seams are in partial files by the part whose code calls them (<c>TestAIService.Service.cs</c>,
/// <c>.Decision</c> and <c>.Action</c>; <c>.Shared</c> holds the seams several parts call). A part that left <see cref="AIService"/>
/// has a double of its own (<see cref="TestAICreatureEventService"/>, <see cref="TestAIAggroService"/>) that answers its seams
/// from this model.
/// </summary>
public sealed partial class TestAIService : AIService
{
    private readonly AIBench _bench;

    public TestAIService(AIBench bench, IPartyRoster party, ICompanionLookup beastMastery, IDMPresenceService dmPresence)
        : base(party, beastMastery, dmPresence, FacadeForwarder.For<IAbilityActivationService>(), FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IActivityService>(), FacadeForwarder.For<ICompanionControlStoreService>(), FacadeForwarder.For<ICompanionTargetAuthorityService>(), FacadeForwarder.LazyFor<IEnmityService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<ICreatureStatService>(), FacadeForwarder.For<IStatusEffectService>())
    {
        _bench = bench;
        Events = new TestAICreatureEventService(bench, this);
        Aggro = new TestAIAggroService(bench, this);
    }

    /// <summary>The enmity tables: enemy, then each creature on its table with the amount.</summary>
    public Dictionary<uint, Dictionary<uint, int>> Tables { get; } = new();

    /// <summary>The creatures each enemy holds proximity enmity for.</summary>
    public Dictionary<uint, HashSet<uint>> Proximity { get; } = new();

    /// <summary>What each creature attacked last (<c>AttackHighestEnmityTarget</c>).</summary>
    public Dictionary<uint, uint> Attacked { get; } = new();

    /// <summary>The profile definitions the module load finds.</summary>
    public List<Type> Definitions { get; } = new();

    /// <summary>The creatures that are controlled companions of a player.</summary>
    public HashSet<uint> ControlledCompanions { get; } = new();

    /// <summary>The controlled companions whose abilities are switched off.</summary>
    public HashSet<uint> CompanionsWithoutAbilities { get; } = new();

    /// <summary>The creatures whose infiltration attempt begins.</summary>
    public HashSet<uint> InfiltrationBegins { get; } = new();

    /// <summary>The creatures that cannot acquire aggro (hidden by stealth).</summary>
    public HashSet<uint> HiddenFromAggro { get; } = new();

    /// <summary>The perk level an effective-level perk answers.</summary>
    public int PerkLevel { get; set; } = 1;

    /// <summary>Whether an ability can be activated now.</summary>
    public Func<uint, uint, FeatType, bool> CanActivate { get; set; } = (_, _, _) => true;

    /// <summary>Whether an ability use succeeds (the ability then counts as used).</summary>
    public Func<uint, uint, FeatType, bool> AbilityUsed { get; set; } = (_, _, _) => false;

    /// <summary>What the controlled companion's hostile target resolves to.</summary>
    public Func<uint, uint, uint, uint> ResolveHostile { get; set; } = (_, _, selected) => selected;

    /// <summary>Whether an effect type may be on the creature (the native pre-check); false skips the effect walk.</summary>
    public bool MayHaveEffect { get; set; } = true;

    public void SetEnmity(uint enemy, uint creature, int amount)
    {
        if (!Tables.TryGetValue(enemy, out var table))
            Tables[enemy] = table = new Dictionary<uint, int>();
        table[creature] = amount;
    }

    public void SetProximityEnmity(uint enemy, uint creature, int amount)
    {
        SetEnmity(enemy, creature, amount);
        if (!Proximity.TryGetValue(enemy, out var set))
            Proximity[enemy] = set = new HashSet<uint>();
        set.Add(creature);
    }

    // One accessor per part of the AI split (Readmes/MaintainabilityRefactorCreaturesSplit.md). Each is this instance until
    // its part moves; the tests call the accessor of the part that owns the member, so a later pull request changes only
    // the accessor.

    /// <summary>The core: profiles, flags, the ally cache and the leash (part AI-1).</summary>
    public TestAIService Service => this;

    /// <summary>The creature event handlers (part AI-2); it reaches the core and the enmity model through this instance.</summary>
    public TestAICreatureEventService Events { get; }

    /// <summary>The aggro-aura handlers and proximity enmity (part AI-3); it reaches the core and the enmity model through this instance.</summary>
    public TestAIAggroService Aggro { get; }

    /// <summary>The trigger throttle, phase and boss timer (part AI-4).</summary>
    public TestAIService Decision => this;

    /// <summary>The action scoring, targeting and execution (part AI-5).</summary>
    public TestAIService Action => this;
}
