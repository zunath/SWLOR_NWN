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
/// The answers of <see cref="TestAbilityService"/> for the activation and target checks (parts 6 and 7). When the ability split
/// (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) moves the part, these overrides become its own double; the
/// answers come from the bench, so the tests that set them do not change.
/// </summary>
public sealed partial class TestAbilityService
{
    protected override string GetUnusableEquipmentError(uint creature) => _bench.EquipmentError;

    protected override bool IsPlayerInSpaceMode(uint player) => _bench.SpaceMode.Contains(player);

    protected override bool ShouldEnforceActiveAbilityFeatReplacement(uint creature, PerkType perkType) => _bench.EnforcesFeatReplacement;

    protected override bool IsCurrentActiveAbilityFeat(FeatType feat, PerkType perkType, int perkLevel) => _bench.RepliesActiveFeat;

    protected override bool HasEquippedWeaponForAbility(uint creature) => _bench.HasEquippedWeapon;
}
