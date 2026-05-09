using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MarkedForDeathStatusEffect : StatusEffectBase
    {
        public override string Name => "Marked for Death";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;

        public MarkedForDeathStatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenFlatAdjustment] = 12;
        }
    }
}
