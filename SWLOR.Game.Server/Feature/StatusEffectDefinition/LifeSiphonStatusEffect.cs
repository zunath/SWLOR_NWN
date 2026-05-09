using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class LifeSiphonStatusEffect : StatusEffectBase
    {
        public override string Name => "Life Siphon";
        public override EffectIconType Icon => EffectIconType.Regenerate;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage)
        {
            if (GetCurrentHitPoints(attacker) >= GetMaxHitPoints(attacker) * 0.5f)
                return;

            ApplyEffectToObject(DurationType.Instant, EffectHeal(PercentOfDamage(damage, 15)), attacker);
            Enmity.ModifyEnmity(attacker, defender, PercentOfDamage(damage, 20));
        }
    }
}
