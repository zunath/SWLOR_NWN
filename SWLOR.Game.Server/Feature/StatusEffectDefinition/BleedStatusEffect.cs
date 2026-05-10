using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BleedStatusEffect : StatusEffectBase
    {
        public override string Name => "Bleed";
        public override EffectIconType Icon => EffectIconType.Wounding;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Bleeding;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit1 |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        protected override void Tick(uint creature)
        {
            var damageAmount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(creature) * 0.04f));
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damageAmount), creature);

            var location = GetLocation(creature);
            var placeable = CreateObject(ObjectType.Placeable, "plc_bloodstain", location);
            DestroyObject(placeable, 48.0f);
        }
    }
}
