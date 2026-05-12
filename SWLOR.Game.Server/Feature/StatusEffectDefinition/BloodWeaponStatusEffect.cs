using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BloodWeaponStatusEffect : StatusEffectBase
    {
        public override string Name => "Blood Weapon";
        public override EffectIconType Icon => EffectIconType.Regenerate;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage)
        {
            ApplyEffectToObject(DurationType.Instant, EffectHeal(PercentOfDamage(damage, 2)), attacker);
        }
    }
}
