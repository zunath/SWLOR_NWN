using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BlazingSpikesStatusEffect : StatusEffectBase
    {
        public override string Name => "Blazing Spikes";
        public override EffectIconType Icon => EffectIconType.ElementalShield;

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (!damageType.IsPhysicalDamageType())
                return;

            var percent = Math.Min(40, 10 + Math.Max(0, GetAbilityScore(defender, AbilityType.Might)));
            var reflectedDamage = (int)Math.Floor(damage * (percent / 100f));
            reflectedDamage = Resistance.ApplyResistanceToDamage(attacker, CombatDamageType.Fire, reflectedDamage);
            if (reflectedDamage <= 0)
                return;

            AssignCommand(defender, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(reflectedDamage, DamageType.Fire), attacker));
        }
    }
}
