using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BloodWeaponStatusEffect : StatusEffectBase
    {
        public override string Name => "Blood Weapon";
        public override EffectIconType Icon => EffectIconType.BloodWeaponStatusEffect;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            var amount = Stat.ApplyHealingReceivedAdjustment(attacker, GameMath.PercentOf(damage, 2));
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), attacker);
        }
    }
}
