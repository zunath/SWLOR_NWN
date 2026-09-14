using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class RangedDeflectionStatusEffect : StatusEffectBase
    {
        private readonly int _deflection;

        public override string Name => "Ranged Deflection";
        public override EffectIconType Icon { get; }
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public RangedDeflectionStatusEffect()
            : this(0, EffectIconType.Invalid)
        {
        }

        public RangedDeflectionStatusEffect(int deflection, EffectIconType icon)
        {
            _deflection = deflection;
            Icon = icon;
            StatGroup.Stats[StatType.RangedDeflection] = deflection;
        }

        public override string CanApply(uint creature)
        {
            return Icon == EffectIconType.Invalid
                ? "Ranged Deflection requires a configured status icon."
                : string.Empty;
        }

        public override IStatusEffect Clone()
        {
            return new RangedDeflectionStatusEffect(_deflection, Icon);
        }
    }
}
