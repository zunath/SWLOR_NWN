using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class StasisVolleyStatusEffect : StatusEffectBase
    {
        public override string Name => "Stasis Volley";
        public override EffectIconType Icon => EffectIconType.StasisVolleyStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public StasisVolleyStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -10;
        }
    }
}
