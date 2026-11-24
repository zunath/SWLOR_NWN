#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The action engine, <see cref="AIActionService"/>: the companion rules, the ability checks and the enmity the actions use.
/// The enmity tables are the model on the <see cref="TestAIService"/> the engine reaches the core through; every other call is
/// written into the bench's call list as a note, in order with the engine calls.
/// </summary>
public sealed class TestAIActionService : AIActionService
{
    private readonly AIBench _bench;
    private readonly TestAIService _model;

    public TestAIActionService(AIBench bench, TestAIService model)
        : base(FacadeForwarder.For<IAbilityActivationService>(), model, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<ICompanionControlStoreService>(), FacadeForwarder.For<ICompanionTargetAuthorityService>(), FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<IPerkService>())
    {
        _bench = bench;
        _model = model;
    }

    protected override uint GetHighestEnmityAttackTarget(uint enemy)
    {
        _bench.Engine.Note($"GetHighestEnmityAttackTarget({enemy})");
        if (!_model.Tables.TryGetValue(enemy, out var table))
            return AIBench.Invalid;

        foreach (var (creature, _) in table.OrderByDescending(entry => entry.Value))
        {
            if (_bench.IsValid(creature) && !_bench.Get(creature).Dead)
                return creature;
        }

        return AIBench.Invalid;
    }

    protected override void AttackHighestEnmityTarget(uint creature) => _model.AttackHighest(creature);

    protected override void IssueAttackCommand(uint creature, uint target) =>
        _bench.Engine.Note($"IssueAttackCommand({creature}, {target})");

    protected override void ModifyEnmity(uint creature, uint enemy, int amount)
    {
        _bench.Engine.Note($"ModifyEnmity({creature}, {enemy}, {amount})");
        _model.SetEnmity(enemy, creature, (_model.Tables.TryGetValue(enemy, out var table) && table.TryGetValue(creature, out var current) ? current : 0) + amount);
    }

    protected override bool IsControlledCompanion(uint creature) => _model.ControlledCompanions.Contains(creature);

    protected override bool AreCompanionAbilitiesEnabled(uint companion) => !_model.CompanionsWithoutAbilities.Contains(companion);

    protected override uint ResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget)
    {
        _bench.Engine.Note($"ResolveHostileAbilityTarget({companion}, {authorizedTarget}, {selectedTarget})");
        return _model.ResolveHostile(companion, authorizedTarget, selectedTarget);
    }

    protected override int GetPerkLevel(uint creature, PerkType perkType) => _model.PerkLevel;

    protected override bool CanActivateAbility(uint activator, uint target, FeatType feat, int effectiveLevel, Location targetLocation)
    {
        _bench.Engine.Note($"CanActivateAbility({activator}, {target}, {feat}, {effectiveLevel})");
        return _model.CanActivate(activator, target, feat);
    }

    protected override bool TryUseAbility(uint creature, uint target, FeatType feat, Location targetLocation)
    {
        _bench.Engine.Note($"TryUseAbility({creature}, {target}, {feat})");
        return _model.AbilityUsed(creature, target, feat);
    }
}
