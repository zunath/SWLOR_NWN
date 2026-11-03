#nullable enable
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The AI instance with the calls into services that are not converted yet under the test's control. The enmity
/// tables are a model kept here (<see cref="Tables"/>, <see cref="Proximity"/>); every other call is written into the
/// bench's call list as a note, in order with the engine calls, and answered with the delegate or set the test
/// configured.
/// </summary>
public sealed class TestAIService : AIService
{
    private readonly AIBench _bench;

    public TestAIService(AIBench bench, IPartyService party, IBeastMasteryService beastMastery, IDMPresenceService dmPresence)
        : base(party, beastMastery, dmPresence)
    {
        _bench = bench;
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

    protected override IEnumerable<Type> GetProfileDefinitionTypes() => Definitions;

    protected override uint GetHighestEnmityTarget(uint enemy) =>
        Tables.TryGetValue(enemy, out var table) && table.Count > 0 ? table.MaxBy(entry => entry.Value).Key : AIBench.Invalid;

    protected override uint GetHighestEnmityAttackTarget(uint enemy)
    {
        _bench.Engine.Note($"GetHighestEnmityAttackTarget({enemy})");
        if (!Tables.TryGetValue(enemy, out var table))
            return AIBench.Invalid;

        foreach (var (creature, _) in table.OrderByDescending(entry => entry.Value))
        {
            if (_bench.IsValid(creature) && !_bench.Get(creature).Dead)
                return creature;
        }

        return AIBench.Invalid;
    }

    protected override Dictionary<uint, int> GetEnmityTable(uint enemy) =>
        Tables.TryGetValue(enemy, out var table) ? new Dictionary<uint, int>(table) : new Dictionary<uint, int>();

    protected override void AttackHighestEnmityTarget(uint creature)
    {
        _bench.Engine.Note($"AttackHighestEnmityTarget({creature})");
        Attacked[creature] = GetHighestEnmityTarget(creature);
    }

    protected override void ResumeAttackAfterActionsCleared(uint creature) =>
        _bench.Engine.Note($"ResumeAttackAfterActionsCleared({creature})");

    protected override void IssueAttackCommand(uint creature, uint target) =>
        _bench.Engine.Note($"IssueAttackCommand({creature}, {target})");

    /// <summary>Runs while an enemys enmity table is cleared, as the status effects removed by a leash reset do.</summary>
    public Action<uint>? WhenEnmityIsCleared { get; set; }

    protected override void ClearEnmityTable(uint enemy)
    {
        _bench.Engine.Note($"ClearEnmityTable({enemy})");
        Tables.Remove(enemy);
        Proximity.Remove(enemy);
        WhenEnmityIsCleared?.Invoke(enemy);
    }

    protected override void RemoveEnmity(uint creature, uint enemy)
    {
        _bench.Engine.Note($"RemoveEnmity({creature}, {enemy})");
        if (Tables.TryGetValue(enemy, out var table))
            table.Remove(creature);
        if (Proximity.TryGetValue(enemy, out var set))
            set.Remove(creature);
    }

    protected override void ModifyEnmity(uint creature, uint enemy, int amount)
    {
        _bench.Engine.Note($"ModifyEnmity({creature}, {enemy}, {amount})");
        SetEnmity(enemy, creature, (Tables.TryGetValue(enemy, out var table) && table.TryGetValue(creature, out var current) ? current : 0) + amount);
    }

    protected override bool HasProximityEnmity(uint creature, uint enemy) =>
        Proximity.TryGetValue(enemy, out var set) && set.Contains(creature);

    protected override void ModifyProximityEnmity(uint creature, uint enemy, int amount)
    {
        _bench.Engine.Note($"ModifyProximityEnmity({creature}, {enemy}, {amount})");
        SetProximityEnmity(enemy, creature, amount);
    }

    protected override bool RemoveTrackedProximityEnmity(uint creature, uint enemy)
    {
        _bench.Engine.Note($"RemoveTrackedProximityEnmity({creature}, {enemy})");
        if (!HasProximityEnmity(creature, enemy))
            return false;

        Proximity[enemy].Remove(creature);
        Tables[enemy].Remove(creature);
        return true;
    }

    protected override void RestoreNPCStats(bool outOfCombatRegen) =>
        _bench.Engine.Note($"RestoreNPCStats({(outOfCombatRegen ? "true" : "false")})");

    protected override void LoadNPCStats() => _bench.Engine.Note("LoadNPCStats()");

    protected override void ApplyCreatureMovementRate(uint creature) =>
        _bench.Engine.Note($"ApplyCreatureMovementRate({creature})");

    protected override bool TryBeginInfiltration(uint player, uint npc)
    {
        _bench.Engine.Note($"TryBeginInfiltration({player}, {npc})");
        return InfiltrationBegins.Contains(player);
    }

    protected override void CompleteInfiltration(uint player, uint npc) =>
        _bench.Engine.Note($"CompleteInfiltration({player}, {npc})");

    protected override bool CanAcquireAggro(uint observer, uint target)
    {
        _bench.Engine.Note($"CanAcquireAggro({observer}, {target})");
        return !HiddenFromAggro.Contains(target);
    }

    protected override bool IsControlledCompanion(uint creature) => ControlledCompanions.Contains(creature);

    protected override bool AreCompanionAbilitiesEnabled(uint companion) => !CompanionsWithoutAbilities.Contains(companion);

    protected override uint ResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget)
    {
        _bench.Engine.Note($"ResolveHostileAbilityTarget({companion}, {authorizedTarget}, {selectedTarget})");
        return ResolveHostile(companion, authorizedTarget, selectedTarget);
    }

    protected override int GetPerkLevel(uint creature, PerkType perkType) => PerkLevel;

    protected override bool CanActivateAbility(uint activator, uint target, FeatType feat, int effectiveLevel, Location targetLocation)
    {
        _bench.Engine.Note($"CanActivateAbility({activator}, {target}, {feat}, {effectiveLevel})");
        return CanActivate(activator, target, feat);
    }

    protected override bool TryUseAbility(uint creature, uint target, FeatType feat, Location targetLocation)
    {
        _bench.Engine.Note($"TryUseAbility({creature}, {target}, {feat})");
        return AbilityUsed(creature, target, feat);
    }

    protected override bool MayHaveScriptType(uint creature, EffectTypeScript effectType) => MayHaveEffect;

    /// <summary>The context reads the static enmity tables for its target, so they are made to hold this model first.</summary>
    protected override void UseAttackableEnmityTarget(AIContext context) => RealEnmityTables.Replace(Tables);

    /// <summary>The instance's own search for profile definitions, for the test that proves it finds the server's.</summary>
    public IEnumerable<Type> FindDefinitionsWithTheOriginalSearch() => base.GetProfileDefinitionTypes();
}
