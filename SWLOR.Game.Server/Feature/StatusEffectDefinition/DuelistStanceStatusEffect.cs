using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DuelistStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Duelist Stance";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public DuelistStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 15;
        }

    }
}
