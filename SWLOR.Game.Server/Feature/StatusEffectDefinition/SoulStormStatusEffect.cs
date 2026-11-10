using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoulStormStatusEffect : StatusEffectBase
    {
        public override string Name => "Soul Storm";
        public override EffectIconType Icon => EffectIconType.SoulStormStatusEffect;
        public SoulStormStatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 20;
        }

    }
}
