using SWLOR.NWN.API.Engine;
using System.Numerics;

namespace SWLOR.Game.Server.Service.CombatService
{
    internal sealed record TwinPistolAbilityActivation(string ActivationId, uint Area, Vector3 Origin,
        Location Destination, uint MainHand, uint OffHand, WeaponShotProfile[] Weapons, uint[] Targets,
        uint ResumeTarget, TwinPistolAbilitySpec Spec, WeaponAbilityTraitActivation Traits);
}
