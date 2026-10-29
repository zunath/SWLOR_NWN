using System.Numerics;

namespace SWLOR.Game.Server.Service.CombatService
{
    internal sealed record TwinPistolAbilityActivation(string ActivationId, uint Area, Vector3 Origin,
        uint MainHand, uint OffHand, WeaponShotProfile[] Weapons, uint Target,
        TwinPistolAbilitySpec Spec, WeaponAbilityTraitActivation Traits);
}
