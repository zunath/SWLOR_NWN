using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Domain.Combat
{
    internal sealed record TwinPistolChannel(TwinPistolAbilityActivation Activation, AbilityDetail Ability, int PaidStamina)
    {
        public WeaponImpactActivation Limits { get; } = new();
        public bool AnyHit { get; set; }
    }
}
