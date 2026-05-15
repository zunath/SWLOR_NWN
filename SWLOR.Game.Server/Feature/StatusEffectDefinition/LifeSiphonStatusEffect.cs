using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class LifeSiphonStatusEffect : StatusEffectBase
    {
        public override string Name => "Life Siphon";
        public override EffectIconType Icon => EffectIconType.Regenerate;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            if (GetCurrentHitPoints(attacker) >= GetMaxHitPoints(attacker) * 0.5f)
                return;

            var amount = Stat.ApplyHealingReceivedAdjustment(attacker, PercentOfDamage(damage, 15));
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), attacker);
            Enmity.ModifyEnmity(attacker, defender, PercentOfDamage(damage, 20));
        }
    }
}
