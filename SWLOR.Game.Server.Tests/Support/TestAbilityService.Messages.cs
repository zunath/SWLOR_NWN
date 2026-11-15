using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The answers of <see cref="TestAbilityService"/> for the impact messages (part 15). When the ability split
/// (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) moves the part, these overrides become its own double; the
/// answers come from the bench, so the tests that set them do not change.
/// </summary>
public sealed partial class TestAbilityService
{
    protected override string BuildAbilityCombatLogMessage(uint observer, uint attacker, uint defender, string abilityName, int attackResultType, int chanceToHit) =>
        $"{abilityName}:{attackResultType}:{chanceToHit}";

    protected override string BuildAbilityNoTargetCombatLogMessage(uint observer, uint attacker, string abilityName) => $"{abilityName}:no target";
}
