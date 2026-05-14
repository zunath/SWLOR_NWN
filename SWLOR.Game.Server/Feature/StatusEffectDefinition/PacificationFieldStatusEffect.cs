using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PacificationFieldStatusEffect : StatusEffectBase
    {
        public override string Name => "Pacification Field";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override bool PersistsOnLogout => false;

        public PacificationFieldStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -10;
        }
    }
}
