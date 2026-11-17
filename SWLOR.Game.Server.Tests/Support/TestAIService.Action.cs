#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Part AI-5, AIActionService: the companion rules, the ability checks and the enmity the actions use.
public sealed partial class TestAIService
{
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

    protected override void IssueAttackCommand(uint creature, uint target) =>
        _bench.Engine.Note($"IssueAttackCommand({creature}, {target})");

    protected override void ModifyEnmity(uint creature, uint enemy, int amount)
    {
        _bench.Engine.Note($"ModifyEnmity({creature}, {enemy}, {amount})");
        SetEnmity(enemy, creature, (Tables.TryGetValue(enemy, out var table) && table.TryGetValue(creature, out var current) ? current : 0) + amount);
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
}
