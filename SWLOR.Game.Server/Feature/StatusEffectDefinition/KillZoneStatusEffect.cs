using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class KillZoneStatusEffect : StatusEffectBase
    {
        public override string Name => "Kill Zone";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public KillZoneStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 20;
        }

    }
}
