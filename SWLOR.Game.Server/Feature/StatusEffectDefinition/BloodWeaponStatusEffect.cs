using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

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
