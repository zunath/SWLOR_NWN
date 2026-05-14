using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSpark3StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Spark III";
        public override EffectIconType Icon => EffectIconType.MovementSpeedDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(ForceSpark1StatusEffect),
            typeof(ForceSpark2StatusEffect),
        };

        public ForceSpark3StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -8;
        }
    }
}
