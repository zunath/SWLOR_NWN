using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class ForceHealStatusEffectBase : StatusEffectBase
    {
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override float Frequency => 6f;
        public override bool PersistsOnLogout => false;

        protected abstract int Amount { get; }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyHeal(creature);
        }

        protected override void Tick(uint creature)
        {
            ApplyHeal(creature);
        }

        private void ApplyHeal(uint target)
        {
            var willBonus = GetAbilityScore(Source, AbilityType.Willpower);
            if (willBonus < 0)
                willBonus = 0;

            var amount = Amount + willBonus + willBonus * Amount / 15 + Random.D10(willBonus / 5);

            ApplyEffectToObject(DurationType.Instant, GetRacialType(target) == RacialType.Undead
                ? EffectDamage(amount)
                : EffectHeal(amount), target);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_S), target);

            Enmity.ModifyEnmityOnAll(Source, 30 + amount);
            CombatPoint.AddCombatPointToAllTagged(Source, SkillType.Force, 3);
        }
    }
}
