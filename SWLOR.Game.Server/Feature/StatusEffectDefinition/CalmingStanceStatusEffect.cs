using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CalmingStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Calming Stance";
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public CalmingStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -40;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -40;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -40;
        }

    }
}
