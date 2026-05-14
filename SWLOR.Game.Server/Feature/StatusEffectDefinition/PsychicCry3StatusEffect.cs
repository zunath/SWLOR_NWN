using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PsychicCry3StatusEffect : StatusEffectBase
    {
        public override string Name => "Psychic Cry III";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(PsychicCry1StatusEffect),
            typeof(PsychicCry2StatusEffect),
        };

        public PsychicCry3StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -12;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = 8;
        }
    }
}
