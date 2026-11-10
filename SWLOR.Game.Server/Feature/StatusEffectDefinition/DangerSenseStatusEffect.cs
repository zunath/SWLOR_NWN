using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DangerSenseStatusEffect : StatusEffectBase
    {
        public override string Name => "Danger Sense";
        public override EffectIconType Icon => EffectIconType.DangerSenseStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public DangerSenseStatusEffect()
        {
            StatGroup.Stats[StatType.DefensePercentAdjustment] = 5;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 5;
        }
    }
}
