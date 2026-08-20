using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SnapRollStatusEffect : StatusEffectBase
    {
        private readonly int _deflection;

        public override string Name => "Snap Roll";
        public override EffectIconType Icon => EffectIconType.SnapRollStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public SnapRollStatusEffect()
            : this(25)
        {
        }

        public SnapRollStatusEffect(int deflection)
        {
            _deflection = deflection;
            StatGroup.Stats[StatType.RangedDeflection] = deflection;
        }

        public override IStatusEffect Clone()
        {
            return new SnapRollStatusEffect(_deflection);
        }
    }
}
