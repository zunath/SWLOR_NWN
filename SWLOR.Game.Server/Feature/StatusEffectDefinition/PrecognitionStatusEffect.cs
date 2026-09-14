using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PrecognitionStatusEffect : StatusEffectBase
    {
        public override string Name => "Precognition";
        public override EffectIconType Icon => EffectIconType.PrecognitionStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public PrecognitionStatusEffect()
        {
            StatGroup.Stats[StatType.DefensePercentAdjustment] = 5;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 5;
        }
    }
}
