using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ToxinStatusEffect : StatusEffectBase
    {
        public override string Name => "Toxin";
        public override EffectIconType Icon => EffectIconType.Poison;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Poison;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        protected override void Tick(uint creature)
        {
            var damageAmount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(creature) * 0.06f));
            damageAmount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damageAmount);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damageAmount, DamageType.Acid), creature);
        }
    }
}
