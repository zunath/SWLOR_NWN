using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Coagulant2StatusEffect : StatusEffectBase
    {
        public override string Name => "Coagulant II";
        public override EffectIconType Icon => EffectIconType.Immunity;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(Coagulant1StatusEffect),
        };

        public Coagulant2StatusEffect()
        {
            StatGroup.Stats[StatType.TraumaResistance] = 100;
            StatGroup.Stats[StatType.PhysicalDamageOverTimeTakenPercentAdjustment] = -20;
        }
    }
}
