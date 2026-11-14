using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BlazingSpikesStatusEffect : StatusEffectBase
    {
        public override string Name => "Blazing Spikes";
        public override EffectIconType Icon => EffectIconType.BlazingSpikesStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Normal;
        public override bool RestoresAfterServerRestart => true;

        protected override void OnDamageTaken(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct)
                return;

            if (!damageType.IsPhysicalDamageType())
                return;

            var percent = Math.Min(40, 10 + Math.Max(0, GetAbilityScore(defender, AbilityType.Might)));
            var reflectedDamage = (int)Math.Floor(damage * (percent / 100f));
            if (reflectedDamage <= 0)
                return;

            GameServices.CombatHitEffects.ApplyTriggeredDamage(defender, attacker, reflectedDamage, CombatDamageType.Fire);
        }
    }
}
