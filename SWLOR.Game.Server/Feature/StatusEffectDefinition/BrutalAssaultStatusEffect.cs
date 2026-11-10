using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BrutalAssaultStatusEffect : StatusEffectBase
    {
        public override string Name => "Brutal Assault";
        public override EffectIconType Icon => EffectIconType.BrutalAssaultStatusEffect;

        public BrutalAssaultStatusEffect()
        {
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 10;
        }
    }
}
