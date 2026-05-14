using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DistractingFeint2StatusEffect : StatusEffectBase
    {
        public override string Name => "Distracting Feint II";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(DistractingFeint3StatusEffect),
        };
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(DistractingFeint1StatusEffect),
        };

        public DistractingFeint2StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -8;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -8;
        }
    }
}
