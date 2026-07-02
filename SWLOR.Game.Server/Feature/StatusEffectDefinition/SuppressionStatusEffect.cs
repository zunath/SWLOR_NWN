using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SuppressionStatusEffect : StatusEffectBase
    {
        public override string Name => "Suppression";
        public override EffectIconType Icon => EffectIconType.SuppressionStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override StatusEffectStackType StackingType => StatusEffectStackType.UnlimitedStacking;

        public int DamageBonus { get; }

        public SuppressionStatusEffect()
            : this(0)
        {
        }

        public SuppressionStatusEffect(int damageBonus)
        {
            DamageBonus = damageBonus;
        }

        public override IStatusEffect Clone()
        {
            return new SuppressionStatusEffect(DamageBonus);
        }
    }
}
