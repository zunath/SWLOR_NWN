using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSpark1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Spark I";
        public override EffectIconType Icon => EffectIconType.MovementSpeedDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(ForceSpark2StatusEffect),
            typeof(ForceSpark3StatusEffect),
        };

        public ForceSpark1StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -4;
        }
    }
}
