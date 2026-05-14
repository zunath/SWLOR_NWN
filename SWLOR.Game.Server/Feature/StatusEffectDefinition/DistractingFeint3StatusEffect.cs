using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DistractingFeint3StatusEffect : StatusEffectBase
    {
        public override string Name => "Distracting Feint III";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(DistractingFeint1StatusEffect),
            typeof(DistractingFeint2StatusEffect),
        };

        public DistractingFeint3StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -12;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -12;
        }
    }
}
