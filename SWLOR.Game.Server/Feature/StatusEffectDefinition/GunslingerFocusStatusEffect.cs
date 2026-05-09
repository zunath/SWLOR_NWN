using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GunslingerFocusStatusEffect : StatusEffectBase
    {
        public override string Name => "Gunslinger Focus";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public GunslingerFocusStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 10;
        }

    }
}
