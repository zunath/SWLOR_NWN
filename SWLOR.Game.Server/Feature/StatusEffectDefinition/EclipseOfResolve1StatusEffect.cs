using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EclipseOfResolve1StatusEffect : StatusEffectBase
    {
        public override string Name => "Eclipse of Resolve";
        public override EffectIconType Icon => EffectIconType.EclipseOfResolve1StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.AbilityDisruption;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;

        public EclipseOfResolve1StatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = -15;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -15;
            StatGroup.Stats[StatType.FPCostPercentAdjustment] = 25;
            StatGroup.Stats[StatType.AbilityStaminaCostPercentAdjustment] = 25;
        }
    }
}
