using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Assault3StatusEffect : StatusEffectBase
    {
        public override string Name => "Assault III";
        public override EffectIconType Icon => EffectIconType.MovementSpeedDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(Assault1StatusEffect),
            typeof(Assault2StatusEffect),
        };

        public Assault3StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -14;
        }
    }
}
