using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ImbuementStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Imbuement Stance";
        public override EffectIconType Icon => EffectIconType.ImbuementStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public ImbuementStanceStatusEffect()
        {
            StatGroup.Stats[StatType.StanceHostileAutoAttackForceConversion] = 1;
            StatGroup.Stats[StatType.StanceHostileAutoAttackFPCost] = 2;
        }
    }
}
