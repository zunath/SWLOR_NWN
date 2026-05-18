using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EclipseOfResolve1StatusEffect : StatusEffectBase
    {
        public override string Name => "Eclipse of Resolve";
        public override EffectIconType Icon => EffectIconType.EclipseOfResolve1StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;

        public EclipseOfResolve1StatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = -20;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -20;
            StatGroup.Stats[StatType.FPCostPercentAdjustment] = 35;
            StatGroup.Stats[StatType.AbilityStaminaCostPercentAdjustment] = 35;
        }
    }
}
