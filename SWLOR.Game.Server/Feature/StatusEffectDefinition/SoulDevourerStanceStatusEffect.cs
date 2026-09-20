using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoulDevourerStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Soul Devourer Stance";
        public override EffectIconType Icon => EffectIconType.SoulDevourerStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            var percent = Math.Max(20, 45 - Math.Max(0, GetAbilityScore(attacker, AbilityType.Might)));
            AssignCommand(attacker, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(GameMath.PercentOf(damage, percent)), attacker));
        }
        public SoulDevourerStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 25;
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 10;
        }

    }
}
