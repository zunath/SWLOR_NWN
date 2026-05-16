using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FractureFocus1StatusEffect : StatusEffectBase
    {
        public override string Name => "Fracture Focus I";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(FractureFocus2StatusEffect),
        };

        public FractureFocus1StatusEffect()
        {
            StatGroup.Stats[StatType.FPCostPercentAdjustment] = 20;
            StatGroup.Stats[StatType.AbilityStaminaCostPercentAdjustment] = 20;
        }
    }
}
