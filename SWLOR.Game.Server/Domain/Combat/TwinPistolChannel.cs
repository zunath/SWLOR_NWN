using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    internal sealed record TwinPistolChannel(TwinPistolAbilityActivation Activation, AbilityDetail Ability, int PaidStamina)
    {
        public WeaponImpactActivation Limits { get; } = new();
        public bool AnyHit { get; set; }
    }
}
