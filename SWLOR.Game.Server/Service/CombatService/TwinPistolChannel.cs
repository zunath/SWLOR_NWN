using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service.CombatService
{
    internal sealed record TwinPistolChannel(TwinPistolAbilityActivation Activation, AbilityDetail Ability, int PaidStamina)
    {
        public WeaponImpactActivation Limits { get; } = new();
        public bool AnyHit { get; set; }
    }
}
